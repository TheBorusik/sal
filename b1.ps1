dotnet build 
if($LastExitCode -ne 0) { Write-Error 'Ошибка сборки' -ErrorAction Stop }
dotnet pack ./SAL.API/SAL.API.csproj  --no-build --output nupkgs
$LastExitCode
dotnet pack ./SAL.Core/SAL.Core.csproj  --no-build --output nupkgs
$LastExitCode
dotnet pack ./SAL.Infrastructure/SAL.Infrastructure.csproj  --no-build --output nupkgs
$LastExitCode
