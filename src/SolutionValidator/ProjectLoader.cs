using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace SolutionValidator
{
    public class ProjectLoader
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, string> _globalProperties;
        private readonly EvaluationContext _evaluationContext;
        private readonly ProjectCollection _projectCollection;
        private readonly ProjectLoadSettings _loadSettings;
        private readonly Dictionary<string, ProjectDetails> _projects;
        private static int _projectCount = 0;

        public ProjectLoader(ILogger<ProjectLoader> logger)
        {
            _logger = logger;
            (Version sdkVersion, string sdkDirectory) = GetSdkPath();
            _globalProperties = GetGlobalProperties(sdkVersion, sdkDirectory);
            _evaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);

            _projectCollection = new ProjectCollection(_globalProperties, new[] { new LoggingProxy(_logger) }, ToolsetDefinitionLocations.Default);

            Toolset toolSet = new Toolset("Current", sdkDirectory, _projectCollection, sdkDirectory);

            _projectCollection.AddToolset(toolSet);
            _loadSettings = ProjectLoadSettings.RejectCircularImports;
            _projects = new Dictionary<string, ProjectDetails>();
        }

        public static Dictionary<string, string> GetGlobalProperties(Version sdkVersion, string sdkDirectory)
        {
            var globalProperties = new Dictionary<string, string>()
            {
                {"RoslynTargetsPath", Path.Combine(sdkDirectory, "Roslyn")},
                {"MSBuildSDKsPath", Path.Combine(sdkDirectory, "Sdks")},
                {"MSBuildExtensionsPath", sdkDirectory},
                {"Configuration", "Release"}
            };

            return globalProperties;
        }

        private (Version SdkVersion, string SdkPath) GetSdkPath()
        {
            using var process = Process.Start(new ProcessStartInfo("dotnet", "--info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            try
            {
                string path = null;
                Version version = null;
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (line.Contains(" Version: ") && line.TrimStart().StartsWith("Version: "))
                    {
                        if (!Version.TryParse(line.Replace("Version: ", "").Trim(), out version))
                        {
                            new ApplicationException("Unable to parse SDK version from dotnet --info.");
                        }
                    }
                    else if (line.Contains("Base Path"))
                    {
                        if (version == null)
                        {
                            new ApplicationException("Unable to find SDK version from dotnet --info");
                        }
                        path = line.Replace("Base Path: ", "").Trim();
                        _logger.LogInformation("Found SDK Path: {path}", path);
                        return (version, path);
                    }
                }
            }
            finally
            {
                process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(60 * 1000))
                {
                    throw new ApplicationException("Timeout waiting for dotnet process to exit");
                }
            }

            throw new ApplicationException("Unable to find SDK path for project loading.");
        }

        public bool TryGetProject(string fullPath, out ProjectDetails projectDetails)
        {
            return TryGetProject(new FilePath(fullPath), out projectDetails);
        }

        public bool TryGetProject(FilePath filePath, out ProjectDetails projectDetails)
        {
            string normalisedPath = filePath.NormalisedPath;
            if (_projects.TryGetValue(normalisedPath, out projectDetails))
            {
                return projectDetails.Valid;
            }

            int projectId = _projectCount++;

            Project project;
            bool loadedSuccessfully = false;
            if (!File.Exists(normalisedPath))
            {
                _logger.LogWarning("Unable to find project file at: {fullPath}. Returning false", normalisedPath);
                projectDetails = ProjectDetails.GetInvalidNode(projectId, normalisedPath);
            }
            else
            {
                try
                {
                    var startTime = DateTime.Now;

                    project = Project.FromFile(normalisedPath,
                        new ProjectOptions
                        {
                            ProjectCollection = _projectCollection,
                            LoadSettings = _loadSettings,
                            EvaluationContext = _evaluationContext,
                            GlobalProperties = _globalProperties,
                        });

                    var duration = DateTime.Now - startTime;
                    if (duration > TimeSpan.FromSeconds(0.5))
                    {
                        _logger.LogWarning("Project loading for {project} took {duration}... which is alarmingly slow - it took longer than 0.5 seconds", normalisedPath, duration);
                    }

                    _logger.LogDebug("Successfully loaded {project} in {timespan}", normalisedPath, duration);

                    projectDetails = new ProjectDetails(projectId, project);
                    loadedSuccessfully = true;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error loading project file {fullPath}.", normalisedPath);
                    projectDetails = ProjectDetails.GetInvalidNode(projectId, normalisedPath);
                }
            }

            _projects.Add(normalisedPath, projectDetails);

            return loadedSuccessfully;
        }

        public HashSet<ProjectDetails> GetTopLevelNonTestProjects(IEnumerable<FilePath> projectPaths)
        {
            HashSet<FilePath> allReferences = new HashSet<FilePath>();
            var projects = new HashSet<ProjectDetails>();

            foreach (var projectPath in projectPaths)
            {
                TryGetProject(projectPath, out ProjectDetails project);
                projects.Add(project);

                // For the purposes of determining project references exclude test project
                IEnumerable<FilePath> projectReferences = project.ProjectReferences;
                if (project.IsTestProject && project.TryGetProjectUnderTest(out FilePath projectUnderTest))
                {
                    projectReferences = projectReferences.Where(p => p != projectUnderTest);
                }

                foreach (var reference in projectReferences)
                {
                    allReferences.Add(reference);
                }
            }

            // Remove any projects which have references and are not Executables
            projects.RemoveWhere(p => p.IsTestProject || allReferences.Contains(p.FilePath) && !p.IsExecutable);

            return projects;
        }

    }
}
