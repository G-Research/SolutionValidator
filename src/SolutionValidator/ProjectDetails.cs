using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Serilog;

namespace SolutionValidator
{
    /// <summary>
    /// Immutable record type representing the details of a project
    /// </summary>
    public class ProjectDetails
    {
        /// <summary>
        /// Globally unique Id for this project.
        /// </summary>
        public int Id { get; }
        public FilePath FilePath { get; }
        public string ProjectName { get; }
        public string OutputType { get; }
        public string Colour { get; }

        public string[] ColourClashes { get; }

        public IReadOnlyList<FilePath> ProjectReferences { get; }
        public IReadOnlyList<TargetFramework> TargetFrameworks { get; private set; }

        public bool IsTestProject => ProjectName.IsTestProjectName();
        public bool IsExecutable => OutputType.ToLower() == "exe";

        public bool Valid { get; }

        public Project Project { get; }
        public string NormalisedPath => FilePath.NormalisedPath;

        // Private constructor for generating invalid nodes when loading of underlying project is not possible.
        private ProjectDetails(int id, string filePath)
        {
            Id = id;
            FilePath = new FilePath(filePath);
            ProjectName = FilePath.Name;
            Valid = false;

            Project = new Project() { FullPath = filePath };
            OutputType = "Unknown";
            Colour = "Default";
            ColourClashes = null;
            ProjectReferences = new FilePath[0];
            TargetFrameworks = new TargetFramework[0];
        }

        public static ProjectDetails GetInvalidNode(int id, string filePath)
        {
            return new ProjectDetails(id, filePath);
        }

        public ProjectDetails(int id, Project project)
        {
            Id = id;

            Project = project;
            FilePath = new FilePath(Project.FullPath);
            ProjectName = FilePath.Name;

            OutputType = project.GetPropertyValue("OutputType");
            Colour = GetStringWithDefault(project.GetPropertyValue("Colour"), "Default");
            ColourClashes = GetItemValues(project.GetPropertyValue("ColourClashes"));
            ProjectReferences = project.GetItemsIgnoringCondition("ProjectReference").Select(projectItem => FilePath.Create(projectItem.Project.DirectoryPath, projectItem.EvaluatedInclude)).ToList();
            TargetFrameworks = GetProjectTargetFrameworks(project);

            // Not possible to have valid project without a target framework specified.
            Valid = TargetFrameworks.Any();
        }

        public void RemoveProjectReferences(IEnumerable<FilePath> projectReferencesToRemove)
        {
            bool projectsRemoved = false;
            var existingProjectReferences = Project.GetItemsIgnoringCondition("ProjectReference").ToList();

            foreach (string referenceToRemove in projectReferencesToRemove)
            {
                string projectNameToRemove = System.IO.Path.GetFileName(referenceToRemove);
                foreach (var existingRef in existingProjectReferences)
                {
                    if (System.IO.Path.GetFileName(existingRef.EvaluatedInclude) == projectNameToRemove)
                    {
                        Project.RemoveItem(existingRef);
                        projectsRemoved = true;
                    }
                }
            }

            if (projectsRemoved)
            {
                Project.Save();
            }
        }

        public void UpdateProjectReferences(Dictionary<string, FilePath> projectReferencesToAdd, bool addIfMissing)
        {
            bool projectChanged = false;
            var existingProjectReferences = Project.GetItemsIgnoringCondition("ProjectReference").ToList();

            foreach (var existingRef in existingProjectReferences)
            {
                var existingPath = FilePath.Create(existingRef.Project.DirectoryPath, existingRef.EvaluatedInclude);

                if (projectReferencesToAdd.TryGetValue(existingPath.Name, out FilePath newPath))
                {
                    if (newPath != existingPath)
                    {
                        Log.Information("Existing path {existingPath} for {projectName} does not match new path {newPath} and needs to be updated.", existingPath, existingPath.Name, newPath);
                        existingRef.UnevaluatedInclude = newPath.GetRelativePath(FilePath.Directory);
                        projectChanged = true;
                    }
                }
                else
                {
                    Log.Information("Existing project {projectName} does not match new path {newPath} and needs to be updated.", existingPath, existingPath.Name, newPath);
                    existingRef.UnevaluatedInclude = newPath.GetRelativePath(FilePath.Directory);
                    projectChanged = true;
                }
            }

            if (projectChanged)
            {
                Project.Save();
            }
        }

