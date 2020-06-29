$configurationName = "Debug";
$versionName = "{0}.{1}.{2}-{3}.{5}+git.commit.{4}" -f $env:MajorVersion, $env:MinorVersion, $env:CI_PIPELINE_ID,$env:CI_COMMIT_BRANCH, $env:CI_COMMIT_SHORT_SHA,$env:CI_BUILD_ID ;

if($env:CI_COMMIT_BRANCH -eq "master")
{
	$configurationName = "Release"
	$buildNumber = 1;	
    $versionName = "{0}.{1}.{2}.{3}" -f $env:MajorVersion, $env:MinorVersion, $env:CI_PIPELINE_ID, $env:CI_BUILD_ID;
}

$versionNumber = "{0}.{1}.{2}.{3}" -f $env:MajorVersion, $env:MinorVersion, $env:CI_PIPELINE_ID, $env:CI_BUILD_ID;

Get-ChildItem -Filter *.csproj -Recurse | %{
	[xml]$xml = Get-Content $_.FullName
	if ($xml.Project.Sdk -eq "Microsoft.NET.Sdk")
	{
		if($xml.Project.PropertyGroup.Version)
		{
			$xml.Project.PropertyGroup.Version = $versionName
			$xml.Project.PropertyGroup.AssemblyVersion = $versionNumber
			$xml.Project.PropertyGroup.FileVersion = $versionNumber
			$xml.Save($_.FullName)
		}
	}
}

dotnet build --configuration $configurationName;
if($LastExitCode -ne 0) { Write-Error 'Ошибка сборки' -ErrorAction Stop }
dotnet pack ./SAL.API/SAL.API.csproj  --no-build --output nupkgs
if($LastExitCode -ne 0) { Write-Error 'Ошибка упаковки SAL.API' -ErrorAction Stop }
dotnet pack ./SAL.Core/SAL.Core.csproj  --no-build --output nupkgs
if($LastExitCode -ne 0) { Write-Error 'Ошибка упаковки SAL.Core' -ErrorAction Stop }
dotnet pack ./SAL.Infrastructure/SAL.Infrastructure.csproj  --no-build --output nupkgs
if($LastExitCode -ne 0) { Write-Error 'Ошибка упаковки SAL.Infrastructure' -ErrorAction Stop }
