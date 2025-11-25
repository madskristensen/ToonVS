using Microsoft.VisualStudio.Text;

namespace ToonVS
{
    public static class ExtensionMethods
    {
        public static Span ToSpan(this ToonTokenizer.Ast.AstNode node)
        {
            return Span.FromBounds(node.StartPosition, node.EndPosition + 1);
        }

        public static Span ToSpan(this ToonTokenizer.Token token)
        {
            return new Span(token.Position, token.Length);
        }

        public static Document GetDocument(this ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new Document(buffer));
        }
    }
}
