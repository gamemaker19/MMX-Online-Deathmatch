param(
[string] 
$baseOutputPath = 'C:\Users\username\Desktop'
)

$buildToolBasePath = "$outputPath\"
$buildFolderName = "MMXOD Editor"

Remove-Item -Path "$baseOutputPath\$buildFolderName" -Recurse -ErrorAction Ignore

yarn package

Rename-Item "out\MMXOD Sprite Editor-win32-x64" "$buildFolderName"
Copy-Item -Path "MMXOD Map Editor.bat" "out\$buildFolderName"
Copy-Item -Path "styles.css" "out\$buildFolderName"
Copy-Item -Path "images" "out\$buildFolderName" -Recurse
Move-Item -Path "out\$buildFolderName" $baseOutputPath