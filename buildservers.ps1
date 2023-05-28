param(
[Parameter(Mandatory=$true)]
[string]
$version,

[string] 
$baseOutputPath = 'C:\Users\username\Desktop'
)

.\buildserver.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x64
.\buildserver.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x86
.\buildserver.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x64 -sc true
.\buildserver.ps1 -version $version -baseOutputPath $baseOutputPath -os win-x86 -sc true