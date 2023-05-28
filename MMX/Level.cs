using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public partial class Level
    {
        #region dynamic lists
        // Any list that can grow over time should be put here for memory leak investigation
        public HashSet<GameObject> gameObjects = new HashSet<GameObject>();
        public List<Actor> mapSprites = new List<Actor>();
        public List<List<HashSet<GameObject>>> grid = new List<List<HashSet<GameObject>>>();
        public HashSet<HashSet<GameObject>> occupiedGridSets = new HashSet<HashSet<GameObject>>();

        // These are returned by getListCounts()
        public HashSet<Effect> effects = new HashSet<Effect>();
        public Dictionary<string, List<float>> recentClipCount = new Dictionary<string, List<float>>();
        public List<LoopingSound> loopingSounds = new List<LoopingSound>();
        public List<MusicWrapper> musicSources = new List<MusicWrapper>();
        public List<BoundBlasterAltProj> boundBlasterAltProjs = new List<BoundBlasterAltProj>();
        public List<CrystalHunterCharged> chargedCrystalHunters = new List<CrystalHunterCharged>();
        public List<DarkHoldProj> darkHoldProjs = new List<DarkHoldProj>();
        public List<GravityWellProj> unchargedGravityWells = new List<GravityWellProj>();
        public List<BackloggedSpawns> backloggedSpawns = new List<BackloggedSpawns>();
        public List<DelayedAction> delayedActions = new List<DelayedAction>();
        public Dictionary<ushort, float> recentlyDestroyedNetActors = new Dictionary<ushort, float>();
        public List<BufferedDestroyActor> bufferedDestroyActors = new List<BufferedDestroyActor>();
        public Dictionary<int, FailedSpawn> failedSpawns = new Dictionary<int, FailedSpawn>();

        public string getListCounts()
        {
            return effects.Count + "," + recentClipCount.Keys.Count + "," + loopingSounds.Count + "," + musicSources.Count + "," + boundBlasterAltProjs.Count + "," + chargedCrystalHunters.Count + "," + unchargedGravityWells.Count + "," + backloggedSpawns.Count + "," +
                delayedActions.Count + "," + recentlyDestroyedNetActors.Keys.Count + "," + bufferedDestroyActors.Count + "," + failedSpawns.Keys.Count;
        }
        #endregion

        public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        public List<SpawnPoint> raceStartSpawnPoints = new List<SpawnPoint>();
        public List<NoScroll> noScrolls = new List<NoScroll>();
        public List<Rect> waterRects = new List<Rect>();
        public List<NavMeshNode> navMeshNodes = new List<NavMeshNode>();
        public List<ItemSpawner> itemSpawners = new List<ItemSpawner>();
        public List<Gate> gates = new List<Gate>();

        public Flag blueFlag;
        public Flag redFlag;
        public NavMeshNode redFlagNode;
        public NavMeshNode blueFlagNode;
        public ControlPoint hill;
        public List<ControlPoint> controlPoints = new List<ControlPoint>();
        public List<MovingPlatform> movingPlatforms = new List<MovingPlatform>();
        public VictoryPoint goal;
        public NavMeshNode goalNode;

        public float gravity;
        public bool camSetFirstTime;
        public float camX;
        public float camY;
        public float zoomScale;
        public int frameCount;
        public float twoFrameCycle;
        public string debugString;
        public string debugString2;
        public float lerpCamTime = 0;
        public GameMode gameMode;
        public float cellWidth;
        public LevelData levelData;
        public int mirrorX;
        public long autoIncActorZIndex = ZIndex.Actor;
        public long autoIncCharZIndex = ZIndex.Character;
        public Texture[,] backgroundSprites;
        public Texture[,] backwallSprites;
        public Texture[,] foregroundSprites;
        public const int gridSize = 50;
        public bool started = false;
        public bool joinedLate;
        public int width;
        public int height;
        public bool equalCharDistribution;
        public bool supportsRideChasers;
        public bool rideArmorFlight;
        public bool hasMovingPlatforms { get { return movingPlatforms.Count > 0; } }

        public float syncValue;
        public float? hostSyncValue;
        public byte crackedWallAutoIncId;
        public List<Action> debugDrawCalls = new List<Action>();

        public List<Parallax> parallaxes = new List<Parallax>();
        public List<Point> parallaxOffsets = new List<Point>();
        public List<Texture[,]> parallaxTextures = new List<Texture[,]>();
        public List<List<ParallaxSprite>> parallaxSprites = new List<List<ParallaxSprite>>();

        public float afkWarnTime = 60;
        public float afkKickTime = 90;

        public int startGoCount;
        public int startGridCount;
        public int flaggerCount;

        public const ushort maxReservedNetId = 50;
        public const ushort maxReservedCharNetId = 50;
        public const ushort redFlagNetId = 10;
        public const ushort blueFlagNetId = 11;
        public const ushort redFlagPedNetId = 12;
        public const ushort blueFlagPedNetId = 13;
        public const ushort cp1NetId = 14;
        public const ushort cp2NetId = 15;

        public ShaderWrapper backgroundShader;
        public Texture backgroundShaderImage;
        public ShaderWrapper parallaxShader;
        public Texture parallaxShaderImage;

        public List<Player> players = new List<Player>();
        public Player mainPlayer;
        public Player otherPlayer
        {
            get
            {
                return players.FirstOrDefault(p => !p.isMainPlayer);
            }
        }
        private Player _specPlayer;
        public Player specPlayer
        {
            get { return _specPlayer; }
            set
            {
                if (_specPlayer == value) return;
                Point? prevCamPos = _specPlayer?.character?.getCamCenterPos();
                _specPlayer = value;
                if (_specPlayer?.character != null)
                {
                    // Every time a spectate target is set, snap to their char position first
                    snapCamPos(_specPlayer.character.getCamCenterPos(), prevCamPos);
                }
            }
        }
        public Player camPlayer
        {
            get
            {
                if (mainPlayer.isSpectator)
                {
                    if (!Global.level.players.Contains(specPlayer))
                    {
                        // Player left match: immediately get next spectate target, and if none, return null
                        specPlayer = getNextSpecPlayer(1);
                        if (specPlayer == null) return mainPlayer;
                        return specPlayer;
                    }
                    return specPlayer;
                }
                return mainPlayer;
            }
        }
        public Player getNextSpecPlayer(int inc)
        {
            var otherPlayers = spectatablePlayers();
            if (otherPlayers.Count == 0) return null;

            int index = otherPlayers.IndexOf(specPlayer);
            if (index == -1) index = 0;

            index += inc;
            if (index < 0) index = otherPlayers.Count - 1;
            if (index >= otherPlayers.Count) index = 0;

            return otherPlayers[index];
        }

        public List<Player> spectatablePlayers()
        {
            return players.Where(p => p != mainPlayer && !p.isSpectator).OrderBy(p => p.name).ToList();
        }

        public List<Player> nonSpecPlayers()
        {
            return players.Where(p => !p.isSpectator).ToList();
        }

        public bool isHost
        {
            get
            {
                if (Global.serverClient == null) return true;
                return Global.serverClient.isHost;
            }
        }

        public float time;

        public List<int> initSelWepIndices;
        public List<int> initAxlSelWepIndices;

        public PlayerCharData playerData;
        public List<PlayerCharData> cpuDatas;
        
        // Radar dimensions
        public float scaleW;
        public float scaleH;
        public float scaledW;
        public float scaledH;

        public Level(LevelData levelData, PlayerCharData playerData, ExtraCpuCharData extraCpuCharData, bool joinedLate)
        {
            this.levelData = levelData;
            zoomScale = 3;
            gravity = Physics.gravity;
            frameCount = 0;
            this.joinedLate = joinedLate;

            this.playerData = playerData;
            cpuDatas = extraCpuCharData?.cpuDatas;

            if (!string.IsNullOrEmpty(levelData.backgroundShader))
            {
                backgroundShader = Helpers.cloneShaderSafe(levelData.backgroundShader);
            }
            if (!string.IsNullOrEmpty(levelData.backgroundShaderImage))
            {
                backgroundShaderImage = Global.textures.GetValueOrDefault(levelData.backgroundShaderImage);
            }
            if (!string.IsNullOrEmpty(levelData.parallaxShader))
            {
                parallaxShader = Helpers.cloneShaderSafe(levelData.parallaxShader);
            }
            if (!string.IsNullOrEmpty(levelData.parallaxShaderImage))
            {
                parallaxShaderImage = Global.textures.GetValueOrDefault(levelData.parallaxShaderImage);
            }
            
            updateLevelShaders();
        }

        public Server server;
        public int equalCharDistributer;
        public int equalCharDistributerRed;
        public int equalCharDistributerBlue;

        public void startLevel(Server server, bool joinedLate)
        {
            started = true;

            if (Global.isOffline)
            {
                SavedMatchSettings.mainOffline.saveToFile();
            }
            else
            {
                SavedMatchSettings.mainOnline.saveToFile();
            }

            InGameMainMenu.selectY = 0;
            UpgradeMenu.onUpgradeMenu = true;
            UpgradeArmorMenu.xGame = 1;

            Menu.exit();
            this.server = server;
            if (Global.quickStart && Global.quickStartFixedCam) server.fixedCamera = true;

            if (server.gameMode == GameMode.Deathmatch)
            {
                gameMode = new FFADeathMatch(this, server.playTo, server.timeLimit);
            }
            else if (server.gameMode == GameMode.TeamDeathmatch)
            {
                gameMode = new TeamDeathMatch(this, server.playTo, server.timeLimit);
            }
            else if (server.gameMode == GameMode.CTF)
            {
                gameMode = new CTF(this, server.playTo, server.timeLimit);
            }
            else if (server.gameMode == GameMode.ControlPoint)
            {
                gameMode = new ControlPoints(this, server.timeLimit);
            }
            else if (server.gameMode == GameMode.Elimination)
            {
                gameMode = new Elimination(this, server.playTo, server.timeLimit);
            }
            else if (server.gameMode == GameMode.TeamElimination)
            {
                gameMode = new TeamElimination(this, server.playTo, server.timeLimit);
            }
            else if (server.gameMode == GameMode.KingOfTheHill)
            {
                gameMode = new KingOfTheHill(this, server.timeLimit);
            }
            else if (server.gameMode == GameMode.Race)
            {
                gameMode = new Race(this);
            }

            // Radar dimensions
            float maxDim = 50f;
            bool reallyWide = levelData.width > levelData.height * 3;
            // Really wide levels get more, like Gallery/Weather
            if (reallyWide) maxDim = 80;

            scaleW = maxDim / Math.Max(levelData.width, levelData.height);
            scaleH = scaleW;

            if (gameMode is Race)
            {
                if (levelData.height * scaleH < 2)
                {
                     scaleH = 2f / levelData.height;
                }
            }

            scaledW = levelData.width * scaleW;
            scaledH = levelData.height * scaleH;

            Global.radarRenderTexture = new RenderTexture((uint)(scaledW * 4), (uint)(scaledH * 4));
            Global.input.lastUpdateTime = 0;

            foreach (var serverPlayer in server.players)
            {
                addPlayer(serverPlayer, joinedLate);
            }

            if (levelData.isTraining())
            {
                AI.trainingBehavior = AITrainingBehavior.Idle;
            }
            else
            {
                AI.trainingBehavior = AITrainingBehavior.Default;
            }

            if (Global.anyQuickStart)
            {
                Global.level.mainPlayer.readyTime = 10;
            }

            if (levelData.isMirrored)
            {
                mirrorX = levelData.mirrorX;
            }

            width = levelData.width;
            height = levelData.height;

            if (server.fixedCamera)
            {
                Global.viewSize = 2;
                Global.view.Size = new Vector2f(Global.viewScreenW, Global.viewScreenH);
                Global.screenRenderTexture = Global.screenRenderTextureL;
                Global.srtBuffer1 = Global.srtBuffer1L;
                Global.srtBuffer2 = Global.srtBuffer2L;
            }
            else
            {
                Global.screenRenderTexture = Global.screenRenderTextureS;
                Global.srtBuffer1 = Global.srtBuffer1S;
                Global.srtBuffer2 = Global.srtBuffer2S;
            }

            levelData.loadLevelImages();

            backgroundSprites = levelData.getBackgroundTextures();
            backwallSprites = levelData.getBackwallTextures();
            foregroundSprites = levelData.getForegroundTextures();

            parallaxes = server.fixedCamera ? levelData.getLargeCamParallaxes() : levelData.getParallaxes();
            for (int i = 0; i < parallaxes.Count; i++)
            {
                parallaxTextures.Add(levelData.getParallaxTextures(parallaxes[i].path));
                parallaxOffsets.Add(new Point());
                parallaxSprites.Add(new List<ParallaxSprite>());
            }

            setupGrid(gridSize);
            foreach (var instance in levelData.levelJson.instances)
            {
                // 0 = both, 1 = non mirrored only, 2 = mirrored only
                if (instance.mirrorEnabled == 1 && levelData.isMirrored)
                {
                    continue;
                }
                if (instance.mirrorEnabled == 2 && !levelData.isMirrored)
                {
                    continue;
                }

                string instanceName = instance.name;
                string objectName = instance.objectName;
                var points = new List<Point>();
                bool? nullableFlipX = instance.properties?.flipX;
                bool flipX = nullableFlipX ?? false;
                int xDir = flipX ? -1 : 1;
                bool flipY = instance.properties?.flipY ?? false;
                int yDir = flipY ? -1 : 1;
                if (instance.points != null)
                {
                    foreach (var point in instance.points)
                    {
                        points.Add(new Point((float)point.x, (float)point.y));
                    }
                }
                Point pos = new Point((float)(instance.pos?.x ?? 0), (float)(instance.pos?.y ?? 0));

                if (objectName == "Collision Shape")
                {    
                    var wall = new Wall(instanceName, points);

                    float moveX = instance?.properties?.moveX ?? 0;
                    wall.moveX = moveX;

                    if (instance?.properties?.slippery != null && instance.properties.slippery == true)
                    {
                        wall.slippery = true;
                    }

                    if (instance?.properties?.topWall != null && instance.properties.topWall == true)
                    {
                        wall.topWall = true;
                    }

                    bool isPitWall = false;
                    if (instance?.properties?.pitWall != null && instance.properties.pitWall == true)
                    {
                        isPitWall = true;
                        wall.collider._shape.points[2] = new Point(wall.collider._shape.points[2].x, Global.level.height + 45);
                        wall.collider._shape.points[3] = new Point(wall.collider._shape.points[3].x, Global.level.height + 45);
                        var rect = wall.collider.shape.getRect();
                        var newRect = new Rect(rect.x1, rect.y2, rect.x2, rect.y2 + 1000);
                        var pitWall = new Wall(wall.name + "Pit", newRect.getPoints());
                        pitWall.collider.isClimbable = false;
                        addGameObject(pitWall);
                    }

                    if (instance?.properties?.unclimbable != null && instance.properties.unclimbable == true)
                    {
                        wall.collider.isClimbable = false;
                    }
                    else
                    {
                        wall.collider.isClimbable = true;
                    }

                    if (instance?.properties?.boundary == true && !isPitWall)
                    {
                        wall.collider.isClimbable = false;
                        var rect = wall.collider.shape.getRect();
                        var newRect = new Rect(rect.x1, rect.y1 - 1000, rect.x2, rect.y1);
                        var unclimbableWall = new Wall(wall.name + "Unclimbable", newRect.getPoints());
                        unclimbableWall.collider.isClimbable = false;
                        addGameObject(unclimbableWall);
                    }

                    addGameObject(wall);
                }
                else if (objectName == "Water Zone")
                {
                    if (!Global.level.isTraining() || Global.underwaterTraining)
                    {
                        var waterRect = new Rect(points[0], points[2]);
                        waterRects.Add(waterRect);
                    }
                }
                else if (objectName == "Ladder")
                {
                    if (!Global.level.isTraining() || Global.debug)
                    {
                        addGameObject(new Ladder(instanceName, points));
                    }
                }
                else if (objectName == "Backwall Zone")
                {
                    if (!Global.disableBackwalls)
                    {
                        addGameObject(new BackwallZone(instanceName, points, (bool?)instance.properties.isExclusion ?? false));
                    }
                }
                else if (objectName == "Gate")
                {
                    if (isRace())
                    {
                        var gate = new Gate(instanceName, points);

                        if (instance?.properties?.unclimbable != null && instance.properties.unclimbable == true)
                        {
                            gate.collider.isClimbable = false;
                        }
                        else
                        {
                            gate.collider.isClimbable = true;
                        }

                        addGameObject(gate);
                        gates.Add(gate);
                    }
                }
                else if (objectName == "No Scroll")
                {
                    if (!server.fixedCamera)
                    {
                        var shape = new Shape(points);
                        ScrollFreeDir dir = ScrollFreeDir.None;
                        bool snap = false;
                        if (instance.properties != null)
                        {
                            if (instance.properties.snap != null) snap = true;
                            if ((string)instance.properties.freeDir == "up") dir = ScrollFreeDir.Up;
                            if ((string)instance.properties.freeDir == "down") dir = ScrollFreeDir.Down;
                            if ((string)instance.properties.freeDir == "left") dir = ScrollFreeDir.Left;
                            if ((string)instance.properties.freeDir == "right") dir = ScrollFreeDir.Right;
                        }
                        noScrolls.Add(new NoScroll(instanceName, shape, dir, snap));
                    }
                }
                else if (objectName == "Kill Zone")
                {
                    bool killInvuln = instance.properties.killInvuln ?? false;
                    float? damage = instance.properties.damage;
                    bool flinch = instance.properties.flinch ?? false;
                    float hitCooldown = instance.properties.hitCooldown ?? 1;

                    var killZone = new KillZone(instanceName, points, killInvuln, damage, flinch, hitCooldown);
                    addGameObject(killZone);
                }
                else if (objectName == "Move Zone")
                {
                    if (levelData.name != "giantdam" || enableGiantDamPropellers())
                    {
                        var moveZone = new MoveZone(instanceName, points, (float)instance.properties.moveX, (float)instance.properties.moveY);
                        addGameObject(moveZone);
                    }
                }
                else if (objectName == "Jump Zone")
                {
                    float jumpTime = instance.properties.jumpTime ?? 1;
                    var jumpZone = new JumpZone(instanceName, points, (string)instance.properties.targetNode, Helpers.convertDynamicToDir(instance.properties.forceDir), jumpTime);
                    addGameObject(jumpZone);
                }
                else if (objectName == "Turn Zone")
                {
                    bool jumpAfterTurn = instance.properties.jumpAfterTurn ?? false;
                    string turnDirStr = instance.properties.turnDir ?? "";
                    int turnDir = turnDirStr == "right" ? 1 : -1;

                    var turnZone = new TurnZone(instanceName, points, turnDir, jumpAfterTurn);
                    addGameObject(turnZone);
                }
                else if (objectName == "Brake Zone")
                {
                    var brakeZone = new BrakeZone(instanceName, points);
                    addGameObject(brakeZone);
                }
                else if (objectName == "Spawn Point")
                {
                    var properties = instance.properties;
                    var sp = new SpawnPoint(instanceName, pos, xDir, -1);
                    if (properties?.raceStartSpawn == true)
                    {
                        if (isRace())
                        {
                            spawnPoints.Add(sp);
                            raceStartSpawnPoints.Add(sp);
                        }
                    }
                    else
                    {
                        spawnPoints.Add(sp);
                    }
                }
                else if (objectName == "Red Spawn")
                {
                    var properties = instance.properties;
                    float xOff = 0;
                    if (levelData.name == "centralcomputer" && gameMode is ControlPoints)
                    {
                        xOff = -500;
                    }

                    int redSpawnXDir = -1;
                    if (nullableFlipX != null)
                    {
                        redSpawnXDir = nullableFlipX.Value == true ? -1 : 1;
                    }

                    spawnPoints.Add(new SpawnPoint(instanceName, pos.addxy(xOff, 0), redSpawnXDir, GameMode.redAlliance));
                }
                else if (objectName == "Blue Spawn")
                {
                    var properties = instance.properties;
                    spawnPoints.Add(new SpawnPoint(instanceName, pos, xDir, GameMode.blueAlliance));
                }
                else if (objectName == "Red Flag")
                {
                    if (gameMode is CTF) 
                    {
                        redFlag = new Flag(GameMode.redAlliance, pos, redFlagNetId, isHost);
                    }
                }
                else if (objectName == "Blue Flag")
                {
                    if (gameMode is CTF) 
                    {
                        blueFlag = new Flag(GameMode.blueAlliance, pos, blueFlagNetId, isHost);
                    }
                }
                else if (objectName == "Control Point")
                {
                    if (gameMode is ControlPoints)
                    {
                        int num = (int)instance.properties.num;
                        ushort netId;
                        float offsetY = 0;
                        if (num == 1)
                        {
                            netId = cp1NetId;
                        }
                        else netId = cp2NetId;
                        int captureTime = instance.properties.captureTime != null ? (int)instance.properties.captureTime : 30;
                        int awardTime = instance.properties.awardTime != null ? (int)instance.properties.awardTime : 0;
                        controlPoints.Add(new ControlPoint(GameMode.redAlliance, pos, num, false, captureTime, awardTime, netId, isHost) { yOff = offsetY });
                    }
                    else if (gameMode is KingOfTheHill)
                    {
                        bool isHill = (bool?)instance.properties.hill ?? false;
                        ushort netId;
                        if (isHill)
                        {
                            netId = cp1NetId;
                            int captureTime = 30;
                            float offsetY = 0;
                            hill = new ControlPoint(GameMode.neutralAlliance, pos, 1, true, captureTime, 0, netId, isHost) { yOff = offsetY };
                        }
                    }
                }
                else if (objectName == "Node")
                {
                    var node = new NavMeshNode(instanceName, pos, instance.properties);
                    navMeshNodes.Add(node);
                }
                else if (objectName == "Large Health")
                {
                    if (!pickupRestricted(instance))
                    {
                        itemSpawners.Add(new ItemSpawner(pos, typeof(LargeHealthPickup), 0, 15, xDir));
                    }
                }
                else if (objectName == "Small Health") 
                {
                    if (!pickupRestricted(instance))
                    {
                        itemSpawners.Add(new ItemSpawner(pos, typeof(SmallHealthPickup), 0, 15, xDir));
                    }
                }
                else if (objectName == "Large Ammo")
                {
                    if (!pickupRestricted(instance))
                    {
                        itemSpawners.Add(new ItemSpawner(pos, typeof(LargeAmmoPickup), 0, 15, xDir));
                    }
                }
                else if (objectName == "Small Ammo")
                {
                    if (!pickupRestricted(instance))
                    {
                        itemSpawners.Add(new ItemSpawner(pos, typeof(SmallAmmoPickup), 0, 15, xDir));
                    }
                }
                else if (objectName == "Ride Armor")
                {
                    if (!pickupRestricted(instance) && !server.disableVehicles)
                    {
                        int rideArmorType = 0;
                        if (instance.properties?.raType == "k") rideArmorType = 1;
                        if (instance.properties?.raType == "h") rideArmorType = 2;
                        if (instance.properties?.raType == "f") rideArmorType = 3;
                        itemSpawners.Add(new ItemSpawner(pos, typeof(RideArmor), rideArmorType, 60, xDir));
                    }
                }
                else if (objectName == "Ride Chaser")
                {
                    supportsRideChasers = true;
                    bool isCheckpoint = instance.properties?.isCheckpoint ?? false;
                    if (!pickupRestricted(instance) && !server.disableVehicles)
                    {
                        if (!isCheckpoint)
                        {
                            var rcSpawner = new ItemSpawner(pos, typeof(RideChaser), 0, 60, xDir);
                            itemSpawners.Add(rcSpawner);
                        }
                        else
                        {
                            var rcSpawners = new List<ItemSpawner>();
                            for (int i = 0; i < 12; i++)
                            {
                                var rcSpawner = new ItemSpawner(pos, typeof(RideChaser), 0, 60, xDir);
                                rcSpawner.isStacked = true;
                                rcSpawner.stackAssignedPlayerId = i;
                                itemSpawners.Add(rcSpawner);
                                rcSpawners.Add(rcSpawner);
                            }
                        }
                    }
                }
                else if (objectName.StartsWith("Map Sprite"))
                {
                    bool enabledInLargeCam = instance.properties.enabledInLargeCam ?? true;
                    int destructableFlag = instance.properties?.destructableFlag ?? 0;
                    int repeatX = instance.properties.repeatX ?? 1;
                    int repeatY = instance.properties.repeatY ?? 1;
                    string spriteName = instance.properties.spriteName;
                    string gibSpriteName = instance.properties.gibSpriteName;
                    int repeatXPadding = instance.properties.repeatXPadding ?? 0;
                    int repeatYPadding = instance.properties.repeatYPadding ?? 0;
                    long zIndex = getZIndexFromProperty(instance.properties.zIndex, ZIndex.Background + 5);
                    int? rawParallaxIndex = instance.properties.parallaxIndex;
                    if (rawParallaxIndex != null)
                    {
                        zIndex = ZIndex.Parallax - 5;
                    }
                    int health = instance.properties.destructableHealth ?? 12;
                    string destroyInstanceName = instance.properties.destroyInstanceName ?? "";

                    var spriteWidth = Global.sprites[spriteName].frames[0].rect.w();
                    var spriteHeight = Global.sprites[spriteName].frames[0].rect.h();

                    if (!enabledInLargeCam && server.fixedCamera)
                    {
                        // Do not add the map sprite
                    }
                    else if (rawParallaxIndex != null)
                    {
                        if (Options.main.enableMapSprites)
                        {
                            int parallaxIndex = rawParallaxIndex.Value - 1;
                            if (parallaxSprites.InRange(parallaxIndex))
                            {
                                for (int i = 0; i < repeatY; i++)
                                {
                                    for (int j = 0; j < repeatX; j++)
                                    {
                                        Point mapSpritePos = new Point
                                        (
                                            pos.x + (j * (xDir) * (repeatXPadding + spriteWidth)),
                                            pos.y + (i * (yDir) * (repeatYPadding + spriteHeight))
                                        );
                                        var parallaxSprite = new ParallaxSprite(spriteName, mapSpritePos, xDir, yDir);
                                        parallaxSprites[parallaxIndex].Add(parallaxSprite);
                                    }
                                }
                            }
                        }
                    }
                    else if (destructableFlag > 0)
                    {
                        for (int i = 0; i < repeatY; i++)
                        {
                            for (int j = 0; j < repeatX; j++)
                            {
                                Point mapSpritePos = new Point
                                (
                                    pos.x + (j * xDir * (repeatXPadding + spriteWidth)),
                                    pos.y + (i * yDir * (repeatYPadding + spriteHeight))
                                );
                                var crackedWall = new CrackedWall(mapSpritePos, spriteName, gibSpriteName, xDir, yDir, destructableFlag, health, destroyInstanceName, true);
                                crackedWall.setzIndex(zIndex);
                            }
                        }
                    }
                    else if (Options.main.enableMapSprites)
                    {
                        for (int i = 0; i < repeatY; i++)
                        {
                            for (int j = 0; j < repeatX; j++)
                            {
                                Point mapSpritePos = new Point
                                (
                                    pos.x + (j * xDir * (repeatXPadding + spriteWidth)),
                                    pos.y + (i * yDir * (repeatYPadding + spriteHeight))
                                );
                                var anim = new Actor(spriteName, mapSpritePos, null, true, true);
                                anim.xDir = xDir;
                                anim.yDir = yDir;
                                anim.setzIndex(zIndex);
                                if (spriteName == "ms_dam_propeller" && !enableGiantDamPropellers())
                                {
                                    anim.frameSpeed = 0;
                                }
                                mapSprites.Add(anim);
                            }
                        }
                    }
                }
                else if (objectName.StartsWith("Jape Memorial"))
                {
                    if (DateTime.UtcNow.Month == 6 && Math.Abs(DateTime.UtcNow.Day - 27) < 4)
                    {
                        var japeMemorial = new Anim(pos, "jape_memorial", 1, null, false);
                        japeMemorial.zIndex = ZIndex.Background + 100;
                    }
                }
                else if (objectName.StartsWith("Goal"))
                {
                    if (isRace())
                    {
                        if (goal != null)
                        {
                            throw new Exception("Multiple goals not allowed in race mode.");
                        }
                        var actor = new VictoryPoint(pos);
                        actor.name = instanceName;
                        goal = actor;
                        addGameObject(actor);
                    }
                }
                else if (objectName.StartsWith("Moving Platform"))
                {
                    string spriteName = instance.properties.spriteName ?? "";
                    string idleSpriteName = instance.properties.idleSpriteName ?? "";
                    string moveData = instance.properties.moveData ?? "";
                    float timeOffset = instance.properties.timeOffset ?? 0;
                    float moveSpeed = instance.properties.moveSpeed ?? 50;

                    string nodeName = instance.properties.nodeName ?? "";
                    string killZoneName = instance.properties.killZoneName ?? "";
                    string crackedWallName = instance.properties.crackedWallName ?? "";

                    bool flipXOnMoveLeft = instance.properties.flipXOnMoveLeft ?? false;
                    bool flipYOnMoveUp = instance.properties.flipYOnMoveUp ?? false;

                    long zIndex = getZIndexFromProperty(instance.properties.zIndex, ZIndex.Default);

                    var platform = new MovingPlatform(spriteName, idleSpriteName, pos, moveData, moveSpeed, timeOffset, nodeName, killZoneName, crackedWallName, zIndex, flipXOnMoveLeft, flipYOnMoveUp);
                    movingPlatforms.Add(platform);
                    addGameObject(platform);
                }
                else if (objectName.StartsWith("Music Source"))
                {
                    string musicName = instance.properties.musicName ?? "";
                    if (!Global.musics.ContainsKey(musicName))
                    {
                        //throw new Exception("Music Source with music name " + musicName + " not found.\nIf music is in custom map folder, format as CUSTOM_MAP_NAME:MUSIC_NAME");
                    }
                    else
                    {
                        var actor = new Actor("empty", pos, null, true, false);
                        actor.useGravity = false;
                        actor.name = instanceName;
                        actor.addMusicSource(musicName, pos, true);
                        addGameObject(actor);
                    }
                }
                else 
                {
                    var actor = new Actor(instance.spriteName, pos, Global.level.mainPlayer.getNextActorNetId(), isHost, false);
                    actor.name = instanceName;
                    addGameObject(actor);
                }
            }

            if (isTraining() && isRace())
            {
                var actor = new VictoryPoint(new Point(151, 177));
                actor.name = "Victory Point1";
                goal = actor;
                addGameObject(actor);
            }

            if (Global.isHost && isNon1v1Elimination())
            {
                var neutralSpawns = spawnPoints.Where((spawnPoint) =>
                {
                    return spawnPoint.alliance == -1;
                }).ToList();

                var randomSpawn = neutralSpawns.GetRandomItem();
                int indexOf = spawnPoints.IndexOf(randomSpawn);
                if (indexOf == -1) indexOf = 0;
                gameMode.safeZoneSpawnIndex = (byte)indexOf;
            }

            controlPoints.Sort((cp1, cp2) => cp1.num - cp2.num);
            foreach (var navMeshNode in navMeshNodes) 
            {
                navMeshNode.setNeighbors(navMeshNodes, getGameObjectArray());
            }

            // Dynamically assign nodes based on their proximity
            if (controlPoints.Count > 0)
            {
                controlPoints[0].navMeshNode = navMeshNodes.OrderBy(node => node.pos.distanceTo(controlPoints[0].pos)).First();
            }
            if (controlPoints.Count > 1)
            {
                controlPoints[1].navMeshNode = navMeshNodes.OrderBy(node => node.pos.distanceTo(controlPoints[1].pos)).First();
            }
            if (hill != null)
            {
                hill.navMeshNode = navMeshNodes.OrderBy(node => node.pos.distanceTo(hill.pos)).First();
            }
            if (redFlag != null)
            {
                redFlagNode = navMeshNodes.OrderBy(node => node.pos.distanceTo(redFlag.pos)).First();
            }
            if (blueFlag != null)
            {
                blueFlagNode = navMeshNodes.OrderBy(node => node.pos.distanceTo(blueFlag.pos)).First();
            }
            if (goal != null && navMeshNodes.Count > 0)
            {
                goalNode = navMeshNodes.OrderBy(node => node.pos.distanceTo(goal.pos)).First();
            }

            if (navMeshNodes.Count == 0)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    navMeshNodes.Add(new NavMeshNode("Node" + spawnPoint.name, spawnPoint.pos, null));
                }
            }

            twoFrameCycle = 0;
            
            if (Global.serverClient == null)
            {
                for (var i = 0; i < server.botCount; i++)
                {
                    int charNum;

                    int alliance;
                    int id = mainPlayer.id + i + 1;
                    if (!gameMode.isTeamMode)
                    {
                        alliance = id;
                    }
                    else
                    {
                        alliance = (i + mainPlayer.serverPlayer.alliance + 1) % 2;
                    }

                    if (equalCharDistribution)
                    {
                        if (!Global.level.gameMode.isTeamMode)
                        {
                            charNum = equalCharDistributer % 5;
                            equalCharDistributer++;
                        }
                        else
                        {
                            if (alliance == GameMode.redAlliance)
                            {
                                charNum = equalCharDistributerRed % 5;
                                equalCharDistributerRed++;
                            }
                            else
                            {
                                charNum = equalCharDistributerBlue % 5;
                                equalCharDistributerBlue++;
                            }
                        }
                    }
                    else
                    {
                        charNum = Helpers.randomRange(0, 4);
                    }

                    PlayerCharData playerData = null;
                    
                    // Overrides from 1v1 select character menu
                    if (i < cpuDatas.Count)
                    {
                        if (!cpuDatas[i].isRandom) charNum = cpuDatas[i].charNum;
                        if (gameMode.isTeamMode && cpuDatas[i].alliance >= 0) alliance = cpuDatas[i].alliance;
                    }
                    playerData = cpuDatas.InRange(i) ? cpuDatas[i] : null;
                    
                    if (Global.anyQuickStart)
                    {
                        charNum = Global.quickStartBotCharNum;
                        Global.quickStartBotCharNum++;
                        Global.quickStartBotCharNum = Global.quickStartBotCharNum % 5;
                    }

                    // CHANGECHARTOZERO, SWITCHCHARTOZERO
                    if (Global.quickStartSameChar != null)
                    {
                        charNum = Global.quickStartSameChar.Value;
                    }

                    var cpu = new Player("CPU" + (i + 1).ToString(), id, charNum, playerData, true, true, alliance, new Input(true), null);
                    players.Add(cpu);
                }
            }

            string musicKey = levelData.getMusicKey(players);
            Global.changeMusic(musicKey);

            if (isHost)
            {
                Global.serverClient?.rpc(RPC.updateStarted);
            }

            if (joinedLate)
            {
                Global.serverClient?.rpc(RPC.joinLateRequest, Helpers.serialize(Global.serverClient.serverPlayer));
            }

            startGoCount = gameObjects.Count;
            startGridCount = getGridCount();

            //var p = Global.level.mainPlayer;
            //new Mechaniloid(new Point(128, 128), p, 1, new MechaniloidWeapon(p, MechaniloidType.Hopper), MechaniloidType.Hopper, p.getNextActorNetId(), true);
        }

        private long getZIndexFromProperty(dynamic property, long defaultZIndex)
        {
            string zIndexString = property;
            long zIndex;
            if (zIndexString == "aboveForeground") zIndex = ZIndex.Foreground + 100;
            else if (zIndexString == "aboveInstances") zIndex = ZIndex.Default - 5;
            else if (zIndexString == "aboveBackground") zIndex = ZIndex.Background + 5;
            else if (zIndexString == "aboveBackwall") zIndex = ZIndex.Backwall + 5;
            else if (zIndexString == "aboveParallax") zIndex = ZIndex.Parallax + 5;
            else zIndex = defaultZIndex;
            return zIndex;
        }

        public void addEffect(Effect effect)
        {
            effects.Add(effect);
        }

        public float getTopScreenY(float charY)
        {
            float topScreenY = Global.level.camY;
            if (Global.level.server.fixedCamera)
            {
                float charYDistToTop = Helpers.clampMin0(charY - (Global.level.camY + Global.screenH / 2));
                float incAmount = Math.Min(charYDistToTop, Global.screenH / 2);
                topScreenY += incAmount;
            }
            return topScreenY;
        }

        public CrackedWall getCrackedWallById(byte crackedWallId)
        {
            foreach (var go in gameObjects)
            {
                if (go is CrackedWall cw && cw.id == crackedWallId)
                {
                    return cw;
                }
            }
            return null;
        }

        private bool enableGiantDamPropellers()
        {
            return levelData.name == "giantdam" && !levelData.isMirrored && gameMode is CTF;
        }

        public void addFailedSpawn(int playerId, Point point, int xDir, ushort netId)
        {
            if (!failedSpawns.ContainsKey(playerId))
            {
                failedSpawns[playerId] = new FailedSpawn(point, xDir, netId);
            }
            else
            {
                failedSpawns[playerId].time += Global.spf;
            }
        }

        public bool pickupRestricted(dynamic instance)
        {
            if (isNon1v1Elimination()) return true;
            if (instance.properties.nonDmOnly == true && Global.level.server.gameMode.Contains(GameMode.Deathmatch)) return true;
            if (instance.properties.nonCpOnly == true && Global.level.server.gameMode.Contains(GameMode.ControlPoint)) return true;
            if (instance.properties.nonCtfOnly == true && Global.level.server.gameMode.Contains(GameMode.CTF)) return true;
            if (instance.properties.nonKothOnly == true && Global.level.server.gameMode.Contains(GameMode.KingOfTheHill)) return true;
            if (instance.properties.dmOnly == true && !Global.level.server.gameMode.Contains(GameMode.Deathmatch)) return true;

            return false;
        }

        public void joinedLateSyncPlayers(List<PlayerPB> hostPlayers)
        {
            if (hostPlayers == null) return;

            foreach (var hostPlayer in hostPlayers)
            {
                if (hostPlayer.serverPlayer.id == mainPlayer.id) continue;
                var player = players.Find(p => p.id == hostPlayer.serverPlayer.id);
                if (player == null) continue;

                player.alliance = hostPlayer.serverPlayer.alliance;
                player.newAlliance = hostPlayer.newAlliance;
                player.kills = hostPlayer.serverPlayer.kills;
                player.deaths = hostPlayer.serverPlayer.deaths;
                player.charNum = hostPlayer.serverPlayer.charNum;
                player.newCharNum = hostPlayer.newCharNum;
                player.curMaxNetId = hostPlayer.curMaxNetId;
                player.warpedIn = hostPlayer.warpedIn;
                player.readyTime = hostPlayer.readyTime;
                player.readyTextOver = hostPlayer.spawnChar;
                player.armorFlag = hostPlayer.armorFlag;
                player.loadout = hostPlayer.loadoutData;
                player.disguise = hostPlayer.disguise;

                if (hostPlayer.charNetId != null && player.character == null)
                {
                    player.spawnCharAtPoint(new Point(hostPlayer.charXPos, hostPlayer.charYPos), hostPlayer.charXDir, (ushort)hostPlayer.charNetId, false);
                    player.changeWeaponFromWi(hostPlayer.weaponIndex);
                    if (hostPlayer.charRollingShieldNetId != null)
                    {
                        new RollingShieldProjCharged(player.weapon, player.character.pos, player.character.xDir, player, hostPlayer.charRollingShieldNetId.Value);
                    }
                }
            }
        }

        public void joinedLateSyncControlPoints(List<ControlPointResponseModel> controlPointResponseModels)
        {
            if (controlPointResponseModels == null) return;

            foreach (var controlPointResponseModel in controlPointResponseModels)
            {
                var controlPoint = controlPoints.Where(c => c.num == controlPointResponseModel.num).FirstOrDefault();
                controlPoint.alliance = controlPointResponseModel.alliance;
                controlPoint.num = controlPointResponseModel.num;
                controlPoint.locked = controlPointResponseModel.locked;
                controlPoint.captured = controlPointResponseModel.captured;
                controlPoint.captureTime = controlPointResponseModel.captureTime;
            }
        }

        public void joinedLateSyncMagnetMines(List<MagnetMineResponseModel> magnetMines)
        {
            if (magnetMines == null) return;
            
            foreach (var magnetMine in magnetMines)
            {
                var player = getPlayerById(magnetMine.playerId);
                if (player == null) continue;
                new MagnetMineProj(new MagnetMine(), new Point(magnetMine.x, magnetMine.y), 1, 1, player, magnetMine.netId);
            }
        }

        public void joinedLateSyncTurrets(List<TurretResponseModel> turrets)
        {
            if (turrets == null) return;

            foreach (var turret in turrets)
            {
                var player = getPlayerById(turret.playerId);
                if (player == null) continue;
                new RaySplasherTurret(new Point(turret.x, turret.y), player, 1, turret.netId, false, false);
            }
        }

        public Actor getActorByNetId(ushort netId)
        {
            foreach (var go in gameObjects)
            {
                var actor = go as Actor;
                if (actor?.netId == netId)
                {
                    return actor;
                }
            }
            return null;
        }

        public string getNetIdDump()
        {
            string dump = "";
            foreach (var go in gameObjects)
            {
                var actor = go as Actor;
                if (actor == null) continue;
                if (actor.netId == null) continue;
                dump += actor.name + ":" + actor.sprite?.name + ":" + actor.netId + "\n";
            }
            return dump;
        }

        public Player getPlayerById(int id)
        {
            return players.Find(p => p.id == id);
        }

        public Player getPlayerByName(string name)
        {
            return players.Find(p => p.name == name);
        }

        public void addPlayer(ServerPlayer serverPlayer, bool joinedLate)
        {
            bool isPlayerIdMainPlayer;

            if (Global.serverClient != null) isPlayerIdMainPlayer = (serverPlayer.id == Global.serverClient.serverPlayer.id);
            else isPlayerIdMainPlayer = (serverPlayer.id == 0);

            // Player already exists, do not re-add
            if (players.Any(p => p.id == serverPlayer.id))
            {
                return;
            }

            Input input = isPlayerIdMainPlayer ? Global.input : new Input(false);

            bool ownedByLocalPlayer = serverPlayer.isBot ? Global.isHost : isPlayerIdMainPlayer;
            bool isBot = Global.isHost ? serverPlayer.isBot : false;

            var player = new Player(serverPlayer.name, serverPlayer.id, serverPlayer.charNum, playerData, isBot, ownedByLocalPlayer, serverPlayer.alliance, input, serverPlayer);
            if (joinedLate)
            {
                player.warpedIn = true;
                player.readyTime = 10;
            }
            players.Add(player);

            if (isPlayerIdMainPlayer)
            {
                mainPlayer = player;
                equalCharDistribution = Helpers.randomRange(0, 1) == 0 ? true : false;
                if (equalCharDistribution)
                {
                    equalCharDistributer = mainPlayer.charNum + 1;
                    equalCharDistributerRed = equalCharDistributer;
                    equalCharDistributerBlue = equalCharDistributer;
                }
            }

            if (joinedLate && gameMode != null && player.ownedByLocalPlayer)
            {
                string joinMsg = serverPlayer.name + " joined";
                if (player.isBot) joinMsg = serverPlayer.name + " added to match.";
                gameMode.chatMenu.addChatEntry(new ChatEntry(joinMsg, null, null, true), sendRpc: true);
            }
        }

        public void removePlayer(Player player)
        {
            if (!Global.level.players.Contains(player)) return;

            string leaveMsg = player.name + " left match.";
            if (player.isBot) leaveMsg = player.name + " removed from match.";
            gameMode.chatMenu.addChatEntry(new ChatEntry(leaveMsg, null, null, true));
            player.destroy();
            players.Remove(player);
            var nsps = nonSpecPlayers();
            if (nsps.Count == 1 && is1v1())
            {
                nsps[0].kills = server.playTo;
            }
        }

        public bool isSendMessageFrame()
        {
            return Global.level.frameCount % Global.normalizeFrames(Global.tickRate) == 0;
        }

        public bool isAfkWarn()
        {
            if (Global.isOffline || server?.hidden == true || Global.debug) return false;
            if (gameMode.isOver && !Global.isHost) return false;
            if (Global.level.mainPlayer.isSpectator) return false;
            return time - Global.input.lastUpdateTime > afkWarnTime;
        }

        public string afkWarnTimeAmount()
        {
            float amount = afkKickTime - (time - Global.input.lastUpdateTime);
            return ((int)amount).ToString();
        }

        public bool isAfk()
        {
            if (Global.isOffline || server?.hidden == true || Global.debug) return false;
            if (gameMode.isOver && !Global.isHost) return false;
            if (Global.level.mainPlayer.isSpectator) return false;
            return time - Global.input.lastUpdateTime > afkKickTime;
        }

        public void checkAfk()
        {
            if (isAfk())
            {
                if (!Global.level.mainPlayer.isSpectator)
                {
                    Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.Kicked, null, "Automatic AFK Kick");
                }
            }
        }

        public void setMainPlayerSpectate()
        {
            Global.level.mainPlayer.setSpectate(!Global.level.mainPlayer.isSpectator);
            Global.level.mainPlayer.forceKill();
            Global.level.specPlayer = Global.level.getNextSpecPlayer(1);
        }

        public void updateLevelShaders()
        {
            if (backgroundShader != null)
            {
                backgroundShader.SetUniform("t", time);
                if (backgroundShaderImage != null)
                {
                    backgroundShader.SetUniform("image", backgroundShaderImage);
                    backgroundShader.SetUniform("imageW", (int)backgroundShaderImage.Size.X);
                    backgroundShader.SetUniform("imageH", (int)backgroundShaderImage.Size.Y);
                }
            }
            if (parallaxShader != null)
            {
                parallaxShader.SetUniform("t", time);
                if (parallaxShaderImage != null)
                {
                    parallaxShader.SetUniform("image", parallaxShaderImage);
                    parallaxShader.SetUniform("imageW", (int)parallaxShaderImage.Size.X);
                    parallaxShader.SetUniform("imageH", (int)parallaxShaderImage.Size.Y);
                }
            }
        }

        public void update()
        {
            time += Global.spf;

            updateLevelShaders();

            checkAfk();

            Global.input.updateAimToggle(mainPlayer);
            Global.input.updateFrameToPressedControls(mainPlayer);

            syncValue += Global.spf;
            if (!Global.isHost && hostSyncValue != null)
            {
                syncValue = Helpers.lerp(syncValue, hostSyncValue.Value, Global.spf * 5f);
            }

            foreach (var platform in movingPlatforms)
            {
                platform.update(syncValue);
            }

            for (int i = 0; i < parallaxOffsets.Count; i++)
            {
                float speedX = parallaxes[i].scrollSpeedX;
                float speedY = parallaxes[i].scrollSpeedY;
                if (speedX != 0 || speedY != 0)
                {
                    parallaxOffsets[i] = parallaxOffsets[i].add(new Point(Global.spf * speedX, Global.spf * speedY));

                    var parallaxTextures = levelData.getParallaxTextures(parallaxes[i].path);
                    if (parallaxTextures == null) continue;

                    Point size = Helpers.getTextureArraySize(parallaxTextures);

                    if (MathF.Abs(parallaxOffsets[i].x) > size.x) parallaxOffsets[i] = new Point(0, parallaxOffsets[i].y);
                    if (MathF.Abs(parallaxOffsets[i].y) > size.y) parallaxOffsets[i] = new Point(parallaxOffsets[i].x, 0);
                }
            }

            if (enableGiantDamPropellers())
            {
                if (Global.frameCount % 30 == 0) new Anim(new Point(1728, 576 + 20), "bubbles", 1, null, false) { vel = new Point(-100, 0), ttl = 4 };
                else if (Global.frameCount % 20 == 0) new Anim(new Point(1728, 600 + 20), "bubbles", 1, null, false) { vel = new Point(-100, 0), ttl = 4 };
                else if (Global.frameCount % 40 == 0) new Anim(new Point(1728, 625 + 20), "bubbles", 1, null, false) { vel = new Point(-100, 0), ttl = 4 };
                else if (Global.frameCount % 30 == 15) new Anim(new Point(1728, 650 + 10), "bubbles", 1, null, false) { vel = new Point(-100, 0), ttl = 4 };
                else if (Global.frameCount % 20 == 10) new Anim(new Point(1728, 675 + 10), "bubbles", 1, null, false) { vel = new Point(-100, 0), ttl = 4 };
                else if (Global.frameCount % 40 == 20) new Anim(new Point(1728, 690 + 10), "bubbles", 1, null, false) { vel = new Point(-100, 0), ttl = 4 };
            }

            foreach (var key in recentClipCount.Keys.ToList())
            {
                var val = recentClipCount[key];
                for (var i = val.Count - 1; i >= 0; i--)
                {
                    val[i] += Global.spf;
                    if (val[i] >= 0.05)
                    {
                        val.RemoveAt(i);
                    }
                }
                recentClipCount[key] = val;
            }

            //this.gameMode.checkIfWin();

            //Sort players by score
            players.Sort((a, b) => {
                if (a.kills > b.kills) return -1;
                else if (a.kills == b.kills)
                {
                    if (a.deaths < b.deaths) return -1;
                    else if (a.deaths == b.deaths) return 0;
                    else return 1;
                }
                else
                {
                    return 1;
                }
            });

            for (int i = delayedActions.Count - 1; i >= 0; i--)
            {
                delayedActions[i].time -= Global.spf;
                if (delayedActions[i].time <= 0)
                {
                    delayedActions[i].action.Invoke();
                    delayedActions.RemoveAt(i);
                }
            }

            float playerX = 0;
            float playerY = 0;
            if (camPlayer.character != null)
            {
                camPlayer.character.stopCamUpdate = false;
                playerX = camPlayer.character.getCamCenterPos().x;
                playerY = camPlayer.character.getCamCenterPos().y;
            }

            bool isNon1v1Elimination = Global.level.isNon1v1Elimination();
            var gos = gameObjects.ToList();
            try
            {
                foreach (var go in gos)
                {
                    if (isTimeSlowed(go, out float slowAmount)) Global.crystalSlowAmount = slowAmount;
                    go.preUpdate();
                    Global.crystalSlowAmount = 1;
                }
                foreach (var ms in mapSprites)
                {
                    ms.sprite?.update();
                }
                foreach (var go in gos)
                {
                    if (isTimeSlowed(go, out float slowAmount)) Global.crystalSlowAmount = slowAmount;
                    go.update();
                    if (isNon1v1Elimination && gameMode.virusStarted > 0 && go is Actor actor && actor.ownedByLocalPlayer && go is IDamagable damagable)
                    {
                        Rect szRect = gameMode.safeZoneRect;
                        if (actor.collider != null)
                        {
                            Rect colRect = actor.collider.shape.getRect();
                            float w4 = colRect.w() / 4;
                            float h4 = colRect.h() / 4;
                            colRect.x1 += w4;
                            colRect.y1 += h4;
                            colRect.x2 -= w4;
                            colRect.y2 -= h4;
                            if (!szRect.overlaps(colRect))
                            {
                                if (!damagable.projectileCooldown.ContainsKey("sigmavirus"))
                                {
                                    damagable.projectileCooldown["sigmavirus"] = 0;
                                }
                                if (damagable.projectileCooldown["sigmavirus"] == 0)
                                {
                                    actor.playSound("hit");
                                    actor.addRenderEffect(RenderEffectType.Hit, 0.05f, 0.1f);
                                    damagable.applyDamage(null, null, 2, null);
                                    damagable.projectileCooldown["sigmavirus"] = 1;
                                }
                            }
                        }
                    }
                    Global.crystalSlowAmount = 1;
                }
                foreach (var go in gos)
                {
                    if (isTimeSlowed(go, out float slowAmount)) Global.crystalSlowAmount = slowAmount;
                    go.postUpdate();
                    Global.crystalSlowAmount = 1;
                    go.netUpdate();
                }
            }
            finally
            {
                Global.crystalSlowAmount = 1;
            }

            if (camPlayer.character != null)
            {
                if (!camPlayer.character.stopCamUpdate)
                {
                    Point camPos = camPlayer.character.getCamCenterPos();
                    Point expectedCamPos = computeCamPos(camPos, new Point(playerX, playerY));

                    var moveDeltaX = camPos.x - playerX;
                    var moveDeltaY = camPos.y - playerY;

                    var fullDeltaX = expectedCamPos.x - camX;
                    var fullDeltaY = expectedCamPos.y - camY;

                    var deltaX = moveDeltaX;
                    var deltaY = moveDeltaY;

                    if (MathF.Abs(moveDeltaX) > 0)
                    {
                        deltaX = Helpers.lerp(deltaX, fullDeltaX, Global.spf * 3);
                    }
                    if (MathF.Abs(moveDeltaY) > 0)
                    {
                        deltaY = Helpers.lerp(deltaY, fullDeltaY, Global.spf * 3);
                    }

                    updateCamPos(deltaX, deltaY);
                }
                else
                {
                    shakeX = 0;
                    shakeY = 0;
                }

                if (camPlayer.character.isZooming())
                {
                    Player p = camPlayer.character.player;

                    p.axlScopeCursorWorldPos.x = Helpers.clamp(p.axlScopeCursorWorldPos.x, camCenterX - Global.halfViewScreenW, camCenterX + Global.halfViewScreenW);
                    p.axlScopeCursorWorldPos.y = Helpers.clamp(p.axlScopeCursorWorldPos.y, camCenterY - Global.halfViewScreenH, camCenterY + Global.halfViewScreenH);
                }
            }
            else
            {
                shakeX = 0;
                shakeY = 0;
            }

            foreach (var effect in effects.ToList())
            {
                effect.update();
            }

            foreach (var player in players.ToList())
            {
                if (player.isAI && AI.trainingBehavior != AITrainingBehavior.Idle)
                {
                    player.clearAiInput();
                }
                player.input.clearMashCount();
            }

            foreach (var player in players.ToList())
            {
                player.update();
            }

            foreach (var pickupSpawner in itemSpawners)
            {
                pickupSpawner.update(); 
            }

            frameCount++;

            twoFrameCycle++;
            if (twoFrameCycle > 2) twoFrameCycle = -2;

            gameMode.update();

            if (Global.serverClient != null)
            {
                if (isSendMessageFrame())
                {
                    Global.serverClient.flush();
                }

                Global.serverClient.getMessages(out var messages, true);

                foreach (var message in messages)
                {
                    if (message.StartsWith("hostdisconnect:"))
                    {
                        string reason = message.Split(':')[1];
                        if (reason != "Recreate")
                        {
                            Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.ServerShutdown, null, null);
                        }
                        else
                        {
                            Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.Rejoin, server, null);
                        }
                    }
                    else if (message.StartsWith("clientdisconnect:"))
                    {
                        var playerLeft = JsonConvert.DeserializeObject<ServerPlayer>(message.RemovePrefix("clientdisconnect:"));
                        if (playerLeft != null)
                        {
                            var player = Global.level.getPlayerById(playerLeft.id);
                            if (player != null)
                            {
                                removePlayer(player);
                            }
                        }
                    }
                }

                for (int i = bufferedDestroyActors.Count - 1; i >= 0; i--)
                {
                    var bufferedDestroyedActor = bufferedDestroyActors[i];
                    bufferedDestroyedActor.time += Global.spf;
                    var actor = getActorByNetId(bufferedDestroyedActor.netId);
                    if (actor != null)
                    {
                        actor.destroySelf(bufferedDestroyedActor.destroySprite, bufferedDestroyedActor.destroySound, rpc: true);
                        bufferedDestroyActors.RemoveAt(i);
                    }
                    else if (bufferedDestroyedActor.time > 5)
                    {
                        bufferedDestroyActors.RemoveAt(i);
                    }
                }

                for (int i = backloggedSpawns.Count - 1; i >= 0; i--)
                {
                    backloggedSpawns[i].time += Global.spf;
                    if (backloggedSpawns[i].time >= 5 || backloggedSpawns[i].trySpawnPlayer())
                    {
                        backloggedSpawns.RemoveAt(i);
                    }
                }

                var keysToRemove = new List<int>();
                foreach (var kvp in failedSpawns)
                {
                    var player = getPlayerById(kvp.Key);
                    if (player == null || player.character != null)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                    else if (kvp.Value.time >= 2.5f)
                    {
                        keysToRemove.Add(kvp.Key);
                        if (player.character == null)
                        {
                            player?.spawnCharAtPoint(kvp.Value.spawnPos, kvp.Value.xDir, kvp.Value.netId, false);
                        }
                    }
                }
                foreach (var key in keysToRemove)
                {
                    failedSpawns.Remove(key);
                }

                foreach (var key in recentlyDestroyedNetActors.Keys.ToList())
                {
                    recentlyDestroyedNetActors[key] += Global.spf;
                    if (recentlyDestroyedNetActors[key] > 5)
                    {
                        recentlyDestroyedNetActors.Remove(key);
                    }
                }
            }

            //this.getTotalCountInGrid();

            updateMusicSources();
        }

        private void updateMusicSources()
        {
            Point listenerPos = new Point(camCenterX, camCenterY);
            if ((is1v1() || isTraining()) && mainPlayer.character != null) listenerPos = mainPlayer.character.getCenterPos();

            bool found = false;
            float foundVolume = 0;
            for (int i = musicSources.Count - 1; i >= 0; i--)
            {
                var musicWrapper = musicSources[i];
                musicWrapper.update();
                musicWrapper.updateMusicSource();
                if (found)
                {
                    musicWrapper.volume = 0;
                    continue;
                }
                musicWrapper.updateMusicSourceVolume(listenerPos);
                if (musicWrapper.volume > 0)
                {
                    found = true;
                    foundVolume = musicWrapper.volume;
                }
            }
            if (found)
            {
                Global.music.volume = 100 - foundVolume;
            }
            else
            {
                Global.music.volume = 100;
                Global.music.updateVolume();
            }
        }

        public bool isTimeSlowed(GameObject go, out float slowAmount)
        {
            slowAmount = 0.75f;
            var actor = go as Actor;
            if (actor == null) return false;

            bool isSlown = false;

            if (actor is Character chr2)
            {
                if (chr2.infectedTime > 0)
                {
                    slowAmount = 1 - (0.25f * (chr2.infectedTime / 8));
                    isSlown = true;
                }
            }

            if (actor is Projectile || actor is Character || actor is Anim || actor is RideArmor || actor is OverdriveOstrich)
            {
                foreach (var cch in chargedCrystalHunters)
                {
                    var chr = go as Character;
                    if (chr != null && chr.player.alliance == cch.owner.alliance) continue;
                    if (chr != null && chr.isCCImmune()) continue;
                    
                    var proj = go as Projectile;
                    if (proj != null && proj.damager.owner.alliance == cch.owner.alliance) continue;
                    if (proj != null && proj.damager?.owner?.character?.isCCImmune() == true) continue;
                    
                    var mech = go as RideArmor;
                    if (mech != null && mech.player != null && mech.player.alliance == cch.owner.alliance) continue;

                    var oo = actor as OverdriveOstrich;
                    if (oo != null && oo.player != null && oo.player.alliance == cch.owner.alliance) continue;

                    if (cch.pos.distanceTo(actor.getCenterPos()) < CrystalHunterCharged.radius)
                    {
                        if (cch.isSnails) slowAmount = 0.5f;
                        if (oo != null && oo.ownedByLocalPlayer && oo.state is not OverdriveOCrystalizedState && oo.crystalizeCooldown == 0)
                        {
                            oo.changeState(new OverdriveOCrystalizedState());
                        }
                        isSlown = true;
                        break;
                    }
                }
            }

            return isSlown;
        }

        float powerplant2DarkTime = -1;
        int powerplant2State = 0;   //0 = light, 1 = fade to black, 2 = black, 3 = fade to light
        public float blackJoinTime;
        public int camNotSetFrames;
        public void render()
        {
            if (Global.level.mainPlayer == null) return;

            if (Global.level.joinedLate && !Global.level.mainPlayer.warpedIn && Global.level.mainPlayer.character == null && blackJoinTime < 3)
            {
                blackJoinTime += Global.spf;
                return;
            }

            if (!camSetFirstTime)
            {
                camNotSetFrames++;
                if (camNotSetFrames > 10)
                {
                    camSetFirstTime = true;
                    Global.view.Center = new Vector2f(camCenterX, camCenterY);
                    Global.window.SetView(Global.view);    
                }

                return;
            }

            RenderTexture srt = null;
            if (Options.main.enablePostProcessing)
            {
                srt = Global.screenRenderTexture;
                Color? bgColor = Global.level?.levelData?.bgColor;
                if (bgColor == null) bgColor = new Color(0, 0, 0, 0);
                srt.Clear(bgColor.Value);
                srt.Display();
            }

            for (int i = 0; i < parallaxes.Count; i++)
            {
                Parallax parallax = parallaxes[i];
                var parallaxTextures = levelData.getParallaxTextures(parallaxes[i].path);
                if (parallaxTextures == null) continue;

                Point parallaxOffset = parallaxOffsets[i];

                float px = parallax.startX + (parallax.speedX * camX);
                float py = parallax.startY + (parallax.speedY * camY);

                DrawWrappers.DrawMapTiles(parallaxTextures, parallaxOffset.x + px, parallaxOffset.y + py, srt, parallaxShader);
                Point size = Helpers.getTextureArraySize(parallaxTextures);

                int signX = MathF.Sign(parallax.scrollSpeedX);
                int signY = MathF.Sign(parallax.scrollSpeedY);

                if (parallax.scrollSpeedX != 0)
                {
                    DrawWrappers.DrawMapTiles(parallaxTextures, parallaxOffset.x + px - (size.x * signX), parallaxOffset.y + py, srt, parallaxShader);
                }

                if (parallax.scrollSpeedY != 0)
                {
                    DrawWrappers.DrawMapTiles(parallaxTextures, parallaxOffset.x + px, parallaxOffset.y + py - (size.y * signY), srt, parallaxShader);
                    DrawWrappers.DrawMapTiles(parallaxTextures, parallaxOffset.x + px - (size.x * signX), parallaxOffset.y + py - (size.y * signY), srt, parallaxShader);
                }

                foreach (ParallaxSprite parallaxSprite in parallaxSprites[i])
                {
                    float ppx = (parallax.speedX * camX);
                    float ppy = (parallax.speedY * camY);
                    parallaxSprite.render(ppx, ppy);
                }
            }

            if (levelData.name == "powerplant2")
            {
                drawPowerplant2();
            }

            if (isNon1v1Elimination() && gameMode.virusStarted > 0)
            {
                drawSigmaVirus();
            }

            foreach (var go in gameObjects)
            {
                go.render(0, 0);
            }
            foreach (var ms in mapSprites)
            {
                ms.render(0, 0);
            }
            foreach (var effect in effects)
            {
                effect.render(0, 0);
            }

            foreach (var debugDrawCall in debugDrawCalls)
            {
                debugDrawCall.Invoke();
            }
            if (debugDrawCalls.Count > 100)
            {
                debugDrawCalls.RemoveAt(0);
            }

            List<long> keys = DrawWrappers.walDrawObjects.Keys.ToList();
            keys.Sort();

            drawKeyRange(keys, long.MinValue, ZIndex.Backwall, srt);

            // If a backwall wasn't set, the background becomes the backwall.
            if (backwallSprites != null)
            {
                DrawWrappers.DrawMapTiles(backwallSprites, 0, 0, srt, backgroundShader);
            }
            else
            {
                DrawWrappers.DrawMapTiles(backgroundSprites, 0, 0, srt, backgroundShader);
            }
            
            drawKeyRange(keys, ZIndex.Backwall, ZIndex.Background, srt);

            // If a backwall wasn't set, the background becomes the backwall.
            if (backwallSprites != null)
            {
                DrawWrappers.DrawMapTiles(backgroundSprites, 0, 0, srt, backgroundShader);
            }
            
            drawKeyRange(keys, ZIndex.Background, ZIndex.Foreground, srt);

            DrawWrappers.DrawMapTiles(foregroundSprites, 0, 0, srt, backgroundShader);

            drawKeyRange(keys, ZIndex.Foreground, long.MaxValue, srt);

            DrawWrappers.walDrawObjects.Clear();

            // Draw the screen render texture with any post processing applied
            if (srt != null)
            {
                var screenSprite = new SFML.Graphics.Sprite(srt.Texture);
                screenSprite.Position = new Vector2f(camX, camY);

                var ppShaders = new List<ShaderWrapper>();
                foreach (var cch in chargedCrystalHunters)
                {
                    if (cch.timeSlowShader != null)
                    {
                        ppShaders.Add(cch.timeSlowShader);
                    }
                }
                foreach (var dhp in darkHoldProjs)
                {
                    if (dhp.screenShader != null)
                    {
                        ppShaders.Add(dhp.screenShader);
                    }
                }

                Global.window.SetView(Global.view);

                if (ppShaders.Count == 1)
                {
                    Global.window.Draw(screenSprite, new RenderStates(ppShaders[0].getShader()));
                }
                else if (ppShaders.Count > 1)
                {
                    RenderTexture prevRT = Global.srtBuffer2;
                    RenderTexture currentRT = Global.srtBuffer1;
                    RenderStates renderStates;

                    for (int i = 0; i < ppShaders.Count; i++)
                    {
                        prevRT = i % 2 == 1 ? Global.srtBuffer1 : Global.srtBuffer2;
                        currentRT = i % 2 == 0 ? Global.srtBuffer1 : Global.srtBuffer2;

                        var bufferSprite = i == 0 ? new SFML.Graphics.Sprite(srt.Texture) : new SFML.Graphics.Sprite(prevRT.Texture);

                        currentRT.Clear(new Color(0, 0, 0, 0));
                        currentRT.Display();
                        renderStates = new RenderStates(ppShaders[i].getShader());
                        currentRT.Draw(bufferSprite, renderStates);
                    }

                    var sprite2 = new SFML.Graphics.Sprite(currentRT.Texture);
                    sprite2.Position = new Vector2f(camX, camY);

                    Global.window.Draw(sprite2);
                }
                else
                {
                    Global.window.Draw(screenSprite);
                }
            }

            foreach (var deferredAction in DrawWrappers.deferredTextDraws)
            {
                deferredAction.Invoke();
            }
            DrawWrappers.deferredTextDraws.Clear();

            // At this point all drawing should be HUD/menu elements only
            gameMode.render();

            if (mainPlayer.readyTime > 0)
            {
                if (mainPlayer.readyTime < 0.4)
                {
                    int frameIndex = (int)Math.Round((mainPlayer.readyTime / 0.4) * 9);
                    Global.sprites["ready"].drawToHUD(frameIndex, (Global.screenW / 2) - 21, Global.screenH / 2);
                }
                else if (mainPlayer.readyTime < 1.75)
                {
                    if ((int)Math.Round(mainPlayer.readyTime * 7.5) % 2 == 0)
                    {
                        Global.sprites["ready"].drawToHUD(9, (Global.screenW / 2) - 21, Global.screenH / 2);
                    }
                }
            }

            if (Global.showGridHitboxes)
            {
                int gridItemCount = 0;
                for (int i = 0; i < grid.Count; i++)
                {
                    for (int j = 0; j < grid[i].Count; j++)
                    {
                        if (grid[i][j].Count > 0)
                        {
                            gridItemCount += grid[i][j].Count;
                            DrawWrappers.DrawRect(j * gridSize, i * gridSize, gridSize + (j * gridSize), gridSize + (i * gridSize), true, new Color(0, 255, 0, 128), 1, ZIndex.HUD + 100, true, Color.Magenta);
                            Helpers.drawTextStd("i:" + i.ToString() + ",j:" + j.ToString(), (j * gridSize) - Global.level.camX / Global.viewSize, 10 + (i * gridSize) - Global.level.camY / Global.viewSize, fontSize: 18);
                            Helpers.drawTextStd("count:" + grid[i][j].Count.ToString(), (j * gridSize) - Global.level.camX / Global.viewSize, (i * gridSize) - Global.level.camY / Global.viewSize, fontSize: 18);
                        }
                    }
                }
                Global.debugString2 = "Grid item count: " + gridItemCount.ToString();
            }

            if (Global.showAIDebug)
            {
                foreach (var navMeshNode in navMeshNodes)
                {
                    float textPosX = navMeshNode.pos.x - Global.level.camX / Global.viewSize;
                    float textPosY = (navMeshNode.pos.y - 20 - Global.level.camY) / Global.viewSize;
                    DrawWrappers.DrawRect(navMeshNode.pos.x - 10, navMeshNode.pos.y - 10, navMeshNode.pos.x + 10, navMeshNode.pos.y + 10, true, new Color(0, 255, 0, 128), 1, ZIndex.HUD + 100, true);
                    DrawWrappers.DrawText(navMeshNode.name, navMeshNode.pos.x, navMeshNode.pos.y - 20, Alignment.Center, true, 0.75f, Color.White, Color.Black, Text.Styles.Regular, 1, true, ZIndex.HUD);
                    //Helpers.drawTextStd(navMeshNode.name, textPosX, textPosY, Alignment.Center, fontSize: 24);
                }
            }

            if (Global.showHitboxes && Global.debug)
            {
                var visitedNodes = new HashSet<WallPathNode>();
                foreach (var wallPathNode in levelData.wallPathNodes)
                {
                    if (visitedNodes.Contains(wallPathNode)) continue;
                    visitedNodes.Add(wallPathNode);
                    DrawWrappers.DrawLine(wallPathNode.point.x, wallPathNode.point.y, wallPathNode.next.point.x, wallPathNode.next.point.y, Color.Red, 1, ZIndex.HUD + 500);
                }
            }
        }

        public void drawKeyRange(List<long> keys, long minVal, long maxVal, RenderTexture srt)
        {
            foreach (long key in keys)
            {
                if (key >= minVal && key < maxVal)
                {
                    var drawLayer = DrawWrappers.walDrawObjects[key];
                    if (srt != null)
                    {
                        srt.Draw(drawLayer);
                    }
                    else
                    {
                        Global.window.Draw(drawLayer);
                    }
                }
            }

        }

        int virusColorState = 0;
        float virusColorTime;
        Color virusColor1 = new Color(99, 20, 99, 128);
        Color virusColor2 = new Color(66, 20, 99, 128);
        private void drawSigmaVirus()
        {
            var rect = gameMode.safeZoneRect;

            if (virusColorState == 0)
            {
                virusColorTime += Global.spf;
                if (virusColorTime >= 1)
                {
                    virusColorState = 1;
                }
            }
            else if (virusColorState == 1)
            {
                virusColorTime -= Global.spf;
                if (virusColorTime <= 0)
                {
                    virusColorState = 0;
                }
            }

            //virusColorTime = (MathF.Sin(Global.time * 2) * 0.5f) + 0.5f;

            Color color = new Color(
                (byte)Helpers.lerp(virusColor1.R, virusColor2.R, virusColorTime),
                (byte)Helpers.lerp(virusColor1.G, virusColor2.G, virusColorTime),
                (byte)Helpers.lerp(virusColor1.B, virusColor2.B, virusColorTime),
                128
            );

            DrawWrappers.DrawRect(0, 0, rect.x1, height, true, color, 1, ZIndex.HUD, isWorldPos: true);
            DrawWrappers.DrawRect(rect.x2, 0, width, height, true, color, 1, ZIndex.HUD, isWorldPos: true);
            DrawWrappers.DrawRect(rect.x1, 0, rect.x2, rect.y1, true, color, 1, ZIndex.HUD, isWorldPos: true);
            DrawWrappers.DrawRect(rect.x1, rect.y2, rect.x2, height, true, color, 1, ZIndex.HUD, isWorldPos: true);
        }

        private void drawPowerplant2()
        {
            byte alpha = 0;
            if (powerplant2State == 0)
            {
                powerplant2DarkTime += Global.spf * 0.5f;
                if (powerplant2DarkTime >= 1)
                {
                    powerplant2State = 1;
                    powerplant2DarkTime = 0;
                }
            }
            else if (powerplant2State == 1)
            {
                alpha = (byte)(powerplant2DarkTime * 255);
                powerplant2DarkTime += Global.spf * 2;
                if (powerplant2DarkTime >= 1)
                {
                    powerplant2State = 2;
                    powerplant2DarkTime = 0;
                }
            }
            else if (powerplant2State == 2)
            {
                alpha = 255;
                powerplant2DarkTime += Global.spf * 0.5f;
                if (powerplant2DarkTime >= 1)
                {
                    powerplant2State = 3;
                    powerplant2DarkTime = 0;
                }
            }
            else if (powerplant2State == 3)
            {
                alpha = (byte)((1 - powerplant2DarkTime) * 255);
                powerplant2DarkTime += Global.spf * 2;
                if (powerplant2DarkTime >= 1)
                {
                    powerplant2State = 0;
                    powerplant2DarkTime = 0;
                }
            }

            DrawWrappers.DrawRect(0, 0, width, height, true, new Color(0, 0, 0, alpha), 1, Global.level.mainPlayer?.character?.zIndex ?? ZIndex.HUD, isWorldPos: true);
        }

        public float camCenterX { get { return camX + Global.halfScreenW * Global.viewSize; } }
        public float camCenterY { get { return camY + Global.halfScreenH * Global.viewSize; } }

        public float killY
        {
            get
            {
                if (levelData.killY != null)
                {
                    return (float)levelData.killY;
                }
                return height + 60;
            }
        }

        public bool ignoreNoScrolls()
        {
            return mainPlayer.weapon is WolfSigmaHandWeapon || mainPlayer.character?.isAnyZoom() == true;
        }

        public void updateCamPos(float deltaX, float deltaY)
        {
            var playerX = camPlayer.character.getCamCenterPos().x;
            var playerY = camPlayer.character.getCamCenterPos().y;

            var dontMoveX = false;
            var dontMoveY = false;

            var scaledCanvasW = Global.viewScreenW;
            var scaledCanvasH = Global.viewScreenH;

            var halfScaledCanvasW = Global.halfScreenW * Global.viewSize;
            var halfScaledCanvasH = Global.halfScreenH * Global.viewSize;

            var maxX = width - halfScaledCanvasW;
            var maxY = height - halfScaledCanvasH;

            if (playerX < halfScaledCanvasW && camX <= 0) dontMoveX = true;
            if (playerY < halfScaledCanvasH && camY <= 0) dontMoveY = true;

            if (playerX > maxX && camX >= width - scaledCanvasW) dontMoveX = true;
            if (playerY > maxY && camY >= height - scaledCanvasH) dontMoveY = true;

            if (playerX > camX + halfScaledCanvasW && deltaX < 0) dontMoveX = true;
            if (playerX < camX + halfScaledCanvasW && deltaX > 0) dontMoveX = true;
            if (playerY > camY + halfScaledCanvasH && deltaY < 0) dontMoveY = true;
            if (playerY < camY + halfScaledCanvasH && deltaY > 0) dontMoveY = true;

            float prevCamX = camX;
            float prevCamY = camY;

            if (!dontMoveX)
            {
                camX += deltaX;
            }
            if (!dontMoveY)
            {
                camY += deltaY;
            }

            var camRect = new Rect(camX, camY, camX + scaledCanvasW, camY + scaledCanvasH);
            var camRectShape = camRect.getShape();

            if (!ignoreNoScrolls())
            {
                foreach (var noScroll in noScrolls)
                {
                    if (!noScroll.snap)
                    {
                        if (camRectShape.maxY > noScroll.shape.minY && camRectShape.minY < noScroll.shape.maxY)
                        {
                            if ((noScroll.freeDir == ScrollFreeDir.Left || noScroll.freeDir == ScrollFreeDir.None) &&
                                prevCamX + Global.screenW <= noScroll.shape.minX && camX + Global.screenW > noScroll.shape.minX)
                            {
                                camX = noScroll.shape.minX - Global.screenW;
                            }
                            if ((noScroll.freeDir == ScrollFreeDir.Right || noScroll.freeDir == ScrollFreeDir.None) &&
                                prevCamX >= noScroll.shape.maxX && camX < noScroll.shape.maxX)
                            {
                                camX = noScroll.shape.maxX;
                            }
                        }
                        if (camRectShape.maxX > noScroll.shape.minX && camRectShape.minX < noScroll.shape.maxX)
                        {
                            if ((noScroll.freeDir == ScrollFreeDir.Up || noScroll.freeDir == ScrollFreeDir.None) &&
                                prevCamY + Global.screenH <= noScroll.shape.minY && camY + Global.screenH > noScroll.shape.minY)
                            {
                                camY = noScroll.shape.minY - Global.screenH;
                            }
                            if ((noScroll.freeDir == ScrollFreeDir.Down || noScroll.freeDir == ScrollFreeDir.None) &&
                                prevCamY >= noScroll.shape.maxY && camY < noScroll.shape.maxY)
                            {
                                camY = noScroll.shape.maxY;
                            }
                        }
                    }
                    else if (noScroll.shape.intersectsShape(camRectShape) != null)
                    {
                        if (noScroll.freeDir == ScrollFreeDir.Left)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(-1, 0));
                            if (mtv != null) camX += ((Point)mtv).x;
                        }
                        else if (noScroll.freeDir == ScrollFreeDir.Right)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(1, 0));
                            if (mtv != null) camX += ((Point)mtv).x;
                        }
                        if (noScroll.freeDir == ScrollFreeDir.Up)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(0, -1));
                            if (mtv != null) camY += ((Point)mtv).y;
                        }
                        else if (noScroll.freeDir == ScrollFreeDir.Down)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(0, 1));
                            if (mtv != null) camY += ((Point)mtv).y;
                        }
                    }
                }
            }

            camX = Helpers.clamp(camX, 0, width - scaledCanvasW);
            camY = Helpers.clamp(camY, 0, height - scaledCanvasH);

            float offsetX = 0;
            float offsetY = 0;
            if (shakeX > 0 || shakeY > 0)
            {
                offsetX = 3 * (float)Math.Sin(shakeX * 100);
                offsetY = 3 * (float)Math.Sin(shakeY * 100);
                shakeX = Helpers.clampMin(shakeX - Global.spf, 0);
                shakeY = Helpers.clampMin(shakeY - Global.spf, 0);
            }

            float yOff = 0;
            if (Global.level.isRace() && server.fixedCamera && Global.level.height < 448)
            {
                yOff = (448 - Global.level.height) / 2;
            }
            Global.view.Center = new Vector2f(camCenterX + offsetX, camCenterY + offsetY + yOff);
            Global.window.SetView(Global.view);
            camSetFirstTime = true;
        }

        public HashSet<byte> getCrackedWallBytes()
        {
            var retBytes = new HashSet<byte>();
            foreach (var go in gameObjects)
            {
                if (go is CrackedWall cw)
                {
                    retBytes.Add(cw.id);
                }
            }
            return retBytes;
        }

        public void syncCrackedWalls(HashSet<byte> crackedWallBytes)
        {
            foreach (var go in gameObjects.ToList())
            {
                if (go is CrackedWall cw && !crackedWallBytes.Contains(cw.id))
                {
                    cw.destroySilently = true;
                    cw.destroySelf();
                }
            }
        }

        public float shakeX;
        public float shakeY;

        public void snapCamPos(Point point, Point? prevCamPos)
        {
            var camPos = computeCamPos(point, prevCamPos);
            camX = camPos.x;
            camY = camPos.y;
        }

        public Point computeCamPos(Point point, Point? prevCamPos)
        {
            var camX = point.x - Global.halfScreenW * Global.viewSize;
            var camY = point.y - Global.halfScreenH * Global.viewSize;

            var camRect = new Rect(camX, camY, camX + Global.viewScreenW, camY + Global.viewScreenH);
            var camRectShape = camRect.getShape();

            if (!ignoreNoScrolls())
            {
                foreach (var noScroll in noScrolls)
                {
                    if (!noScroll.snap && prevCamPos != null)
                    {
                        continue;
                    }

                    if (noScroll.shape.intersectsShape(camRectShape) != null)
                    {
                        if (noScroll.freeDir == ScrollFreeDir.Left)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(-1, 0));
                            if (mtv != null) camX += ((Point)mtv).x;
                        }
                        else if (noScroll.freeDir == ScrollFreeDir.Right)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(1, 0));
                            if (mtv != null) camX += ((Point)mtv).x;
                        }
                        else if (noScroll.freeDir == ScrollFreeDir.Up)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(0, -1));
                            if (mtv != null) camY += ((Point)mtv).y;
                        }
                        else if (noScroll.freeDir == ScrollFreeDir.Down)
                        {
                            var mtv = camRectShape.getMinTransVectorDir(noScroll.shape, new Point(0, 1));
                            if (mtv != null) camY += ((Point)mtv).y;
                        }
                    }
                }
            }

            if (camX < 0) camX = 0;
            if (camY < 0) camY = 0;

            var maxX = width - Global.viewScreenW;
            var maxY = height - Global.viewScreenH;

            if (camX > maxX) camX = maxX;
            if (camY > maxY) camY = maxY;

            return new Point(camX, camY);
        }

        public NavMeshNode getClosestNodeInSight(Point pos)
        {
            NavMeshNode minNode = null;
            float minDist = float.MaxValue;
            foreach (var node in navMeshNodes)
            {
                if (noWallsInBetween(pos, node.pos))
                {
                    var dist = pos.distanceTo(node.pos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minNode = node;
                    }
                }
            }
            if (minNode != null) return minNode;
            minDist = float.MaxValue;
            foreach (var node in navMeshNodes)
            {
                var dist = pos.distanceTo(node.pos);
                if (dist < minDist)
                {
                    minDist = dist;
                    minNode = node;
                }
            }

            return minNode;
        }

        public NavMeshNode getRandomNode()
        {
            return navMeshNodes.GetRandomItem();
        }

        public SpawnPoint getClosestSpawnPoint(Point pos)
        {
            return spawnPoints.OrderBy(s => s.pos.distanceTo(pos)).FirstOrDefault();
        }

        public SpawnPoint getSpawnPoint(Player player, bool isFirstTime)
        {
            if (Global.overrideSpawnPoint != null)
            {
                var sp = spawnPoints.FirstOrDefault(s => s.name == Global.overrideSpawnPoint);
                if (sp != null) return sp;
            }

            if (isRace())
            {
                if (player.lastDeathPos != null)
                {
                    var pos = player.lastDeathPos.Value;
                    var orderedSpawnPoints = spawnPoints.OrderBy(s => s.pos.distanceTo(pos)).ToList();
                    var retval = orderedSpawnPoints.FirstOrDefault(s => s.pos.x <= pos.x);
                    if (retval == null) retval = orderedSpawnPoints.FirstOrDefault();
                    return retval;
                }
                else
                {
                    return raceStartSpawnPoints[player.getSpawnIndex(raceStartSpawnPoints.Count)];
                }
            }

            if (isNon1v1Elimination() && gameMode.virusStarted > 0)
            {
                return spawnPoints[gameMode.safeZoneSpawnIndex];
            }

            if (is1v1())
            {
                return spawnPoints[player.getSpawnIndex(spawnPoints.Count)];
            }

            if (Global.quickStart && Global.quickStartSpawn != null) return spawnPoints[Global.quickStartSpawn.Value];
            var availableSpawns = spawnPoints.Where((spawnPoint) => 
            {
                return (!gameMode.useTeamSpawns() && spawnPoint.alliance == -1) || (gameMode.useTeamSpawns() && spawnPoint.alliance == player.newAlliance);
            }).ToList();

            if (isFirstTime)
            {
                return availableSpawns[player.getSpawnIndex(availableSpawns.Count)];
            }

            var unoccupied = availableSpawns.Where((spawnPoint) =>
            {
                return !spawnPoint.occupied();
            }).ToList();

            return unoccupied.GetRandomItem();
        }

        public bool isRace()
        {
            return gameMode is Race;
        }

        public ControlPoint getCurrentControlPoint()
        {
            foreach (var controlPoint in controlPoints)
            {
                if (!controlPoint.locked && !controlPoint.captured) return controlPoint;
            }
            return null;
        }

        public bool is1v1()
        {
            return levelData.is1v1();
        }

        public bool isHyper1v1()
        {
            return is1v1() && server?.customMatchSettings?.hyperModeMatch == true;
        }

        public bool isTraining()
        {
            return levelData.isTraining();
        }

        public bool isElimination()
        {
            return gameMode is Elimination || gameMode is TeamElimination;
        }

        public bool isNon1v1Elimination()
        {
            return isElimination() && !is1v1();
        }

        public Point getSoundListenerOrigin()
        {
            if (Global.level == null)
            {
                return new Point();
            }

            Point originPoint = new Point(Global.level.camCenterX, Global.level.camCenterY);

            Character mainChar = Global.level.mainPlayer?.character;
            Maverick mainMaverick = Global.level.mainPlayer?.currentMaverick;
            if (mainChar != null && mainChar.isSoundCentered())
            {
                originPoint = mainChar.pos;
            }
            else if (mainMaverick != null)
            {
                originPoint = mainMaverick.pos;
            }
            else if (mainChar == null && Global.level.mainPlayer != null && Global.level.mainPlayer.lastDeathPos != null && !Global.level.mainPlayer.isSpectator)
            {
                originPoint = Global.level.mainPlayer.lastDeathPos.Value;
            }

            return originPoint;
        }

        public void destroy()
        {
            for (int i = loopingSounds.Count - 1; i >= 0; i--)
            {
                loopingSounds[i].destroy();
            }

            for (int i = musicSources.Count - 1; i >= 0; i--)
            {
                musicSources[i].destroy();
            }

            Global.level.levelData.unloadLevelImages();
            Global.level = null;
            Global.serverClient = null;

            Global.viewSize = 1;
            Global.view.Size = new Vector2f(Global.viewScreenW, Global.viewScreenH);

            Global.radarRenderTexture.Dispose();
            Global.radarRenderTexture = null;

            Global.showHitboxes = false;

            foreach (var musicWrapper in musicSources)
            {
                musicWrapper.destroy();
            }

            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        public string getFlagDataDump()
        {
            string dump = "NET ID DUMP\n\n";
            foreach (var go in gameObjects)
            {
                var actor = go as Actor;
                if (actor == null) continue;
                if (actor.netId == null) continue;
                dump += actor.name + ":" + actor.sprite?.name + ":" + actor.netId + "\n";
            }
            dump += "\nFLAG DATA DUMP\n\n";
            dump += "Red Flag:\n";
            if (redFlag != null)
            {
                dump += "NetId: " + redFlag.netId + "\n";
                dump += "Owned By Local Player: " + redFlag.ownedByLocalPlayer + "\n";
                dump += "Position: " + redFlag.pos.ToString() + "\n";
                dump += "Carrier: " + redFlag.chr?.player?.name + "\n";
                if (redFlag.destroyed) dump += "DESTROYED\n";
            }
            dump += "\nBlue Flag:\n";
            if (blueFlag != null)
            {
                dump += "NetId: " + blueFlag.netId + "\n"; ;
                dump += "Owned By Local Player: " + blueFlag.ownedByLocalPlayer + "\n"; ;
                dump += "Position: " + blueFlag.pos.ToString() + "\n"; ;
                dump += "Carrier: " + blueFlag.chr?.player?.name + "\n";
                if (blueFlag.destroyed) dump += "DESTROYED\n";
            }

            return dump;
        }

        public void resetFlags()
        {
            if (!Global.isHost) return;

            if (Global.level.redFlag == null || Global.level.blueFlag == null)
            {
                Logger.logException(new Exception("Host tried to reset flag but they were null"), false);
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("Could not reset flags. Error: flags null", null, null, true), sendRpc: true);
                return;
            }
            if (Global.level.redFlag.destroyed || Global.level.blueFlag.destroyed)
            {
                Logger.logException(new Exception("Host tried to reset flag but they were destroyed"), false);
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("Flags were reset. Error: flags destroyed", null, null, true), sendRpc: true);
                return;
            }
            if (!Global.level.gameObjects.Contains(Global.level.redFlag) || !Global.level.gameObjects.Contains(Global.level.blueFlag))
            {
                Logger.logException(new Exception("Host tried to reset flag but they were not in gameobject set"), false);
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("Flags were reset. Error: flags missing", null, null, true), sendRpc: true);
                return;
            }

            if (!Global.level.redFlag.ownedByLocalPlayer)
            {
                Global.level.redFlag.takeOwnership();
                Global.level.redFlag.pedestal?.takeOwnership();
            }
            if (!Global.level.blueFlag.ownedByLocalPlayer)
            {
                Global.level.blueFlag.takeOwnership();
                Global.level.blueFlag.pedestal?.takeOwnership();
            }

            Global.level.redFlag.returnFlag();
            Global.level.blueFlag.returnFlag();

            Global.level.redFlag.pickupCooldown = 1;
            Global.level.blueFlag.pickupCooldown = 1;

            Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("Flags were reset.", null, null, true), sendRpc: true);
        }

        public int getLeaderKills()
        {
            var players = Global.level?.server?.players;
            if (players == null || players.Count == 0) return 0;
            return players.Max(p => p.kills);
        }
    }

    public class DelayedAction
    {
        public Action action;
        public float time;
        public DelayedAction(Action action, float time)
        {
            this.action = action;
            this.time = time;
        }
    }
}
