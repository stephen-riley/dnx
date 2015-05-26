// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.PackageManager.SourceControl
{
    internal static class SourceControlProviderFactory
    {
        public static SourceControlProvider ResolveProvider(string sourceControlType, Reports buildReports)
        {
            if (string.IsNullOrEmpty(sourceControlType))
            {
                throw new ArgumentNullException(nameof(sourceControlType));
            }

            if (sourceControlType.Equals("git", StringComparison.OrdinalIgnoreCase))
            {
                return new GitSourceControlProvider(buildReports);
            }

            return null;
        }
    }
}
