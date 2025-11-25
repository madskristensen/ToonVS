using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using ToonTokenizer;

namespace ToonVS
{
    public class Document : IDisposable
    {
        private readonly ITextBuffer _buffer;
        private bool _isDisposed;

        public Document(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += OnBufferChanged;

            FileName = _buffer.GetFileName();
            ParseAsync().FireAndForget();
        }

        public string FileName { get; }

        public bool IsParsing { get; private set; }

        public ToonParseResult Result { get; private set; }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ParseAsync().FireAndForget();
        }

        private async Task ParseAsync()
        {
            IsParsing = true;
            var success = false;

            try
            {
                await TaskScheduler.Default; // move to a background thread

                var text = _buffer.CurrentSnapshot.GetText();

                if (Toon.TryParse(text, out ToonParseResult result))
                {
                    Result = result;
                }

                success = true;
            }
            finally
            {
                IsParsing = false;

                if (success)
                {
                    Parsed?.Invoke(this);
                }
            }

        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _buffer.Changed -= OnBufferChanged;
                Result = null;
            }

            _isDisposed = true;
        }

        public event Action<Document> Parsed;
    }
}
