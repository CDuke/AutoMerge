using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMerge
{
    public class ChangesetByIdChangesetProvider : ChangesetProviderBase
    {
        private readonly IEnumerable<int> _changesetIds;

        public ChangesetByIdChangesetProvider(IServiceProvider serviceProvider, IEnumerable<int> changesetIds)
            : base(serviceProvider)
        {
            if (changesetIds == null)
                throw new ArgumentNullException(nameof(changesetIds));

            _changesetIds = changesetIds;
        }

        protected override List<ChangesetViewModel> GetChangesetsInternal()
        {
            var changesets = new List<ChangesetViewModel>();
            var changesetService = GetChangesetService();
            if (changesetService != null)
            {
                changesets = _changesetIds
                    .Select(changesetService.GetChangeset)
                    .Where(c => c != null)
                    .Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
                    .ToList();
            }

            return changesets;
        }
    }
}
