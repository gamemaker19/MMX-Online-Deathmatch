param(
[Parameter(Mandatory=$true)]
[string]
$version,

[string] 
$baseOutputPath = 'C:\Users\username\Desktop',

[string]
[ValidateSet('win-x86', 'win-x64', 'osx-x64', 'debian-x64')]
$os = 'win-x64',

[string]
[ValidateSet('false', 'true')]
$sc = 'false'
)

$ErrorActionPreference = "Stop"

$outputPath = "$baseOutputPath\RelayServer"

$versionFileName = $version.Replace('.','_')
$osStr = $os.Replace('-','_')
$scStr = If ($sc -eq 'true') {"_sc"} Else {""}
$outputPath = "$baseOutputPath\RelayServerV$versionFileName" + "_" + $osStr + $scStr

$content = Get-Content -path "MMX\Global.cs"
$content -Replace 'public static decimal version = (.+)m', "public static decimal version = $($version)m" |  Out-File "MMX\Global.cs"

$constant = "WINDOWS"
If (($os -eq 'debian-x64') -or ($os -eq 'osx-x64'))
{
    $constant = "LINUX"
}

dotnet publish .\RelayServer\RelayServer.csproj -o $outputPath -c Release -r $os --self-contained $sc -p:DefineConstants="$constant%3BRELAYSERVER"

# Copy over misc game files
Copy-Item -Path .\GameFiles\overrideversion.txt -Destination $outputPath -Force
Copy-Item -Path .\GameFiles\RelayServerHelp.url -Destination $outputPath -Force