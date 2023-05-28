using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFML.Graphics;
using SFML.System;
using SFML.Audio;
using SFML.Window;
using Newtonsoft.Json;
using static SFML.Window.Keyboard;
using SFML.Graphics.Glsl;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using Lidgren.Network;
using System.Collections.Specialized;
using System.Text;

namespace MMXOnline
{
    class Program
    {
#if WINDOWS
        [STAThread]
#endif
        static void Main(string[] args)
       {
#if !DEBUG
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Logger.LogFatalException(e);
                Logger.logException(e, false, "Fatal exception", true);
                Thread.Sleep(3000);
            }
#else
            if (Debugger.IsAttached)
            {
                Run();
            }
            else
            {
                try
                {
                    Run();
                }
                catch (Exception e)
                {
                    string crashDump = e.Message + "\n\n" + e.StackTrace + "\n\nInner exception: " + e.InnerException?.Message + "\n\n" + e.InnerException?.StackTrace;
                    Helpers.showMessageBox(crashDump.Truncate(1000), "Fatal Error!");
                    throw;
                }
            }
#endif
        }

        static void Run()
        {
#if MAC
            Global.assetPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "/";
            Global.writePath = Global.assetPath;
#endif
            Global.Init();
            if (Global.debug)
            {
                if (Enum.GetNames(typeof(WeaponIds)).Length > 256)
                {
                    throw new Exception("Too many weapon ids, max 256");
                }
            }

            if (Global.debug)
            {
                Global.promptDebugSettings();
            }

            if (!Global.debug || Global.testDocumentsInDebug)
            {
                string baseDocumentsPath = Helpers.getBaseDocumentsPath();
                string mmxodDocumentsPath = Helpers.getMMXODDocumentsPath();

#if WINDOWS
                if (string.IsNullOrEmpty(mmxodDocumentsPath) && !string.IsNullOrEmpty(baseDocumentsPath) && !Options.main.autoCreateDocFolderPromptShown)
                {
                    Options.main.autoCreateDocFolderPromptShown = true;
                    if (Helpers.showMessageBoxYesNo("Auto-create MMXOD folder in Documents folder?\nThis will be used to store settings, controls, logs and more and will persist across updates.", "MMXOD folder not found in Documents"))
                    {
                        try
                        {
                            Directory.CreateDirectory(baseDocumentsPath + "/MMXOD");
                            mmxodDocumentsPath = Helpers.getMMXODDocumentsPath();
                        }
                        catch (Exception e)
                        {
                            Helpers.showMessageBox("Could not create MMXOD folder in Documents. Error details:\n\n" + e.Message, "Error creating MMXOD folder");
                        }
                    }
                }
#endif

                if (!string.IsNullOrEmpty(mmxodDocumentsPath))
                {
                    Global.writePath = mmxodDocumentsPath;
                    if (Directory.Exists(mmxodDocumentsPath + "/assets"))
                    {
                        Global.assetPath = Global.writePath;
                    }
                }
            }

            if (!checkSystemRequirements())
            {
                return;
            }

            Global.initMainWindow(Options.main);
            RenderWindow window = Global.window;

            window.Closed += new EventHandler(onClosed);
            window.Resized += new EventHandler<SizeEventArgs>(onWindowResized);
            window.KeyPressed += new EventHandler<KeyEventArgs>(onKeyPressed);
            window.KeyReleased += new EventHandler<KeyEventArgs>(onKeyReleased);
            window.MouseMoved += new EventHandler<MouseMoveEventArgs>(onMouseMove);
            window.MouseButtonPressed += new EventHandler<MouseButtonEventArgs>(onMousePressed);
            window.MouseButtonReleased += new EventHandler<MouseButtonEventArgs>(onMouseReleased);
            window.MouseWheelScrolled += new EventHandler<MouseWheelScrollEventArgs>(onMouseScrolled);

            if (Options.main.areShadersDisabled() == false)
            {
                loadShaders();
            }

            loadImages();
            loadSprites();
            loadLevels();
            loadSounds();
            loadMusics();

            Global.computeChecksum();

            Global.input = new Input(false);

            string regionJson =
@"{
   ""name"": """",
   ""ip"": """"
}";
            if (!Helpers.FileExists("region.txt"))
            {
                Helpers.WriteToFile("region.txt", regionJson);
            }

