using EnvDTE;
using EnvDTE80;

namespace ToonVS
{
    [Command(PackageIds.ToggleCustomToolCommand)]
    internal sealed class ToggleCustomToolCommand : BaseCommand<ToggleCustomToolCommand>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            DTE2 dte = await VS.GetRequiredServiceAsync<DTE, DTE2>();

            ProjectItem item = dte.SelectedItems.Item(1).ProjectItem;

            item.Properties.Item("CustomTool").Value = ToonGenerator.Name;
        }
    }
}
