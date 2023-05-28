# Overview

- This is the project for the level and sprite editors of MMXOD, used for both sprite mods/custom maps as well as offical development of the game. Both editors are part of this project and are bundled in one app.
- This project is a JavaScript/TypeScript Electron Desktop app. React is used as the UI library.
- This template was used to create the project: https://github.com/diego3g/electron-typescript-react
- Redux is NOT used. A simple, custom state management system was developed. This does rely heavily on forceUpdate which may seem crude but makes things simpler and easier especially when interacting with low level canvas drawing.
- As per design of the Electron framework, the UI thread is separate from the main thread. The UI code is rendered in React via tsx files. The main thread code can be found in main.ts and has code related to the window, process, file IO, etc. The UI thread communicates with the main thread via bride.ts.

# Local development

First time setup
- Install node.js. If latest doesn't work, 16.13.0 works: https://nodejs.org/dist/v16.13.0/node-v16.13.0-x64.msi
- Install yarn in Powershell admin: `npm install --global yarn`
- Run `yarn` in Powershell in LevelEditor folder to install packages.

Then, to test your changes locally, run the following in Powershell in LevelEditor folder:
- `yarn start` starts the sprite editor
- `yarn start-le` starts the level editor

Changes to main thread code require closing and restarting the app with the yarn start commands. However, changes to UI code support hot reload and should automatically refresh when you change the UI files.

# Building a release

To create a release build of the project, run buildeditor.ps1 (or buildeditor_x86.ps1 for 32 bit). You must first change the output path in the script to a location on your machine.
Unlike the C#/.NET code there is no concept of "self contained" builds since this is JavaScript/Electron.