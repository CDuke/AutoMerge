using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMerge
{
	public class MyChangesetChangesetProvider : ChangesetProviderBase
	{
		private const int MaxChangesets = 20;
		private readonly string _userLogin;

		public MyChangesetChangesetProvider(IServiceProvider serviceProvider)
			: base(serviceProvider)
		{
			_userLogin = VersionControlNavigationHelper.GetAuthorizedUser(serviceProvider);
		}

		protected override List<ChangesetViewModel> GetChangesetsInternal()
		{
			var changesets = new List<ChangesetViewModel>();

			if (!string.IsNullOrEmpty(_userLogin))
			{
				var changesetService = GetChangesetService();

				if (changesetService != null)
				{
					var tfsChangesets = changesetService.GetUserChangesets(_userLogin, MaxChangesets);
					changesets = tfsChangesets
						.Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
						.ToList();
				}
			}

			return changesets;
		}
	}
}