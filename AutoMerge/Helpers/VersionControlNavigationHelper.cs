using System;
using AutoMerge.VersionControl;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common.Internal;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AutoMerge
{
	public class VersionControlNavigationHelper
	{
		private static readonly Guid TfsProviderGuid = new Guid("4CA58AB2-18FA-4F8D-95D4-32DDF27D184C");
		private static readonly Guid GitProviderGuid = new Guid("11b8e6d7-c08b-4385-b321-321078cdd1f8");

		static VersionControlNavigationHelper()
		{
		}

		public static bool IsProviderActive(IServiceProvider serviceProvider, VersionControlProvider provider)
		{
			if (serviceProvider != null)
			{
				var service = serviceProvider.GetService<IVsRegisterScciProvider>();
				if (service != null)
				{
					// ISSUE: variable of a compiler-generated type
					var providerInterface = service as IVsGetScciProviderInterface;
					if (providerInterface != null)
					{
						Guid pguidSCCProvider;
						// ISSUE: reference to a compiler-generated method
						providerInterface.GetSourceControlProviderID(out pguidSCCProvider);
						if (pguidSCCProvider == GetProviderGuid(provider))
							return true;
						if (Guid.Empty.Equals(pguidSCCProvider))
							return provider == VersionControlProvider.TeamFoundation;
						return false;
					}
				}
			}
			return true;
		}

		public static UIContext GetProviderUIContext(VersionControlProvider provider)
		{
			return UIContext.FromUIContextGuid(GetProviderGuid(provider));
		}

		public static bool IsConnectedToTfsCollectionAndProject(IServiceProvider provider)
		{
			var context = GetContext(provider);
			if (context != null)
			{
				return context.HasCollection && context.HasTeamProject;
			}

			return false;
		}

		public static bool IsConnectedToTfsCollectionAndProject(ITeamFoundationContext context)
		{
			if (context != null)
			{
				return context.HasCollection && context.HasTeamProject;
			}

			return false;
		}

		private static Guid GetProviderGuid(VersionControlProvider provider)
		{
			switch (provider)
			{
				case VersionControlProvider.TeamFoundation:
					return TfsProviderGuid;
				case VersionControlProvider.Git:
					return GitProviderGuid;
				default:
					return Guid.Empty;
			}
		}

		public static ITeamFoundationContext GetContext(IServiceProvider serviceProvider)
		{
			if (serviceProvider != null)
			{
				var tfContextManager = serviceProvider.GetService<ITeamFoundationContextManager>();
				if (tfContextManager != null)
				{
					var context = tfContextManager.CurrentContext;
					return context;
				}
			}

			return null;
		}
	}
}