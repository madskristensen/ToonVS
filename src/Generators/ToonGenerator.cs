using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TextTemplating.VSHost;

namespace ToonVS
{
    [Guid("b8315e83-7ba0-4a40-a5ec-a71c8d7a0ec6")]
    public sealed class ToonGenerator : BaseCodeGeneratorWithSite
    {
        public const string Name = nameof(ToonGenerator);
        public const string Description = "Generates TOON version of the source JSON file.";

        public override string GetDefaultExtension()
        {
            return ".toon";
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            try
            {
                var code = ToonTokenizer.Toon.Encode(inputFileContent);
                return Encoding.UTF8.GetBytes(code);
            }
            catch
            {
                VS.StatusBar.ShowMessageAsync("ToonGenerator: Failed to generate code. Please check the input file for errors.").FireAndForget();
                return null;
            }
        }
    }
}
