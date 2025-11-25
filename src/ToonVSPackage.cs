global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextTemplating.VSHost;

namespace ToonVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.ToonVSString)]

    [ProvideLanguageService(typeof(LanguageFactory), Constants.LanguageName, 0, ShowHotURLs = false, DefaultToNonHotURLs = true, EnableLineNumbers = true, EnableAsyncCompletion = true, ShowCompletion = true, ShowDropDownOptions = true)]
    [ProvideLanguageExtension(typeof(LanguageFactory), Constants.FileExtension)]

    [ProvideEditorFactory(typeof(LanguageFactory), 214, false, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(LanguageFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]
    [ProvideEditorExtension(typeof(LanguageFactory), Constants.FileExtension, 65536, NameResourceID = 214)]

    [ProvideCodeGenerator(typeof(ToonGenerator), ToonGenerator.Name, ToonGenerator.Description, true, RegisterCodeBase = true)]
    [ProvideFileIcon(Constants.FileExtension, "KnownMonikers.Script")]
    [ProvideBindingPath()]

    [ProvideUIContextRule(PackageGuids.JsonFileUIContextString,
        name: "UI Context",
        expression: "json | jsonc",
        termNames: ["json", "jsonc"],
        termValues: ["HierSingleSelectionName:.json$", "HierSingleSelectionName:.jsonc$"])]
    public sealed class ToonVSPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
        }
    }
}