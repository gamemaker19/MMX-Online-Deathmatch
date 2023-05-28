param(
[Parameter(Mandatory=$true)]
[string]
$version,

[string] 
$baseOutputPath = 'C:\Users\username\Desktop',

[string]
[ValidateSet('win-x86', 'win-x64', 'osx-x64', 'debian-x64')]
$os = 'win-x86',

[string]
[ValidateSet('false', 'true')]
$sc = 'true',

[string]
[ValidateSet('false', 'true')]
$runBuildTools = 'true',

[string]
[ValidateSet('false', 'true')]
$includeAssets = 'true',

[string]
[ValidateSet('false', 'true')]
$zip = 'false',

[string]
[ValidateSet('false', 'true')]
$deleteAfterZip = 'false'
)

$ErrorActionPreference = "Stop"

$scStr = If ($sc -eq 'true') {"_sc"} Else {""}
$osStr = $os.Replace('-','_')
$versionFileName = $version.Replace('.','_')
$name = "MMXOnlineV$versionFileName" + "_$osStr" + "$scStr"
$outputPath = "$baseOutputPath\$name"

$constant = "WINDOWS"
If (($os -eq 'debian-x64') -or ($os -eq 'osx-x64'))
{
    $constant = "LINUX"
}

if ($runBuildTools -eq 'true')
{
    # Build and run the build tools
    dotnet publish .\BuildTools\BuildTools.csproj -c Release
    cd .\BuildTools\bin\Release\net5.0-windows
    Write-Host "Start run build tools."
    Start-Process .\BuildTools.exe -NoNewWindow -Wait
    Write-Host "End run build tools."
    cd ..\..\..\..\
}

# Update version number in code
$content = Get-Content -path "MMX\Global.cs"
$content -Replace 'public static decimal version = (.+)m', "public static decimal version = $($version)m" |  Out-File "MMX\Global.cs"

# Build the game
dotnet publish .\MMX\MMX.csproj -o $outputPath -c Release -r $os --self-contained $sc -p:PublishSingleFile=True -p:DefineConstants="$constant"

If ($includeAssets -eq 'true')
{
    # Copy over assets folder, without levels
    Copy-Item -Path .\LevelEditor\assets -Destination $outputPath -Force -Recurse
}

# Copy over misc game files
Copy-Item -Path .\GameFiles\GetShaderErrors.bat -Destination $outputPath -Force
Copy-Item -Path .\GameFiles\Help.url -Destination $outputPath -Force

# Copy over optimized asset files for Mac build (these will be version controlled)
If (($os -eq 'debian-x64') -or ($os -eq 'osx-x64'))
{
    Remove-Item -Path "MacOS\assets" -Recurse -ErrorAction Ignore
    New-Item -Path "MacOS\" -Name "assets" -ItemType "directory" -Force
    Copy-Item -Path ".\LevelEditor\assets\sprites_optimized" -Destination "MacOS\assets" -Recurse
    Copy-Item -Path ".\LevelEditor\assets\spritesheets_optimized" -Destination "MacOS\assets" -Recurse
}

<#
Start-Sleep -Seconds 3

If ($zip -eq 'true')
{
    Compress-Archive -Path $outputPath -DestinationPath "$baseOutputPath\$name.zip"
    if ($deleteAfterZip -eq 'true')
    {
        Remove-Item -Path $outputPath -Recurse
    }
}
#>