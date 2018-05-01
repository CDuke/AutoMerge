using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMerge
{
	public class MyChangesetChangesetProvider : ChangesetProviderBase
	{
	    private readonly int _maxChangesetCount;
        private readonly string _userLogin;

		public MyChangesetChangesetProvider(IServiceProvider serviceProvider, int maxChangesetCount, string userLogin)
			: base(serviceProvider)
		{
		    _maxChangesetCount = maxChangesetCount;
            _userLogin = userLogin;
		}

	    protected override List<ChangesetViewModel> GetChangesetsInternal()
		{
			var changesets = new List<ChangesetViewModel>();

			if (!string.IsNullOrEmpty(_userLogin))
			{
				var changesetService = GetChangesetService();

				if (changesetService != null)
				{
				    var projectName = ProjectNameHelper.GetProjectName(ServiceProvider);
					var tfsChangesets = changesetService.GetUserChangesets(projectName, _userLogin, _maxChangesetCount);
					changesets = tfsChangesets
						.Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
						.ToList();
				}
			}

			return changesets;
		}
	}
}
