using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using ToonTokenizer;

namespace ToonVS
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(TokenTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class TokenTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new TokenTagger(buffer)) as ITagger<T>;
    }

    internal class TokenTagger : TokenTaggerBase, IDisposable
    {
        private readonly Document _document;
        private static readonly ImageId _errorIcon = KnownMonikers.StatusWarning.ToImageId();
        private bool _isDisposed;

        internal TokenTagger(ITextBuffer buffer) : base(buffer)
        {
            _document = buffer.GetDocument();
            _document.Parsed += ReParse;
        }

        private void ReParse(Document document)
        {
            _ = TokenizeAsync();
        }

        public override Task TokenizeAsync()
        {
            // Make sure this is running on a background thread.
            ThreadHelper.ThrowIfOnUIThread();

            List<ITagSpan<TokenTag>> list = [];

            if (_document.Result != null)
            {
                TagAll(list);

                if (list.Any())
                {
                    CreateErrorListItems(list);
                    OnTagsUpdated(list);
                }
            }

            return Task.CompletedTask;
        }

        private void TagAll(List<ITagSpan<TokenTag>> list)
        {
            // find all tokens in the _document and create tags for them and add to list
            foreach (Token token in _document.Result.Tokens)
            {
                var supportsOutlining = false;
                SnapshotSpan span = new(Buffer.CurrentSnapshot, token.ToSpan());
                TokenTag tag = CreateToken(token.Type, false, supportsOutlining, null);
                list.Add(new TagSpan<TokenTag>(span, tag));
            }
        }

        private void CreateErrorListItems(List<ITagSpan<TokenTag>> list)
        {
            foreach (ToonError error in _document.Result.Errors)
            {
                ITagSpan<TokenTag> span =
                    list.FirstOrDefault(s => s.Span.Start <= error.Position - 1 && s.Span.End >= error.EndPosition - 1) ??
                    list.FirstOrDefault(s => s.Span.Start.GetContainingLineNumber() == error.Line - 1);

                if (span == null)
                {
                    continue;
                }

                span.Tag.Errors = [new  ErrorListItem
                {
                    ProjectName = "",
                    FileName = _document.FileName,
                    Message = error.Message.Replace("␍␤", ""),
                    ErrorCategory = PredefinedErrorTypeNames.SyntaxError,
                    Severity = __VSERRORCATEGORY.EC_WARNING,
                    Line = error.Line - 1,
                    Column = error.Column,
                    BuildTool = Vsix.Name,
                }];
            }
        }

        public override Task<object> GetTooltipAsync(SnapshotPoint triggerPoint)
        {
            ITagSpan<TokenTag> item = TagsCache.FirstOrDefault(s => s.Tag.Errors.Any() && s.Span.Contains(triggerPoint.Position));

            // Error messages
            if (item != null)
            {
                ContainerElement elm = new(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_errorIcon),
                    string.Join(Environment.NewLine, item.Tag.Errors.Select(e => e.Message)));

                return Task.FromResult<object>(elm);
            }

            return Task.FromResult<object>(null);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _document.Parsed -= ReParse;
                _document.Dispose();
            }

            _isDisposed = true;
        }
    }
}