        public void UpdateProjectReferences(IEnumerable<FilePath> projectReferencesToAdd, bool addIfMissing)
        {
            bool projectsAdded = false;
            var existingProjectReferences = Project.GetItemsIgnoringCondition("ProjectReference").ToList();

            foreach (FilePath referenceToAdd in projectReferencesToAdd)
            {
                bool existingReferenceFound = false;
                foreach (var existingRef in existingProjectReferences)
                {
                    if (existingRef.EvaluatedInclude.IsFileNameMatch(referenceToAdd))
                    {
                        existingRef.UnevaluatedInclude = referenceToAdd.GetRelativePath(FilePath.Directory);
                        existingReferenceFound = true;
                        projectsAdded = true;
                        break; // Assume that you can only have one matching project name
                    }
                }

                if (!existingReferenceFound && addIfMissing)
                {
                    Project.AddItem("ProjectReference", referenceToAdd.GetRelativePath(FilePath.Directory));
                    projectsAdded = true;
                }
            }

            if (projectsAdded)
            {
                Project.Save();
            }
        }

        public void AddTargetFrameworks(IEnumerable<TargetFramework> missingFrameworks)
        {
            var existingFrameworks = new List<TargetFramework>(TargetFrameworks);
            bool frameworkAdded = false;
            foreach (var missingFramework in missingFrameworks)
            {
                if (!existingFrameworks.Contains(missingFramework))
                {
                    frameworkAdded = true;
                    existingFrameworks.Add(missingFramework);
                }
            }

            if (frameworkAdded)
            {
                ProjectProperty targetFrameworkProperty = Project.GetProperty("TargetFramework");
                if (targetFrameworkProperty != null)
                {
                    Project.RemoveProperty(targetFrameworkProperty);
                }

                existingFrameworks.Sort();
                string targetFrameworks = string.Join(';', existingFrameworks.Select(f => f.Framework));
                Log.Information("Setting target framework property for {projectName} to {targetFrameworks}.", ProjectName, targetFrameworks);

                Project.SetProperty("TargetFrameworks", targetFrameworks);
                Project.Save();

                // Reload the property since it has been updated.
                TargetFrameworks = GetProjectTargetFrameworks(Project);
            }
        }

        public void RemoveTargetFramework(TargetFramework frameworkToRemove)
        {
            var remainingFrameworks = new List<TargetFramework>(TargetFrameworks);
            remainingFrameworks.Remove(frameworkToRemove);

            var targetFrameworksProperty = Project.GetProperty("TargetFrameworks");
            if (remainingFrameworks.Count == 1)
            {
                if (targetFrameworksProperty != null)
                {
                    Project.RemoveProperty(targetFrameworksProperty);
                }

                Log.Information("Setting target framework property for {projectName} to {targetFramework}.", ProjectName, remainingFrameworks[0].Framework);
                Project.SetProperty("TargetFramework", remainingFrameworks[0].Framework);
            }
            else
            {
                remainingFrameworks.Sort();
                string targetFrameworks = string.Join(';', remainingFrameworks.Select(f => f.Framework));
                Log.Information("Setting target frameworks property for {projectName} to {targetFramework}.", ProjectName, targetFrameworks);
                targetFrameworksProperty.UnevaluatedValue = targetFrameworks;
            }

            Project.Save();

            // Reload the property since it has been updated.
            TargetFrameworks = GetProjectTargetFrameworks(Project);
        }

        public bool TryGetProjectUnderTest(out FilePath projectUnderTest)
        {
            if (!IsTestProject)
            {
                projectUnderTest = null;
                return false;
            }

            string expectedNonTestProjectName = ProjectExtensions.TestAssemblyRegex.Replace(ProjectName, "");

            projectUnderTest = ProjectReferences.SingleOrDefault(pr => pr.Name == expectedNonTestProjectName);

            if (projectUnderTest != null)
            {
                Log.Debug("Found expected matching project under test {projectUnderTest} for test project with name {name}", projectUnderTest.Name, ProjectName);
                return true;
            }

            int index = expectedNonTestProjectName.LastIndexOf('.');
            while (index != -1)
            {
                expectedNonTestProjectName = expectedNonTestProjectName.Substring(0, index);
                projectUnderTest = ProjectReferences.SingleOrDefault(pr => pr.Name == expectedNonTestProjectName);

                if (projectUnderTest != null)
                {
                    return true;
                }

                Log.Information("Unable to find reference for project with name {name}", expectedNonTestProjectName);
                index = expectedNonTestProjectName.LastIndexOf('.');
            }

            Log.Warning("Unable to find project under test for test project {testProject}", ProjectName);
            projectUnderTest = null;
            return false;
        }

