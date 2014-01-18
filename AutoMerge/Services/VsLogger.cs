using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace AutoMerge
{
	[Export(typeof(ILogger))]
	public class VsLogger : ILogger
	{
		private readonly IVsOutputWindowPane _pane;

		[ImportingConstructor]
		public VsLogger(IServiceProvider sp)
		{
			var window = (IVsOutputWindow)sp.GetService(typeof(SVsOutputWindow));

			var page = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;

			// If the pane does not yet exist, create it
			var hr = window.GetPane(ref page, out _pane);
			if (ErrorHandler.Failed(hr))
				ErrorHandler.ThrowOnFailure(window.CreatePane(ref page, "General", 1, 0));

			ErrorHandler.ThrowOnFailure(window.GetPane(ref page, out _pane));
		}

		public void Log(string message)
		{
			ErrorHandler.ThrowOnFailure(_pane.OutputStringThreadSafe(DateTime.Now + ": AutoMerge: " + message + "\n"));
		}

		public void Log(string message, Exception ex)
		{
			Log(message + "\n" + ex);
		}
	}
}