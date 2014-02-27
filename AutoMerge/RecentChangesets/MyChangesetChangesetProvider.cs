using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class MyChangesetChangesetProvider : IChangesetProvider
	{
		private readonly IServiceProvider _serviceProvider;

		public MyChangesetChangesetProvider(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public Task<List<ChangesetViewModel>> GetChangesets()
		{
			return Task.Run(() => GetChangesetsInternal());
		}

		private List<ChangesetViewModel> GetChangesetsInternal()
		{
			var changesets = new List<ChangesetViewModel>();
			var context = VersionControlNavigationHelper.GetContext(_serviceProvider);
			if (context != null && VersionControlNavigationHelper.IsConnectedToTfsCollectionAndProject(context))
			{
				var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
				if (vcs != null)
				{
					var changesetService = new ChangesetService(vcs, context.TeamProjectName);
					var tfsChangesets = changesetService.GetUserChangesets(vcs.AuthorizedUser);
					foreach (var tfsChangeset in tfsChangesets)
					{
						var changeset = new ChangesetViewModel
						{
							ChangesetId = tfsChangeset.ChangesetId,
							Comment = tfsChangeset.Comment,
							Branches = changesetService.GetAssociatedBranches(tfsChangeset.ChangesetId)
								.Select(i => i.Item)
								.ToList()
						};
						changesets.Add(changeset);
					}
				}
			}

			return changesets;
		}
	}
}