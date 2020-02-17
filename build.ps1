$configurationName = "Debug";
$buildNumber = 0;
$versionName = "{0}.{1}.0-{3}.{2}+git.commit.{4}" -f $env:MajorVersion, $env:MinorVersion, $env:CI_BUILD_ID,$env:CI_COMMIT_BRANCH, $env:CI_COMMIT_SHORT_SHA ;

if($env:CI_COMMIT_BRANCH -eq "master")
{
	$configurationName = "Release"
	$buildNumber = 1;	
    $versionName = "{0}.{1}.{2}.{3}" -f $env:MajorVersion, $env:MinorVersion, $env:CI_BUILD_ID, $buildNumber;
}

$versionNumber = "{0}.{1}.{2}.{3}" -f $env:MajorVersion, $env:MinorVersion, $env:CI_BUILD_ID, $buildNumber;


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

