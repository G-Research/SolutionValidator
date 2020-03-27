echo off

dotnet tool restore

dotnet msbuild /t:PublishArtefact .\src\SolutionValidator\SolutionValidator.csproj && (echo SolutionValidator Publish successful) || (exit 1)