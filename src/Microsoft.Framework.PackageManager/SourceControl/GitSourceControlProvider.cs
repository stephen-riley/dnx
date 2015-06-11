// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Framework.PackageManager.SourceControl
{
    internal class GitSourceControlProvider: SourceControlProvider
    {
        private const string RepositoryApp = "git";
        
        private const string RepositoryUrlKey = "url";
        private const string CommitHashKey = "commit";
        private const string ProjectPathKey = "path";

        private bool? _isInstalled;

        public GitSourceControlProvider(Reports buildReports)
            :base(buildReports)
        {
        }

        public override bool IsInstalled
        {
            get
            {
                if (!_isInstalled.HasValue)
                {
                    _isInstalled = ProcessUtilities.ExecutableExists(RepositoryApp);
                }

                return _isInstalled.Value;
            }
        }

        public override void AddMissingSnapshotInformation(string folderName, IDictionary<string, string> snapshotInformation)
        {
            if (!snapshotInformation.ContainsKey(RepositoryUrlKey) || string.IsNullOrEmpty(snapshotInformation[RepositoryUrlKey]))
            {
                throw new ArgumentNullException("The repository URL must be specified.");
            }

            if (!snapshotInformation.ContainsKey(CommitHashKey))
            {
                snapshotInformation[CommitHashKey] = GetHeadCommitId(folderName);
            }

            if (!snapshotInformation.ContainsKey(ProjectPathKey))
            {
                var repoRoot = GetRepoRoot(folderName);

                // Get the path relative to the repo root
                var pathRelativeToRepoRoot = Path.GetDirectoryName(folderName)
                    .Substring(repoRoot.Length)
                    .Replace('\\', '/')
                    .TrimStart('/');
                snapshotInformation[ProjectPathKey] = pathRelativeToRepoRoot;
            }
        }

        private string GetHeadCommitId(string folderName)
        {
            string stdOut;
            string stdErr;

            if (ProcessUtilities.RunApp(RepositoryApp, "rev-parse HEAD", folderName, out stdOut, out stdErr) &&
                string.IsNullOrEmpty(stdErr))
            {
                return stdOut;
            }

            throw new InvalidOperationException(stdErr);
        }

        private string GetRepoRoot(string folderName)
        {
            string stdOut;
            string stdErr;

            if (ProcessUtilities.RunApp(RepositoryApp, "rev-parse --show-toplevel", folderName, out stdOut, out stdErr) &&
                string.IsNullOrEmpty(stdErr))
            {
                return stdOut;
            }

            throw new InvalidOperationException(stdErr);
        }

        public override string CreateShortFolderName(IDictionary<string, string> snapshotInfo)
        {
            var repoUrl = snapshotInfo[RepositoryUrlKey];
            var commitHash = snapshotInfo[CommitHashKey];

            string repoName = Path.GetFileNameWithoutExtension(repoUrl);
            string shortHash = commitHash.Substring(0, 8);

            return repoName + shortHash;
        }

        public override bool GetSources(string destinationFolder, IDictionary<string, string> snapshotInfo)
        {
            var repoUrl = snapshotInfo[RepositoryUrlKey];
            var commitHash = snapshotInfo[CommitHashKey];

            string stdOut;
            string stdErr;

            _buildReports.WriteInformation($"Cloning from: {repoUrl}");

            // First clone
            if (!ProcessUtilities.RunApp(
                RepositoryApp, 
                $"clone {repoUrl} {destinationFolder}",
                workingDirectory: null,
                stdOut: out stdOut,
                stdErr: out stdErr))
            {
                _buildReports.WriteError(stdErr);
                return false;
            }

            _buildReports.WriteVerbose($"Resetting to commit hash: {repoUrl}");

            // Then sync to that particular commit
            if (!ProcessUtilities.RunApp(
                RepositoryApp,
                $"reset --hard {commitHash}",
                workingDirectory: destinationFolder,
                stdOut: out stdOut,
                stdErr: out stdErr))
            {
                _buildReports.WriteError(stdErr);
                return false;
            }

            return true;
        }

        public override string GetSourceFolderPath(IDictionary<string, string> snapshotInfo)
        {
            return snapshotInfo[ProjectPathKey];
        }

        public override bool ValidateBuildSnapshotInformation(IDictionary<string, string> snapshotInfo, bool minimal)
        {
            bool noErrors = true;
            if (!snapshotInfo.ContainsKey(RepositoryUrlKey))
            {
                _buildReports.WriteError("The repository information is missing the repository URL.");
                noErrors = false;
            }

            if (!minimal)
            {
                if (!snapshotInfo.ContainsKey(CommitHashKey))
                {
                    _buildReports.WriteError("The repository information is missing the commit hash.");
                    noErrors = false;
                }
                if (!snapshotInfo.ContainsKey(ProjectPathKey))
                {
                    _buildReports.WriteError("The repository information is missing the project path.");
                    noErrors = false;
                }
            }

            return noErrors;
        }

        public override bool IsRepo(string folder)
        {
            return ProcessUtilities.RunApp(RepositoryApp, "status", workingDirectory: folder);
        }
    }
}
