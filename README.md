# SolutionValidator

<img src="./Solution.png" width="300px" />

A tool for validating solution files and viewing project dependencies

### Prerequisites

* .NET 5 Sdk

### Build the solution.

```dotnet build SolutionValidator.sln```

### Testing

To build and run the associated tests run:

```build.cmd```

This will build the application, run the unit test and run the application in a number of modes against test data.

## Usage

SolutionValidator should be installed as a dotnet tool using `dotnet tool install SolutionValidator`.

SolutionValidator contains a number of different modes to either validate or fix solutions, these include:

### SolutionValidation

Runs a standard set of validators across all solutions which have been passed in.

```
dotnet solution-validator validate-solutions --solutions {list of solution files} 
```

Results of validation steps are logged and a summary of the errors printed after all solutions have completed. The application will exit with a return code that can be interpreted by CI pipeline.

### Validate Merged Solutions

Validates that specified merged solution is superset of all the projects in the supplied solutions.

```
dotnet solution-validator validate-merged-solution --merged-solution {merged-solution} --solutions {list of solution files} [--strict]
```

`--strict` will validate that merged solution is strict superset of the supplied projects otherwise this will validate that the solutions are a superset of the merged solution.

### Validate Project Dependencies

Validates the project dependency graph adheres to dependency rules.

```
dotnet solution-validator validate-dependency-graph --solutions list-of-solution-files [--colour-chart colour-chart-file] [--add-missing-colours]
```

For more details on dependency validation logic, including format of colour swab file and necessary project settings refer to [DependencyValidation.md](./DependencyValidation.md)

### Generate Dependency Graph

Generates a Graphviz .gv file for the project dependency graph of the solution

```
dotnet solution-validator generate-graph --input-files {list of solutions and projects} [--colour-chart colour-chart-file]
```

Example for QuantPlatform:
```
dotnet solution-validator generate-graph --input-files "./**/*.sln" --exclude-patterns "**/ARP.RoslynAnalyzers.sln" "**/QTGQuantBasics/*" --output-file MyBigGraph.dot --colour-chart ColourChart.json
dot -Tsvg -O MyBigGraph.dot
start MyBigGraph.dot.svg
```

### Validate Project Paths

Validates that all the projects in a solution reside under one the supplied path roots

```
dotnet solution-validator validate-project-paths --solution solution-file [--valid-path-roots {list of roots}]
```

### Build Solution

Generates a solution based on the full dependency graph generated from input solutions and projects

```
dotnet solution-validator build-solution --input-files {list of input files} --output-file {solution-file-to-write-to} [--file-mode overwrite] [--exclude-test-projects]
```

### Fix Solution

Fixes most solution validation errors by adding or removing projects to relevant solutions. This does NOT fix dependency issues.

```
dotnet solution-validator fix-solutions --solutions {list of solutions}
```

### Fix Merged Solution

Fixes merged solutions to ensure that they contain superset of supplied solutions.

```
dotnet solution-validator fix-merged-solutions --solutions {list of solutions} [--strict] [--exclude-patterns {glob to exclude}] [--solution-tags {tag1} {tag2}...]
```

### Tag Solutions

Tags solutions so that we can filter based on tags in other commands. Particularly useful to indicate team ownership for generating and maintaining merged solutions.

```
dotnet solution-validator tag-solutions --solutions {list of solutions} --solution-tags {tag1} {tag2}... [--tagMode Set|Add|Remove]
```

### Generate-Footprint

Generates a full solution footprint based on the full dependency graph generated from input solutions and projects

```
dotnet solution-validator generate-footprint --code-root {code root} --input-files {list of input files} --output-file {file to write footprint to}
```

## Versioning

Versioned using NerdBank.GitVersioning
