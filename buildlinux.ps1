param(
[Parameter(Mandatory=$true)]
[string]
$version,

[string] 
$baseOutputPath = 'C:\Users\username\Desktop'
)

.\build.ps1 -version $version -baseOutputPath $baseOutputPath -os debian-x64 -sc true -zip true -deleteAfterZip true -includeAssets false