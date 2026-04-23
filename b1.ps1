
$version = "3.0.6"

dotnet pack ./SAL.Infrastructure/SAL.Infrastructure.csproj -c Release -p:Version=$version --include-source --include-symbols --no-restore --output pub
# dotnet nuget delete -s http://localhost:5555/v3/index.json SAL3.Infrastructure $version -k 123456 --non-interactive
dotnet nuget push -s http://localhost:5555/v3/index.json --skip-duplicate ./pub/SAL3.Infrastructure.$version.nupkg -k 123456

dotnet pack ./SAL.API/SAL.API.csproj -c Release -p:Version=$version --include-source --include-symbols --no-restore  --output pub
# dotnet nuget delete -s http://localhost:5555/v3/index.json SAL3.API $version -k 123456 --non-interactive
dotnet nuget push -s http://localhost:5555/v3/index.json --skip-duplicate ./pub/SAL3.API.$version.nupkg -k 123456

dotnet pack ./SAL.Core/SAL.Core.csproj -c Release -p:Version=$version --include-source --include-symbols --no-restore -p:SymbolPackageFormat=snupkg  --output pub
# dotnet nuget delete -s http://localhost:5555/v3/index.json SAL3.Core $version -k 123456 --non-interactive
dotnet nuget push -s http://localhost:5555/v3/index.json --skip-duplicate ./pub/SAL3.Core.$version.nupkg -k 123456

