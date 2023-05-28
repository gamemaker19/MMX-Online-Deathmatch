BASE_PATH="/Users/UserName/Desktop/MMXOnlineV12_3"
BASE_REPO_PATH="/Users/UserName/Desktop/MMXOnlineCode"

cd BASE_REPO_PATH
dotnet publish ./MMX/MMX.csproj -o "${BASE_PATH}/Contents/MacOS" -c Release -r "osx-x64" --self-contained true -p:DefineConstants="MAC"
cp -a ./MacOS/Frameworks/. "${BASE_PATH}/Contents/Frameworks"
cp -a ./MacOS/dylibs/. "${BASE_PATH}/Contents/MacOS"

mkdir "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/fonts" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/menu" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/music" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/shaders" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/shaders2" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/sounds" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/sprites" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/spritesheets" "${BASE_PATH}/Contents/MacOS/assets"
cp -a "${BASE_REPO_PATH}/LevelEditor/assets/backgrounds" "${BASE_PATH}/Contents/MacOS/assets"
cp -a ./MacOS/assets/. "${BASE_PATH}/Contents/MacOS/assets"

cp -a ./MacOS/Info.plist "${BASE_PATH}/Contents"
mkdir "${BASE_PATH}/Contents/Resources"
cp -a ./MacOS/icon.icns "${BASE_PATH}/Contents/Resources"
cd "${BASE_PATH}/Contents/MacOS"
install_name_tool -add_rpath @executable_path/../Frameworks MMX
mv "${BASE_PATH}" "${BASE_PATH}.app"