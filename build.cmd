echo off
dotnet build .\SolutionValidator.sln --configuration Release /nodeReuse:false && (echo Build successful) || (exit 1)

dotnet test .\SolutionValidator.sln --no-build --configuration Release && (echo Test successful) || (exit 1) 

dotnet tool restore

dotnet tool run vstest-runner --test-assemblies .\tests\SolutionValidator.Tests\bin\Release\netcoreapp3.1\SolutionValidator.Tests.dll --docker-image mcr.microsoft.com/dotnet/sdk:6.0.408-focal-amd64 --no-metrics && (echo Successfully run the tests in Docker) || (exit 1) 

dotnet tool run vstest-runner --test-assemblies .\tests\SolutionValidator.Tests\bin\Release\net5.0\SolutionValidator.Tests.dll --docker-image mcr.microsoft.com/dotnet/sdk:6.0.408-focal-amd64 --no-metrics && (echo Successfully run the tests in Docker) || (exit 1)

dotnet tool run vstest-runner --test-assemblies .\tests\SolutionValidator.Tests\bin\Release\net6.0\SolutionValidator.Tests.dll --docker-image mcr.microsoft.com/dotnet/sdk:6.0.408-focal-amd64 --no-metrics && (echo Successfully run the tests in Docker) || (exit 1)

dotnet run --project src/SolutionValidator/SolutionValidator.csproj --framework net6.0 -- validate-solutions --solutions .\SolutionValidator.sln && (echo Validate Self Successful) || (exit 1) 

dotnet run --project src/SolutionValidator/SolutionValidator.csproj --framework netcoreapp3.1 -- validate-project-paths --solution .\SolutionValidator.sln --valid-path-roots . && (echo Validate Project Paths Successful) || (exit 1)

dotnet run --project src/SolutionValidator/SolutionValidator.csproj --framework netcoreapp3.1 -- validate-dependency-graph --solutions .\SolutionValidator.sln --colour-chart .\ColourChart.json && (echo Validate Dependency Graph Successful) || (exit 1)

dotnet run --project src/SolutionValidator/SolutionValidator.csproj --framework net6.0 -- validate-solutions --solutions .\tests\SolutionValidator.Tests\test-cases\non-standard-sdk-handling\non-standard-sdk-handling.sln && (echo Successfully Processed Non-Standard SDK projects) || (exit 1)

dotnet run --project src/SolutionValidator/SolutionValidator.csproj --framework net5.0 -- build-solution --input-files .\src\SolutionValidator\*.csproj --output-file ./solutionValidator.sln.tmp --file-mode Overwrite && (echo Successfully Built Solution) || (exit 1)

dotnet run --project src/SolutionValidator/SolutionValidator.csproj --framework net6.0 -- validate-merged-solution --merged-solution .\SolutionValidator.sln --solutions  ./solutionValidator.sln.tmp --strict && (echo Validate Self Successful) || (exit 1)

Echo "Running generate-footprint in Docker"
docker run --rm -v "%~dp0:/src/solutionvalidator" mcr.microsoft.com/dotnet/sdk:6.0.408-focal-amd64 dotnet /src/solutionvalidator/src/SolutionValidator/bin/Release/net5.0/SolutionValidator.dll generate-footprint --input-files /src/solutionvalidator/SolutionValidator.sln --output-file /src/solutionvalidator/solutionvalidator-footprint --code-root /src && (echo Validate Footprint Generation in Linux Successful) || (exit 1)
