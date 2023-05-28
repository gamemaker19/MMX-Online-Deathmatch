using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public partial class Player
    {
        public Input input;
        public Character character;
        public Character lastCharacter;
        public bool ownedByLocalPlayer;
        public int? awakenedScrapEnd;
        public float fgMoveAmmo = 32;
        public bool isDefenderFavored
        {
            get
            {
                if (character != null && !character.ownedByLocalPlayer) return character.isDefenderFavoredBS.getValue();
                if (Global.level?.server == null) return false;
                if (Global.serverClient == null) return false;
                if (Global.level.server.netcodeModel == NetcodeModel.FavorAttacker)
                {
                    return getPingOrStartPing() >= Global.level.server.netcodeModelPing;
                }
                return true;
            }
        }

        public string getDisplayPing()
        {
            int? pingOrStartPing = getPingOrStartPing();
            if (pingOrStartPing == null) return "?";
            return pingOrStartPing.Value.ToString();
        }

        public int? getPingOrStartPing()
        {
            if (ping == null)
            {
                return serverPlayer?.startPing;
            }
            return ping.Value;
        }

        public Character preTransformedAxl;
        public bool isDisguisedAxl
        { 
            get
            {
                return disguise != null;
            }
        }
        public List<Weapon> savedDNACoreWeapons = new List<Weapon>();
        public int axlBulletType;
        public List<bool> axlBulletTypeBought = new List<bool>() { true, false, false, false, false, false, false };
        public List<float> axlBulletTypeAmmo = new List<float>() { 0, 0, 0, 0, 0, 0, 0 };
        public List<float> axlBulletTypeLastAmmo = new List<float>() { 32, 32, 32, 32, 32, 32, 32 };
        public int lastDNACoreIndex;
        public DNACore lastDNACore;
        public Point axlCursorPos;
        public Point? assassinCursorPos;
        public Point axlCursorWorldPos { get { return axlCursorPos.addxy(Global.level.camX, Global.level.camY); } }
        public Point axlScopeCursorWorldPos;
        public Point axlScopeCursorWorldLerpPos;
        public Point axlZoomOutCursorDestPos;
        public Point axlLockOnCursorPos;
        public Point axlGenericCursorWorldPos
        {
            get
            {
                if (character == null || !character.isZooming() || character.isZoomingIn || character.isZoomOutPhase1Done)
                {
                    return axlCursorWorldPos;
                }
                return axlScopeCursorWorldPos;
            }
        }
        public float zoomRange 
        { 
            get 
            {
                if (character != null && (character.isWhiteAxl() || character.hyperAxlStillZoomed)) return 100000;
                return Global.viewScreenW * 2.5f;
            } 
        }
        public RaycastHitData assassinHitPos;

        public bool canUpgradeXArmor()
        {
            return charNum == 0 && !isDisguisedAxl && character?.isHyperX != true && character?.charState is not XRevive && character?.charState is not XReviveStart;
        }

        public float adjustedZoomRange { get { return zoomRange - 40; } }

        public int getVileWeight(int? overrideLoadoutWeight = null)
        {
            if (overrideLoadoutWeight == null)
            {
                overrideLoadoutWeight = loadout.vileLoadout.getTotalWeight();
            }
            return overrideLoadoutWeight.Value;
        }

        public int getVileWeightActive()
        {
            int weight =
                vileCannonWeapon.vileWeight +
                vileVulcanWeapon.vileWeight +
                vileMissileWeapon.vileWeight +
                vileRocketPunchWeapon.vileWeight +
                vileNapalmWeapon.vileWeight +
                vileBallWeapon.vileWeight +
                vileCutterWeapon.vileWeight +
                vileFlamethrowerWeapon.vileWeight +
                vileLaserWeapon.vileWeight;

            return weight;
        }

        public Point? lastDeathPos;
        public bool lastDeathWasVileMK2;
        public bool lastDeathWasVileMK5;
        public bool lastDeathWasSigmaHyper;
        public bool lastDeathWasXHyper;
        public const int reviveVileScrapCost = 5;
        public const int reviveSigmaScrapCost = 10;
        public const int reviveXScrapCost = 10;
        public bool lastDeathCanRevive;
        public int vileFormToRespawnAs;
        public bool hyperSigmaRespawn;
        public bool hyperXRespawn;
        public float trainingDpsTotalDamage;
        public float trainingDpsStartTime;
        public bool showTrainingDps { get { return isAI && Global.serverClient == null && Global.level.isTraining(); } }

        public bool aiTakeover;
        public MaverickAIBehavior currentMaverickCommand;

        public bool isX { get { return charNum == 0; } }
        public bool isZero { get { return charNum == 1; } }
        public bool isVile { get { return charNum == 2; } }
        public bool isAxl { get { return charNum == 3; } }
        public bool isSigma { get { return charNum == 4; } }

        public float health;
        public float maxHealth;
        public bool isDead
        {
            get
            {
                if (isSigma && currentMaverick != null)
                {
                    return false;
                }
                if (character == null) return true;
                if (ownedByLocalPlayer && character.charState is Die) return true;
                else if (!ownedByLocalPlayer)
                {
                    return health <= 0;
                }
                return false;
            }
        }
        public const float armorHealth = 16;
        public float respawnTime;
        public bool lockWeapon;
        public string[] randomTip;
        public int aiArmorPath;
        public float teamHealAmount;
        public List<CopyShotDamageEvent> copyShotDamageEvents = new List<CopyShotDamageEvent>();
        public bool usedChipOnce;

        public bool scanned;
        public bool tagged;

        public List<int> aiArmorUpgradeOrder;
        public int aiArmorUpgradeIndex;
        public bool isAI;   //DO NOT USE THIS for determining if a player is a bot to non hosts in online matches, use isBot below
        //A bot is a subset of an AI; an AI that's in an online match
        public bool isBot
        {
            get
            {
                if (serverPlayer == null)
                {
                    return isAI;
                }
                return serverPlayer.isBot;
            }
        }

        public bool isLocalAI
        {
            get { return isAI && Global.serverClient == null; }
        }

        public int realCharNum
        {
            get
            {
                if (isAxl || isDisguisedAxl) return 3;
                return charNum;
            }
        }

        public bool warpedInOnce;
        public bool spawnedOnce;

        public bool isMuted;

        // Subtanks
        public Dictionary<int, List<SubTank>> charSubTanks = new Dictionary<int, List<SubTank>>()
        {
            { 0, new List<SubTank>() },
            { 1, new List<SubTank>() },
            { 2, new List<SubTank>() },
            { 3, new List<SubTank>() },
            { 4, new List<SubTank>() }
        };
        public List<SubTank> subtanks
        {
            get
            {
                return charSubTanks[isDisguisedAxl ? 3 : charNum];
            }
            set
            {
                charSubTanks[isDisguisedAxl ? 3 : charNum] = value;
            }
        }

        // Heart tanks
        public Dictionary<int, int> charHeartTanks = new Dictionary<int, int>()
        {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
        };
        public int heartTanks
        {
            get
            {
                return charHeartTanks[isDisguisedAxl ? 3 : charNum];
            }
            set
            {
                charHeartTanks[isDisguisedAxl ? 3 : charNum] = value;
            }
        }

        // Scrap
        public Dictionary<int, int> charScrap = new Dictionary<int, int>();
        public int scrap
        {
            get
            {
                return charScrap[isDisguisedAxl ? 3 : charNum];
            }
            set
            {
                charScrap[isDisguisedAxl ? 3 : charNum] = value;
            }
        }

        public bool isSpectator
        { 
            get 
            {
                if (Global.serverClient == null) return isOfflineSpectator;
                return serverPlayer.isSpectator;
            }
            set
            {
                if (Global.serverClient == null) isOfflineSpectator = value;
                else serverPlayer.isSpectator = value;
            }
        }
        private bool isOfflineSpectator;
        public bool is1v1Combatant;

        public void setSpectate(bool newSpectateValue)
        {
            if (Global.serverClient != null)
            {
                string msg = name + " now spectating.";
                if (newSpectateValue == false) msg = name + " stopped spectating.";
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(msg, null, null, true), sendRpc: true);
                RPC.makeSpectator.sendRpc(id, newSpectateValue);
            }
            else
            {
                isSpectator = newSpectateValue;
            }
        }

        public int selectedRAIndex = Global.quickStartMechNum ?? 0;
        public bool isSelectingRA()
        {
            if (weapon is MechMenuWeapon mmw && mmw.isMenuOpened)
            {
                return true;
            }
            return false;
        }

        public int selectedCommandIndex = 0;
        public bool isSelectingCommand()
        {
            if (weapon is MaverickWeapon mw && mw.isMenuOpened)
            {
                return true;
            }
            return false;
        }

        // Things needed to be synced to late joiners. Note: these are not automatically applied, you need to add code in Global.level.joinedLateSyncPlayers and update PlayerSync class at top of this file
        public int kills;
        public int assists;
        public int deaths;
        public string getDeathScore()
        {
            if (Global.level.gameMode is Elimination || Global.level.gameMode is TeamElimination) return (Global.level.gameMode.playingTo - deaths).ToString();
            return deaths.ToString();
        }
        public ushort curMaxNetId;
        public bool warpedIn = false;
        public float readyTime;
        public const float maxReadyTime = 1.75f;
        public bool readyTextOver = false;
        public ServerPlayer serverPlayer;
        public LoadoutData loadout;
        public LoadoutData previousLoadout;
        public LoadoutData oldAxlLoadout;
        public AxlLoadout axlLoadout { get { return loadout.axlLoadout; } }

        public bool frozenCastlePurchased;
        public bool speedDevilPurchased;

        // Every time you add an armor, add an "old" version and update DNA Core code appropriately
        public ushort armorFlag;
        public ushort oldArmorFlag;
        public bool ultimateArmor;
        public bool oldUltimateArmor;
        public bool frozenCastle;
        public bool oldFrozenCastle;
        public bool speedDevil;
        public bool oldSpeedDevil;
        
        public Disguise disguise;

        public int newAlliance;     // Not sure what this is useful for, seems like a pointless clone of alliance that needs to be kept in sync

        // Things that ServerPlayer already has
        public string name;
        public int id;
        public int alliance;    // Only set on spawn with data read from ServerPlayer alliance. The ServerPlayer alliance changes earlier on team change/autobalance
        public int charNum;

        public int newCharNum;
        public int? delayedNewCharNum;
        public int? ping;
        public void syncFromServerPlayer(ServerPlayer serverPlayer)
        {
            if (!this.serverPlayer.isHost && serverPlayer.isHost)
            {
                promoteToHost();
            }
            this.serverPlayer = serverPlayer;
            name = serverPlayer.name;
            ping = serverPlayer.ping;

            if (!ownedByLocalPlayer)
            {
                kills = serverPlayer.kills;
                deaths = serverPlayer.deaths;
            }

            if (ownedByLocalPlayer && serverPlayer.autobalanceAlliance != null && newAlliance != serverPlayer.autobalanceAlliance.Value)
            {
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(name + " was autobalanced to " + GameMode.getTeamName(serverPlayer.autobalanceAlliance.Value), null, null, true), true);
                forceKill();
                scrap += 5;
                Global.serverClient?.rpc(RPC.switchTeam, RPCSwitchTeam.getSendMessage(id, serverPlayer.autobalanceAlliance.Value));
                newAlliance = serverPlayer.autobalanceAlliance.Value;
            }
        }

        // Character specific data populated on RPC request
        public ushort? charNetId;
        public ushort? charRollingShieldNetId;
        public float charXPos;
        public float charYPos;
        public int charXDir;
        public Dictionary<int, int> charNumToKills = new Dictionary<int, int>()
        {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 }
        };

        public int hyperChargeSlot;
        public int xArmor1v1;
        public float vileAmmo = 32;
        public float vileMaxAmmo = 32;
        public float sigmaAmmo = 32;
        public float sigmaMaxAmmo = 32;
        public int? maverick1v1;
        public bool maverick1v1Spawned;
        public bool isNon1v1MaverickSigma()
        {
            return isSigma && maverick1v1 == null;
        }

        public bool is1v1MaverickX1()
        {
            return maverick1v1 <= 8;
        }

        public bool is1v1MaverickX2()
        {
            return maverick1v1 > 8 && maverick1v1 <= 17;
        }

        public bool is1v1MaverickX3()
        {
            return maverick1v1 > 17;
        }

        public bool is1v1MaverickFakeZero()
        {
            return maverick1v1 == 17;
        }

        public int getStartHeartTanks()
        {
            if (Global.level.isNon1v1Elimination() && Global.level.gameMode.playingTo < 3)
            {
                return 8;
            }
            if (Global.level.is1v1())
            {
                return 8;
            }
            if (Global.level?.server?.customMatchSettings != null)
            {
                return Global.level.server.customMatchSettings.startHeartTanks;
            }

            return 0;
        }

        public int getStartHeartTanksForChar()
        {
            if (!Global.level.server.disableHtSt && Global.level?.server?.customMatchSettings == null && !Global.level.gameMode.isTeamMode)
            {
                int leaderKills = Global.level.getLeaderKills();
                if (leaderKills >= 32) return 8;
                if (leaderKills >= 28) return 7;
                if (leaderKills >= 24) return 6;
                if (leaderKills >= 20) return 5;
                if (leaderKills >= 16) return 4;
                if (leaderKills >= 12) return 3;
                if (leaderKills >= 8) return 2;
                if (leaderKills >= 4) return 1;
            }
            return 0;
        }

        public int getStartSubTanks()
        {
            if (Global.level?.server?.customMatchSettings != null)
            {
                return Global.level.server.customMatchSettings.startSubTanks;
            }

            return 0;
        }

        public int getStartSubTanksForChar()
        {
            if (!Global.level.server.disableHtSt && Global.level?.server?.customMatchSettings == null && !Global.level.gameMode.isTeamMode)
            {
                int leaderKills = Global.level.getLeaderKills();
                if (leaderKills >= 32) return 4;
                if (leaderKills >= 24) return 3;
                if (leaderKills >= 16) return 2;
                if (leaderKills >= 8) return 1;
            }

            return 0;
        }

        public int getSameCharNum()
        {
            if (Global.level?.server?.customMatchSettings != null)
            {
                if (Global.level.gameMode.isTeamMode && alliance == GameMode.redAlliance)
                {
                    return Global.level.server.customMatchSettings.redSameCharNum;
                }
                return Global.level.server.customMatchSettings.sameCharNum;
            }
            return -1;
        }

        public Player(string name, int id, int charNum, PlayerCharData playerData, bool isAI, bool ownedByLocalPlayer, int alliance, Input input, ServerPlayer serverPlayer)
        {
            this.name = name;
            this.id = id;
            curMaxNetId = getFirstAvailableNetId();
            this.alliance = alliance;
            newAlliance = alliance;
            this.isAI = isAI;

            if (getSameCharNum() != -1) charNum = getSameCharNum();
            if (charNum > 4)
            {
                if (Global.level.is1v1())
                {
                    maverick1v1 = charNum - 5;
                    charNum = 4;
                }
                else
                {
                    charNum = 4;
                    playerData.charNum = 4;
                }
            }
            this.charNum = charNum;
            newCharNum = charNum;
            
            this.input = input;
            this.ownedByLocalPlayer = ownedByLocalPlayer;

            this.xArmor1v1 = playerData?.armorSet ?? 1;
            if (Global.level.is1v1() && isX)
            {
                bootsArmorNum = xArmor1v1;
                bodyArmorNum = xArmor1v1;
                helmetArmorNum = xArmor1v1;
                armArmorNum = xArmor1v1;
            }

            for (int i = 0; i < 5; i++)
            {
                charScrap[i] = getStartScrap();
            }
            foreach (var key in charHeartTanks.Keys)
            {
                int htCount = key == charNum ? getStartHeartTanksForChar() : getStartHeartTanks();
                charHeartTanks[key] = htCount;
            }
            foreach (var key in charSubTanks.Keys)
            {
                int stCount = key == charNum ? getStartSubTanksForChar() : getStartSubTanks();
                for (int i = 0; i < stCount; i++)
                {
                    charSubTanks[key].Add(new SubTank());
                }
            }

            maxHealth = getMaxHealth();
            health = maxHealth;

            aiArmorPath = new List<int>() { 1, 2, 3 }.GetRandomItem();
            aiArmorUpgradeOrder = new List<int>() { 0, 1, 2, 3 }.Shuffle();

            this.serverPlayer = serverPlayer;

            if (ownedByLocalPlayer && !isAI)
            {
                loadout = LoadoutData.createFromOptions(id);
            }
            else
            {
                loadout = LoadoutData.createRandom(id);
            }

            configureWeapons();

            is1v1Combatant = !isSpectator;
        }

        public int getHeartTankModifier()
        {
            return Helpers.clamp(Global.level.server?.customMatchSettings?.heartTankHp ?? 1, 1, 2);
        }

        public float getMaverickMaxHp()
        {
            if (!Global.level.is1v1() && isTagTeam())
            {
                //return 16 + (heartTanks * getHeartTankModifier());
                return 24 * getHealthModifier();
            }
            
            return 24 * getHealthModifier();
        }

        public bool hasAllItems()
        {
            return subtanks.Count >= 4 && heartTanks >= 8;
        }

        public float getHealthModifier()
        {
            var level = Global.level;
            float modifier = 1;
            if (level.is1v1())
            {
                if (Global.level.server.playTo == 1) modifier = 4f;
                if (Global.level.server.playTo == 2) modifier = 2f;
            }
            if (level.server.customMatchSettings != null)
            {
                modifier = level.server.customMatchSettings.healthModifier;
                if (level.gameMode.isTeamMode && alliance == GameMode.redAlliance)
                {
                    modifier = level.server.customMatchSettings.redHealthModifier;
                }
            }
            return modifier;
        }

        public float getDamageModifier()
        {
            if (Global.level.server.customMatchSettings != null)
            {
                if (Global.level.gameMode.isTeamMode && alliance == GameMode.redAlliance)
                {
                    return Global.level.server.customMatchSettings.redDamageModifier;
                }
                return Global.level.server.customMatchSettings.damageModifier;
            }
            return 1;
        }

        public float getMaxHealth()
        {
            // 1v1 is the only mode without possible heart tanks/sub tanks
            if (Global.level.is1v1())
            {
                return 32 * getHealthModifier();
            }
            int bonus = 0;
            if (isSigma && isPuppeteer()) bonus = 4;
            return (16 + bonus + (heartTanks * getHeartTankModifier())) * getHealthModifier();
        }

        public void creditHealing(float healAmount)
        {
            teamHealAmount += healAmount;
            if (teamHealAmount >= 16)
            {
                teamHealAmount = 0;
                scrap++;
            }
        }

        public void applyLoadoutChange()
        {
            loadout = LoadoutData.createFromOptions(id);
            if (Global.level.is1v1() && isSigma)
            {
                if (maverick1v1 != null) loadout.sigmaLoadout.commandMode = 3;
                else loadout.sigmaLoadout.commandMode = 2;
            }
            syncLoadout();
        }

        public void syncLoadout()
        {
            RPC.broadcastLoadout.sendRpc(this);
        }

        public int? teamAlliance
        {
            get
            {
                if (Global.level.gameMode.isTeamMode)
                {
                    return alliance;
                }
                return null;
            }
        }

        public int getHudLifeSpriteIndex()
        {
            return charNum + (maverick1v1 ?? -1) + 1;
        }

        public const int netIdsPerPlayer = 5000;

        // The first net id this player could possibly own. This includes the "reserved" ones
        public ushort getStartNetId()
        {
            return (ushort)(Level.maxReservedNetId + (ushort)(id * netIdsPerPlayer));
        }

        // The character net id is always the first net id of the player
        public ushort getCharActorNetId()
        {
            if (isLocalAI)
            {
                return Global.level.mainPlayer.getStartNetId();
            }

            return getStartNetId();
        }

        public static int? getPlayerIdFromCharNetId(ushort charNetId)
        {
            int netIdInt = charNetId;
            int maxIdInt = Level.maxReservedNetId;
            int diff = (netIdInt - maxIdInt);
            if (diff < 0) return null;
            if (diff % netIdsPerPlayer != 0)
            {
                return null;
            }
            netIdInt -= maxIdInt;
            return netIdInt / netIdsPerPlayer;
        }

        // First available unreserved net id for general instantiation use of new objects
        public ushort getFirstAvailableNetId()
        {
            // +0 = char
            // +1 = ride armor
            return (ushort)(getStartNetId() + 2);
        }

        // Usually, only the main player is allowed to get the next actor net id. The exception is if you call setNextActorNetId() first. The assert checks for that in debug.
        public ushort getNextActorNetId(bool allowNonMainPlayer = false)
        {
            if (isLocalAI)
            {
                return Global.level.mainPlayer.getStartNetId();
            }

            var retId = curMaxNetId;
            curMaxNetId++;

            if (curMaxNetId >= getStartNetId() + netIdsPerPlayer)
            {
                curMaxNetId = getFirstAvailableNetId();
            }

            return retId;
        }

        public void setNextActorNetId(ushort curMaxNetId)
        {
            this.curMaxNetId = curMaxNetId;
        }

        public bool isCrouchHeld()
        {
            if (isControllingPuppet())
            {
                return true;
            }

            if (character != null && !character.canCrouch())
            {
                return false;
            }

            if (!isAxl || Options.main.axlAimMode == 2)
            {
                return input.isHeld(Control.Down, this);
            }

            if (input.isHeld(Control.AxlCrouch, this))
            {
                return true;
            }

            if (Options.main.axlSeparateAimDownAndCrouch)
            {
                return input.isHeld(Control.Down, this);
            }

            if (Options.main.axlAimMode == 1)
            {
                return input.isHeld(Control.Down, this) && !input.isHeld(Control.AimAngleDown, this);
            }
            else
            {
                return input.isHeld(Control.Down, this) && !input.isHeld(Control.AimDown, this);
            }
        }

        public void update()
        {
            if (character != null) character.fgMotion = false;

            for (int i = copyShotDamageEvents.Count - 1; i >= 0; i--)
            {
                if (Global.time - copyShotDamageEvents[i].time > 2)
                {
                    copyShotDamageEvents.RemoveAt(i);
                }
            }

            for (int i = grenades.Count - 1; i >= 0; i--)
            {
                if (grenades[i].destroyed || grenades[i] == null)
                {
                    grenades.RemoveAt(i);
                }
            }

            readyTime += Global.spf;
            if (readyTime >= maxReadyTime)
            {
                readyTextOver = true;
            }

            if (Global.level.gameMode.isOver && aiTakeover)
            {
                aiTakeover = false;
                isAI = false;
                if (character != null) character.ai = null;
            }

            if (!Global.level.gameMode.isOver)
            {
                respawnTime -= Global.spf;
            }

            if (ownedByLocalPlayer && Global.isOnFrame(30))
            {
                RPC.updatePlayer.sendRpc(id, kills, deaths);
            }

            if (character == null && respawnTime <= 0 && eliminated())
            {
                if (Global.serverClient != null && isMainPlayer)
                {
                    RPC.makeSpectator.sendRpc(id, true);
                }
                else
                {
                    isSpectator = true;
                }
                return;
            }

            vileFormToRespawnAs = 0;
            hyperSigmaRespawn = false;
            hyperXRespawn = false;

            if (isVile)
            {
                int maxRAIndex = (character?.isVileMK2 == true || character?.isVileMK5 == true) ? 4 : 3;
                if (isSelectingRA())
                {
                    if (input.isPressedMenu(Control.MenuDown))
                    {
                        selectedRAIndex--;
                        if (selectedRAIndex < 0) selectedRAIndex = maxRAIndex;
                    }
                    else if (input.isPressedMenu(Control.MenuUp))
                    {
                        selectedRAIndex++;
                        if (selectedRAIndex > maxRAIndex) selectedRAIndex = 0;
                    }
                }

                if (canReviveVile())
                {
                    if (input.isPressed(Control.Special1, this) || Global.shouldAiAutoRevive)
                    {
                        reviveVile(false);
                    }
                    else if (input.isPressed(Control.Shoot, this) && !lastDeathWasVileMK2)
                    {
                        reviveVile(true);
                    }
                }
            }
            else if (isSigma)
            {
                if (isSelectingCommand())
                {
                    if (maverickWeapon.selCommandIndexX == 1)
                    {
                        if (input.isPressedMenu(Control.MenuDown))
                        {
                            maverickWeapon.selCommandIndex--;
                            if (maverickWeapon.selCommandIndex < 1)
                            {
                                maverickWeapon.selCommandIndex = MaverickWeapon.maxCommandIndex;
                            }
                        }
                        else if (input.isPressedMenu(Control.MenuUp))
                        {
                            maverickWeapon.selCommandIndex++;
                            if (maverickWeapon.selCommandIndex > MaverickWeapon.maxCommandIndex) maverickWeapon.selCommandIndex = 1;
                        }
                        
                        /*
                        if (maverickWeapon.selCommandIndex == 2)
                        {
                            if (input.isPressedMenu(Control.Left))
                            {
                                maverickWeapon.selCommandIndexX--;
                            }
                            else if (input.isPressedMenu(Control.Right))
                            {
                                maverickWeapon.selCommandIndexX++;
                            }
                        }
                        */
                    }
                    else
                    {
                        if (input.isPressedMenu(Control.Left) && maverickWeapon.selCommandIndexX == 2)
                        {
                            maverickWeapon.selCommandIndexX = 1;
                        }
                        else if (input.isPressedMenu(Control.Right) && maverickWeapon.selCommandIndexX == 0)
                        {
                            maverickWeapon.selCommandIndexX = 1;
                        }
                    }
                }

                if (canReviveSigma(out var spawnPoint) && (input.isPressed(Control.Special1, this) || Global.level.isHyper1v1() || Global.shouldAiAutoRevive))
                {
                    reviveSigma(spawnPoint);
                }
            }
            else if (isX)
            {
                if (canReviveX() && (input.isPressed(Control.Special1, this) || Global.shouldAiAutoRevive))
                {
                    reviveX();
                }
            }

            // Never spawn a character if it already exists
            if (character == null)
            {
                bool sendRpc = ownedByLocalPlayer;
                var charNetId = getCharActorNetId();
                if (shouldRespawn())
                {
                    var spawnPoint = Global.level.getSpawnPoint(this, !warpedInOnce);
                    if (spawnPoint == null) return;
                    int spawnPointIndex = Global.level.spawnPoints.IndexOf(spawnPoint);
                    spawnCharAtSpawnIndex(spawnPointIndex, charNetId, sendRpc);
                }
            }

            updateWeapons();
        }

        public bool eliminated()
        {
            if (Global.level.gameMode is Elimination || Global.level.gameMode is TeamElimination)
            {
                if (!isSpectator && (deaths >= Global.level.gameMode.playingTo || (Global.level.isNon1v1Elimination() && serverPlayer?.joinedLate == true)))
                {
                    return true;
                }
            }
            return false;
        }

        public bool shouldRespawn()
        {
            if (character != null) return false;
            if (respawnTime > 0) return false;
            if (!ownedByLocalPlayer) return false;
            if (isSpectator) return false;
            if (eliminated()) return false;
            if (isAI) return true;
            if (Global.level.is1v1()) return true;
            if (!spawnedOnce)
            {
                spawnedOnce = true;
                return true;
            }
            if (!Menu.inMenu && input.isPressedMenu(Control.MenuSelectPrimary))
            {
                return true;
            }
            if (respawnTime < -10)
            {
                return true;
            }
            return false;
        }

        public void spawnCharAtSpawnIndex(int spawnPointIndex, ushort charNetId, bool sendRpc)
        {
            if (Global.level.spawnPoints == null || spawnPointIndex >= Global.level.spawnPoints.Count)
            {
                return;
            }

            var spawnPoint = Global.level.spawnPoints[spawnPointIndex];

            spawnCharAtPoint(new Point(spawnPoint.pos.x, spawnPoint.getGroundY()), spawnPoint.xDir, charNetId, sendRpc);
        }

        public void spawnCharAtPoint(Point pos, int xDir, ushort charNetId, bool sendRpc)
        {
            if (sendRpc)
            {
                RPC.spawnCharacter.sendRpc(pos, xDir, id, charNetId);
            }

            if (Global.level.gameMode.isTeamMode)
            {
                alliance = newAlliance;
            }

            // ONRESPAWN, SPAWN, RESPAWN, ON RESPAWN, ON SPAWN LOGIC, SPAWNLOGIC
            charNum = newCharNum;
            if (isMainPlayer)
            {
                previousLoadout = loadout;
                applyLoadoutChange();
                hyperChargeSlot = Global.level.is1v1() ? 0 : Options.main.hyperChargeSlot;
                currentMaverickCommand = Options.main.maverickStartFollow ? MaverickAIBehavior.Follow : MaverickAIBehavior.Defend;
            }
            else if (isAI && Global.level.isTraining())
            {
                previousLoadout = loadout;
                applyLoadoutChange();
                hyperChargeSlot = Global.level.is1v1() ? 0 : Options.main.hyperChargeSlot;
                currentMaverickCommand = Options.main.maverickStartFollow ? MaverickAIBehavior.Follow : MaverickAIBehavior.Defend;
            }

            configureWeapons();
            maxHealth = getMaxHealth();
            if (isSigma)
            {
                if (isSigma1()) sigmaAmmo = sigmaMaxAmmo;
                else if (isSigma2()) sigmaAmmo = 0;
            }
            health = maxHealth;
            assassinHitPos = null;

            if (character == null)
            {
                bool mk2VileOverride = false;
                // Hyper mode overrides (PRE)
                if (Global.level.isHyper1v1() && ownedByLocalPlayer)
                {
                    if (isVile)
                    {
                        mk2VileOverride = true;
                        scrap = 9999;
                    }
                }

                character = new Character(this, pos.x, pos.y, xDir, false, charNetId, ownedByLocalPlayer, mk2VileOverride: mk2VileOverride, mk5VileOverride: false);

                // Hyper mode overrides (POST)
                if (Global.level.isHyper1v1() && ownedByLocalPlayer)
                {
                    if (isX)
                    {
                        setUltimateArmor(true);
                    }
                    if (isZero)
                    {
                        if (loadout.zeroLoadout.hyperMode == 0)
                        {
                            character.blackZeroTime = 100000;
                            character.hyperZeroUsed = true;
                        }
                        else
                        {
                            character.awakenedZeroTime = Global.spf;
                            character.hyperZeroUsed = true;
                            scrap = 9999;
                        }
                    }
                    if (isAxl)
                    {
                        if (loadout.axlLoadout.hyperMode == 0)
                        {
                            character.whiteAxlTime = 100000;
                            character.hyperAxlUsed = true;
                            var db = new DoubleBullet();
                            weapons[0] = db;
                        }
                        else
                        {
                            character.stingChargeTime = 8;
                            character.hyperAxlUsed = true;
                            scrap = 9999;
                        }
                    }
                }

                lastCharacter = character;
            }

            if (isAI)
            {
                character.addAI();
            }

            if (character.rideArmor != null) character.rideArmor.xDir = xDir;

            if (isCamPlayer)
            {
                Global.level.snapCamPos(character.getCamCenterPos(), null);
                //console.log(Global.level.camX + "," + Global.level.camY);
            }
            warpedIn = true;
        }

        public float possessedTime;
        public const float maxPossessedTime = 12;
        public Player possesser;
        public void startPossess(Player possesser, bool sendRpc = false)
        {
            possessedTime = maxPossessedTime;
            this.possesser = possesser;
            if (sendRpc)
            {
                RPC.possess.sendRpc(possesser.id, id, false);
            }
        }

        public void possesseeUpdate()
        {
            if (Global.isOnFrameCycle(60) && character != null)
            {
                character.damageHistory.Add(new DamageEvent(possesser, 136, (int)ProjIds.Sigma2ViralPossess, true, Global.time));
            }

            float myMashValue = mashValue();
            possessedTime -= myMashValue;
            if (possessedTime < 0)
            {
                possessedTime = 0;
                RPC.possess.sendRpc(0, id, true);
            }
        }

        public void possesserUpdate()
        {
            if (character == null || character.destroyed) return;

            // Held section
            input.possessedControlHeld[Control.Left] = Global.input.isHeld(Control.Left, Global.level.mainPlayer);
            input.possessedControlHeld[Control.Right] = Global.input.isHeld(Control.Right, Global.level.mainPlayer);
            input.possessedControlHeld[Control.Up] = Global.input.isHeld(Control.Up, Global.level.mainPlayer);
            input.possessedControlHeld[Control.Down] = Global.input.isHeld(Control.Down, Global.level.mainPlayer);
            input.possessedControlHeld[Control.Jump] = Global.input.isHeld(Control.Jump, Global.level.mainPlayer);
            input.possessedControlHeld[Control.Dash] = Global.input.isHeld(Control.Dash, Global.level.mainPlayer);
            input.possessedControlHeld[Control.Taunt] = Global.input.isHeld(Control.Taunt, Global.level.mainPlayer);

            byte inputHeldByte = Helpers.boolArrayToByte(new bool[]
            {
                input.possessedControlHeld[Control.Left],
                input.possessedControlHeld[Control.Right],
                input.possessedControlHeld[Control.Up],
                input.possessedControlHeld[Control.Down],
                input.possessedControlHeld[Control.Jump],
                input.possessedControlHeld[Control.Dash],
                input.possessedControlHeld[Control.Taunt],
                false,
            });

            // Pressed section
            input.possessedControlPressed[Control.Left] = Global.input.isPressed(Control.Left, Global.level.mainPlayer);
            input.possessedControlPressed[Control.Right] = Global.input.isPressed(Control.Right, Global.level.mainPlayer);
            input.possessedControlPressed[Control.Up] = Global.input.isPressed(Control.Up, Global.level.mainPlayer);
            input.possessedControlPressed[Control.Down] = Global.input.isPressed(Control.Down, Global.level.mainPlayer);
            input.possessedControlPressed[Control.Jump] = Global.input.isPressed(Control.Jump, Global.level.mainPlayer);
            input.possessedControlPressed[Control.Dash] = Global.input.isPressed(Control.Dash, Global.level.mainPlayer);
            input.possessedControlPressed[Control.Taunt] = Global.input.isPressed(Control.Taunt, Global.level.mainPlayer);

            byte inputPressedByte = Helpers.boolArrayToByte(new bool[]
            {
                input.possessedControlPressed[Control.Left],
                input.possessedControlPressed[Control.Right],
                input.possessedControlPressed[Control.Up],
                input.possessedControlPressed[Control.Down],
                input.possessedControlPressed[Control.Jump],
                input.possessedControlPressed[Control.Dash],
                input.possessedControlPressed[Control.Taunt],
                false,
            });

            RPC.syncPossessInput.sendRpc(id, inputHeldByte, inputPressedByte);
        }

        public void unpossess(bool sendRpc = false)
        {
            possessedTime = 0;
            possesser = null;
            input.possessedControlHeld.Clear();
            input.possessedControlPressed.Clear();
            if (sendRpc)
            {
                RPC.possess.sendRpc(0, id, true);
            }
        }

        public bool isPossessed()
        {
            return possessedTime > 0;
        }

        public bool canBePossessed()
        {
            if (character == null || character.destroyed) return false;
            if (character.isCCImmune()) return false;
            if (character.flag != null) return false;
            if (possessedTime > 0) return false;
            if (character.isVaccinated()) return false;
            return true;
        }

        public void transformAxl(DNACore dnaCore)
        {
            disguise = new Disguise(dnaCore.name);
            charNum = dnaCore.charNum;
            
            oldArmorFlag = armorFlag;
            oldFrozenCastle = frozenCastle;
            oldSpeedDevil = speedDevil;
            oldUltimateArmor = ultimateArmor;

            armorFlag = dnaCore.armorFlag;
            frozenCastle = dnaCore.frozenCastle;
            speedDevil = dnaCore.speedDevil;
            ultimateArmor = dnaCore.ultimateArmor;

            if (ownedByLocalPlayer)
            {
                string json = JsonConvert.SerializeObject(new RPCAxlDisguiseJson(id, disguise.targetName));
                Global.serverClient?.rpc(RPC.axlDisguise, json);
            }

            maxHealth = dnaCore.maxHealth + (heartTanks * getHeartTankModifier());

            oldAxlLoadout = loadout;
            loadout = dnaCore.loadout;

            oldWeapons = weapons;
            weapons = new List<Weapon>(dnaCore.weapons);
            configureStaticWeapons();

            if (charNum == 1)
            {
                if (loadout.zeroLoadout.melee == 0) weapons.Add(new ZSaber(this));
                else if (loadout.zeroLoadout.melee == 1) weapons.Add(new KKnuckleWeapon(this));
                else weapons.Add(new ZeroBuster());
            }
            if (charNum == 4) weapons.Add(new SigmaMenuWeapon());
            weapons.Add(new AssassinBullet());
            weapons.Add(new UndisguiseWeapon());
            weaponSlot = 0;

            zeroGigaAttackWeapon.ammo = dnaCore.rakuhouhaAmmo;
            zeroDarkHoldWeapon.ammo = dnaCore.rakuhouhaAmmo;
            sigmaAmmo = dnaCore.rakuhouhaAmmo;

            bool isVileMK2 = charNum == 2 && dnaCore.hyperMode == DNACoreHyperMode.VileMK2;
            bool isVileMK5 = charNum == 2 && dnaCore.hyperMode == DNACoreHyperMode.VileMK5;
            var retChar = new Character(this, character.pos.x, character.pos.y, character.xDir, true, character.netId, true, isWarpIn: false, mk2VileOverride: isVileMK2, mk5VileOverride: isVileMK5);

            if (isVileMK5) retChar.vileForm = 2;
            else if (isVileMK2) retChar.vileForm = 1;

            retChar.addTransformAnim();

            if (isAI)
            {
                retChar.addAI();
            }

            retChar.xDir = character.xDir;
            //retChar.heal(maxHealth);

            character.changeState(new Idle(), true);
            character = retChar;

            if (charNum == 3)
            {
                character.axlSwapTime = 0.25f;
            }
            else
            {
                weapon.shootTime = 0.25f;
            }
            
            if (charNum == 1 && dnaCore.hyperMode == DNACoreHyperMode.BlackZero)
            {
                character.blackZeroTime = character.maxHyperZeroTime;
                RPC.playerToggle.sendRpc(id, RPCToggleType.SetBlackZero);
            }
            else if (charNum == 3 && dnaCore.hyperMode == DNACoreHyperMode.WhiteAxl)
            {
                character.whiteAxlTime = character.maxHyperAxlTime;
                RPC.playerToggle.sendRpc(id, RPCToggleType.SetWhiteAxl);
            }
            else if (charNum == 1 && dnaCore.hyperMode == DNACoreHyperMode.AwakenedZero)
            {
                character.awakenedZeroTime = Global.spf;
            }
            else if (charNum == 1 && dnaCore.hyperMode == DNACoreHyperMode.NightmareZero)
            {
                character.isNightmareZero = true;
            }
        }

        // If you change this method change revertToAxlDeath() too
        public void revertToAxl()
        {
            disguise = null;

            if (ownedByLocalPlayer)
            {
                string json = JsonConvert.SerializeObject(new RPCAxlDisguiseJson(id, ""));
                Global.serverClient?.rpc(RPC.axlDisguise, json);
            }

            var oldPos = character.pos;
            var oldDir = character.xDir;
            character.destroySelf();
            Global.level.gameObjects.Add(preTransformedAxl);
            character = preTransformedAxl;
            character.addTransformAnim();
            preTransformedAxl = null;
            charNum = 3;
            character.pos = oldPos;
            character.xDir = oldDir;
            maxHealth = getMaxHealth();
            health = Math.Min(health, maxHealth);
            loadout = oldAxlLoadout;
            weapons = oldWeapons;
            configureStaticWeapons();
            weaponSlot = 0;
            
            armorFlag = oldArmorFlag;
            speedDevil = oldSpeedDevil;
            frozenCastle = oldFrozenCastle;
            ultimateArmor = oldUltimateArmor;

            if (weapons != null)
            {
                foreach (var weapon in weapons)
                {
                    if (!weapon.isCmWeapon())
                    {
                        weapon.ammo = weapon.maxAmmo;
                    }
                }
            }
            character.changeSpriteFromName("idle", true);
        }

        // If you change this method change revertToAxl() too
        public void revertToAxlDeath()
        {
            disguise = null;

            if (ownedByLocalPlayer)
            {
                string json = JsonConvert.SerializeObject(new RPCAxlDisguiseJson(id, ""));
                Global.serverClient?.rpc(RPC.axlDisguise, json);
            }

            preTransformedAxl = null;
            charNum = 3;
            maxHealth = 16;
            health = Math.Min(health, maxHealth);
            loadout = oldAxlLoadout;
            configureWeapons();
            weaponSlot = 0;
            
            armorFlag = oldArmorFlag;
            speedDevil = oldSpeedDevil;
            frozenCastle = oldFrozenCastle;
            ultimateArmor = oldUltimateArmor;

            character.addTransformAnim();
        }

        public bool isMainPlayer
        {
            get { return Global.level.mainPlayer == this; }
        }

        public bool isCamPlayer
        {
            get { return this == Global.level.camPlayer; }
        }

        public bool hasArmor()
        {
            return bodyArmorNum > 0 || bootsArmorNum > 0 || armArmorNum > 0 || helmetArmorNum > 0;
        }

        public bool hasArmor(int version)
        {
            return bodyArmorNum == version || bootsArmorNum == version || armArmorNum == version || helmetArmorNum == version;
        }

        public bool hasAllArmor()
        {
            return bodyArmorNum > 0 && bootsArmorNum > 0 && armArmorNum > 0 && helmetArmorNum > 0;
        }

        public bool hasAllX3Armor()
        {
            return bodyArmorNum >= 3 && bootsArmorNum >= 3 && armArmorNum >= 3 && helmetArmorNum >= 3;
        }

        public bool canUpgradeGoldenX()
        {
            return character != null && isX && !isDisguisedAxl && character.charState is not Die && !Global.level.is1v1() && hasAllX3Armor() && !hasAnyChip() && !hasUltimateArmor() && !hasGoldenArmor() && scrap >= 5 && !usedChipOnce;
        }

        public bool canUpgradeUltimateX()
        {
            return character != null && isX && !isDisguisedAxl && character.charState is not Die && !Global.level.is1v1() && !hasUltimateArmor() && !canUpgradeGoldenX() && hasAllArmor() && scrap >= 10;
        }

        public void destroy()
        {
            character?.destroySelf();
            character = null;
            removeOwnedActors();
        }

        public void removeOwnedActors()
        {
            foreach (var go in Global.level.gameObjects)
            {
                if (go is Actor actor && actor.netOwner == this && actor.cleanUpOnPlayerLeave())
                {
                    actor.destroySelf();
                }
            }
        }

        public List<MagnetMineProj> magnetMines = new List<MagnetMineProj>();
        public void removeOwnedMines()
        {
            for (int i = magnetMines.Count - 1; i >= 0; i--)
            {
                magnetMines[i].destroySelf();
            }
        }

        public List<RaySplasherTurret> turrets = new List<RaySplasherTurret>();
        public void removeOwnedTurrets()
        {
            for (int i = turrets.Count - 1; i >= 0; i--)
            {
                turrets[i].destroySelf();
            }
        }

        public List<GrenadeProj> grenades = new List<GrenadeProj>();
        public void removeOwnedGrenades()
        {
            for (int i = grenades.Count - 1; i >= 0; i--)
            {
                grenades[i].destroySelf();
            }
            grenades.Clear();
        }

        public List<ChillPIceStatueProj> iceStatues = new List<ChillPIceStatueProj>();
        public void removeOwnedIceStatues()
        {
            for (int i = iceStatues.Count - 1; i >= 0; i--)
            {
                iceStatues[i].destroySelf();
            }
            iceStatues.Clear();
        }

        public List<WSpongeSpike> seeds = new List<WSpongeSpike>();
        public void removeOwnedSeeds()
        {
            for (int i = seeds.Count - 1; i >= 0; i--)
            {
                seeds[i].destroySelf();
            }
            seeds.Clear();
        }

        public List<Actor> mechaniloids = new List<Actor>();
        public void removeOwnedMechaniloids()
        {
            for (int i = mechaniloids.Count - 1; i >= 0; i--)
            {
                mechaniloids[i].destroySelf();
            }
        }

        public int tankMechaniloidCount()
        {
            return mechaniloids.Count(m => m is Mechaniloid ml && ml.type == MechaniloidType.Tank);
        }

        public int hopperMechaniloidCount()
        {
            return mechaniloids.Count(m => m is Mechaniloid ml && ml.type == MechaniloidType.Hopper);
        }

        public int birdMechaniloidCount()
        {
            return mechaniloids.Count(m => m is BirdMechaniloidProj);
        }

        public int fishMechaniloidCount()
        {
            return mechaniloids.Count(m => m is Mechaniloid ml && ml.type == MechaniloidType.Fish);
        }

        public bool canControl
        {
            get
            {
                if (Global.level.gameMode.isOver)
                {
                    return false;
                }
                if (!isAI && Menu.inChat)
                {
                    return false;
                }
                if (!isAI && Menu.inMenu)
                {
                    return false;
                }
                if (character != null && currentMaverick == null)
                {
                    InRideArmor inRideArmor = character?.charState as InRideArmor;
                    if (inRideArmor != null && (inRideArmor.frozenTime > 0 || inRideArmor.stunTime > 0 || inRideArmor.crystalizeTime > 0))
                    {
                        return false;
                    }
                    if (character.shotgunIceChargeTime > 0 || character.charState is Frozen)
                    {
                        return false;
                    }
                    if (character.charState is Stunned || character.charState is Crystalized)
                    {
                        return false;
                    }
                    if (character.aiming)
                    {
                        return false;
                    }
                    if (character.rideArmor?.rideArmorState is RADropIn)
                    {
                        return false;
                    }
                }
                if (isSigma && character != null && character.tagTeamSwapProgress > 0)
                {
                    return false;
                }
                if (isPossessed())
                {
                    return false;
                }
                /*
                if (character?.charState?.isGrabbedState == true)
                {
                    return false;
                }
                */
                return true;
            }
        }

        public void awardScrap()
        {
            if (axlBulletType == (int)AxlBulletWeaponType.AncientGun && isAxl) return;
            if (character?.isCCImmuneHyperMode() == true) return;
            if (character != null && (character.isNightmareZero)) return;
            if (character != null && character.isBlackZero2()) return;
            if (character != null && character.rideArmor != null && character.charState is InRideArmor && character.rideArmor.raNum == 4) return;
            if (isX && hasUltimateArmor()) return;
            //if (isX && hasAnyChip() && !hasGoldenArmor()) return;
            //if (isX && hasGoldenArmor()) return;
            if (Global.level.is1v1()) return;

            if (isZero || isVile) fillSubtank(2);
            if (isAxl) fillSubtank(3);
            if (isX || isSigma) fillSubtank(4);

            scrap++;
        }

        public int getStartScrap()
        {
            if (Global.level.levelData.isTraining() || Global.anyQuickStart) return 9999;
            if (Global.level?.server?.customMatchSettings != null) return Global.level.server.customMatchSettings.startScrap;
            return 3;
        }

        public int getRespawnTime()
        {
            if (Global.level.isTraining() || Global.level.isRace())
            {
                return 2;
            }
            if (Global.level.gameMode is ControlPoints && alliance == GameMode.redAlliance)
            {
                return 8;
            }
            if (Global.level.gameMode is KingOfTheHill)
            {
                return 7;
            }
            return 5;
        }

        ExplodeDieEffect explodeDieEffect;
        public Character limboChar;

        public bool canReviveVile()
        {
            return !Global.level.isElimination() && limboChar != null && lastDeathCanRevive && isVile && newCharNum == 2 && scrap >= reviveVileScrapCost && !lastDeathWasVileMK5 && !limboChar.summonedGoliath;
        }

        public bool canReviveSigma(out Point spawnPoint)
        {
            spawnPoint = Point.zero;
            if (Global.level.isHyper1v1() && !lastDeathWasSigmaHyper && limboChar != null && isSigma && newCharNum == 4)
            {
                return true;
            }

            bool basicCheck = !Global.level.isElimination() && limboChar != null && lastDeathCanRevive && isSigma && newCharNum == 4 && scrap >= reviveSigmaScrapCost && !lastDeathWasSigmaHyper;
            if (!basicCheck) return false;

            if (isSigma1())
            {
                Point deathPos = limboChar.pos;

                // Get ground snapping pos
                var rect = new Rect(deathPos.addxy(-7, 0), deathPos.addxy(7, 112));
                var hits = Global.level.checkCollisionsShape(rect.getShape(), null);
                Point? closestHitPoint = Helpers.getClosestHitPoint(hits, deathPos, typeof(Wall));
                if (closestHitPoint != null)
                {
                    deathPos = new Point(deathPos.x, closestHitPoint.Value.y);
                }
                else
                {
                    if (isSigma1())
                    {
                        return false;
                    }
                }

                // Check if ample space to revive in
                int w = 10;
                int h = 120;
                rect = new Rect(new Point(deathPos.x - w / 2, deathPos.y - h), new Point(deathPos.x + w / 2, deathPos.y - 25));
                hits = Global.level.checkCollisionsShape(rect.getShape(), null);
                if (hits.Any(h => h.gameObject is Wall))
                {
                    return false;
                }

                if (deathPos.x - 100 < 0 || deathPos.x + 100 > Global.level.width)
                {
                    return false;
                }
                foreach (var player in Global.level.players)
                {
                    if (player.character?.isHyperSigmaBS.getValue() == true && player.isSigma1Or3() && player.character.pos.distanceTo(deathPos) < Global.screenW)
                    {
                        return false;
                    }
                }
            }
            else if (isSigma2())
            {
                return true;
            }
            else if (isSigma3())
            {
                return limboChar != null && limboChar.canKaiserSpawn(out spawnPoint);
            }

            return true;
        }

        public bool canReviveX()
        {
            return !Global.level.isElimination() && armorFlag == 0 && character?.charState is Die && lastDeathCanRevive && isX && newCharNum == 0 && scrap >= reviveXScrapCost && !lastDeathWasXHyper;
        }

        public void reviveVile(bool toMK5)
        {
            scrap -= reviveVileScrapCost;
            if (toMK5)
            {
                vileFormToRespawnAs = 2;
            }
            else if (limboChar.vileForm == 0)
            {
                vileFormToRespawnAs = 1;
            }
            else if (limboChar.vileForm == 1)
            {
                vileFormToRespawnAs = 2;
            }
            
            respawnTime = 0;
            character = limboChar;
            character.alreadySummonedNewMech = false;
            character.visible = true;
            if (explodeDieEffect != null)
            {
                explodeDieEffect.destroySelf();
                explodeDieEffect = null;
            }
            limboChar = null;
            if (!weapons.Any(w => w is MechMenuWeapon))
            {
                weapons.Add(new MechMenuWeapon(VileMechMenuType.All));
            }
            character.changeState(new VileRevive(vileFormToRespawnAs == 2), true);
            RPC.playerToggle.sendRpc(id, vileFormToRespawnAs == 2 ? RPCToggleType.ReviveVileTo5 : RPCToggleType.ReviveVileTo2);
        }

        public void reviveVileNonOwner(bool toMK5)
        {
            if (toMK5)
            {
                vileFormToRespawnAs = 2;
            }
            else
            {
                vileFormToRespawnAs = 1;
            }

            respawnTime = 0;
            character.alreadySummonedNewMech = false;
            character.visible = true;
            if (explodeDieEffect != null)
            {
                explodeDieEffect.destroySelf();
                explodeDieEffect = null;
            }
            character.changeState(new VileRevive(toMK5), true);
        }

        public void reviveSigma(Point spawnPoint)
        {
            scrap -= reviveSigmaScrapCost;
            hyperSigmaRespawn = true;
            respawnTime = 0;
            character = limboChar;
            limboChar = null;
            clearSigmaWeapons();
            maxHealth = 32 * getHealthModifier();
            if (isSigma1())
            {
                if (Global.level.is1v1())
                {
                    character.changePos(new Point(Global.level.width / 2, character.pos.y));
                }
                character.changeState(new WolfSigmaRevive(explodeDieEffect), true);
            }
            else if (isSigma2())
            {
                explodeDieEffect.changeSprite("sigma2_revive");
                character.changeState(new ViralSigmaRevive(explodeDieEffect), true);
            }
            else if (isSigma3())
            {
                explodeDieEffect.changeSprite("sigma3_revive");
                if (Global.level.is1v1() && spawnPoint.isZero())
                {
                    var closestSpawn = Global.level.spawnPoints.OrderBy(s => s.pos.distanceTo(character.pos)).FirstOrDefault();
                    spawnPoint = closestSpawn?.pos ?? new Point(Global.level.width / 2, character.pos.y);
                }
                character.changeState(new KaiserSigmaRevive(explodeDieEffect, spawnPoint), true);
            }
        }

        public void reviveX()
        {
            scrap -= reviveXScrapCost;
            hyperXRespawn = true;
            respawnTime = 0;
            character.changeState(new XReviveStart(), true);
        }

        public void reviveXNonOwner()
        {
        }

        public void explodeDieStart()
        {
            respawnTime = getRespawnTime(); // * (suicided ? 2 : 1);
            randomTip = Tips.getRandomTip(charNum);

            explodeDieEffect = ExplodeDieEffect.createFromActor(character.player, character, 20, 1.5f, false);
            Global.level.addEffect(explodeDieEffect);
            limboChar = character;
            character = null;
        }

        public void explodeDieEnd()
        {
            if (limboChar != null)
            {
                limboChar.destroySelf();
                limboChar = null;
            }
            explodeDieEffect = null;
            Global.serverClient?.rpc(RPC.destroyCharacter, (byte)id);
        }

        public void destroySigmaEffect()
        {
            ExplodeDieEffect.createFromActor(this, character, 25, 2, false);
        }

        public void destroySigma()
        {
            respawnTime = getRespawnTime();// * (suicided ? 2 : 1);
            randomTip = Tips.getRandomTip(charNum);

            if (character == null)
            {
                return;
            }

            character.destroySelf();
            character = null;
            Global.serverClient?.rpc(RPC.destroyCharacter, (byte)id);
            onCharacterDeath();
        }

        public bool suicided;
        public void destroyCharacter()
        {
            respawnTime = getRespawnTime();// * (suicided ? 2 : 1);
            randomTip = Tips.getRandomTip(charNum);

            if (character == null)
            {
                return;
            }

            if (isAxl)
            {
                //axlBulletTypeBought[6] = false;
                //if (axlBulletType == (int)AxlBulletWeaponType.AncientGun) axlBulletType = 0;
            }

            if (isZero && awakenedScrapEnd != null && scrap >= awakenedScrapEnd)
            {
                scrap = awakenedScrapEnd.Value;
                awakenedScrapEnd = null;
            }

            if (!character.player.isVile && !character.player.isSigma)
            {
                character.playSound("die");
                /*
                if (character.player == Global.level.mainPlayer)
                {
                    Global.playSound("die");
                }
                else
                {
                    character.playSound("die");
                }
                */
                new DieEffect(character.getCenterPos(), charNum);
            }

            character.destroySelf();
            character = null;

            onCharacterDeath();
        }

        // Must be called on any character death
        public void onCharacterDeath()
        {
            if (delayedNewCharNum != null && Global.level.mainPlayer.charNum != delayedNewCharNum.Value)
            {
                Global.level.mainPlayer.newCharNum = delayedNewCharNum.Value;
                Global.serverClient?.rpc(RPC.switchCharacter, (byte)Global.level.mainPlayer.id, (byte)delayedNewCharNum.Value);
            }
            delayedNewCharNum = null;
            suicided = false;
            unpossess();
        }

        public void maverick1v1Kill()
        {
            character?.applyDamage(null, null, 1000, null);
            character?.destroySelf();
            character = null;
            respawnTime = getRespawnTime() * (suicided ? 2 : 1);
            suicided = false;
            randomTip = Tips.getRandomTip(charNum);
            maverick1v1Spawned = false;
        }

        public void forceKill()
        {
            if (maverick1v1 != null && Global.level.is1v1())
            {
                //character?.applyDamage(null, null, 1000, null);
                currentMaverick?.applyDamage(null, null, 1000, null);
                return;
            }

            if (currentMaverick != null && isTagTeam())
            {
                destroyCharacter();
            }
            else
            {
                character?.applyDamage(null, null, 1000, null);
            }
            foreach (var maverick in mavericks)
            {
                maverick.applyDamage(null, null, 1000, null);
            }
        }

        public bool isGridModeEnabled()
        {
            if (isAxl || isDisguisedAxl)
            {
                if (Options.main.useMouseAim) return false;
                if (Global.level.is1v1()) return Options.main.gridModeAxl > 0;
                return Options.main.gridModeAxl > 1;
            }
            else if (isX)
            {
                if (Global.level.is1v1()) return Options.main.gridModeX > 0;
                return Options.main.gridModeX > 1;
            }

            return false;
        }

        public Point[] gridModePoints()
        {
            if (weapons.Count < 2) return null;
            if (weapons.Count == 2) return new Point[] { new Point(0, 0), new Point(-1, 0) };
            if (weapons.Count == 3) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0) };
            if (weapons.Count == 4) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1) };
            if (weapons.Count == 5) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1) };
            if (weapons.Count == 6) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1), new Point(-1, -1) };
            if (weapons.Count == 7) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1), new Point(-1, -1), new Point(1, -1) };
            if (weapons.Count == 8) return new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1), new Point(-1, -1), new Point(1, -1), new Point(-1, 1) };
            if (weapons.Count == 9) return new Point[] { new Point(0, 0), new Point(-1, -1), new Point(0, -1), new Point(1, -1), new Point(-1, 0), new Point(1, 0), new Point(-1, 1), new Point(0, 1), new Point(1, 1) };
            if (weapons.Count >= 10) return new Point[] { new Point(0, 0), new Point(-1, -1), new Point(0, -1), new Point(1, -1), new Point(-1, 0), new Point(1, 0), new Point(-1, 1), new Point(0, 1), new Point(1, 1), new Point(2, 1) };
            return null;
        }

        // 0000 0000 0000 0000 [boots][body][helmet][arm]
        // 0000 = none, 0001 = x1, 0010 = x2, 0011 = x3, 1111 = chip
        public static int getArmorNum(int armorFlag, int armorIndex, bool isChipCheck)
        {
            List<string> bits = Convert.ToString(armorFlag, 2).Select(s => s.ToString()).ToList();
            while (bits.Count < 16)
            {
                bits.Insert(0, "0");
            }

            string bitStr = "";
            if (armorIndex == 0) bitStr = bits[0] + bits[1] + bits[2] + bits[3];
            if (armorIndex == 1) bitStr = bits[4] + bits[5] + bits[6] + bits[7];
            if (armorIndex == 2) bitStr = bits[8] + bits[9] + bits[10] + bits[11];
            if (armorIndex == 3) bitStr = bits[12] + bits[13] + bits[14] + bits[15];

            int retVal = Convert.ToInt32(bitStr, 2);
            if (retVal > 3 && !isChipCheck) retVal = 3;
            return retVal;
        }

        public void setArmorNum(int armorIndex, int val)
        {
            List<string> bits = Convert.ToString(armorFlag, 2).Select(s => s.ToString()).ToList();
            while (bits.Count < 16)
            {
                bits.Insert(0, "0");
            }

            List<string> valBits = Convert.ToString(val, 2).Select(s => s.ToString()).ToList();
            while (valBits.Count < 4)
            {
                valBits.Insert(0, "0");
            }

            int i = armorIndex * 4;
            bits[i] = valBits[0];
            bits[i + 1] = valBits[1];
            bits[i + 2] = valBits[2];
            bits[i + 3] = valBits[3];

            armorFlag = Convert.ToUInt16(string.Join("", bits), 2);
        }

        public void removeArmorNum(int armorIndex)
        {
            setArmorNum(armorIndex, 0);
        }

        public bool hasAnyChip()
        {
            return hasChip(0) || hasChip(1) || hasChip(2) || hasChip(3);
        }

        public bool hasChip(int armorIndex)
        {
            if (!hasAllX3Armor()) return false;
            return getArmorNum(armorFlag, armorIndex, true) == 15;
        }

        public void setChipNum(int armorIndex, bool remove)
        {
            if (!remove) usedChipOnce = true;
            setArmorNum(0, 3);
            setArmorNum(1, 3);
            setArmorNum(2, 3);
            setArmorNum(3, 3);
            setArmorNum(armorIndex, remove ? 3 : 15);
        }

        ushort savedArmorFlag;
        public void setGoldenArmor(bool addOrRemove)
        {
            if (addOrRemove)
            {
                savedArmorFlag = armorFlag;
                armorFlag = ushort.MaxValue;
            }
            else
            {
                armorFlag = savedArmorFlag;
            }
        }

        public bool hasGoldenArmor()
        {
            return armorFlag == ushort.MaxValue;
        }

        public void setUltimateArmor(bool addOrRemove)
        {
            if (addOrRemove)
            {
                ultimateArmor = true;
                addNovaStrike();
            }
            else
            {
                ultimateArmor = false;
                removeNovaStrike();
            }
        }

        public bool hasUltimateArmor()
        {
            return ultimateArmor;
        }

        public int bootsArmorNum
        {
            get { return getArmorNum(armorFlag, 0, false); }
            set { setArmorNum(0, value); }
        }
        public int bodyArmorNum
        {
            get { return getArmorNum(armorFlag, 1, false); }
            set { setArmorNum(1, value); }
        }
        public int helmetArmorNum
        {
            get { return getArmorNum(armorFlag, 2, false); }
            set { setArmorNum(2, value); }
        }
        public int armArmorNum
        {
            get { return getArmorNum(armorFlag, 3, false); }
            set { setArmorNum(3, value); }
        }

        public bool hasBootsArmor(int xGame) { return bootsArmorNum == xGame; }
        public bool hasBodyArmor(int xGame) { return bodyArmorNum == xGame; }
        public bool hasHelmetArmor(int xGame) { return helmetArmorNum == xGame; }
        public bool hasArmArmor(int xGame) { return armArmorNum == xGame; }

        public bool[] headArmorsPurchased = new bool[3] { false, false, false };
        public bool[] bodyArmorsPurchased = new bool[3] { false, false, false };
        public bool[] armArmorsPurchased = new bool[3] { false, false, false };
        public bool[] bootsArmorsPurchased = new bool[3] { false, false, false };

        public bool isHeadArmorPurchased(int xGame) { return headArmorsPurchased[xGame - 1]; }
        public bool isBodyArmorPurchased(int xGame) { return bodyArmorsPurchased[xGame - 1]; }
        public bool isArmArmorPurchased(int xGame) { return armArmorsPurchased[xGame - 1]; }
        public bool isBootsArmorPurchased(int xGame) { return bootsArmorsPurchased[xGame - 1]; }

        public void setHeadArmorPurchased(int xGame) { headArmorsPurchased[xGame - 1] = true; }
        public void setBodyArmorPurchased(int xGame) { bodyArmorsPurchased[xGame - 1] = true; }
        public void setArmArmorPurchased(int xGame) { armArmorsPurchased[xGame - 1] = true; }
        public void setBootsArmorPurchased(int xGame) { bootsArmorsPurchased[xGame - 1] = true; }

        public bool hasAllArmorsPurchased()
        {
            for (int i = 0; i < 3; i++)
            {
                if (!headArmorsPurchased[i]) return false;
                if (!bodyArmorsPurchased[i]) return false;
                if (!armArmorsPurchased[i]) return false;
                if (!bootsArmorsPurchased[i]) return false;
            }
            return true;
        }

        public bool hasAnyArmorPurchased()
        {
            for (int i = 0; i < 3; i++)
            {
                if (headArmorsPurchased[i]) return true;
                if (bodyArmorsPurchased[i]) return true;
                if (armArmorsPurchased[i]) return true;
                if (bootsArmorsPurchased[i]) return true;
            }
            return false;
        }

        public bool hasAnyArmor()
        {
            return bootsArmorNum > 0 || armArmorNum > 0 || bodyArmorNum > 0 || helmetArmorNum > 0;
        }

        public void press(string inputMapping)
        {
            string keyboard = "keyboard";
            int? control = Control.controllerNameToMapping[keyboard].GetValueOrDefault(inputMapping);
            if (control == null) return;
            Key key = (Key)control;
            input.keyPressed[key] = !input.keyHeld.ContainsKey(key) || !input.keyHeld[key];
            input.keyHeld[key] = true;
        }

        public void release(string inputMapping)
        {
            string keyboard = "keyboard";
            int? control = Control.controllerNameToMapping[keyboard].GetValueOrDefault(inputMapping);
            if (control == null) return;
            Key key = (Key)control;
            input.keyHeld[key] = false;
            input.keyPressed[key] = false;
        }

        public void clearAiInput()
        {
            input.keyHeld.Clear();
            input.keyPressed.Clear();
            if (character != null && character.ai.framesChargeHeld > 0)
            {
                press("shoot");
            }
            if (character != null)
            {
                if (character.ai.jumpTime > 0)
                {
                    press("jump");
                }
                else
                {
                    release("jump");
                }
            }
        }

        public bool dashPressed(out string dashControl)
        {
            dashControl = "";
            if (input.isPressed(Control.Dash, this))
            {
                dashControl = Control.Dash;
                return true;
            }
            else if (!Options.main.disableDoubleDash)
            {
                if (input.isPressed(Control.Left, this) && input.checkDoubleTap(Control.Left))
                {
                    dashControl = Control.Left;
                    return true;
                }
                else if (input.isPressed(Control.Right, this) && input.checkDoubleTap(Control.Right))
                {
                    dashControl = Control.Right;
                    return true;
                }
            }
            return false;
        }

        public bool isSpecialBuster()
        {
            return loadout.xLoadout.melee == 0 && (character == null || !character.isHyperX);
        }

        public bool isSpecialSaber()
        {
            return character?.isHyperX == true || loadout.xLoadout.melee == 1;
        }

        public bool chargeButtonHeld()
        {
            if (isX || isZero)
            {
                if (isX && isSpecialBuster() && input.isHeld(Control.Special1, this))
                {
                    return true;
                }
                return input.isHeld(Control.Shoot, this);
            }
            else if (isAxl)
            {
                return input.isHeld(Control.Special1, this);
            }
            return false;
        }

        public void promoteToHost()
        {
            if (this == Global.level.mainPlayer)
            {
                Global.serverClient.isHost = true;
                if (Global.level?.gameMode != null)
                {
                    Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("You were promoted to host.", null, null, true));
                }
                if (Global.level?.redFlag != null)
                {
                    Global.level.redFlag.takeOwnership();
                    Global.level.redFlag.pedestal?.takeOwnership();
                }
                if (Global.level?.blueFlag != null)
                {
                    Global.level.blueFlag.takeOwnership();
                    Global.level.blueFlag.pedestal?.takeOwnership();
                }
                foreach (var cp in Global.level.controlPoints)
                {
                    cp.takeOwnership();
                }
                Global.level?.hill?.takeOwnership();

                foreach (var player in Global.level.players)
                {
                    if (player.serverPlayer.isBot)
                    {
                        player.ownedByLocalPlayer = true;
                        player.isAI = true;
                        player.character?.addAI();
                        player.character?.takeOwnership();
                    }
                }
            }
            else
            {
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(name + " promoted to host.", null, null, true));
            }
        }

        public void addKill()
        {
            if (Global.serverClient == null)
            {
                kills++;
            }
            else if (ownedByLocalPlayer)
            {
                kills++;
                charNumToKills[realCharNum]++;
                RPC.updatePlayer.sendRpc(id, kills, deaths);
            }
        }

        public void addAssist()
        {
            assists++;
        }

        public void addDeath(bool isSuicide)
        {
            if (isSigma && maverick1v1 == null && Global.level.isHyper1v1() && !lastDeathWasSigmaHyper)
            {
                return;
            }

            suicided = isSuicide;
            if (Global.serverClient == null)
            {
                deaths++;
                if (isSuicide)
                {
                    kills = Helpers.clamp(kills - 1, 0, int.MaxValue);
                    scrap = Helpers.clamp(scrap - 1, 0, int.MaxValue);
                }
            }
            else if (ownedByLocalPlayer)
            {
                deaths++;
                if (isSuicide)
                {
                    kills = Helpers.clamp(kills - 1, 0, int.MaxValue);
                    scrap = Helpers.clamp(scrap - 1, 0, int.MaxValue);
                }
                RPC.updatePlayer.sendRpc(id, kills, deaths);
            }
        }

        public float lastMashAmount;
        public int lastMashAmountSetFrame;

        public float mashValue()
        {
            int mashCount = input.mashCount;
            if (isAI && character?.ai != null)
            {
                if (character.ai.mashType == 1)
                {
                    mashCount = Helpers.randomRange(0, 6) == 0 ? 1 : 0;
                }
                else if (character.ai.mashType == 2)
                {
                    mashCount = Helpers.randomRange(0, 3) == 0 ? 1 : 0;
                }
            }

            float healthPercent = 0.3333f + ((health / maxHealth) * 0.6666f);
            float mashAmount = (healthPercent * mashCount * 0.25f);

            if (Global.frameCount - lastMashAmountSetFrame > 10)
            {
                lastMashAmount = 0;
            }

            float prevLastMashAmount = lastMashAmount;
            lastMashAmount += mashAmount;
            if (mashAmount > 0 && prevLastMashAmount == 0)
            {
                lastMashAmountSetFrame = Global.frameCount;
            }

            return (Global.spf + mashAmount);
        }

        public bool showHyperBusterCharge()
        {
            if (character?.flag != null) return false;
            return weapon is HyperBuster hb && hb.canShootIncludeCooldown(this);
        }

        public bool hasKnuckle()
        {
            return loadout?.zeroLoadout?.melee == 1;
        }

        public bool isZBusterZero()
        {
            return isZero && loadout.zeroLoadout.melee == 2;
        }

        // Sigma helper functions

        public bool isSigma1AndSigma()
        {
            return isSigma1() && isSigma;
        }

        public bool isSigma2AndSigma()
        {
            return isSigma2() && isSigma;
        }

        public bool isSigma3AndSigma()
        {
            return isSigma3() && isSigma;
        }

        public bool isSigma1()
        {
            return loadout?.sigmaLoadout?.sigmaForm == 0;
        }

        public bool isSigma2()
        {
            return loadout?.sigmaLoadout?.sigmaForm == 1;
        }

        public bool isSigma3()
        {
            return loadout?.sigmaLoadout?.sigmaForm == 2;
        }

        public bool isSigma1Or3()
        {
            return isSigma1() || isSigma3();
        }

        public bool isWolfSigma()
        {
            return isSigma && isSigma1() && character?.isHyperSigmaBS.getValue() == true;
        }

        public bool isViralSigma()
        {
            return isSigma && isSigma2() && character?.isHyperSigmaBS.getValue() == true;
        }
        
        public bool isKaiserSigma()
        {
            return isSigma && isSigma3() && character?.isHyperSigmaBS.getValue() == true;
        }

        public bool isKaiserViralSigma()
        {
            return character != null && character.sprite.name.StartsWith("sigma3_kaiser_virus");
        }

        public bool isKaiserNonViralSigma()
        {
            return isKaiserSigma() && !isKaiserViralSigma();
        }

        public bool isSummoner()
        {
            return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 0;
        }

        public bool isPuppeteer()
        {
            return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 1;
        }

        public bool isStriker()
        {
            return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 2;
        }

        public bool isTagTeam()
        {
            return loadout?.sigmaLoadout != null && loadout.sigmaLoadout.commandMode == 3;
        }

        public bool isRefundableMode()
        {
            return isSummoner() || isPuppeteer() || isTagTeam();
        }

        public bool isAlivePuppeteer()
        {
            return isPuppeteer() && health > 0;
        }

        public bool isControllingPuppet()
        {
            return isSigma && isPuppeteer() && currentMaverick != null && weapon is MaverickWeapon;
        }

        public bool hasSubtankCapacity()
        {
            var subtanks = this.subtanks;
            for (int i = 0; i < subtanks.Count; i++)
            {
                if (subtanks[i].health < SubTank.maxHealth)
                {
                    return true;
                }
            }
            return false;
        }

        public bool canUseSubtank(SubTank subtank)
        {
            if (isDead) return false;
            if (character.healAmount > 0) return false;
            if (health <= 0 || health >= maxHealth) return false;
            if (subtank.health <= 0) return false;
            if (character.charState is WarpOut) return false;
            if (character.charState.invincible) return false;
            if (character.isCStingInvisible()) return false;
            if (character.isHyperSigmaBS.getValue()) return false;

            return true;
        }

        public void fillSubtank(float amount)
        {
            if (character?.healAmount > 0) return;
            var subtanks = this.subtanks;
            for (int i = 0; i < subtanks.Count; i++)
            {
                if (subtanks[i].health < SubTank.maxHealth)
                {
                    subtanks[i].health += amount;
                    if (subtanks[i].health >= SubTank.maxHealth)
                    {
                        subtanks[i].health = SubTank.maxHealth;
                        if (isMainPlayer) Global.playSound("subtankFull");
                    }
                    else
                    {
                        if (isMainPlayer) Global.playSound("subtankFill");
                    }
                    break;
                }
            }
        }

        public bool isUsingSubTank()
        {
            return character?.usedSubtank != null;
        }

        public int getSpawnIndex(int spawnPointCount)
        {
            var nonSpecPlayers = Global.level.nonSpecPlayers();
            nonSpecPlayers = nonSpecPlayers.OrderBy(p => p.id).ToList();
            int index = nonSpecPlayers.IndexOf(this) % spawnPointCount;
            if (index < 0) index = 0;
            return index;
        }

        public void delaySubtank()
        {
            if (isMainPlayer)
            {
                UpgradeMenu.subtankDelay = UpgradeMenu.maxSubtankDelay;
            }
        }
    }

    [ProtoContract]
    public class Disguise
    {
        [ProtoMember(1)]
        public string targetName { get; set; }

        public Disguise() { }

        public Disguise(string name)
        {
            targetName = name;
        }
    }
}
