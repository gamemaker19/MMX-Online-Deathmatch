param(
[string] 
$baseOutputPath = 'C:\Users\username\Desktop',

[string]
[ValidateSet('win-x86', 'win-x64', 'osx-x64', 'debian-x64')]
$os = 'win-x86',

[string]
[ValidateSet('false', 'true')]
$sc = 'true'
)

$ErrorActionPreference = "Stop"

$name = "MMXBanTool"
$outputPath = "$baseOutputPath\$name"

$constant = "WINDOWS"

dotnet publish .\BanTool\BanTool.csproj -o $outputPath -c Release -r $os --self-contained $sc -p:DefineConstants="$constant"

Remove-Item -Path "$outputPath\MMX.exe" -Recurse