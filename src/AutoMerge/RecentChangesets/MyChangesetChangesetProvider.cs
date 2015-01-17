using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMerge
{
	public class MyChangesetChangesetProvider : ChangesetProviderBase
	{
		private const int MaxChangesets = 20;

		public MyChangesetChangesetProvider(IServiceProvider serviceProvider)
			: base(serviceProvider)
		{
		}

		protected override List<ChangesetViewModel> GetChangesetsInternal(string userLogin)
		{
			var changesets = new List<ChangesetViewModel>();

			if (!string.IsNullOrEmpty(userLogin))
			{
				var changesetService = GetChangesetService();

				if (changesetService != null)
				{
				    var projectName = GetProjectName();
					var tfsChangesets = changesetService.GetUserChangesets(projectName, userLogin, MaxChangesets);
					changesets = tfsChangesets
						.Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
						.ToList();
				}
			}

			return changesets;
		}
	}
}
