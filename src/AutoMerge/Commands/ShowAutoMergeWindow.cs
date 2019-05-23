using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Common.Internal;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace AutoMerge.Commands
{
    internal sealed class ShowAutoMergeWindow
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;

            // must match the button GUID and ID specified in the .vsct file
            var cmdId = new CommandID(GuidList.ShowAutoMergeCmdSet, 0x0100);
            var cmd = new MenuCommand((s, e) => Execute(package), cmdId);
            commandService.AddCommand(cmd);
        }

        private static void Execute(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var teamExplorer = package.GetService<ITeamExplorer>();
            teamExplorer.NavigateToPage(GuidList.AutoMergePageGuid, null);
        }
    }
}
