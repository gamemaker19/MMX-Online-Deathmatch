param(
[Parameter(Mandatory=$true)]
[string]
$version,

[string] 
$baseOutputPath = 'C:\Users\username\Desktop'
)

#.\build.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x86 -sc true -zip true -deleteAfterZip true
.\build.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x86 -sc false -includeAssets false -runBuildTools false -zip true -deleteAfterZip true
.\build.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x64 -sc true -includeAssets false -runBuildTools false -zip true -deleteAfterZip true
.\build.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x64 -sc false -includeAssets false -runBuildTools false -zip true -deleteAfterZip true

Copy-Item -Path .\LevelEditor\assets -Destination $baseOutputPath -Force -Recurse
#Compress-Archive -Path "$baseOutputPath\assets" -DestinationPath "$baseOutputPath\assets.zip"
#Remove-Item -Path "$baseOutputPath\assets" -Recurse