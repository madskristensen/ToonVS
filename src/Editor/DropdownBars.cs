using System.Collections;
using System.Collections.Generic;
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
                _ = textView.SendExplicitFocus();
                _ = _textView.Caret.MoveToNextCaretPosition();
                _textView.Caret.PositionChanged += CaretPositionChanged;
                _ = _textView.Caret.MoveToPreviousCaretPosition();
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

            _ = ThreadHelper.JoinableTaskFactory.StartOnIdle(_languageService.SynchronizeDropdowns, VsTaskRunContext.UIThreadIdlePriority);
        }

        public override bool OnSynchronizeDropdowns(LanguageService languageService, IVsTextView textView, int line, int col, ArrayList dropDownTypes, ArrayList dropDownMembers, ref int selectedType, ref int selectedMember)
        {
            if (_hasBufferChanged || dropDownMembers.Count == 0)
            {
                dropDownMembers.Clear();

                List<PropertyNode> tables = _document.Result.Document.Properties;

                foreach (PropertyNode property in tables)
                {
                    AddPropertyWithChildren(property, textView, dropDownMembers, 0);
                }
            }

            if (dropDownTypes.Count == 0)
            {
                var thisExt = $"{Vsix.Name} ({Vsix.Version})";
                var poweredBy = $"   Powered by Toon Tokenizer";
                _ = dropDownTypes.Add(new DropDownMember(thisExt, new TextSpan(), 126, DROPDOWNFONTATTR.FONTATTR_GRAY));
                _ = dropDownTypes.Add(new DropDownMember(poweredBy, new TextSpan(), 126, DROPDOWNFONTATTR.FONTATTR_GRAY));
            }

            DropDownMember currentDropDown = dropDownMembers
                .OfType<DropDownMember>()
                .Where(d => d.Span.iStartLine <= line)
                .LastOrDefault();

            selectedMember = currentDropDown != null ? dropDownMembers.IndexOf(currentDropDown) : dropDownMembers.Count > 0 ? 0 : -1;
            selectedType = 0;
            _hasBufferChanged = false;

            return true;
        }


        /// <summary>
        /// Recursively adds a <see cref="PropertyNode"/> and its child properties to the dropdown members list,
        /// building a hierarchical structure for the dropdown bar.
        /// </summary>
        /// <param name="property">The property node to add to the dropdown.</param>
        /// <param name="textView">The text view used to determine the span of the property.</param>
        /// <param name="dropDownMembers">The list to which dropdown members are added.</param>
        /// <param name="depth">The current depth in the property hierarchy, used for indentation and formatting.</param>
        /// <remarks>
        /// This method is recursive: for each property that contains child properties (i.e., its value is an <see cref="ObjectNode"/>),
        /// it calls itself for each child, incrementing the depth to reflect the hierarchy.
        /// </remarks>

        private static void AddPropertyWithChildren(PropertyNode property, IVsTextView textView, ArrayList dropDownMembers, int depth)
        {
            DropDownMember member = CreateDropDownMember(property, textView, depth);
            _ = dropDownMembers.Add(member);

            if (property.Value is ObjectNode objectNode && objectNode.Properties != null)
            {
                foreach (PropertyNode childProperty in objectNode.Properties)
                {
                    if (childProperty.Value is ObjectNode)
                    {
                        AddPropertyWithChildren(childProperty, textView, dropDownMembers, depth + 1);
                    }
                }
            }
        }

        private static DropDownMember CreateDropDownMember(PropertyNode property, IVsTextView textView, int depth)
        {
            TextSpan textSpan = GetTextSpan(property, textView);
            var headingText = GetTableName(property, depth, out DROPDOWNFONTATTR format);

            return new DropDownMember(headingText, textSpan, 126, format);
        }

        private static string GetTableName(PropertyNode property, int depth, out DROPDOWNFONTATTR format)
        {
            format = depth == 0 ? DROPDOWNFONTATTR.FONTATTR_BOLD : DROPDOWNFONTATTR.FONTATTR_PLAIN;

            var indent = new string(' ', depth * 2);
            return indent + property.Key;
        }

        private static TextSpan GetTextSpan(PropertyNode property, IVsTextView textView)
        {
            TextSpan textSpan = new();

            // Check HRESULTs to ensure positions are valid
            var hrStart = textView.GetLineAndColumn(property.StartPosition, out textSpan.iStartLine, out textSpan.iStartIndex);
            var hrEnd = textView.GetLineAndColumn(property.EndPosition + 1, out textSpan.iEndLine, out textSpan.iEndIndex);

            if (hrStart != 0 || hrEnd != 0)
            {
                // Return a default TextSpan if either call fails
                return new TextSpan();
            }


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