            // Only used to initialize the Global.ignoreUpgradeChecks variable
            var primeRegions = Global.regions;

            Global.regionPingTask = Task.Run(() =>
            {
                foreach (var region in Global.regions)
                {
                    region.computePing();
                }
            });

            setupControllers(window);

            // Force startup config to be fetched

            Menu.change(new MainMenu());
            Global.changeMusic("menu");

            Clock fpsClock = new Clock();
            Clock diagnosticsClock = new Clock();
            Stopwatch packetDiagStopwatch = new Stopwatch();

            while (window.IsOpen)
            {
                if (Global.level != null && Global.level.started)
                {
                    Global.spf = fpsClock.ElapsedTime.AsSeconds();
                    if (Global.spf != 0 && Global.frameCount % 60 == 0) Global.currentFPS = 1 / Global.spf;
                    if (Global.spf > 0.033f) Global.spf = 0.033f;
                    fpsClock.Restart();
                }

                window.DispatchEvents();
                if (Global.isMouseLocked)
                {
                    Mouse.SetPosition(new Vector2i((int)Global.halfScreenW, (int)Global.halfScreenH), Global.window);
                }

                var clearColor = Color.Black;
                if (Global.level?.levelData?.bgColor != null) clearColor = Global.level.levelData.bgColor;
                window.Clear(clearColor);

                long prevPackets = 0;
                if (Global.showDiagnostics)
                {
                    diagnosticsClock.Restart();
                    prevPackets = getBytesPerFrame();
                }

                update();
                render();

                if (Global.showDiagnostics)
                {
                    Global.lastFrameProcessTime = diagnosticsClock.ElapsedTime.AsMilliseconds();
                    Global.lastFrameProcessTimes.Add(Global.lastFrameProcessTime);
                    if (Global.lastFrameProcessTimes.Count > 120) Global.lastFrameProcessTimes.RemoveAt(0);

                    long packetIncrease = getBytesPerFrame() - prevPackets;
                    Global.lastFramePacketIncreases.Add(packetIncrease);
                    if (Global.lastFramePacketIncreases.Count > 120) Global.lastFramePacketIncreases.RemoveAt(0);

                    if (!packetDiagStopwatch.IsRunning) packetDiagStopwatch.Start();
                    if (packetDiagStopwatch.ElapsedMilliseconds > 1000)
                    {
                        long packetTotalDelta = getPacketsReceived() - Global.packetTotal1SecondAgo;
                        Global.packetTotal1SecondAgo = getPacketsReceived();
                        packetDiagStopwatch.Restart();
                        Global.last10SecondsPacketsReceived.Add(packetTotalDelta);
                        if (Global.last10SecondsPacketsReceived.Count > 10) Global.last10SecondsPacketsReceived.RemoveAt(0);

                    }
                }

                window.Display();
            }
        }

        static long getPacketsReceived()
        {
            return Global.serverClient?.packetsReceived ?? 0;
        }

        static long getBytesPerFrame()
        {
            if (Global.serverClient?.client?.ServerConnection?.Statistics != null)
            {
                long downloadedBytes = Global.serverClient.client.ServerConnection.Statistics.ReceivedBytes;
                long uploadedBytes = Global.serverClient.client.ServerConnection.Statistics.SentBytes;
                return (downloadedBytes + uploadedBytes);
            }
            return 0;
        }

        static void setupControllers(Window window)
        {
            // Set up joysticks
            window.JoystickButtonPressed += new EventHandler<JoystickButtonEventArgs>(onJoystickButtonPressed);
            window.JoystickButtonReleased += new EventHandler<JoystickButtonEventArgs>(onJoystickButtonReleased);
            window.JoystickMoved += new EventHandler<JoystickMoveEventArgs>(onJoystickMoved);
            window.JoystickConnected += new EventHandler<JoystickConnectEventArgs>(onJoystickConnected);
            window.JoystickDisconnected += new EventHandler<JoystickConnectEventArgs>(onJoystickDisconnected);
            Joystick.Update();
            if (Joystick.IsConnected(0))
            {
                joystickConnectedHelper(0);
            }

        }

        private static void update()
        {
            if (Global.levelStarted())
            {
                Helpers.tryWrap(Global.level.update, false);
            }
            Menu.update();
            if (Global.leaveMatchSignal != null)
            {
                if (Global.level == null)
                {
                    Global.leaveMatchSignal = null;
                    Menu.change(new MainMenu());
                    return;
                }

                string disconnectMessage = "";
                switch (Global.leaveMatchSignal.leaveMatchScenario)
                {
                    case LeaveMatchScenario.LeftManually:
                        disconnectMessage = "Manually left";
                        break;
                    case LeaveMatchScenario.MatchOver:
                        disconnectMessage = "Match over";
                        break;
                    case LeaveMatchScenario.ServerShutdown:
                        disconnectMessage = "Server was shut down, or you disconnected.";
                        break;
                    case LeaveMatchScenario.Recreate:
                        disconnectMessage = "Recreate";
                        break;
                    case LeaveMatchScenario.Rejoin:
                        disconnectMessage = "Rejoin";
                        break;
                    case LeaveMatchScenario.Kicked:
                        disconnectMessage = "Kicked";
                        break;
                }

                Global.serverClient?.disconnect(disconnectMessage);
                Global.level.destroy();
                
                if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.Recreate)
                {
                    Global.leaveMatchSignal.createNewServer();
                }
                else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.Rejoin)
                {
                    Global.leaveMatchSignal.rejoinNewServer();
                }
                else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.ServerShutdown)
                {
                    Menu.change(new ErrorMenu(disconnectMessage, new MainMenu()));
                }
                else if (Global.leaveMatchSignal.leaveMatchScenario == LeaveMatchScenario.Kicked)
                {
                    Menu.change(new ErrorMenu(new string[] { "You were kicked from the server.", "Reason: " + Global.leaveMatchSignal.kickReason }, new MainMenu()));
                }
                else
                {
                    Menu.change(new MainMenu());
                }

                Global.view.Center = new Vector2f(0, 0);
                Global.music?.stop();

                Global.leaveMatchSignal = null;
            }

            bool isPaused = false; //(Global.menu != null || Global.dialogBox != null);
            if (!isPaused)
            {
                Global.frameCount++;
                Global.time += Global.spf;
                Global.calledPerFrame = 0;

                if (!Global.paused)
                {
                    if (Global.debug)
                    {
                        Global.cheats();
                    }

                    if (Options.main.isDeveloperConsoleEnabled() && Menu.chatMenu != null)
                    {
#if MAC
                        if (Global.input.isPressed(Key.F1))
#else
                        if (Global.input.isPressed(Key.Tilde))
#endif
                        {
                            DevConsole.toggleShow();
                        }
                    }

                    for (int i = Global.sounds.Count - 1; i >= 0; i--)
                    {
                        Global.sounds[i].update();
                        if (!Global.sounds[i].deleted && Global.sounds[i].sound.Status == SoundStatus.Stopped)
                        {
                            Global.sounds[i].sound.Dispose();
                            Global.sounds[i].deleted = true;
                            Global.sounds.RemoveAt(i);
                        }
                    }
                    
                    Global.music.update();
                }

                Global.input.clearInput();
            }
        }

        private static void render()
        {
            if (Global.levelStarted())
            {
                Helpers.tryWrap(Global.level.render, false);
            }

            if (Global.levelStarted())
            {
                Helpers.tryWrap(Menu.render, false);
            }
            else
            {
                Menu.render();
            }

            if (Options.main.showFPS && Global.level != null && Global.level.started)
            {
                int fps = MathF.Round(Global.currentFPS);
                float yPos = 215;
                if (Global.level.gameMode.shouldDrawRadar()) yPos = 219;
                Helpers.drawTextStd(TCat.HUD, "FPS:" + fps.ToString(), Global.screenW - 5, yPos, Alignment.Right, fontSize: 18);
            }

            if (Global.debug)
            {
                //Draw debug strings
                //Global.debugString1 = ((int)Math.Round(1.0f / Global.spf2)).ToString();
                //if(Global.level != null && Global.level.character != null) Global.debugString2 = Mathf.Floor(Global.level.character.pos.x / 8).ToString("0") + "," + Mathf.Floor(Global.level.character.pos.y / 8).ToString("0");

                Helpers.drawTextStd(Global.debugString1, 20, 20);
                Helpers.drawTextStd(Global.debugString2, 20, 40);
                Helpers.drawTextStd(Global.debugString3, 20, 60);
            }

            DevConsole.drawConsole();
        }
        
        /// <summary>
        /// Function called when the window is closed
        /// </summary>
        private static void onClosed(object sender, EventArgs e)
        {
            var openClients = new List<NetClient>();
            if (Global.serverClient?.client != null)
            {
                openClients.Add(Global.serverClient.client);
            }
            var regions = Global.regions.Concat(Global.lanRegions);
            foreach (var region in regions)
            {
                if (region.getPingClient() != null)
                {
                    openClients.Add(region.getPingClient());
                }
            }

            foreach (var client in openClients)
            {
                client.Shutdown("user quit application");
                client.FlushSendQueue();
            }

            while (true)
            {
                int tries = 0;
                Thread.Sleep(10);
                tries++;
                if (tries > 200) break;
                if (openClients.All(c => c.ServerConnection == null))
                {
                    break;
                }
            }

            RenderWindow window = (RenderWindow)sender;
            window.Close();
        }

        private static void onWindowResized(object sender, SizeEventArgs e)
        {
            if (Global.debug)
            {
                if (e.Width / (float)e.Height > 1.33f)
                {
                    e.Width = (uint)(e.Height * (298 / 224f));
                    Global.window.Size = new Vector2u(e.Width, e.Height);
                }
            }
        }

        /// <summary>
        /// Function called when a key is pressed
        /// </summary>
        private static void onKeyPressed(object sender, KeyEventArgs e)
        {
            RenderWindow window = (RenderWindow)sender;
            //if (e.Code == Keyboard.Key.Escape)
            //    window.Close();

            Global.input.keyPressed[e.Code] = !Global.input.keyHeld.ContainsKey(e.Code) || !Global.input.keyHeld[e.Code];
            Global.input.keyHeld[e.Code] = true;
            Global.input.setLastUpdateTime();
            if (Global.input.keyPressed[e.Code])
            {
                Global.input.mashCount++;
            }

            // Check for AI takeover
            if (e.Code == Key.F12 && Global.level?.mainPlayer != null)
            {
                if (Global.level.isTraining() && Global.serverClient == null)
                {
                    if (AI.trainingBehavior == AITrainingBehavior.Default) AI.trainingBehavior = AITrainingBehavior.Idle;
                    else if (AI.trainingBehavior == AITrainingBehavior.Idle) AI.trainingBehavior = AITrainingBehavior.Attack;
                    else if (AI.trainingBehavior == AITrainingBehavior.Attack) AI.trainingBehavior = AITrainingBehavior.Jump;
                    else if (AI.trainingBehavior == AITrainingBehavior.Jump) AI.trainingBehavior = AITrainingBehavior.Default;
                }
                else
                {
                    if (Global.level.isTraining())
                    {
                        if (!Global.level.mainPlayer.isAI)
                        {
                            AI.trainingBehavior = AITrainingBehavior.Attack;
                            Global.level.mainPlayer.aiTakeover = true;
                            Global.level.mainPlayer.isAI = true;
                            Global.level.mainPlayer.character?.addAI();
                        }
                        else
                        {
                            if (AI.trainingBehavior == AITrainingBehavior.Attack) AI.trainingBehavior = AITrainingBehavior.Jump;
                            else if (AI.trainingBehavior == AITrainingBehavior.Jump) AI.trainingBehavior = AITrainingBehavior.Default;
                            else if (AI.trainingBehavior == AITrainingBehavior.Default)
                            {
                                AI.trainingBehavior = AITrainingBehavior.Idle;
                                Global.level.mainPlayer.aiTakeover = false;
                                Global.level.mainPlayer.isAI = false;
                                if (Global.level.mainPlayer.character != null) Global.level.mainPlayer.character.ai = null;
                            }
                        }
                    }
                    else
                    {
                        if (!Global.level.mainPlayer.isAI)
                        {
                            Global.level.mainPlayer.aiTakeover = true;
                            Global.level.mainPlayer.isAI = true;
                            Global.level.mainPlayer.character?.addAI();
                        }
                        else
                        {
                            if (Global.level.isTraining())
                            {
                                AI.trainingBehavior = AITrainingBehavior.Idle;
                            }
                            Global.level.mainPlayer.aiTakeover = false;
                            Global.level.mainPlayer.isAI = false;
                            if (Global.level.mainPlayer.character != null) Global.level.mainPlayer.character.ai = null;
                        }
                    }
                }
            }
            if (e.Code == Key.F12)
            {
                return;
            }

            ControlMenu controlMenu = Menu.mainMenu as ControlMenu;
            if (controlMenu != null && controlMenu.listenForKey && controlMenu.bindFrames == 0)
            {
                controlMenu.bind((int)e.Code);
            }
        }

        private static void onKeyReleased(object sender, KeyEventArgs e)
        {
            Global.input.keyHeld[e.Code] = false;
            Global.input.keyPressed[e.Code] = false;
        }

        static void onMouseMove(object sender, MouseMoveEventArgs e)
        {
            Input.mouseDeltaX = e.X - Global.halfScreenW;
            Input.mouseDeltaY = e.Y - Global.halfScreenH;
            Global.input.setLastUpdateTime();
        }

        static void onMousePressed(object sender, MouseButtonEventArgs e)
        {
            if (Global.debug && Global.level == null)
            {
                if (e.Button == Mouse.Button.Middle)
                {
                    Global.debugString1 = (e.X / Options.main.windowScale) + "," + (e.Y / Options.main.windowScale);
                }
                else
                {
                    Global.debugString1 = "";
                }
            }
            Input.mousePressed[e.Button] = true;
            Input.mouseHeld[e.Button] = true;
            Global.input.setLastUpdateTime();
            Global.input.mashCount++;
        }

        static void onMouseReleased(object sender, MouseButtonEventArgs e)
        {
            int button = (int)e.Button;
            Input.mousePressed[e.Button] = false;
            Input.mouseHeld[e.Button] = false;
        }

        static void onMouseScrolled(object sender, MouseWheelScrollEventArgs e)
        {
            if (e.Delta > 0) Input.mouseScrollUp = true;
            else if (e.Delta < 0) Input.mouseScrollDown = true;
            Global.input.setLastUpdateTime();
        }

        private static void onJoystickButtonPressed(object sender, JoystickButtonEventArgs e)
        {
            int button = (int)e.Button;
            buttonPressedHelper(e.JoystickId, button);
            Global.input.mashCount++;
            Global.input.setLastUpdateTime();
        }

        private static void buttonPressedHelper(uint joystickId, int button)
        {
            var buttonPressed = Global.input.buttonPressed;
            var buttonHeld = Global.input.buttonHeld;
            buttonPressed[button] = !buttonHeld.ContainsKey(button) || !buttonHeld[button];
            buttonHeld[button] = true;

            ControlMenu controlMenu = Menu.mainMenu as ControlMenu;
            if (controlMenu != null && controlMenu.listenForKey && controlMenu.bindFrames == 0)
            {
                controlMenu.bind(button);
            }
        }

        private static void onJoystickButtonReleased(object sender, JoystickButtonEventArgs e)
        {
            int button = (int)e.Button;
            buttonReleasedHelper(e.JoystickId, button);
        }

        private static void buttonReleasedHelper(uint joystickId, int button)
        {
            var buttonPressed = Global.input.buttonPressed;
            var buttonHeld = Global.input.buttonHeld;
            buttonHeld[button] = false;
            buttonPressed[button] = false;
        }

        private static void onJoystickMoved(object sender, JoystickMoveEventArgs e)
        {
            Global.input.setLastUpdateTime();

            Player currentPlayer = Global.level?.mainPlayer;

            int threshold = 70;
            int rawAxisNum = (int)e.Axis;
            int axisNum = 1000 + rawAxisNum;   //1000 = x, 1001 = y

            var cMap = Control.getControllerMapping(currentPlayer?.realCharNum ?? -1, Options.main.axlAimMode);
            if (cMap != null)
            {
                int? rightAxis = cMap.GetValueOrDefault(Control.AimRight);
                int? downAxis = cMap.GetValueOrDefault(Control.AimDown);

                if (rightAxis != null && axisNum == rightAxis)
                {
                    Input.lastAimX = Input.aimX;
                    Input.aimX = e.Position;
                }
                if (downAxis != null && axisNum == downAxis)
                {
                    Input.lastAimY = Input.aimY;
                    Input.aimY = e.Position;
                }
            }

            if (Math.Abs(e.Position) < threshold - 5)
            {
                buttonReleasedHelper(e.JoystickId, -axisNum);
                buttonReleasedHelper(e.JoystickId, axisNum);
            }
            else if (e.Position < -threshold)
            {
                buttonPressedHelper(e.JoystickId, -axisNum);
            }
            else if (e.Position > threshold)
            {
                buttonPressedHelper(e.JoystickId, axisNum);
            }
        }

        private static void onJoystickConnected(object sender, JoystickConnectEventArgs e)
        {
            joystickConnectedHelper(e.JoystickId);
        }

        private static void joystickConnectedHelper(uint joystickId)
        {
            if (Control.isJoystick()) return;
            string controllerName = Joystick.GetIdentification(joystickId).Name;

            Global.input.buttonPressed = new Dictionary<int, bool>();
            Global.input.buttonHeld = new Dictionary<int, bool>();
            
            if (Control.joystick == null)
            {
                Control.joystick = new JoystickInfo(joystickId);
            }

            if (!Control.controllerNameToMapping.ContainsKey(controllerName))
            {
                Control.controllerNameToMapping[Control.getControllerName()] = Control.getGenericMapping();
            }
        }

        private static void onJoystickDisconnected(object sender, JoystickConnectEventArgs e)
        {
            if (Control.joystick != null && Control.joystick.id == e.JoystickId)
            {
                Control.joystick = null;
            }
        }

        static void loadImages()
        {
            string spritesheetPath = "assets/spritesheets";
            if (Options.main.shouldUseOptimizedAssets()) spritesheetPath += "_optimized";
            var spritesheets = Helpers.getFiles(Global.assetPath + spritesheetPath, false, "png", "psd");
            
            var menuImages = Helpers.getFiles(Global.assetPath + "assets/menu", true, "png", "psd");
            spritesheets.AddRange(menuImages);

            for (int i = 0; i < spritesheets.Count; i++)
            {
                string path = spritesheets[i];
                Texture texture = new Texture(path);
                Global.textures[Path.GetFileNameWithoutExtension(path)] = texture;
            }

            var mapSpriteImages = Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "png", "psd");
            foreach (var mapSpriteImage in mapSpriteImages)
            {
                var pieces = mapSpriteImage.Split("/sprites/");
                if (pieces.Length == 2 && pieces[1].EndsWith(".png"))
                {
                    string spriteImageName = pieces[1].Replace(".png", "");
                    Texture texture = new Texture(mapSpriteImage);
                    string mapName = mapSpriteImage.Replace("/sprites/" + pieces[1], "").Split("/").ToList().Pop();
                    Global.textures[mapName + ":" + spriteImageName] = texture;
                }
            }
        }

        static void addToFileChecksumBlob(Dictionary<string, string> fileNamesToContents)
        {
            string entireBlob = "";
            var keys = fileNamesToContents.Keys.ToList();
            keys.Sort(Helpers.invariantStringCompare);

            foreach (var key in keys)
            {
                entireBlob += key.ToLowerInvariant() + " " + fileNamesToContents[key];
            }
            Global.fileChecksumBlob += entireBlob + "|";
        }

        static void loadLevels()
        {
            var levelPaths = Helpers.getFiles(Global.assetPath + "assets/maps", true, "json");

            var fileChecksumDict = new Dictionary<string, string>();
            var invertedMaps = new HashSet<string>();
            foreach (string levelPath in levelPaths)
            {
                string levelText = File.ReadAllText(levelPath);
                var levelData = new LevelData(levelText, false);

                var pathPieces = levelPath.Split('/').ToList();
                string fileName = pathPieces.Pop();
                string folderName = pathPieces.Pop();
                fileChecksumDict[folderName + "/" + fileName] = levelText;

                if (levelData.name.EndsWith("_inverted"))
                {
                    invertedMaps.Add(levelData.name.Replace("_inverted", ""));
                    continue;
                }
                else if (levelData.name.EndsWith("_mirrored"))
                {
                    levelData.name = levelData.name.Replace("_inverted", "");
                    Global.levelDatas.Add(levelData.name, levelData);
                    levelData.isMirrored = true;
                    levelData.name = levelData.name.Replace("_mirrored", "");
                }
                else
                {
                    Global.levelDatas.Add(levelData.name, levelData);
                }
            }
            addToFileChecksumBlob(fileChecksumDict);

            var customLevelPaths = Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "json");
            foreach (string levelPath in customLevelPaths)
            {
                if (levelPath.Contains("/sprites/")) continue;

                string levelText = File.ReadAllText(levelPath);
                var levelData = new LevelData(levelText, true);
                if (levelData.name.EndsWith("_mirrored"))
                {
                    Global.levelDatas.Add(levelData.name, levelData);
                    levelData.isMirrored = true;
                    levelData.name = levelData.name.Replace("_mirrored", "");
                }
                else
                {
                    Global.levelDatas.Add(levelData.name, levelData);
                }
            }

            foreach (string invertedMap in invertedMaps)
            {
                Global.levelDatas[invertedMap].supportsMirrored = true;
            }

            foreach (var levelData in Global.levelDatas.Values)
            {
                levelData.populateMirrorMetadata();
            }
        }

        static void loadSprites()
        {
            string spritePath = "assets/sprites";
            
            List<string> spriteFilePaths = Helpers.getFiles(Global.assetPath + spritePath, false, "json");
            if (spriteFilePaths.Count > 65536)
            {
                throw new Exception("Exceeded max sprite limit of 65536. Fix actor.cs netUpdate() to support more sprites.");
            }
            
            var fileChecksumDict = new Dictionary<string, string>();
            foreach (string spriteFilePath in spriteFilePaths)
            {
                string name = Path.GetFileNameWithoutExtension(spriteFilePath);
                string json = File.ReadAllText(spriteFilePath);

                fileChecksumDict[name] = json;

                Sprite sprite = new Sprite(json, name, null);
                Global.sprites[sprite.name] = sprite;
            }
            addToFileChecksumBlob(fileChecksumDict);

            // Override sprite mods
            string overrideSpriteSource = "assets/sprites_visualmods";
            if (Options.main.shouldUseOptimizedAssets()) overrideSpriteSource = "assets/sprites_optimized";
            
            List<string> overrideSpritePaths = Helpers.getFiles(Global.assetPath + overrideSpriteSource, false, "json");
            foreach (string overrideSpritePath in overrideSpritePaths)
            {
                string name = Path.GetFileNameWithoutExtension(overrideSpritePath);
                string json = File.ReadAllText(overrideSpritePath);

                Sprite sprite = new Sprite(json, name, null);
                if (Global.sprites.ContainsKey(sprite.name))
                {
                    Global.sprites[sprite.name].overrideSprite(sprite);
                }
            }

            // Set up aliases here
            foreach (var spriteName in Global.sprites.Keys.ToList())
            {
                string alias = Global.spriteAliases.GetValueOrDefault(spriteName);
                if (!string.IsNullOrEmpty(alias))
                {
                    var pieces = alias.Split(',');
                    foreach (var piece in pieces)
                    {
                        Global.sprites[piece] = Global.sprites[spriteName].clone();
                        Global.sprites[piece].name = piece;
                    }
                }
            }
        }

        static void loadSounds()
        {
            var soundNames = Helpers.getFiles(Global.assetPath + "assets/sounds", true, "ogg", "wav");
            if (soundNames.Count > 65535)
            {
                throw new Exception("Cannot have more than 65535 sounds.");
            }

            var fileChecksumDict = new Dictionary<string, string>();
            for (int i = 0; i < soundNames.Count; i++)
            {
                string file = soundNames[i];
                string name = Path.GetFileNameWithoutExtension(file);
                fileChecksumDict[name] = "";
                Global.soundBuffers.Add(name, new SoundBufferWrapper(name, file, SoundPool.Regular));
            }
            addToFileChecksumBlob(fileChecksumDict);

            // Voices
            var voiceNames = Helpers.getFiles(Global.assetPath + "assets/voices", true, "ogg", "wav");
            for (int i = 0; i < voiceNames.Count; i++)
            {
                string file = voiceNames[i];
                string name = Path.GetFileNameWithoutExtension(file);
                Global.voiceBuffers.Add(name, new SoundBufferWrapper(name, file, SoundPool.Voice));
            }

            // Char-Specific Overrides
            var overrideNames = Helpers.getFiles(Global.assetPath + "assets/sounds_overrides", true, "ogg", "wav");
            for (int i = 0; i < overrideNames.Count; i++)
            {
                string file = overrideNames[i];
                string name = Path.GetFileNameWithoutExtension(file);
                if (Global.soundBuffers.ContainsKey(name))
                {
                    Global.soundBuffers[name] = new SoundBufferWrapper(name, file, SoundPool.Regular);
                }
                else
                {
                    Global.charSoundBuffers.Add(name, new SoundBufferWrapper(name, file, SoundPool.CharOverride));
                }
            }
        }

        static void loadMusics()
        {
            string path = Global.assetPath + "assets/music";
            List<string> files = Helpers.getFiles(path, true, "ogg");
            files = files.Concat(Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "ogg")).ToList();

            for (int i = 0; i < files.Count; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                
                var pieces = name.Split('.');
                string baseName = pieces[0];
                if (files[i].Contains("assets/maps_custom"))
                {
                    var filePieces = files[i].Split('/').ToList();
                    filePieces.Pop();
                    string customMapName = filePieces.Pop();
                    if (baseName == "music")
                    {
                        baseName = customMapName;
                    }
                    else
                    {
                        baseName = customMapName + ":" + baseName;
                    }
                }

                int pieceIndex = 1;
                double startPos = 0;
                double endPos = 0;
                if (pieceIndex < pieces.Length && double.TryParse(pieces[pieceIndex].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out startPos))
                {
                    pieceIndex++;
                }
                if (pieceIndex < pieces.Length && double.TryParse(pieces[pieceIndex].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out endPos))
                {
                    pieceIndex++;
                }
                string charOverride = pieceIndex < pieces.Length ? ("." + pieces[pieceIndex]) : "";
                MusicWrapper musicWrapper = new MusicWrapper(files[i], startPos, endPos, loop: (endPos != 0));

                Global.musics[baseName + charOverride] = musicWrapper;
            }
        }

        static void loadShaders()
        {
            string path = Global.assetPath + "assets/shaders";
            List<string> files = Helpers.getFiles(path, false, "frag");
            files = files.Concat(Helpers.getFiles(Global.assetPath + "assets/maps_custom", true, "frag")).ToList();

            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].Contains("standard.vertex")) continue;
                bool isCustomMapShader = files[i].Contains("assets/maps_custom");
                string customMapName = "";
                if (isCustomMapShader)
                {
                    var pieces = files[i].Split('/').ToList();
                    pieces.Pop();
                    customMapName = pieces.Pop();
                }

                string shaderContents = File.ReadAllText(files[i]);
                string shaderName = Path.GetFileNameWithoutExtension(files[i]);
                if (isCustomMapShader)
                {
                    shaderName = customMapName + ":" + shaderName;
                }

                Global.shaderCodes[shaderName] = shaderContents;

                try
                {
                    Global.shaders[shaderName] = Helpers.createShader(shaderName);
                    Global.shaderWrappers[shaderName] = new ShaderWrapper(shaderName);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains(Helpers.noShaderSupportMsg))
                    {
                        Global.shadersNotSupported = true;
                    }
                    else
                    {
                        Global.shadersFailed.Add(shaderName);
                    }
                }
            }
        }

        static bool checkSystemRequirements()
        {
            List<string> errors = new List<string>();

            uint maxTextureSize = Texture.MaximumSize;
            if (maxTextureSize < 1024)
            {
                errors.Add("Your GPU max texture size (" + maxTextureSize + ") is too small. Required is 1024. The game cannot be played as most visuals require a larger GPU max texture size.\nAttempt to launch game anyway?");
                string errorMsg = string.Join(Environment.NewLine, errors);
                bool result = Helpers.showMessageBoxYesNo(errorMsg, "System Requirements Not Met");
                return result;
            }

            if (Options.main.graphicsPreset == null)
            {
                OptionsMenu.inferPresetQuality(maxTextureSize);
            }

            if (Options.main.showSysReqPrompt)
            {
                if (Global.shadersNotSupported)
                {
                    errors.Add("Your system does not support shaders. You can still play the game, but you will not see special effects or weapon palettes.");
                }
                else if (Global.shadersFailed.Count > 0)
                {
                    string failedShaderStr = string.Join(",", Global.shadersFailed);
                    errors.Add("Failed to compile the following shaders:\n\n" + failedShaderStr + "\n\nYou can still play the game, but you will not see these shaders' special effects.");
                }
            }

            if (errors.Count > 0)
            {
                string errorMsg = string.Join(Environment.NewLine + Environment.NewLine, errors);
                Helpers.showMessageBox(errorMsg, "System Requirements Not Met");
            }

            return true;
        }
    }
}