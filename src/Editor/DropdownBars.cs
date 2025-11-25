using System.Collections;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using ToonTokenizer.Ast;

namespace ToonVS
{
    internal partial class DropdownBars : TypeAndMemberDropdownBars, IDisposable
    {
        private readonly LanguageService _languageService;
        private readonly IWpfTextView _textView;
        private readonly Document _document;
        private bool _disposed;
        private bool _hasBufferChanged;

        public DropdownBars(IVsTextView textView, LanguageService languageService) : base(languageService)
        {
            _languageService = languageService;
            _textView = textView.ToIWpfTextView();
            _document = _textView.TextBuffer.GetDocument();
            _document.Parsed += OnDocumentParsed;

            InitializeAsync(textView).FireAndForget();
        }

        // This moves the caret to trigger initial drop down load
        private Task InitializeAsync(IVsTextView textView)
        {
            return ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                textView.SendExplicitFocus();
                _textView.Caret.MoveToNextCaretPosition();
                _textView.Caret.PositionChanged += CaretPositionChanged;
                _textView.Caret.MoveToPreviousCaretPosition();
            }).Task;
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e) => SynchronizeDropdowns();
        private void OnDocumentParsed(Document document)
        {
            _hasBufferChanged = true;
            SynchronizeDropdowns();
        }

        private void SynchronizeDropdowns()
        {
            if (_document.IsParsing)
            {
                return;
            }

            _ = ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                _languageService.SynchronizeDropdowns();
            }, VsTaskRunContext.UIThreadIdlePriority);
        }

        public override bool OnSynchronizeDropdowns(LanguageService languageService, IVsTextView textView, int line, int col, ArrayList dropDownTypes, ArrayList dropDownMembers, ref int selectedType, ref int selectedMember)
        {
            if (_hasBufferChanged || dropDownMembers.Count == 0)
            {
                dropDownMembers.Clear();

                var tables = _document.Result.Document.Properties;
                tables.Insert(0, new PropertyNode() { Key = "Document" });

                tables
                    .Select(headingBlock => CreateDropDownMember(headingBlock, textView))
                    .ToList()
                    .ForEach(ddm => dropDownMembers.Add(ddm));
            }

            if (dropDownTypes.Count == 0)
            {
                var thisExt = $"{Vsix.Name} ({Vsix.Version})";
                var markdig = Path.GetFileName($"   Powered by Toon Tokenizer");
                dropDownTypes.Add(new DropDownMember(thisExt, new TextSpan(), 126, DROPDOWNFONTATTR.FONTATTR_GRAY));
                dropDownTypes.Add(new DropDownMember(markdig, new TextSpan(), 126, DROPDOWNFONTATTR.FONTATTR_GRAY));
            }

            DropDownMember currentDropDown = dropDownMembers
                .OfType<DropDownMember>()
                .Where(d => d.Span.iStartLine <= line)
                .LastOrDefault();

            selectedMember = dropDownMembers.IndexOf(currentDropDown);
            selectedType = 0;
            _hasBufferChanged = false;

            return true;
        }

        private static DropDownMember CreateDropDownMember(PropertyNode property, IVsTextView textView)
        {
            TextSpan textSpan = GetTextSpan(property, textView);
            var headingText = GetTableName(property, out DROPDOWNFONTATTR format);

            return new DropDownMember(headingText, textSpan, 126, format);
        }

        private static string GetTableName(PropertyNode property, out DROPDOWNFONTATTR format)
        {
            format = DROPDOWNFONTATTR.FONTATTR_PLAIN;

            return property.Key;
        }

        private static TextSpan GetTextSpan(PropertyNode property, IVsTextView textView)
        {
            TextSpan textSpan = new()
            {
                iStartIndex = property.StartPosition,
                iEndIndex = property.EndPosition + 1,
                iStartLine = property.StartLine,
                iEndLine = property.EndLine
            };

            return textSpan;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _textView.Caret.PositionChanged -= CaretPositionChanged;
            _document.Parsed -= OnDocumentParsed;
        }
    }
}
