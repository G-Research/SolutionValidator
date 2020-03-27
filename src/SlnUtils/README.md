# SlnUtils

This project is as direct copy of some of the internal code from the dotnet sdk which is not currently exposed via nuget package and/or is mostly internal.
At the time of writing the internal solution file logic comes from:

https://github.com/dotnet/sdk/tree/b1223209644d900702287faea8e9b71f95ec49f8/src/Cli/Microsoft.DotNet.Cli.Sln.Internal

The rest of the logic is pulled from the dotnet sdk cli and adapted for reuse in this context where appropriate:

https://github.com/dotnet/sdk/tree/b1223209644d900702287faea8e9b71f95ec49f8/src/Cli/Microsoft.DotNet.Cli.Utils

The main changes are:

1. Exposing some of the internal types and extensions as public
2. Changing add/remove project extensions to accept evaluation project types instead of execution types
3. Rewriting the Reporter type to pass through onto standard Serilog logger

The hope in reusing this code is that we replicate as closely as possible the logic in the standard `dotnet sln` tooling and have an easy way of upgrading should additional features be added. 
