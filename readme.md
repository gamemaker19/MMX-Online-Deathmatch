# Mega Man X Online: Deathmatch

Welcome to the Mega Man X Online: Deathmatch (MMXOD) repository! This repo hosts the source code of the official game.

PLEASE NOTE: This project is no longer under development or support. I am not accepting pull requests. I might fix documentation or website mistakes, but that's pretty much it at this point.

If you are not satisfied with the game in its final state, you are welcome to clone/fork the code in a separate repo and rebrand it as a mod of the game, and develop your own community, discord, youtube trailers, etc. around it.

The license for the source code is [MIT](https://choosealicense.com/licenses/mit/). However please be mindful that Capcom owns much of the assets and IP in this project. Thus, charging people money for forks or mods is probably not a good idea.

## Game Website and Other Related Repo's

The game website can be found here, which has user-facing help guides, release downloads, credits for the project, and more: https://gamemaker19.github.io/MMXOnlineDesktop

The repository of the game website can be found here: https://github.com/gamemaker19/MMXOnlineDesktop. This repository also stores releases for MMXOD tools and bonuses (i.e. map/sprite editor, voice packs, etc.)

The repo for the old web version of the game can be found here as a curiosity: https://github.com/gamemaker19/MMXOnline. Note that for legacy reasons, the downloadable releases of MMXOD are stored in this repo, because the actual MMXOD repo was private until now and private repo's can't have public releases available for download. (Yes, the website repo would probably have been a better choice, but I'm not changing things around at this point.)

## Dev Setup

For the C# parts of the code, you will need Visual Studio/.NET 5 SDK/C# installed. (Note: .NET 5 SDK is no longer supported, and future forks of the project should upgrade to the newest .NET version)

For the Javascript/Typescript parts of the code, you will Node.js installed and preferrably Visual Studio Code as the IDE. As with .NET 5, the Node.js version may be old and unsupported. If so, future forks should look into upgrading it.

Please make sure you are on at least Windows 10. Older versions could run into many more issues not documented. Also make sure you are on a recent Visual Studio version. I'm not sure how old a VS version MMXOD supports, but the older the version, the more likely there will be issues. (Since this game is no longer development, too *new* versions in the far future might pose issues, too. VS 2022 should work.)

## Overview

The code is made up of many components. Most are C#/.NET, with the exception of the level editor which is JavaScript/TypeScript.

- MMX: this is the actual game client. It's what players launch to run the game. SFML is the multimedia library used. This is a C++ library so the C# binding library was used. Note: SFML has a networking module but Lidgren was used instead for the networking library.
- RelayServer: this is the relay server, a command line program that runs the server of the game. Its jobs include acting as a "relay server" (i.e. ferrying packets across clients, allowing them to communicate over NAT), as well as matchmaking, bans, and a small amount of game logic. This is what is needed to run internet or LAN matches.
- ServerTools: Command line tool to quickly update the game, check bans, etc, for internet hosted relay servers, without having to remote desktop into the server. This is not strictly required for anything, it just automates things and prevents having to constantly remote into the server.
- BanTool: A desktop application (winforms) for in-game mods to warn and ban people from internet hosted relay servers.
- BuildTools: Utilities used by the build scripts to build the game. Handles things like generating optimized sprites.
- Lidgren.Network: Library used for UDP networking for the game. Note, it is not installed via Nuget but actually put in the repo as a project dependency which makes it easier to modify, investigate issues, etc.
- LevelEditor: contains the map and sprite editor code. Unlike the other components, this is a JavaScript (TypeScript) project using the Electron framework. See the readme in the LevelEditor folder for more details on that project. This is both used by development of the official game sprites and maps, as well as for sprite mods and custom maps. For official development, simply make changes to assets in the assets folder found in the LevelEditor folder which are the "official" game assets that are part of the repo and packaged by the build scripts.

For the C# projects, open RelayServer.sln for Relay Server development and MMX.sln for everything else. In MMX.sln, the default startup project is MMX.csproj which is the game client itself. Just do the standard F5 or Ctrl+F5 here to run it locally. Change the startup project from MMX (which is the default and is the game client itself) to one of the others if you want to run the other projects locally with F5 or Ctrl+F5 (i.e. Build/Server/Ban Tools, etc.) When dev'ing locally, there are some differences in code logic to facilitate local development. For example there are "quick start" options at the top of Global.cs you can use to quickly jump into Training on launch, automatic connections to localhost relay server, etc. See Global.cs and the `debug` variable in it for more context.

For level editors, open the LevelEditor folder in VS Code to get started. See the readme in the LevelEditor folder for more details.

## High level code notes

- Program.cs is the starting point for the app.
- Global.cs has some important constants that are global to the app, including version number, checksum, regions, and a lot more.
- Every entity in the game is a GameObject. They can either be geometries (Geometry.cs, i.e. walls, collision zones) or actors (Actor.cs, i.e. objects like characters, projectiles, etc.)
- Level.cs is the main class for running in-match logic.
- Character.cs has character logic. Each specific character is split out into multiple partial classes in the Characters folder but they all share the same character class.
- Code for weapons and Mavericks is generally found in the Weapons and Mavericks folder, respectively.

This is just a starting point and if you want to know more about the code and its structure and files, all of the C# code for the game client can be found in the MMX folder under top level repo.

## High level netcode architecture

MMXOD's netcode model is...strange. It can be best described as a combination of P2P and authoritarian server. If I had to call it something I would call it "ownership based". It allows clients to provide and update most of the state for game objects they "own". This typically includes their character and projectiles they generate. Thus, most game state is not actually run by the match host but done by the clients. Hence the "isOwnedByLocalPlayer" check in various parts of the code. However, there are a few things the host or relay server controls, such as match score, moving platform positions, and a few other exceptions.

The relay server is also a bit strange. It acts as a traditional "relay server" in that it does not actually run almost any of the match's game logic. There is the concept of a match host, but the hoster and the relay server are not necessarily the same machine. But the relay server also has a small amount of game logic, such as storing deathmatch kills.

There are some major weaknesses with this approach such as ease of cheating since the game is too trusting of the client to provide the state of everything it owns, as well as increased bandwidth. However, there is a benefit in that it does allow for instantaneous and snappy movement with no input lag. It also makes the netcode simpler in several aspects (but not all of them) and allows writing game logic almost as if it was an offline game.

The netcode isn't good or professional grade quality and is patchwork at this point, but in the end it does work to some degree, as evidenced by the 2 1/2 years of people successfully playing online MMXOD matches. However if I were to re-write the netcode from scratch I would probably use a proper client/server architecture with some sort of client side prediction. This approach was done mostly as a rush to get something out the door as my first major netcode project without having to implement the complexity of something like client side prediction, rollback, or re-writing the game engine to have serializable game state.

TCP (via standard .NET libraries) is only used for matchmaking. (Creating/finding matches, etc.) UDP (via Lidgren) is used for all in-match netcode as well as to query the ping of servers in the menus. Most UDP server to client communication is done via RPCs, in RPC.cs.

## Creating release builds

- buildwindows.ps1 builds four release builds, one for each combination of SC\*/Non-SC + x86/x64 processor. Note, only the x86 SC build has assets bundled. But the asset folder is generated as well from this script.
- buildservers.ps1 builds four relay server builds, one for each combination of SC/Non-SC + x86/x64 processor.
- buildlinux.ps1 builds the linux build. Note, this does not have assets bundled.
- buildmac.sh builds the mac os build. Note, this requires a mac. It also requires buildlinux.ps1 to have been run on a windows PC first and the files generated in MacOS to be present on the mac machine's MMXOD repo. Overall mac building is very complex and will not be documented in great detail.

\*SC stands for "Self Contained", meaning a build that does not require .NET 5 pre-installed. The disadvantage is that the build size will be much larger.

Before building, the Global.cs assetChecksum variable should be set to the checksum obtained by pressing F1 in main menu, if you changed any assets. This step could be automated as a future enhancement.
Note, if you want to create a mod of the game, set the Global.cs checksumPrefix variable to something unique to your mod to ensure there are no conflicts with other mods or the base game.

Replace $baseOutputPath in these scripts (and the ones they call) with the path to your desktop (or wherever you want to generate the build files at). The build scripts will not zip up the folders. You should do that manually.

For the ban tool run buildbantool.ps1. For the server tools, there is no powershell script. You have to manually build a release (or even debug) build in Visual Studio and copy/zip up the bin folders accordingly for distribution.
Note, these are only useful if you want to host your own server AND manage a warning/ban system. If so you'll need to update the ip address in the code (BanTool/Form1.cs, ServerTools/Program.cs) to the one of your server before building/releasing it.

## Self hosting

To self host an online server, take the relay server build (or grab the v19.12 relay server build or later from the downloads page) and run it on any server on the internet with a public IP address, with TCP and UDP ports 14242-14342 opened on it. Then provide people with the IP address of the server which they can update their region.txt file with to connect to your server via internet option.

The game website has a quick start guide and ARM template here that can be used to quickly spin up an Azure MMXOD server: https://gamemaker19.github.io/MMXOnlineDesktop/azure_server_help.html

For the relay server, if you plan on hosting an internet server (as opposed to LAN) and want warnings/bans/reporting to work, be sure to create an encryptionKey.txt file and secretPrefix.txt file in the relay server folder before deploying it. (Don't release these in source or to the public, obviously.) These should each contain a secret password. encryptionKey.txt should be a string password, and must be exactly 32 characters. secretPrefix.txt should also be secure and not guessable as well but does not need to be as long. If you want the ban and server tools to work, you also need to put the secretPrefix.txt file in those builds' folders, as well. You'll also need to update the source of the ban and server tool projects to point to your server ip (BanTool/Form1.cs, ServerTools/Program.cs) before building them. Also needed is a banlist.json file in the relay server folder, initially set to empty array JSON, i.e. `[]`. Then, bans generated by the tools will update this file if the server is running.

It is not recommended to use this warn/ban system or rely on it though since it is very complex and difficult to test and debug, not well tested, and since source is available could be easily circumventable. It is better to limit to audience of the server IP address you share to only people you trust, i.e. friends only. Even if you do decide to use this system, you might run into bugs or snags that require code changes to fix, since there are many moving parts and configs. If you're not careful you could also leak user IP addresses as well. Thus, I cannot recommend this ban system and will not document it in great detail, but all the code you need is there if you still want to try it.