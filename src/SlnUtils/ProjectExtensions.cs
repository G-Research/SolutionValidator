// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace SlnUtils
{
    public static class ProjectExtensions
    {
        public static string GetProjectTypeGuid(this Project project)
        {
            return project.Xml
                .Properties
                .FirstOrDefault(p => string.Equals(p.Name, "ProjectTypeGuids", StringComparison.OrdinalIgnoreCase))
                ?.Value
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault(g => !string.IsNullOrWhiteSpace(g));
        }

        public static string GetProjectId(this Project project)
        {
            var projectGuidProperty = project.GetPropertyValue("ProjectGuid");
            var projectGuid = string.IsNullOrEmpty(projectGuidProperty)
                ? Guid.NewGuid()
                : new Guid(projectGuidProperty);
            return projectGuid.ToString("B").ToUpper();
        }

        public static string GetDefaultProjectTypeGuid(this Project project)
        {
            return project.GetPropertyValue("DefaultProjectTypeGuid");
        }

        public static IEnumerable<string> GetPlatforms(this Project project)
        {
            return (project.GetPropertyValue("Platforms") ?? "")
                .Split(
                    new char[] { ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .DefaultIfEmpty("AnyCPU");
        }

        public static IEnumerable<string> GetConfigurations(this Project project)
        {
            return (project.GetPropertyValue("Configurations") ?? "Debug;Release")
                .Split(
                    new char[] { ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .DefaultIfEmpty("Debug");
        }
    }
}