        private static string GetStringWithDefault(string value, string @default)
        {
            return string.IsNullOrEmpty(value) ? @default : value;
        }

        private static string[] GetItemValues(string value)
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                return value.Split(';');
            }
        }

        private static List<TargetFramework> GetProjectTargetFrameworks(Project project)
        {
            var targetFramework = project.GetPropertyValue("TargetFramework");
            if (!string.IsNullOrEmpty(targetFramework))
            {
                return new List<TargetFramework> { new TargetFramework(targetFramework) };
            }
            else
            {
                var targetFrameworks = project.GetPropertyValue("TargetFrameworks");

                if (!string.IsNullOrEmpty(targetFrameworks))
                {
                    return targetFrameworks.Split(";").Select(f => new TargetFramework(f)).ToList();
                }
                else
                {
                    Log.Error("Unable to determine target framework for project '{projectPath}'", project.FullPath);
                    return new List<TargetFramework>();
                }
            }
        }

        public bool IsMatchForTargetFramework(string inputCondition, TargetFramework targetFramework)
        {
            // Could Split conditions for AND/OR but YAGNI says not now
            string evaluatedCondition = null;
            if (inputCondition.Contains("$(TargetFrameworkIdentifier)"))
            {
                evaluatedCondition = inputCondition.Replace("$(TargetFrameworkIdentifier)", targetFramework.TargetFrameworkIndentifier);
            }

            if (inputCondition.Contains("$(TargetFramework)"))
            {
                evaluatedCondition = inputCondition.Replace("$(TargetFramework)", targetFramework.Framework);
            }

            // Having project references with conditions which don't include the target framework seems wrong!
            if (evaluatedCondition == null)
            {
                Log.Error("Found conditional {inputCondition} project references in {projectName} which could not be evaluated by target framework. Assume match!", inputCondition, ProjectName);
                return true;
            }

            if (evaluatedCondition.Contains("=="))
            {
                var components = evaluatedCondition.Split("==");

                if (components.Length == 2)
                {
                    return components[0].Trim() == components[1].Trim();
                }
            }
            else if (evaluatedCondition.Contains("!="))
            {
                var components = evaluatedCondition.Split("!=");

                if (components.Length == 2)
                {
                    return components[0].Trim() != components[1].Trim();
                }
            }

            Log.Error("Unable to process condition {inputCondition} in {projectName}. Assume match!", inputCondition, ProjectName);
            return true;
        }

        public void RemoveProjectReferences(HashSet<FilePath> projectsToRemove)
        {
            bool dirty = false;
            foreach (var projectReference in Project.GetItemsIgnoringCondition("ProjectReference").ToList())
            {
                var filePath = FilePath.Create(projectReference.Project.DirectoryPath, projectReference.EvaluatedInclude);

                if (projectsToRemove.Contains(filePath))
                {
                    dirty = true;
                    Project.RemoveItem(projectReference);
                }
            }

            if (dirty)
            {
                Project.Save();
            }
        }

        public IReadOnlyList<FilePath> GetProjectReferences(TargetFramework targetFramework)
        {
            // Find all project references with either no conditions or conditions which match this target framework;
            var items = Project.GetItemsIgnoringCondition("ProjectReference");
            var itemGroups = Project.Xml.ItemGroups.Where(ig => ig.Children.Any(c => c.ElementName == "ProjectReference"));
            var references = new List<FilePath>();

            foreach (var itemGroup in itemGroups)
            {
                if (string.IsNullOrEmpty(itemGroup.Condition) || IsMatchForTargetFramework(itemGroup.Condition, targetFramework))
                {
                    foreach (ProjectItemElement projectRef in itemGroup.AllChildren.Where(c => c.ElementName == "ProjectReference"))
                    {
                        if (string.IsNullOrEmpty(projectRef.Condition) || IsMatchForTargetFramework(projectRef.Condition, targetFramework))
                        {
                            string projectReferencePath = projectRef.Include;
                            if (Environment.OSVersion.Platform == PlatformID.Unix)
                            {
                                projectReferencePath = projectReferencePath.Replace('\\', '/');
                            }

                            references.Add(FilePath.Create(Project.DirectoryPath, projectReferencePath));
                        }
                    }
                }
            }

            return references;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ProjectDetails other && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
