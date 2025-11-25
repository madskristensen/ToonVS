using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using ToonTokenizer;

namespace ToonVS
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(Constants.LanguageName)]
    public class SyntaxHighligting : TokenClassificationTaggerBase
    {
        public override Dictionary<object, string> ClassificationMap { get; } = new()
        {
            { TokenType.Identifier, PredefinedClassificationTypeNames.SymbolDefinition },
            { TokenType.Comment, PredefinedClassificationTypeNames.Comment },
            { TokenType.Colon, PredefinedClassificationTypeNames.Punctuation},
            { TokenType.Comma, PredefinedClassificationTypeNames.Punctuation },
            { TokenType.False, PredefinedClassificationTypeNames.Keyword },
            { TokenType.True, PredefinedClassificationTypeNames.Keyword },
            { TokenType.LeftBrace, PredefinedClassificationTypeNames.Punctuation },
            { TokenType.RightBrace, PredefinedClassificationTypeNames.Punctuation },
            { TokenType.LeftBracket, PredefinedClassificationTypeNames.Punctuation },
            { TokenType.RightBracket, PredefinedClassificationTypeNames.Punctuation },
            { TokenType.String, PredefinedClassificationTypeNames.String },
            { TokenType.Null, PredefinedClassificationTypeNames.Keyword },
            { TokenType.Pipe, PredefinedClassificationTypeNames.Punctuation },
            { TokenType.Invalid, PredefinedClassificationTypeNames.Literal },
            { TokenType.Number, PredefinedClassificationTypeNames.Number },
            { TokenType.Newline, PredefinedClassificationTypeNames.WhiteSpace },
            { TokenType.Dedent, PredefinedClassificationTypeNames.WhiteSpace },
            { TokenType.Indent, PredefinedClassificationTypeNames.WhiteSpace },
            { TokenType.Whitespace, PredefinedClassificationTypeNames.WhiteSpace },
        };
    }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(Constants.LanguageName)]
    public class Outlining : TokenOutliningTaggerBase
    { }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(Constants.LanguageName)]
    public class ErrorSquigglies : TokenErrorTaggerBase
    { }

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType(Constants.LanguageName)]
    internal sealed class Tooltips : TokenQuickInfoBase
    { }

    [Export(typeof(IBraceCompletionContextProvider))]
    [BracePair('(', ')')]
    [BracePair('[', ']')]
    [BracePair('{', '}')]
    [BracePair('"', '"')]
    [BracePair('*', '*')]
    [ContentType(Constants.LanguageName)]
    [ProvideBraceCompletion(Constants.LanguageName)]
    internal sealed class BraceCompletion : BraceCompletionBase
    { }

    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [ContentType(Constants.LanguageName)]
    internal sealed class CompletionCommitManager : CompletionCommitManagerBase
    {
        public override IEnumerable<char> CommitChars => [' ', '\'', '"', ',', '.', ';', ':', '\\', '$'];
    }

    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(TextMarkerTag))]
    [ContentType(Constants.LanguageName)]
    internal sealed class BraceMatchingTaggerProvider : BraceMatchingBase
    {
        // This will match parenthesis, curly brackets, and square brackets by default.
        // Override the BraceList property to modify the list of braces to match.
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType(Constants.LanguageName)]
    [TagType(typeof(TextMarkerTag))]
    public class SameWordHighlighter : SameWordHighlighterBase
    { }

    //[Export(typeof(IWpfTextViewCreationListener))]
    //[ContentType(Constants.LanguageName)]
    //[TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    //public class UserRatings : WpfTextViewCreationListener
    //{
    //    private DateTime _openedDate;
    //    private RatingPrompt _rating;

    //    protected override void Created(DocumentView docView)
    //    {
    //        _openedDate = DateTime.Now;
    //        _rating = new RatingPrompt(Constants.MarketplaceId, Vsix.Name, AdvancedOptions.Instance, 5);
    //    }

    //    protected override void Closed(IWpfTextView textView)
    //    {
    //        if (_openedDate.AddMinutes(2) < DateTime.Now)
    //        {
    //            _rating.RegisterSuccessfulUsage();
    //        }
    //    }
    //}
}

