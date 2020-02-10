$lastIdPath = "lastId"

$lastVersion = Get-Content -Path $lastIdPath | ConvertFrom-Json
if ($lastVersion.IncBuildNumber -eq "true")
{
	$lastVersion.BuildNumber++
}


$versionNumber = "{0}.{1}.{2}.{3}" -f $lastVersion.MajorVersion, $lastVersion.MinorVersion, $lastVersion.BuildVersion, $lastVersion.BuildNumber
if ([string]::IsNullOrEmpty($lastVersion.PreRealise))
{
	$versionName = "{0}.{1}.{2}.{3}" -f $lastVersion.MajorVersion, $lastVersion.MinorVersion, $lastVersion.BuildVersion, $lastVersion.BuildNumber
}
else
{
	$versionName = "{0}.{1}.{2}-{4}.{3}" -f $lastVersion.MajorVersion, $lastVersion.MinorVersion, $lastVersion.BuildVersion, $lastVersion.BuildNumber, $lastVersion.PreRealise
}

Get-ChildItem -Filter *.csproj -Recurse | %{
	[xml]$xml = Get-Content $_.FullName
	if ($xml.Project.Sdk -eq "Microsoft.NET.Sdk")
	{
		$xml.Project.PropertyGroup.Version = $versionName
		$xml.Project.PropertyGroup.AssemblyVersion = $versionNumber
		$xml.Project.PropertyGroup.FileVersion = $versionNumber
		$xml.Save($_.FullName)
	}
	
}

$lastVersion | ConvertTo-Json | Set-Content -Path $lastIdPath

