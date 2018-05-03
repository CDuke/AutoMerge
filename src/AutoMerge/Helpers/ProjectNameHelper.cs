using System;

namespace AutoMerge
{
    public static class ProjectNameHelper
    {
        public static string GetProjectName(IServiceProvider serviceProvider)
        {
            var context = VersionControlNavigationHelper.GetTeamFoundationContext(serviceProvider);

            if (context != null)
            {
                return context.TeamProjectName;
            }
            return null;
        }
    }
}
