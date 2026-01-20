using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMerge
{
    public class BranchValidator
    {
        private readonly Workspace _workspace;
        private readonly IEnumerable<ExtendedMerge> _trackMerges;

        public BranchValidator(Workspace workspace, IEnumerable<ExtendedMerge> trackMerges)
        {
            _workspace = workspace;
            _trackMerges = trackMerges;
        }

        public MergeInfoViewModel Validate(MergeInfoViewModel branchInfo)
        {
            branchInfo.ValidationResult = ValidateItem(_workspace, branchInfo, _trackMerges);
            branchInfo.ValidationMessage = ToMessage(branchInfo.ValidationResult);

            return branchInfo;
        }

        private static BranchValidationResult ValidateItem(Workspace workspace, MergeInfoViewModel mergeInfoViewModel, IEnumerable<ExtendedMerge> trackMerges)
        {
            var result = BranchValidationResult.Success;

            if (result == BranchValidationResult.Success)
            {
                var isMerged = IsMerged(mergeInfoViewModel.SourcePath, mergeInfoViewModel.TargetPath, trackMerges);
                if (isMerged)
                    result = BranchValidationResult.AlreadyMerged;
            }

            if (result == BranchValidationResult.Success)
            {
                var userHasAccess = UserHasAccess(workspace.VersionControlServer, mergeInfoViewModel.TargetPath);
                if (!userHasAccess)
                    result = BranchValidationResult.NoAccess;
            }

            if (result == BranchValidationResult.Success)
            {
                var isMapped = IsMapped(workspace, mergeInfoViewModel.TargetPath);
                if (!isMapped)
                    result = BranchValidationResult.BranchNotMapped;
            }

            if (result == BranchValidationResult.Success)
            {
                var hasLocalChanges = HasLocalChanges(workspace, mergeInfoViewModel.TargetPath);
                if (hasLocalChanges)
                    result = BranchValidationResult.ItemHasLocalChanges;
            }
            return result;
        }

        private static bool IsMerged(string sourcePath, string targetPath, IEnumerable<ExtendedMerge> trackMerges)
        {
            if (trackMerges == null)
                return false;

            return trackMerges.Any(m =>
                    (string.Equals(m.TargetItem.Item, sourcePath, StringComparison.OrdinalIgnoreCase) && string.Equals(m.SourceItem.Item.ServerItem, targetPath, StringComparison.OrdinalIgnoreCase))
                || (string.Equals(m.TargetItem.Item, targetPath, StringComparison.OrdinalIgnoreCase) && string.Equals(m.SourceItem.Item.ServerItem, sourcePath, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool UserHasAccess(VersionControlServer versionControlServer, string targetPath)
        {
            var permissions = versionControlServer.GetEffectivePermissions(versionControlServer.AuthorizedUser, targetPath);

            if (permissions == null || permissions.Length < 4)
                return false;

            return permissions.Contains("Read")
                && permissions.Contains("PendChange")
                && permissions.Contains("Checkin")
                && permissions.Contains("Merge");
        }

        private static bool IsMapped(Workspace workspace, string targetItem)
        {
            return workspace.IsServerPathMapped(targetItem);
        }

        private static bool HasLocalChanges(Workspace workspace, string targetPath)
        {
            return workspace.GetPendingChangesEnumerable(targetPath, RecursionType.Full).Any();
        }

        private static string ToMessage(BranchValidationResult validationResult)
        {
            switch (validationResult)
            {
                case BranchValidationResult.Success:
                    return null;
                case BranchValidationResult.AlreadyMerged:
                    return "Already merged";
                case BranchValidationResult.BranchNotMapped:
                    return "Branch not mapped";
                case BranchValidationResult.ItemHasLocalChanges:
                    return "Folder has local changes. Check-in or undo it";
                case BranchValidationResult.NoAccess:
                    return "You have not rights for edit";
                default:
                    return "Unknown error";
            }
        }
    }
}
