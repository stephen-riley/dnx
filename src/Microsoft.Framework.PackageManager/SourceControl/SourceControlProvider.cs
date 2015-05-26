// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.PackageManager.SourceControl
{
    internal abstract class SourceControlProvider
    {
        protected readonly Reports _buildReports;

        public SourceControlProvider(Reports buildReports)
        {
            if (buildReports == null)
            {
                throw new ArgumentNullException(nameof(buildReports));
            }

            _buildReports = buildReports;
        }

        public abstract bool IsInstalled { get; }

        public abstract bool IsRepo(string folder);

        public abstract bool ValidateBuildSnapshotInformation(IDictionary<string, string> snapshotInfo, bool minimal);

        public abstract void AddMissingSnapshotInformation(string folderName, IDictionary<string, string> snapshotInfo);

        public abstract string CreateShortFolderName(IDictionary<string, string> snapshotInfo);

        public abstract string GetSourceFolderPath(IDictionary<string, string> snapshotInfo);

        public abstract bool GetSources(string destinationFolder, IDictionary<string, string> snapshotInfo);
    }
}