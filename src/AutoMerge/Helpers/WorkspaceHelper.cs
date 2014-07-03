using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.Win32;

namespace AutoMerge
{
    public static class WorkspaceHelper
    {
        private const string VCRegistryRootDefault = "TeamFoundation\\SourceControl";
        private const string GeneralLastWorkspaceString = "LastWorkspace";

        public static Workspace GetWorkspace(VersionControlServer versionControlServer,
            IEnumerable<Workspace> userWorkspaces)
        {
            var workspace = GetLastPendingChangesWorkspace(versionControlServer);
            if (workspace != null)
            {
                return FindWorkspace(userWorkspaces, workspace);
            }
            workspace = GetLastSourceControlExplorerWorkspace(versionControlServer);
            if (workspace != null)
            {
                return FindWorkspace(userWorkspaces, workspace);
            }

            return userWorkspaces.FirstOrDefault();
        }

        private static Workspace FindWorkspace(IEnumerable<Workspace> userWorkspaces, Workspace workspace)
        {
            return userWorkspaces.FirstOrDefault(w => w.QualifiedName == workspace.QualifiedName);
        }

        private static Workspace GetLastSourceControlExplorerWorkspace(VersionControlServer versionControlServer)
        {
            return GetLastWorkspaceForServer(versionControlServer, "Explorer");
        }

        private static Workspace GetLastPendingChangesWorkspace(VersionControlServer versionControlServer)
        {
            return GetLastWorkspaceForServer(versionControlServer, "PendingCheckins");
        }

        private static Workspace GetLastWorkspaceForServer(VersionControlServer versionControlServer, string viewName)
        {
            var workspace = (Workspace)null;
            try
            {
                var workspaceSpecForServer = GetLastWorkspaceSpecForServer(versionControlServer, viewName);
                if (!string.IsNullOrEmpty(workspaceSpecForServer))
                {

                    string workspaceName;
                    string workspaceOwner;
                    WorkspaceSpec.Parse(workspaceSpecForServer, versionControlServer.AuthorizedUser, out workspaceName, out workspaceOwner);
                    var localWorkspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(versionControlServer, workspaceName, workspaceOwner);
                    if (localWorkspaceInfo != null)
                        workspace = localWorkspaceInfo.GetWorkspace(versionControlServer.TeamProjectCollection);
                }
            }
            catch (Exception)
            {
            }
            return workspace;
        }

        private static string GetLastWorkspaceSpecForServer(VersionControlServer vcServer, string viewName)
        {
            var str1 = (string)null;
            try
            {
                if (vcServer != null)
                {
                    if (!string.IsNullOrEmpty(viewName))
                    {
                        using (var featureServerKey = GetFeatureServerKey(viewName, vcServer.ServerGuid.ToString()))
                        {
                            if (featureServerKey != null)
                            {
                                var str2 = featureServerKey.GetValue(GeneralLastWorkspaceString) as string;
                                if (!string.IsNullOrEmpty(str2))
                                    str1 = str2;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return str1;
        }

        private static RegistryKey GetFeatureServerKey(string featurePath, string serverGuid)
        {
            using (var registryKey = UIHost.UserRegistryRoot)
            {
                if (registryKey != null)
                {
                    var subKey = string.Join("\\", VCRegistryRootDefault, featurePath, serverGuid);
                    return registryKey.OpenSubKey(subKey, true);
                }
            }
            return null;
        }
    }
}
