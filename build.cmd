dotnet build --configuration Debug
dotnet pack ./SAL.API/SAL.API.csproj  --no-build --output nupkgs
dotnet pack ./SAL.Core/SAL.Core.csproj  --no-build --output nupkgs
dotnet pack ./SAL.Infrastructure/SAL.Infrastructure.csproj  --no-build --output nupkgs
 