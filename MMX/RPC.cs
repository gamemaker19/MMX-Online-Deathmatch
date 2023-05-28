using Lidgren.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MMXOnline
{
    public class RPC
    {
        public NetDeliveryMethod netDeliveryMethod;
        public bool isString;
        public bool toHostOnly;
        public bool isServerMessage;

        // Need templates? Use these:
        // -Sending a value to an actor: RPCChangeDamage

        public static RPCSendString sendString;
        public static RPCStartLevel startLevel;
        public static RPCSpawnCharacter spawnCharacter;
        public static RPCUpdateActor updateActor;
        public static RPCApplyDamage applyDamage;
        public static RPCDecShieldAmmo decShieldAmmo;
        public static RPCShoot shoot;
        public static RPCShoot shootFast;
        public static RPCDestroyActor destroyActor;
        public static RPCPlayerToggle playerToggle;
        public static RPCDestroyPlayer destroyCharacter;
        public static RPCKillPlayer killPlayer;
        public static RPCCreateAnim createAnim;
        public static RPCCreateProj createProj;
        public static RPCCreateActor createActor;
        public static RPCSwitchCharacter switchCharacter;
        public static RPCReflectProj reflectProj;
        public static RPCJoinLateRequest joinLateRequest;
        public static RPCJoinLateResponse joinLateResponse;
        public static RPCUpdateStarted updateStarted;
        public static RPCHostPromotion hostPromotion;
        public static RPCMatchOver matchOver;
        public static RPCSwitchTeam switchTeam;
        public static RPCSyncTeamScores syncTeamScores;
        public static RPCSyncGameTime syncGameTime;
        public static RPCSyncSetupTime syncSetupTime;
        public static RPCSendKillFeedEntry sendKillFeedEntry;
        public static RPCSendChatMessage sendChatMessage;
        public static RPCSyncControlPoints syncControlPoints;
        public static RPCSetHyperZeroTime setHyperZeroTime;
        public static RPCAxlShoot axlShoot;
        public static RPCAxlDisguise axlDisguise;
        public static RPCReportPlayerRequest reportPlayerRequest;
        public static RPCReportPlayerResponse reportPlayerResponse;
        public static RPCKickPlayerRequest kickPlayerRequest;
        public static RPCKickPlayerResponse kickPlayerResponse;
        public static RPCVoteKickStart voteKickStart;
        public static RPCVoteKickEnd voteKickEnd;
        public static RPCVoteKick voteKick;
        public static RPCEndMatchRequest endMatchRequest;
        public static RPCPeriodicServerSync periodicServerSync;
        public static RPCPeriodicServerPing periodicServerPing;
        public static RPCPeriodicHostSync periodicHostSync;
        public static RPCUpdatePlayer updatePlayer;
        public static RPCAddBot addBot;
        public static RPCRemoveBot removeBot;
        public static RPCMakeSpectator makeSpectator;
        public static RPCSyncValue syncValue;
        public static RPCHeal heal;
        public static RPCCommandGrabPlayer commandGrabPlayer;
        public static RPCClearOwnership clearOwnership;
        public static RPCActorToggle actorToggle;
        public static RPCPlaySound playSound;
        public static RPCStopSound stopSound;
        public static RPCAddDamageText addDamageText;
        public static RPCSyncAxlBulletPos syncAxlBulletPos;
        public static RPCSyncAxlScopePos syncAxlScopePos;
        public static RPCBoundBlasterStick boundBlasterStick;
        public static RPCBroadcastLoadout broadcastLoadout;
        public static RPCCreditPlayerKillMaverick creditPlayerKillMaverick;
        public static RPCCreditPlayerKillVehicle creditPlayerKillVehicle;
        public static RPCChangeDamage changeDamage;
        public static RPCLogWeaponKills logWeaponKills;
        public static RPCCheckRAEnter checkRAEnter;
        public static RPCRAEnter raEnter;
        public static RPCCheckRCEnter checkRCEnter;
        public static RPCRCEnter rcEnter;
        public static RPCUseSubTank useSubtank;
        public static RPCPossess possess;
        public static RPCSyncPossessInput syncPossessInput;
        public static RPCFeedWheelGator feedWheelGator;
        public static RPCHealDoppler healDoppler;
        public static RPCResetFlag resetFlags;

        public static List<RPC> templates = new List<RPC>()
        {
            (sendString = new RPCSendString()),
            (startLevel = new RPCStartLevel()),
            (spawnCharacter = new RPCSpawnCharacter()),
            (updateActor = new RPCUpdateActor()),
            (applyDamage = new RPCApplyDamage()),
            (decShieldAmmo = new RPCDecShieldAmmo()),
            (shoot = new RPCShoot(NetDeliveryMethod.ReliableOrdered)),
            (shootFast = new RPCShoot(NetDeliveryMethod.Unreliable)),
            (destroyActor = new RPCDestroyActor()),
            (destroyCharacter = new RPCDestroyPlayer()),
            (playerToggle = new RPCPlayerToggle()),
            (killPlayer = new RPCKillPlayer()),
            (createAnim = new RPCCreateAnim()),
            (createProj = new RPCCreateProj()),
            (createActor = new RPCCreateActor()),
            (switchCharacter = new RPCSwitchCharacter()),
            (reflectProj = new RPCReflectProj()),
            (joinLateRequest = new RPCJoinLateRequest()),
            (joinLateResponse = new RPCJoinLateResponse()),
            (updateStarted = new RPCUpdateStarted()),
            (hostPromotion = new RPCHostPromotion()),
            (matchOver = new RPCMatchOver()),
            (switchTeam = new RPCSwitchTeam()),
            (syncTeamScores = new RPCSyncTeamScores()),
            (syncGameTime = new RPCSyncGameTime()),
            (syncSetupTime = new RPCSyncSetupTime()),
            (sendKillFeedEntry = new RPCSendKillFeedEntry()),
            (sendChatMessage = new RPCSendChatMessage()),
            (syncControlPoints = new RPCSyncControlPoints()),
            (setHyperZeroTime = new RPCSetHyperZeroTime()),
            (axlShoot = new RPCAxlShoot()),
            (axlDisguise = new RPCAxlDisguise()),
            (reportPlayerRequest = new RPCReportPlayerRequest()),
            (reportPlayerResponse = new RPCReportPlayerResponse()),
            (kickPlayerRequest = new RPCKickPlayerRequest()),
            (kickPlayerResponse = new RPCKickPlayerResponse()),
            (voteKickStart = new RPCVoteKickStart()),
            (voteKickEnd = new RPCVoteKickEnd()),
            (voteKick = new RPCVoteKick()),
            (endMatchRequest = new RPCEndMatchRequest()),
            (periodicServerSync = new RPCPeriodicServerSync()),
            (periodicServerPing = new RPCPeriodicServerPing()),
            (periodicHostSync = new RPCPeriodicHostSync()),
            (updatePlayer = new RPCUpdatePlayer()),
            (addBot = new RPCAddBot()),
            (removeBot = new RPCRemoveBot()),
            (makeSpectator = new RPCMakeSpectator()),
            (syncValue = new RPCSyncValue()),
            (heal = new RPCHeal()),
            (commandGrabPlayer = new RPCCommandGrabPlayer()),
            (clearOwnership = new RPCClearOwnership()),
            (actorToggle = new RPCActorToggle()),
            (playSound = new RPCPlaySound()),
            (stopSound = new RPCStopSound()),
            (addDamageText = new RPCAddDamageText()),
            (syncAxlBulletPos = new RPCSyncAxlBulletPos()),
            (syncAxlScopePos = new RPCSyncAxlScopePos()),
            (boundBlasterStick = new RPCBoundBlasterStick()),
            (broadcastLoadout = new RPCBroadcastLoadout()),
            (creditPlayerKillMaverick = new RPCCreditPlayerKillMaverick()),
            (creditPlayerKillVehicle = new RPCCreditPlayerKillVehicle()),
            (changeDamage = new RPCChangeDamage()),
            (logWeaponKills = new RPCLogWeaponKills()),
            (checkRAEnter = new RPCCheckRAEnter()),
            (raEnter = new RPCRAEnter()),
            (checkRCEnter = new RPCCheckRCEnter()),
            (rcEnter = new RPCRCEnter()),
            (useSubtank = new RPCUseSubTank()),
            (possess = new RPCPossess()),
            (syncPossessInput = new RPCSyncPossessInput()),
            (feedWheelGator = new RPCFeedWheelGator()),
            (healDoppler = new RPCHealDoppler()),
            (resetFlags = new RPCResetFlag()),
        };

        public virtual void invoke(params byte[] arguments)
        {
        }

        public virtual void invoke(string message)
        {
        }

        public void sendFromServer(NetServer s_server, byte[] bytes)
        {
            if (s_server.Connections.Count == 0) return;

            var om = s_server.CreateMessage();
            om.Write((byte)templates.IndexOf(this));
            if (bytes.Length > ushort.MaxValue)
            {
                throw new Exception("SendFromServer RPC bytes too big, max ushort.MaxValue");
            }
            ushort argCount = (ushort)bytes.Length;
            var argCountBytes = BitConverter.GetBytes(argCount);
            om.Write(argCountBytes[0]);
            om.Write(argCountBytes[1]);
            if (bytes.Length > 0)
            {
                om.Write(bytes);
            }
            s_server.SendMessage(om, s_server.Connections, netDeliveryMethod, 0);
        }
    }

    public class RPCSendString : RPC
    {
        public RPCSendString()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }
    }

    public class RPCStartLevel : RPC
    {
        public RPCStartLevel()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string message)
        {
            var rpcStartLevelJson = JsonConvert.DeserializeObject<RPCStartLevelJson>(message);

            // Sometimes server won't have player in it preventing mainPlayer from being set, in this case need to be a late joiner
            if (!rpcStartLevelJson.server.players.Any(p => p.id == Global.serverClient.serverPlayer.id))
            {
                Global.serverClient.disconnect("Host recreated before client could reconnect");
                Global.serverClient = null;
                Menu.change(new ErrorMenu(new string[] { "Could not reconnect in time.", "Please rejoin the server manually." }, new JoinMenu(false)));
                return;
            }

            Global.level.startLevel(rpcStartLevelJson.server, false);
        }
    }

    public class RPCStartLevelJson
    {
        public Server server;
        public RPCStartLevelJson(Server server)
        {
            this.server = server;
        }
    }

    public class BackloggedSpawns
    {
        public int playerId;
        public Point spawnPoint;
        public int xDir;
        public ushort charNetId;
        public float time;
        public BackloggedSpawns(int playerId, Point spawnPoint, int xDir, ushort charNetId)
        {
            this.playerId = playerId;
            this.spawnPoint = spawnPoint;
            this.xDir = xDir;
            this.charNetId = charNetId;
            time = 0;
        }
        public bool trySpawnPlayer()
        {
            var player = Global.level.getPlayerById(playerId);
            // Player could not exist yet if late joiner.
            if (player != null)
            {
                player.spawnCharAtPoint(spawnPoint, xDir, charNetId, false);
                return true;
            }
            return false;
        }
    }

    public class RPCSpawnCharacter : RPC
    {
        public RPCSpawnCharacter()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            float x = BitConverter.ToSingle(new byte[] { arguments[0], arguments[1], arguments[2], arguments[3] }, 0);
            float y = BitConverter.ToSingle(new byte[] { arguments[4], arguments[5], arguments[6], arguments[7] }, 0);
            int xDir = arguments[8] - 128;
            int playerId = arguments[9];
            ushort charNetId = BitConverter.ToUInt16(new byte[] { arguments[10], arguments[11] }, 0);

            var player = Global.level.getPlayerById(playerId);
            // Player could not exist yet if late joiner.
            if (player != null)
            {
                player.spawnCharAtPoint(new Point(x, y), xDir, charNetId, false);
            }
            else
            {
                Global.level.backloggedSpawns.Add(new BackloggedSpawns(playerId, new Point(x, y), xDir, charNetId));
            }
        }

        public void sendRpc(Point spawnPos, int xDir, int playerId, ushort charNetId)
        {
            if (Global.serverClient == null) return;

            byte[] xBytes = BitConverter.GetBytes(spawnPos.x);
            byte[] yBytes = BitConverter.GetBytes(spawnPos.y);
            byte[] netIdBytes = BitConverter.GetBytes(charNetId);

            Global.serverClient.rpc(this, xBytes[0], xBytes[1], xBytes[2], xBytes[3], yBytes[0], yBytes[1], yBytes[2], yBytes[3], (byte)(xDir + 128), (byte)playerId, netIdBytes[0], netIdBytes[1]);
        }
    }

    public class FailedSpawn
    {
        public Point spawnPos;
        public int xDir;
        public ushort netId;
        public float time;
        public FailedSpawn(Point spawnPos, int xDir, ushort netId)
        {
            this.spawnPos = spawnPos;
            this.xDir = xDir;
            this.netId = netId;
        }
    }

    public class RPCApplyDamage : RPC
    {
        public RPCApplyDamage()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int ownerId = arguments[0];
            float damage = BitConverter.ToSingle(new byte[] { arguments[1], arguments[2], arguments[3], arguments[4] }, 0);
            float hitCooldown = BitConverter.ToSingle(new byte[] { arguments[5], arguments[6], arguments[7], arguments[8] }, 0);
            int flinch = (int)arguments[9];
            ushort victimId = BitConverter.ToUInt16(new byte[] { arguments[10], arguments[11] }, 0);
            bool weakness = (int)arguments[12] == 1;
            int weaponIndex = arguments[13];
            int weaponKillFeedIndex = arguments[14];
            ushort actorId = BitConverter.ToUInt16(new byte[] { arguments[15], arguments[16] }, 0);
            ushort projId = BitConverter.ToUInt16(new byte[] { arguments[17], arguments[18] }, 0);

            var player = Global.level.getPlayerById(ownerId);
            var victim = Global.level.getActorByNetId(victimId);
            var actor = actorId == 0 ? null : Global.level.getActorByNetId(actorId);

            if (player != null && victim != null)
            {
                Damager.applyDamage(
                    player,
                    damage,
                    hitCooldown,
                    flinch,
                    victim,
                    weakness,
                    weaponIndex,
                    weaponKillFeedIndex,
                    actor,
                    projId,
                    sendRpc: false);
            }
        }

        public void sendRpc(byte[] byteArray)
        {
            Global.serverClient?.rpc(applyDamage, byteArray);
        }
    }

    public class BackloggedDamage
    {
        public ushort actorId;
        public Action<Actor> damageAction;
        public float time;
        public BackloggedDamage(ushort actorId, Action<Actor> damageAction)
        {
            this.actorId = actorId;
            this.damageAction = damageAction;
        }
    }

    public class RPCDecShieldAmmo : RPC
    {
        public RPCDecShieldAmmo()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            float decAmmoAmount = BitConverter.ToSingle(new byte[] { arguments[1], arguments[2], arguments[3], arguments[4] }, 0);

            var player = Global.level.getPlayerById(playerId);

            if (player?.character?.chargedRollingShieldProj != null)
            {
                player.character.chargedRollingShieldProj.decAmmo(decAmmoAmount);
            }
        }
    }

    public class RPCShoot : RPC
    {
        public RPCShoot(NetDeliveryMethod netDeliveryMethod)
        {
            this.netDeliveryMethod = netDeliveryMethod;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            int xPos = BitConverter.ToInt16(new byte[] { arguments[1], arguments[2] }, 0);
            int yPos = BitConverter.ToInt16(new byte[] { arguments[3], arguments[4] }, 0);
            int xDir = (int)arguments[5] - 128;
            int chargeLevel = (int)arguments[6];
            ushort projNetId = BitConverter.ToUInt16(new byte[] { arguments[7], arguments[8] }, 0);
            int weaponIndex = (int)arguments[9];

            var player = Global.level.getPlayerById(playerId);
            player?.character?.shootRpc(new Point(xPos, yPos), weaponIndex, xDir, chargeLevel, projNetId, false);
        }
    }

    public class BufferedDestroyActor
    {
        public ushort netId;
        public string destroySprite;
        public string destroySound;
        public float time;
        public BufferedDestroyActor(ushort netId, string destroySprite, string destroySound)
        {
            this.netId = netId;
            this.destroySprite = destroySprite;
            this.destroySound = destroySound;
        }
    }

    public class RPCDestroyActor : RPC
    {
        public RPCDestroyActor()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableUnordered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            int spriteIndex = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);
            int soundIndex = BitConverter.ToUInt16(new byte[] { arguments[4], arguments[5] }, 0);
            
            string destroySprite = null;
            string destroySound = null;
            if (spriteIndex < Global.spriteNames.Count) destroySprite = Global.spriteNames[spriteIndex];
            if (soundIndex < Global.soundNames.Count) destroySound = Global.soundNames[soundIndex];

            float x = BitConverter.ToSingle(new byte[] { arguments[6], arguments[7], arguments[8], arguments[9] }, 0);
            float y = BitConverter.ToSingle(new byte[] { arguments[10], arguments[11], arguments[12], arguments[13] }, 0);
            Point destroyPos = new Point(x, y);

            bool favorDefenderProjDestroy = arguments[14] == 1;
            float speed = BitConverter.ToSingle(new byte[] { arguments[15], arguments[16], arguments[17], arguments[18] }, 0);

            var actor = Global.level.getActorByNetId(netId);
            if (actor != null)
            {
                // Special case for favor the defender projectiles: give it time to move to its position of destruction, before destroying it
                if (actor is Projectile proj && speed > 0 && favorDefenderProjDestroy)
                {
                    proj.moveToPosThenDestroy(destroyPos, speed);
                    return;
                }

                if (actor is FrostShieldProj fsp)
                {
                    fsp.noSpawn = true;
                }

                actor.changePos(destroyPos);
                // Any actors with custom destroySelf methods that are invoked by RPC need to be specified here
                if (actor is Character)
                {
                    (actor as Character).destroySelf(destroySprite, destroySound, rpc: true);
                }
                else if (actor is RollingShieldProjCharged)
                {
                    (actor as RollingShieldProjCharged).destroySelf(destroySprite, destroySound, rpc: true);
                }
                else
                {
                    actor.destroySelf(destroySprite, destroySound, rpc: true);
                }
            }
            else
            {
                Global.level.bufferedDestroyActors.Add(new BufferedDestroyActor(netId, destroySprite, destroySound));
            }
        }

        public void sendRpc(ushort netId, ushort spriteIndex, ushort soundIndex, Point pos, bool favorDefenderProjDestroy, float speed)
        {
            var netIdBytes = BitConverter.GetBytes(netId);
            var spriteIndexBytes = BitConverter.GetBytes(spriteIndex);
            var soundIndexBytes = BitConverter.GetBytes(soundIndex);
            var xBytes = BitConverter.GetBytes(pos.x);
            var yBytes = BitConverter.GetBytes(pos.y);
            var speedBytes = BitConverter.GetBytes(speed);

            Global.serverClient?.rpc(RPC.destroyActor, netIdBytes[0], netIdBytes[1], spriteIndexBytes[0], spriteIndexBytes[1], soundIndexBytes[0], soundIndexBytes[1], 
                xBytes[0], xBytes[1], xBytes[2], xBytes[3], yBytes[0], yBytes[1], yBytes[2], yBytes[3],
                (byte)(favorDefenderProjDestroy ? 1 : 0),
                speedBytes[0], speedBytes[1], speedBytes[2], speedBytes[3]);
        }
    }

    public class RPCDestroyPlayer : RPC
    {
        public RPCDestroyPlayer()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableUnordered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];

            var player = Global.level.getPlayerById(playerId);
            player?.destroyCharacter();
        }
    }

    public enum RPCToggleType
    {
        AddTransformEffect,
        PlayDingSound,
        StartCrystalize,
        StopCrystalize,
        StrikeChainReversed,
        StockCharge,
        UnstockCharge,
        StartRaySplasher,
        StopRaySplasher,
        StartBarrier,
        StopBarrier,
        StockSaber,
        UnstockSaber,
        SetBlackZero,
        SetWhiteAxl,
        ReviveVileTo2,
        ReviveVileTo5,
        ReviveX,
        StartRev,
        StopRev,
    }

    public class RPCPlayerToggle : RPC
    {
        public RPCPlayerToggle()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            RPCToggleType toggleId = (RPCToggleType)(int)arguments[1];

            var player = Global.level.getPlayerById(playerId);
            if (player?.character == null)
            {
                return;
            }
            else if (toggleId == RPCToggleType.AddTransformEffect)
            {
                player.character?.addTransformAnim();
            }
            else if (toggleId == RPCToggleType.PlayDingSound)
            {
                player.character?.playSound("ding");
            }
            else if (toggleId == RPCToggleType.StartCrystalize)
            {
                player.character?.crystalizeStart();
            }
            else if (toggleId == RPCToggleType.StopCrystalize)
            {
                player.character?.crystalizeEnd();
            }
            else if (toggleId == RPCToggleType.StrikeChainReversed)
            {
                player.character?.strikeChainProj?.reverseDir();
            }
            else if (toggleId == RPCToggleType.StockCharge)
            {
                player.character.stockedCharge = true;
            }
            else if (toggleId == RPCToggleType.UnstockCharge)
            {
                player.character.stockedCharge = false;
            }
            else if (toggleId == RPCToggleType.StartRaySplasher)
            {
                player.character.isShootingRaySplasher = true;
            }
            else if (toggleId == RPCToggleType.StopRaySplasher)
            {
                player.character.isShootingRaySplasher = false;
            }
            else if (toggleId == RPCToggleType.StartBarrier)
            {
                player.character.barrierTime = player.character.barrierDuration;
            }
            else if (toggleId == RPCToggleType.StopBarrier)
            {
                player.character.barrierTime = 0;
            }
            else if (toggleId == RPCToggleType.StockSaber)
            {
                player.character.stockedXSaber = true;
            }
            else if (toggleId == RPCToggleType.UnstockSaber)
            {
                player.character.stockedXSaber = false;
            }
            else if (toggleId == RPCToggleType.SetBlackZero)
            {
                player.character.blackZeroTime = player.character.maxHyperZeroTime;
            }
            else if (toggleId == RPCToggleType.SetWhiteAxl)
            {
                player.character.whiteAxlTime = player.character.maxHyperAxlTime;
            }
            else if (toggleId == RPCToggleType.ReviveVileTo2)
            {
                player.reviveVileNonOwner(false);
            }
            else if (toggleId == RPCToggleType.ReviveVileTo5)
            {
                player.reviveVileNonOwner(true);
            }
            else if (toggleId == RPCToggleType.ReviveX)
            {
                player.reviveXNonOwner();
            }
            else if (toggleId == RPCToggleType.StartRev)
            {
                player.character.isNonOwnerRev = true;
            }
            else if (toggleId == RPCToggleType.StopRev)
            {
                player.character.isNonOwnerRev = false;
            }
        }

        public void sendRpc(int playerId, RPCToggleType toggleType)
        {
            Global.serverClient?.rpc(this, (byte)playerId, (byte)toggleType);
        }
    }

    public enum RPCActorToggleType
    {
        SonicSlicerBounce,
        StartGravityWell,
        CrackedWallDamage,
        CrackedWallDestroy,
        AddWolfSigmaMusicSource,
        AddDrLightMusicSource,
        AddDrDopplerMusicSource,
        AddGoliathMusicSource,
        StartMechSelfDestruct,
        ShakeCamera,
        ReverseRocketPunch,
        DropFlagManual,
        AwardScrap,
        AddViralSigmaMusicSource,
        MorphMothCocoonSelfDestruct,
        AddKaiserSigmaMusicSource,
        AddKaiserViralSigmaMusicSource,
        ChangeToParriedState,
        KaiserShellFadeOut,
        AddVaccineTime,
        ActivateBlackZero2,
    }

    public class RPCActorToggle : RPC
    {
        public RPCActorToggle()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            RPCActorToggleType toggleId = (RPCActorToggleType)arguments[2];

            // A hack to avoid having to create new RPC and redeploy server code
            if (toggleId == RPCActorToggleType.CrackedWallDamage)
            {
                if (Global.isHost)
                {
                    byte crackedWallId = arguments[0];
                    byte damage = arguments[1];
                    CrackedWall crackedWall = Global.level.getCrackedWallById(crackedWallId);
                    if (crackedWall != null)
                    {
                        crackedWall.applyDamage(null, null, damage, null);
                    }
                }
                return;
            }
            else if (toggleId == RPCActorToggleType.CrackedWallDestroy)
            {
                byte crackedWallId = arguments[0];
                CrackedWall crackedWall = Global.level.getCrackedWallById(crackedWallId);
                if (crackedWall != null)
                {
                    crackedWall.destroySelf();
                }
                return;
            }

            ushort netId = BitConverter.ToUInt16(arguments, 0);
            var actor = Global.level.getActorByNetId(netId);
            if (actor == null) return;

            if (toggleId == RPCActorToggleType.SonicSlicerBounce)
            {
                actor.playSound("dingX2");
                new Anim(actor.pos, "sonicslicer_sparks", actor.xDir, null, true);
            }
            else if (toggleId == RPCActorToggleType.StartGravityWell && actor is GravityWellProjCharged gw)
            {
                gw.started = true;
            }
            else if (toggleId == RPCActorToggleType.AddWolfSigmaMusicSource)
            {
                actor.addMusicSource("wolfsigma", actor.pos.addxy(0, -75), false);
            }
            else if (toggleId == RPCActorToggleType.AddDrLightMusicSource)
            {
                actor.addMusicSource("drlight", actor.getCenterPos(), false);
            }
            else if (toggleId == RPCActorToggleType.AddDrDopplerMusicSource)
            {
                actor.addMusicSource("drdoppler", actor.getCenterPos(), false);
            }
            else if (toggleId == RPCActorToggleType.AddGoliathMusicSource)
            {
                actor.addMusicSource("goliath", actor.getCenterPos(), true);
            }
            else if (toggleId == RPCActorToggleType.AddViralSigmaMusicSource)
            {
                actor.addMusicSource("viralsigma", actor.getCenterPos(), true);
            }
            else if (toggleId == RPCActorToggleType.AddKaiserSigmaMusicSource)
            {
                actor.destroyMusicSource();
                actor.addMusicSource("kaisersigma", actor.getCenterPos(), true);
            }
            else if (toggleId == RPCActorToggleType.AddKaiserViralSigmaMusicSource)
            {
                actor.destroyMusicSource();
                actor.addMusicSource("kaisersigmavirus", actor.getCenterPos(), true);
            }
            else if (toggleId == RPCActorToggleType.StartMechSelfDestruct && actor is RideArmor ra)
            {
                ra.selfDestructTime = Global.spf;
            }
            else if (toggleId == RPCActorToggleType.ShakeCamera)
            {
                actor.shakeCamera();
            }
            else if (toggleId == RPCActorToggleType.ReverseRocketPunch)
            {
                if (actor is RocketPunchProj rpp)
                {
                    rpp.reversed = true;
                }
            }
            else if (toggleId == RPCActorToggleType.DropFlagManual)
            {
                if (Global.isHost && actor is Character chr)
                {
                    chr.dropFlag();
                    chr.dropFlagCooldown = 1;
                }
            }
            else if (toggleId == RPCActorToggleType.AwardScrap)
            {
                if (actor is Character chr)
                {
                    chr.player.scrap += 5;
                }
            }
            else if (toggleId == RPCActorToggleType.MorphMothCocoonSelfDestruct)
            {
                if (actor is MorphMothCocoon mmc)
                {
                    mmc.selfDestructTime = 0.1f;
                }
            }
            else if (toggleId == RPCActorToggleType.ChangeToParriedState)
            {
                (actor as Character)?.changeState(new ParriedState(), true);
            }
            else if (toggleId == RPCActorToggleType.KaiserShellFadeOut)
            {
                (actor as Anim)?.setFadeOut(0.25f);
            }
            else if (toggleId == RPCActorToggleType.AddVaccineTime)
            {
                (actor as Character)?.addVaccineTime(2);
            }
            else if (toggleId == RPCActorToggleType.ActivateBlackZero2)
            {
                if (actor is Character chr)
                {
                    chr.blackZeroTime = 9999;
                }
            }
        }

        public void sendRpc(ushort? netId, RPCActorToggleType toggleType)
        {
            if (netId == null) return;
            byte[] netIdBytes = BitConverter.GetBytes((ushort)netId);
            Global.serverClient?.rpc(this, netIdBytes[0], netIdBytes[1], (byte)toggleType);
        }

        // A hack to avoid having to create new RPC and redeploy server code
        public void sendRpcDamageCw(byte crackedWallId, byte damage)
        {
            Global.serverClient?.rpc(this, crackedWallId, damage, (byte)RPCActorToggleType.CrackedWallDamage);
        }

        // A hack to avoid having to create new RPC and redeploy server code
        public void sendRpcDestroyCw(byte crackedWallId)
        {
            Global.serverClient?.rpc(this, crackedWallId, 0, (byte)RPCActorToggleType.CrackedWallDestroy);
        }
    }

    public class RPCKillPlayer : RPC
    {
        public RPCKillPlayer()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }
        public override void invoke(params byte[] arguments)
        {
            int hasOwnerId = arguments[0];
            int killerId = arguments[1];
            int assisterId = arguments[2];
            ushort victimId = BitConverter.ToUInt16(new byte[] { arguments[3], arguments[4] }, 0);
            int? weaponIndex = null;
            ushort? projId = null;
            if (arguments.Length >= 6)
            {
                weaponIndex = arguments[5];
            }
            if (arguments.Length >= 7)
            {
                projId = BitConverter.ToUInt16(new byte[] { arguments[6], arguments[7] }, 0);
            }

            var victim = Global.level.getPlayerById(victimId);
            var killer = (hasOwnerId == 0) ? null : Global.level.getPlayerById(killerId);
            var assister = (hasOwnerId == 0) ? null : Global.level.getPlayerById(assisterId);

            // If assister is passed in as the same as the killer it is a sentinel value for no killer
            if (assister == killer) assister = null;

            victim?.lastCharacter?.killPlayer(killer, assister, weaponIndex, projId);
        }
    }

    public class RPCCreateAnim : RPC
    {
        public RPCCreateAnim()
        {
            netDeliveryMethod = NetDeliveryMethod.Unreliable;
        }

        public override void invoke(params byte[] arguments)
        {
            var netProjByte = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            int spriteIndex = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);
            float xPos = BitConverter.ToSingle(new byte[] { arguments[4], arguments[5], arguments[6], arguments[7] }, 0);
            float yPos = BitConverter.ToSingle(new byte[] { arguments[8], arguments[9], arguments[10], arguments[11] }, 0);
            int xDir = (int)arguments[12] - 128;

            if (!Global.spriteNames.InRange(spriteIndex)) return;

            if (Global.spriteNames[spriteIndex] == "parasitebomb_latch_start")
            {
                new ParasiteAnim(new Point(xPos, yPos), Global.spriteNames[spriteIndex], netProjByte, sendRpc: false, ownedByLocalPlayer: false);
                return;
            }

            // The rest of the bytes are for optional, expensive-to-sync data that should be used sparingly.
            RPCAnimModel extendedAnimModel = null;
            Actor zIndexRelActor = null;
            if (arguments.Length > 13)
            {
                var argumentsList = arguments.ToList();
                var restofArgs = argumentsList.GetRange(13, argumentsList.Count - 13);
                extendedAnimModel = Helpers.deserialize<RPCAnimModel>(restofArgs.ToArray());
                if (extendedAnimModel.zIndexRelActorNetId != null)
                {
                    zIndexRelActor = Global.level.getActorByNetId(extendedAnimModel.zIndexRelActorNetId.Value);
                }
            }

            new Anim(new Point(xPos, yPos), Global.spriteNames[spriteIndex], xDir, netProjByte, true, ownedByLocalPlayer: false,
                zIndex: extendedAnimModel?.zIndex, zIndexRelActor: zIndexRelActor, fadeIn: extendedAnimModel?.fadeIn ?? false,
                hasRaColorShader: extendedAnimModel?.hasRaColorShader ?? false);
        }
    }

    public class RPCSwitchCharacter : RPC
    {
        public RPCSwitchCharacter()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            int charNum = arguments[1];
            var player = Global.level.getPlayerById(playerId);
            if (player == null) return;
            player.newCharNum = charNum;
        }
    }

    public class RPCSwitchTeam : RPC
    {
        public const string prefix = "changeteam:";

        public RPCSwitchTeam()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string message)
        {
            if (Global.level == null) return;
            getMessageParts(message, out int playerId, out int alliance);
            var player = Global.level.getPlayerById(playerId);
            if (player == null) return;
            player.newAlliance = alliance;
        }

        public static string getSendMessage(int playerId, int alliance)
        {
            return prefix + playerId + ":" + alliance;
        }

        public static void getMessageParts(string message, out int playerId, out int alliance)
        {
            var pieces = message.RemovePrefix(prefix).Split(':');
            playerId = int.Parse(pieces[0]);
            alliance = int.Parse(pieces[1]);
        }
    }

    // Only covers airblast type reflect
    public class RPCReflectProj : RPC
    {
        public RPCReflectProj()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort projNetId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            int playerId = arguments[2];
            int angle = arguments[3];

            Player reflecter = Global.level.getPlayerById(playerId);
            if (reflecter == null) return;

            var proj = Global.level.getActorByNetId(projNetId) as Projectile;
            if (proj != null)
            {
                float floatAngle = angle * 2;
                proj.reflect2(reflecter, floatAngle);
            }
        }

        public void sendRpc(ushort? netId, int reflecterPlayerId, float angle)
        {
            if (netId == null) return;
            var netIdBytes = BitConverter.GetBytes((ushort)netId);
            angle = Helpers.to360(angle);
            angle *= 0.5f;
            Global.serverClient?.rpc(reflectProj, netIdBytes[0], netIdBytes[1], (byte)reflecterPlayerId, (byte)(int)(angle));
        }
    }

    public class RPCJoinLateRequest : RPC
    {
        public RPCJoinLateRequest()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            toHostOnly = true;
        }

        public override void invoke(params byte[] arguments)
        {
            var serverPlayer = Helpers.deserialize<ServerPlayer>(arguments);

            Global.level.addPlayer(serverPlayer, true);

            foreach (var player in Global.level.players)
            {
                player.charNetId = null;
                if (player.character != null)
                {
                    player.charNetId = player.character.netId;
                    player.charXPos = player.character.pos.x;
                    player.charYPos = player.character.pos.y;
                    player.charXDir = player.character.xDir;
                    player.charRollingShieldNetId = player.character.chargedRollingShieldProj?.netId;
                }
            }

            var controlPoints = new List<ControlPointResponseModel>();
            foreach (var cp in Global.level.controlPoints)
            {
                controlPoints.Add(new ControlPointResponseModel()
                {
                    alliance = cp.alliance,
                    num = cp.num,
                    locked = cp.locked,
                    captured = cp.captured,
                    captureTime = cp.captureTime
                });
            }

            var magnetMines = new List<MagnetMineResponseModel>();
            foreach (var go in Global.level.gameObjects)
            {
                var magnetMine = go as MagnetMineProj;
                if (magnetMine != null && magnetMine.netId != null && magnetMine.player != null)
                {
                    magnetMines.Add(new MagnetMineResponseModel()
                    {
                        x = magnetMine.pos.x,
                        y = magnetMine.pos.y,
                        netId = magnetMine.netId.Value,
                        playerId = magnetMine.player.id
                    });
                }
            }

            var turrets = new List<TurretResponseModel>();
            foreach (var go in Global.level.gameObjects)
            {
                var turret = go as RaySplasherTurret;
                if (turret != null && turret.netId != null && turret.netOwner != null)
                {
                    turrets.Add(new TurretResponseModel()
                    {
                        x = turret.pos.x,
                        y = turret.pos.y,
                        netId = turret.netId.Value,
                        playerId = turret.netOwner.id
                    });
                }
            }

            var joinLateResponseModel = new JoinLateResponseModel()
            {
                players = Global.level.players.Select(p => new PlayerPB(p)).ToList(),
                newPlayer = serverPlayer,
                controlPoints = controlPoints,
                magnetMines = magnetMines,
                turrets = turrets
            };

            Global.serverClient?.rpc(RPC.joinLateResponse, Helpers.serialize(joinLateResponseModel));
        }
    }

    public class RPCJoinLateResponse : RPC
    {
        public RPCJoinLateResponse()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            JoinLateResponseModel joinLateResponseModel = null;
            try
            {
                joinLateResponseModel = Helpers.deserialize<JoinLateResponseModel>(arguments);
            }
            catch
            {
                try 
                {
                    //Logger.logEvent("error", "Bad joinLateResponseModel bytes. name: " + Options.main.playerName + ", match: " + Global.level?.server?.name + ", bytes: " + arguments.ToString());
                    //Console.Write(message); 
                }
                catch { }
                throw;
            }

            // Original requester
            if (Global.serverClient.serverPlayer.id == joinLateResponseModel.newPlayer.id)
            {
                Global.level.joinedLateSyncPlayers(joinLateResponseModel.players);
                Global.level.joinedLateSyncControlPoints(joinLateResponseModel.controlPoints);
                Global.level.joinedLateSyncMagnetMines(joinLateResponseModel.magnetMines);
                Global.level.joinedLateSyncTurrets(joinLateResponseModel.turrets);
            }
            else
            {
                Global.level.addPlayer(joinLateResponseModel.newPlayer, true);
            }
        }
    }

    public class RPCUpdateStarted : RPC
    {
        public RPCUpdateStarted()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isServerMessage = true;
        }
    }

    public class RPCHostPromotion : RPC
    {
        public RPCHostPromotion()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];

            var player = Global.level.getPlayerById(playerId);
            if (player == null) return;

            if (!player.serverPlayer.isHost)
            {
                player.serverPlayer.isHost = true;
                player.promoteToHost();
            }
        }
    }

    public class RPCMatchOver : RPC
    {
        public RPCMatchOver()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string message)
        {
            var rpcMatchOverResponse = JsonConvert.DeserializeObject<RPCMatchOverResponse>(message);
            Global.level?.gameMode?.matchOverRpc(rpcMatchOverResponse);
        }
    }

    public class RPCSyncTeamScores : RPC
    {
        public RPCSyncTeamScores()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            var redScore = arguments[0];
            var blueScore = arguments[1];
            if (Global.level?.gameMode != null)
            {
                Global.level.gameMode.redPoints = redScore;
                Global.level.gameMode.bluePoints = blueScore;
            }
        }
    }

    public class RPCSyncGameTime : RPC
    {
        public RPCSyncGameTime()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            if (Global.level?.gameMode == null) return;

            int time = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            Global.level.gameMode.remainingTime = time;
            if (Global.level.gameMode.remainingTime.Value <= 10 && Global.level.gameMode.remainingTime.Value > 0) Global.playSound("tick");
            if (arguments.Length >= 4)
            {
                int elimTime = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);
                Global.level.gameMode.eliminationTime = elimTime;
                Global.level.gameMode.localElimTimeInc = 0;
            }
        }
    }

    public class RPCSyncSetupTime : RPC
    {
        public RPCSyncSetupTime()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            if (Global.level?.gameMode == null) return;

            int time = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            Global.level.gameMode.setupTime = time;
            if (Global.level.gameMode.setupTime <= 0)
            {
                Global.level.gameMode.setupTime = 0;
                Global.level.gameMode.removeAllGates();
            }
        }
    }

    public class RPCKillFeedEntryResponse
    {
        public string message;
        public int alliance;
        public int? playerId;

        public RPCKillFeedEntryResponse(string message, int alliance, int? id)
        {
            this.message = message;
            this.alliance = alliance;
            playerId = id;
        }
    }

    public class RPCSendKillFeedEntry : RPC
    {
        public RPCSendKillFeedEntry()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string message)
        {
            var response = JsonConvert.DeserializeObject<RPCKillFeedEntryResponse>(message);
            Player player = null;
            if (response.playerId != null)
            {
                player = Global.level.getPlayerById(response.playerId.Value);
            }
            Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(response.message, response.alliance, player));
        }
    }

    public class RPCSendChatMessage : RPC
    {
        public RPCSendChatMessage()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string message)
        {
            var response = JsonConvert.DeserializeObject<ChatEntry>(message);
            Global.level?.gameMode?.chatMenu.addChatEntry(response);
        }
    }

    public class RPCSyncControlPoints : RPC
    {
        public RPCSyncControlPoints()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int cpIndex = arguments[0];
            int alliance = arguments[1];
            int captureProgress = arguments[2];
            int attackerCount = arguments[3];
            int defenderCount = arguments[4];
            bool locked = arguments[5] == 0 ? false : true;
            bool captured = arguments[6] == 0 ? false : true;
            int redCaptureProgress = arguments[7];
            int blueCaptureProgress = arguments[8];
            int redRemainingTime = arguments[9];
            int blueRemainingTime = arguments[10];
            byte hillAttackerCountSync = arguments[11];

            ControlPoint cp;
            if (Global.level?.gameMode is ControlPoints)
            {
                if (cpIndex >= Global.level.controlPoints.Count) return;
                cp = Global.level.controlPoints[cpIndex];
            }
            else
            {
                cp = Global.level.hill;
            }

            cp.alliance = alliance;
            cp.captureTime = captureProgress;
            cp.attackerCount = attackerCount;
            cp.defenderCount = defenderCount;
            cp.locked = locked;
            cp.captured = captured;
            cp.redCaptureTime = redCaptureProgress;
            cp.blueCaptureTime = blueCaptureProgress;
            cp.redRemainingTime = redRemainingTime;
            cp.blueRemainingTime = blueRemainingTime;
            cp.hillAttackerCountSync = hillAttackerCountSync;
        }
    }

    public class RPCSetHyperZeroTime : RPC
    {
        public RPCSetHyperZeroTime()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            int time = arguments[1];
            int type = arguments[2];
            var player = Global.level.getPlayerById(playerId);
            if (player?.character == null) return;
            if (type == 0) player.character.blackZeroTime = time;
            if (type == 1) player.character.whiteAxlTime = time;
            if (type == 2) player.character.awakenedZeroTime = time;
        }

        public void sendRpc(int playerId, float time, int type)
        {
            Global.serverClient?.rpc(this, (byte)playerId, (byte)(int)time, (byte)type);
        }
    }

    public class RPCAxlShoot : RPC
    {
        public RPCAxlShoot()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            int projId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[3], arguments[4] }, 0);
            float x = BitConverter.ToSingle(new byte[] { arguments[5], arguments[6], arguments[7], arguments[8] }, 0);
            float y = BitConverter.ToSingle(new byte[] { arguments[9], arguments[10], arguments[11], arguments[12] }, 0);
            int xDir = Helpers.byteToDir(arguments[13]);
            float angle = Helpers.byteToAngle(arguments[14]);
            int axlBulletWeaponType = arguments.InRange(15) ? arguments[16] : 0;

            var player = Global.level.getPlayerById(playerId);
            if (player?.character == null) return;

            if (projId == (int)ProjIds.AxlBullet || projId == (int)ProjIds.MetteurCrash || projId == (int)ProjIds.BeastKiller || projId == (int)ProjIds.MachineBullets || projId == (int)ProjIds.RevolverBarrel || projId == (int)ProjIds.AncientGun)
            {
                var pos = new Point(x, y);
                var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
                flash.angle = angle;
                flash.frameSpeed = 1;
                if (projId == (int)ProjIds.AxlBullet)
                {
                    var bullet = new AxlBulletProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
                    player.character.playSound("axlBullet");
                }
                else if (projId == (int)ProjIds.MetteurCrash)
                {
                    //var bullet = new MettaurCrashProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
                    player.character.playSound("mettaurCrash");
                }
                else if (projId == (int)ProjIds.BeastKiller)
                {
                    //var bullet = new BeastKillerProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
                    player.character.playSound("beastKiller");
                }
                else if (projId == (int)ProjIds.MachineBullets)
                {
                    //var bullet = new MachineBulletProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
                    player.character.playSound("machineBullets");
                }
                else if (projId == (int)ProjIds.RevolverBarrel)
                {
                    //var bullet = new RevolverBarrelProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
                    player.character.playSound("revolverBarrel");
                }
                else if (projId == (int)ProjIds.AncientGun)
                {
                    //var bullet = new AncientGunProj(new AxlBullet((AxlBulletWeaponType)axlBulletWeaponType), pos, player, Point.createFromAngle(angle), netId);
                    player.character.playSound("ancientGun3");
                }
            }
            else if (projId == (int)ProjIds.CopyShot)
            {
                var pos = new Point(x, y);
                var bullet = new CopyShotProj(new AxlBullet(), pos, 0, player, Point.createFromAngle(angle), netId);
                var flash = new Anim(pos, "axl_pistol_flash_charged", 1, null, true);
                flash.angle = angle;
                flash.frameSpeed = 3;
                player.character.playSound("axlBulletCharged");
            }
            else if (projId == (int)ProjIds.GLauncher)
            {
                var pos = new Point(x, y);
                var bullet = new GrenadeProj(new GLauncher(0), pos, xDir, player, Point.createFromAngle(angle), null, new Point(), 0, netId);
                var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
                flash.angle = angle;
                flash.frameSpeed = 1;
                player.character.playSound("grenadeShoot");
            }
            else if (projId == (int)ProjIds.Explosion)
            {
                var pos = new Point(x, y);
                var bullet = new GrenadeProjCharged(new GLauncher(0), pos, xDir, player, Point.createFromAngle(angle), null, netId);
                var flash = new Anim(pos, "axl_pistol_flash_charged", 1, null, true);
                flash.angle = angle;
                flash.frameSpeed = 3;
                player.character.playSound("rocketShoot");
            }
            else if (projId == (int)ProjIds.RayGun || projId == (int)ProjIds.RayGun2)
            {
                var pos = new Point(x, y);
                Point velDir = Point.createFromAngle(angle);
                if (projId == (int)ProjIds.RayGun)
                {
                    var bullet = new RayGunProj(new RayGun(0), pos, xDir, player, velDir, netId);
                    player.character.playSound("raygun");
                }
                else if (projId == (int)ProjIds.RayGun2)
                {
                    var bullet = Global.level.getActorByNetId(netId) as RayGunAltProj;
                    if (bullet == null)
                    {
                        new RayGunAltProj(new RayGun(0), pos, pos, xDir, player, netId);
                    }
                }

                string fs = "axl_raygun_flash";
                if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) fs = "axl_raygun_flash2";
                var flash = new Anim(pos, fs, 1, null, true);
                flash.setzIndex(player.character.zIndex - 100);
                flash.xScale = 0.75f;
                flash.yScale = 0.75f;
                flash.angle = angle;
                flash.frameSpeed = 1;
            }
            else if (projId == (int)ProjIds.SpiralMagnum || projId == (int)ProjIds.SpiralMagnumScoped)
            {
                var pos = new Point(x, y);
                var bullet = new SpiralMagnumProj(new SpiralMagnum(0), pos, 0, 0, player, Point.createFromAngle(angle), null, null, netId);
                if (projId == (int)ProjIds.SpiralMagnumScoped)
                {
                    AssassinBulletTrailAnim trail = new AssassinBulletTrailAnim(pos, bullet);
                }
                var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
                flash.angle = angle;
                flash.frameSpeed = 1;
                player.character.playSound("spiralMagnum");
            }
            else if (projId == (int)ProjIds.IceGattling || projId == (int)ProjIds.IceGattlingHyper)
            {
                var pos = new Point(x, y);
                var bullet = new IceGattlingProj(new IceGattling(0), pos, xDir, player, Point.createFromAngle(angle), netId);
                var flash = new Anim(pos, "axl_pistol_flash", 1, null, true);
                flash.angle = angle;
                flash.frameSpeed = 1;
                player.character.playSound("iceGattling");
            }
            else if (projId == (int)ProjIds.AssassinBullet || projId == (int)ProjIds.AssassinBulletQuick)
            {
                var pos = new Point(x, y);
                var bullet = new AssassinBulletProj(new AssassinBullet(), pos, new Point(), xDir, player, null, null, netId);
                AssassinBulletTrailAnim trail = new AssassinBulletTrailAnim(pos, bullet);
                var flash = new Anim(pos, "axl_pistol_flash_charged", 1, null, true);
                flash.angle = angle;
                flash.frameSpeed = 3;
                player.character.playSound("assassinate");
            }
        }

        public void sendRpc(int playerId, int projId, ushort netId, Point pos, int xDir, float angle)
        {
            var xBytes = BitConverter.GetBytes(pos.x);
            var yBytes = BitConverter.GetBytes(pos.y);
            var netIdBytes = BitConverter.GetBytes(netId);
            var projIdBytes = BitConverter.GetBytes((ushort)projId);
            Global.serverClient?.rpc(this, (byte)playerId, projIdBytes[0], projIdBytes[1], netIdBytes[0], netIdBytes[1], xBytes[0], xBytes[1], xBytes[2], xBytes[3], yBytes[0], yBytes[1], yBytes[2], yBytes[3], Helpers.dirToByte(xDir), Helpers.angleToByte(angle));
        }
    }

    public class RPCAxlDisguiseJson
    {
        public int playerId;
        public string targetName;
        public RPCAxlDisguiseJson() { }
        public RPCAxlDisguiseJson(int playerId, string targetName)
        {
            this.playerId = playerId;
            this.targetName = targetName;
        }
    }

    public class RPCAxlDisguise : RPC
    {
        public RPCAxlDisguise()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string json)
        {
            var rpcAxlDisguiseJson = JsonConvert.DeserializeObject<RPCAxlDisguiseJson>(json);
            var player = Global.level.getPlayerById(rpcAxlDisguiseJson.playerId);
            if (player == null) return;
            if (string.IsNullOrEmpty(rpcAxlDisguiseJson.targetName))
            {
                player.disguise = null;
            }
            else
            {
                player.disguise = new Disguise(rpcAxlDisguiseJson.targetName);
            }
        }
    }

    public class RPCReportPlayerRequest : RPC
    {
        public RPCReportPlayerRequest()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
            isServerMessage = true;
        }

        public override void invoke(string playerName)
        {
        }
    }

    public class RPCReportPlayerResponse : RPC
    {
        public RPCReportPlayerResponse()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string json)
        {
            var reportedPlayer = JsonConvert.DeserializeObject<ReportedPlayer>(json);
            reportedPlayer.chatHistory = Global.level.gameMode.chatMenu.chatHistory.Select(c => c.getDisplayMessage()).ToList();
            reportedPlayer.timestamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            reportedPlayer.description = "";
            Helpers.WriteToFile(reportedPlayer.getFileName(), JsonConvert.SerializeObject(reportedPlayer));
        }
    }

    public class RPCKickPlayerJson
    {
        public string playerName;
        public string deviceId;
        public int banTimeMinutes;
        public string banReason;
        public VoteType type;
        public RPCKickPlayerJson(VoteType type, string playerName, string deviceId, int banTimeMinutes, string banReason)
        {
            this.type = type;
            this.playerName = playerName;
            this.deviceId = deviceId;
            this.banTimeMinutes = banTimeMinutes;
            this.banReason = banReason;
        }
    }

    public class RPCKickPlayerRequest : RPC
    {
        public RPCKickPlayerRequest()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
            isServerMessage = true;
        }

        public override void invoke(string playerName)
        {
        }
    }

    public class RPCKickPlayerResponse : RPC
    {
        public RPCKickPlayerResponse()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string kickPlayerJson)
        {
            var kickPlayerObj = JsonConvert.DeserializeObject<RPCKickPlayerJson>(kickPlayerJson);
            if (kickPlayerObj.playerName == Global.level.mainPlayer.name)
            {
                Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.Kicked, null, kickPlayerObj.banReason);
            }
            else
            {
                string kickMsg = string.Format("{0} was kicked for reason: {1}.", kickPlayerObj.playerName, kickPlayerObj.banReason);
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(kickMsg, null, null, true));
            }
        }
    }

    public class RPCVoteKickStart : RPC
    {
        public RPCVoteKickStart()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isString = true;
        }

        public override void invoke(string kickPlayerJson)
        {
            var rpcKickPlayerObj = JsonConvert.DeserializeObject<RPCKickPlayerJson>(kickPlayerJson);
            var player = Global.level.getPlayerByName(rpcKickPlayerObj.playerName);
            if (player == null) return;
            VoteKick.sync(player, rpcKickPlayerObj.type, rpcKickPlayerObj.banTimeMinutes, rpcKickPlayerObj.banReason);
        }
    }

    public class RPCVoteKickEnd : RPC
    {
        public RPCVoteKickEnd()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            Global.level.gameMode.currentVoteKick = null;
        }
    }

    public class RPCVoteKick : RPC
    {
        public RPCVoteKick()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            if (Global.level.gameMode.currentVoteKick == null) return;

            if (arguments[0] == 0)
            {
                Global.level.gameMode.currentVoteKick.yesVotes++;
            }
            else
            {
                Global.level.gameMode.currentVoteKick.noVotes++;
            }
        }
    }

    public class RPCEndMatchRequest : RPC
    {
        public RPCEndMatchRequest()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            if (Global.isHost && Global.level?.gameMode != null)
            {
                Global.level.gameMode.noContest = true;
            }
        }

        public void sendRpc()
        {
            Global.serverClient?.rpc(this);
        }
    }

    public class RPCPeriodicServerSync : RPC
    {
        public RPCPeriodicServerSync()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            var syncModel = Helpers.deserialize<PeriodicServerSyncModel>(arguments);
            if (syncModel.players == null) return;
            if (!Global.level.started)
            {
                var waitMenu = Menu.mainMenu as WaitMenu;
                if (waitMenu != null)
                {
                    foreach (var player in waitMenu.server.players)
                    {
                        var updatePlayer = syncModel.players.Find(p => p.id == player.id);
                        if (updatePlayer == null) continue;
                        player.isSpectator = updatePlayer.isSpectator;
                    }
                }
                return;
            }

            foreach (ServerPlayer serverPlayer in syncModel.players)
            {
                Player player = Global.level.getPlayerById(serverPlayer.id);
                if (player != null)
                {
                    player.syncFromServerPlayer(serverPlayer);
                }
                else
                {
                    Global.level.addPlayer(serverPlayer, serverPlayer.joinedLate);
                }
            }
            foreach (var player in Global.level.players.ToList())
            {
                if (!syncModel.players.Any(sp => sp.id == player.id))
                {
                    Global.level.removePlayer(player);
                }
            }
        }
    }

    public class RPCPeriodicServerPing : RPC
    {
        public RPCPeriodicServerPing()
        {
            netDeliveryMethod = NetDeliveryMethod.Unreliable;
        }
    }

    public class RPCPeriodicHostSync : RPC
    {
        public RPCPeriodicHostSync()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            if (Global.level?.gameMode == null) return;

            var syncModel = Helpers.deserialize<PeriodicHostSyncModel>(arguments);

            if (syncModel.matchOverResponse != null)
            {
                Global.level.gameMode.matchOverRpc(syncModel.matchOverResponse);
            }
            Global.level.gameMode.bluePoints = syncModel.bluePoints;
            Global.level.gameMode.redPoints = syncModel.redPoints;
            Global.level.syncCrackedWalls(syncModel.crackedWallBytes);
            Global.level.gameMode.virusStarted = syncModel.virusStarted;
            Global.level.gameMode.safeZoneSpawnIndex = syncModel.safeZoneSpawnIndex;
        }

        public void sendRpc()
        {
            var syncModel = new PeriodicHostSyncModel()
            {
                matchOverResponse = Global.level.gameMode.matchOverResponse,
                bluePoints = Global.level.gameMode.bluePoints,
                redPoints = Global.level.gameMode.redPoints,
                crackedWallBytes = Global.level.getCrackedWallBytes(),
                virusStarted = Global.level.gameMode.virusStarted,
                safeZoneSpawnIndex = Global.level.gameMode.safeZoneSpawnIndex
            };

            var bytes = Helpers.serialize(syncModel);

            Global.serverClient?.rpc(this, bytes);
        }
    }

    public class RPCUpdatePlayer : RPC
    {
        public RPCUpdatePlayer()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isServerMessage = true;
        }

        public void sendRpc(int playerId, int kills, int deaths)
        {
            byte[] killsBytes = BitConverter.GetBytes((ushort)kills);
            byte[] deathBytes = BitConverter.GetBytes((ushort)deaths);
            Global.serverClient?.rpc(this, (byte)playerId, killsBytes[0], killsBytes[1], deathBytes[0], deathBytes[1]);
        }
    }

    public class RPCAddBot : RPC
    {
        public RPCAddBot()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isServerMessage = true;
        }

        public void sendRpc(int charNum, int team)
        {
            if (charNum == -1) charNum = 255;
            if (team == -1) team = 255;
            Global.serverClient?.rpc(this, (byte)charNum, (byte)team);
        }
    }

    public class RPCRemoveBot : RPC
    {
        public RPCRemoveBot()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isServerMessage = true;
        }

        public void sendRpc(int playerId)
        {
            Global.serverClient?.rpc(this, (byte)playerId);
        }
    }

    public class RPCMakeSpectator : RPC
    {
        public RPCMakeSpectator()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isServerMessage = true;
        }

        public void sendRpc(int playerId, bool makeSpectator)
        {
            Global.serverClient?.rpc(this, (byte)playerId, makeSpectator ? (byte)0 : (byte)1);
        }
    }

    public class RPCSyncValue : RPC
    {
        public RPCSyncValue()
        {
            netDeliveryMethod = NetDeliveryMethod.Unreliable;
        }

        public override void invoke(params byte[] arguments)
        {
            float syncValue = BitConverter.ToSingle(arguments, 0);
            Global.level.hostSyncValue = syncValue;
        }

        public void sendRpc(float syncValue)
        {
            var bytes = BitConverter.GetBytes(syncValue);
            Global.serverClient?.rpc(this, bytes[0], bytes[1], bytes[2], bytes[3]);
        }
    }

    public class RPCHeal : RPC
    {
        public RPCHeal()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            ushort healNetId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
            int healAmount = arguments[3];

            var actor = Global.level.getActorByNetId(healNetId);
            if (actor == null) return;
            var player = Global.level.getPlayerById(playerId);

            var damagable = actor as IDamagable;
            if (damagable != null)
            {
                if (actor.ownedByLocalPlayer)
                {
                    damagable.heal(player, healAmount, allowStacking: false, drawHealText: true);
                }
            }
        }

        public void sendRpc(Player player, ushort healNetId, int healAmount)
        {
            var healNetIdBytes = BitConverter.GetBytes(healNetId);
            Global.serverClient?.rpc(this, (byte)player.id, healNetIdBytes[0], healNetIdBytes[1], (byte)healAmount);
        }
    }

    public enum CommandGrabScenario
    {
        StrikeChain,
        MK2Grab,
        UPGrab,
        WhirlpoolGrab,
        DeadLiftGrab,
        WSpongeChain,
        WheelGGrab,
        FStagGrab,
        MagnaCGrab,
        BeetleLiftGrab,
        CrushCGrab,
        BBuffaloGrab,
        Release,
    }

    public class RPCCommandGrabPlayer : RPC
    {
        public RPCCommandGrabPlayer()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public void maverickGrabCode(Maverick grabber, Character victimChar, CharState grabbedState, bool isDefenderFavored, MaverickState optionalGrabberState = null)
        {
            if (grabber == null || victimChar == null) return;
            if (!victimChar.canBeGrabbed()) return;

            if (!isDefenderFavored)
            {
                if (victimChar.ownedByLocalPlayer && !Helpers.isOfClass(victimChar.charState, grabbedState.GetType()))
                {
                    victimChar.changeState(grabbedState, true);
                }
            }
            else
            {
                if (grabber.ownedByLocalPlayer)
                {
                    if (optionalGrabberState != null)
                    {
                        grabber.changeState(optionalGrabberState, true);
                    }
                    grabber.state.trySetGrabVictim(victimChar);
                }
            }
        }

        public override void invoke(params byte[] arguments)
        {
            ushort grabberNetId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            ushort victimNetId = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);
            CommandGrabScenario hookScenario = (CommandGrabScenario)arguments[4];
            bool isDefenderFavored = Helpers.byteToBool(arguments[5]);

            var grabber = Global.level.getActorByNetId(grabberNetId);
            if (grabber == null) return;

            var victim = Global.level.getActorByNetId(victimNetId);
            if (victim == null) return;

            Character grabberChar = grabber as Character;
            Maverick grabberMaverick = grabber as Maverick;
            Character victimChar = victim as Character;

            if (hookScenario == CommandGrabScenario.StrikeChain)
            {
                if (victimChar == null) return;

                if (!isDefenderFavored)
                {
                    var scp = Global.level.getActorByNetId(grabberNetId) as Projectile;
                    if (scp is not StrikeChainProj && scp is not WSpongeSideChainProj) return;
                    victimChar.hook(scp);
                }
                else if (grabber is StrikeChainProj scp)
                {
                    scp.hookActor(victimChar);
                }
            }
            else if (hookScenario == CommandGrabScenario.MK2Grab)
            {
                if (grabberChar == null || victimChar == null) return;
                if (!victimChar.canBeGrabbed()) return;

                if (!isDefenderFavored)
                {
                    if (victim.ownedByLocalPlayer && victimChar.charState is not VileMK2Grabbed)
                    {
                        victimChar.changeState(new VileMK2Grabbed(grabberChar), true);
                    }
                }
                else
                {
                    if (grabberChar.ownedByLocalPlayer)
                    {
                        grabberChar.changeState(new VileMK2GrabState(victimChar));
                    }
                }
            }
            else if (hookScenario == CommandGrabScenario.UPGrab)
            {
                if (grabberChar == null || victimChar == null) return;
                if (!victimChar.canBeGrabbed()) return;

                if (!isDefenderFavored)
                {
                    if (victimChar.ownedByLocalPlayer && victimChar.charState is not UPGrabbed)
                    {
                        victimChar.changeState(new UPGrabbed(grabberChar), true);
                    }
                }
                else
                {
                    if (grabberChar.ownedByLocalPlayer)
                    {
                        grabberChar.changeState(new XUPGrabState(victimChar));
                    }
                }
            }
            else if (hookScenario == CommandGrabScenario.WhirlpoolGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new WhirlpoolGrabbed(grabber as LaunchOctopus), isDefenderFavored);
            }
            else if (hookScenario == CommandGrabScenario.DeadLiftGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new DeadLiftGrabbed(grabber as BoomerKuwanger), isDefenderFavored);
            }
            else if (hookScenario == CommandGrabScenario.WheelGGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new WheelGGrabbed(grabber as WheelGator), isDefenderFavored);
            }
            else if (hookScenario == CommandGrabScenario.FStagGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new FStagGrabbed(grabber as FlameStag), isDefenderFavored, optionalGrabberState: new FStagUppercutState(victimChar));
            }
            else if (hookScenario == CommandGrabScenario.MagnaCGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new MagnaCDrainGrabbed(grabber as MagnaCentipede), isDefenderFavored);
            }
            else if (hookScenario == CommandGrabScenario.BeetleLiftGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new BeetleGrabbedState(grabber as GravityBeetle), isDefenderFavored);
            }
            else if (hookScenario == CommandGrabScenario.CrushCGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new CrushCGrabbed(grabber as CrushCrawfish), isDefenderFavored, optionalGrabberState: new CrushCGrabState(victimChar));
            }
            else if (hookScenario == CommandGrabScenario.BBuffaloGrab)
            {
                maverickGrabCode(grabberMaverick, victimChar, new BBuffaloDragged(grabber as BlizzardBuffalo), isDefenderFavored);
            }
            else if (hookScenario == CommandGrabScenario.Release)
            {
                if (victimChar != null)
                {
                    victimChar.charState?.releaseGrab();
                }
            }
        }

        public void sendRpc(ushort? grabberNetId, ushort? victimCharNetId, CommandGrabScenario hookScenario, bool isDefenderFavored)
        {
            if (victimCharNetId == null) return;

            var grabberNetIdBytes = BitConverter.GetBytes(grabberNetId.Value);
            var victimNetIdBytes = BitConverter.GetBytes(victimCharNetId.Value);
            Global.serverClient?.rpc(this, grabberNetIdBytes[0], grabberNetIdBytes[1], victimNetIdBytes[0], victimNetIdBytes[1], (byte)hookScenario, Helpers.boolToByte(isDefenderFavored));
        }
    }

    public class RPCClearOwnership : RPC
    {
        public RPCClearOwnership()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            var actor = Global.level.getActorByNetId(netId);
            if (actor == null) return;
            actor.ownedByLocalPlayer = false;
        }

        public void sendRpc(ushort? netId)
        {
            if (netId == null) return;
            var netIdBytes = BitConverter.GetBytes(netId.Value);
            Global.serverClient?.rpc(this, netIdBytes[0], netIdBytes[1]);
        }
    }

    public class RPCPlaySound : RPC
    {
        public RPCPlaySound()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(arguments, 0);
            ushort soundIndex = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);

            var actor = Global.level.getActorByNetId(netId);
            if (actor == null) return;

            if (Global.soundNames.InRange(soundIndex))
            {
                string sound = Global.soundNames[soundIndex];
                var soundWrapper = actor.playSound(sound);
                actor.netSounds[soundIndex] = soundWrapper;
            }
        }

        public void sendRpc(string sound, ushort? netId)
        {
            if (netId == null) return;
            if (Global.serverClient == null) return;

            int soundIndex = Global.soundNames.IndexOf(sound);
            if (soundIndex == -1) return;
            byte[] netIdBytes = BitConverter.GetBytes((ushort)netId);
            byte[] soundIndexBytes = BitConverter.GetBytes((ushort)soundIndex);
            Global.serverClient?.rpc(this, netIdBytes[0], netIdBytes[1], soundIndexBytes[0], soundIndexBytes[1]);
        }
    }

    public class RPCStopSound : RPC
    {
        public RPCStopSound()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(arguments, 0);
            ushort soundIndex = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);

            var actor = Global.level.getActorByNetId(netId);
            if (actor == null) return;

            if (actor.netSounds.ContainsKey(soundIndex))
            {
                var soundWrapper = actor.netSounds[soundIndex];
                if (!soundWrapper.deleted) soundWrapper.sound?.Stop();
            }
        }

        public void sendRpc(string sound, ushort? netId)
        {
            if (netId == null) return;
            if (Global.serverClient == null) return;

            int soundIndex = Global.soundNames.IndexOf(sound);
            if (soundIndex == -1) return;
            byte[] netIdBytes = BitConverter.GetBytes((ushort)netId);
            byte[] soundIndexBytes = BitConverter.GetBytes((ushort)soundIndex);
            Global.serverClient?.rpc(this, netIdBytes[0], netIdBytes[1], soundIndexBytes[0], soundIndexBytes[1]);
        }
    }

    public class RPCAddDamageText : RPC
    {
        public RPCAddDamageText()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            short damage = BitConverter.ToInt16(new byte[] { arguments[2], arguments[3] }, 0);
            int attackerId = arguments[4];

            if (Global.level?.mainPlayer == null) return;
            if (Global.level.mainPlayer.id != attackerId) return;
            var actor = Global.level.getActorByNetId(netId);
            if (actor == null) return;

            float floatDamage = damage / 10f;

            actor.addDamageText(floatDamage);

            if (floatDamage < 0)
            {
                Global.level.mainPlayer.creditHealing(-floatDamage);
            }
        }

        public void sendRpc(int attackerId, ushort? netId, float damage)
        {
            if (netId == null) return;
            if (Global.serverClient == null) return;

            byte[] netIdBytes = BitConverter.GetBytes((ushort)netId);

            short damageShort = (short)MathF.Round(damage * 10);

            byte[] damageBytes = BitConverter.GetBytes(damageShort);
            Global.serverClient?.rpc(this, netIdBytes[0], netIdBytes[1], damageBytes[0], damageBytes[1], (byte)attackerId);
        }
    }

    public class RPCSyncAxlBulletPos : RPC
    {
        public RPCSyncAxlBulletPos()
        {
            netDeliveryMethod = NetDeliveryMethod.Unreliable;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];

            short xPos = BitConverter.ToInt16(new byte[] { arguments[1], arguments[2] }, 0);
            short yPos = BitConverter.ToInt16(new byte[] { arguments[3], arguments[4] }, 0);

            var player = Global.level.getPlayerById(playerId);
            if (player?.character == null) return;

            player.character.nonOwnerAxlBulletPos = new Point(xPos, yPos);
        }

        public void sendRpc(int playerId, Point bulletPos)
        {
            byte[] xBytes = BitConverter.GetBytes((short)MathF.Round(bulletPos.x));
            byte[] yBytes = BitConverter.GetBytes((short)MathF.Round(bulletPos.y));
            Global.serverClient?.rpc(this, (byte)playerId, xBytes[0], xBytes[1], yBytes[0], yBytes[1]);
        }
    }

    public class RPCSyncAxlScopePos : RPC
    {
        public RPCSyncAxlScopePos()
        {
            netDeliveryMethod = NetDeliveryMethod.Unreliable;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];

            var player = Global.level.getPlayerById(playerId);
            if (player?.character == null) return;

            bool isZooming = arguments[1] == 1 ? true : false;

            player.character.isNonOwnerZoom = isZooming;

            short sxPos = BitConverter.ToInt16(new byte[] { arguments[2], arguments[3] }, 0);
            short syPos = BitConverter.ToInt16(new byte[] { arguments[4], arguments[5] }, 0);

            short exPos = BitConverter.ToInt16(new byte[] { arguments[6], arguments[7] }, 0);
            short eyPos = BitConverter.ToInt16(new byte[] { arguments[8], arguments[9] }, 0);

            player.character.nonOwnerScopeStartPos = new Point(sxPos, syPos);
            player.character.netNonOwnerScopeEndPos = new Point(exPos, eyPos);
        }

        public void sendRpc(int playerId, bool isZooming, Point startScopePos, Point endScopePos)
        {
            byte[] sxBytes = BitConverter.GetBytes((short)MathF.Round(startScopePos.x));
            byte[] syBytes = BitConverter.GetBytes((short)MathF.Round(startScopePos.y));

            byte[] exBytes = BitConverter.GetBytes((short)MathF.Round(endScopePos.x));
            byte[] eyBytes = BitConverter.GetBytes((short)MathF.Round(endScopePos.y));

            Global.serverClient?.rpc(this, (byte)playerId, isZooming ? (byte)1 : (byte)0, sxBytes[0], sxBytes[1], syBytes[0], syBytes[1], exBytes[0], exBytes[1], eyBytes[0], eyBytes[1]);
        }
    }

    public class RPCBoundBlasterStick : RPC
    {
        public RPCBoundBlasterStick()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort beaconNetId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            ushort stuckActorNetId = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] }, 0);

            short xPos = BitConverter.ToInt16(new byte[] { arguments[4], arguments[5] }, 0);
            short yPos = BitConverter.ToInt16(new byte[] { arguments[6], arguments[7] }, 0);

            BoundBlasterAltProj beaconActor = Global.level.getActorByNetId(beaconNetId) as BoundBlasterAltProj;
            Actor stuckActor = Global.level.getActorByNetId(stuckActorNetId);

            if (beaconActor == null || stuckActor == null) return;

            beaconActor.isActorStuck = true;
            beaconActor.stuckActor = stuckActor;
            beaconActor.stopSyncingNetPos = true;
            beaconActor.changePos(new Point(xPos, yPos));
        }

        public void sendRpc(ushort? beaconNetId, ushort? stuckActorNetId, Point hitPos)
        {
            if (beaconNetId == null) return;
            if (stuckActorNetId == null) return;

            byte[] beaconNetIdBytes = BitConverter.GetBytes(beaconNetId.Value);
            byte[] stuckActorNetIdBytes = BitConverter.GetBytes(stuckActorNetId.Value);

            byte[] sxBytes = BitConverter.GetBytes((short)MathF.Round(hitPos.x));
            byte[] syBytes = BitConverter.GetBytes((short)MathF.Round(hitPos.y));

            Global.serverClient?.rpc(this, beaconNetIdBytes[0], beaconNetIdBytes[1], stuckActorNetIdBytes[0], stuckActorNetIdBytes[1],
                sxBytes[0], sxBytes[1], syBytes[0], syBytes[1]);
        }
    }

    public class RPCBroadcastLoadout : RPC
    {
        public RPCBroadcastLoadout()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            LoadoutData loadout = Helpers.deserialize<LoadoutData>(arguments);
            var player = Global.level?.getPlayerById(loadout.playerId);
            if (player == null) return;

            player.loadout = loadout;
            player.configureStaticWeapons();
        }

        public void sendRpc(Player player)
        {
            byte[] loadoutBytes = Helpers.serialize(player.loadout);
            Global.serverClient?.rpc(this, loadoutBytes);
        }
    }

    public class RPCCreditPlayerKillMaverick : RPC
    {
        public RPCCreditPlayerKillMaverick()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int killerId = arguments[0];
            int assisterId = arguments[1];
            ushort victimNetId = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] });
            int? weaponIndex = null;
            if (arguments.Length >= 5) weaponIndex = arguments[4];

            Player killer = Global.level.getPlayerById(killerId);
            Player assister = Global.level.getPlayerById(assisterId);
            Maverick victim = Global.level.getActorByNetId(victimNetId) as Maverick;

            victim?.creditMaverickKill(killer, assister, weaponIndex);
        }

        public void sendRpc(Player killer, Player assister, Maverick victim, int? weaponIndex)
        {
            if (killer == null) return;
            if (victim?.netId == null) return;

            byte assisterId = assister == null ? byte.MaxValue : (byte)assister.id;
            var victimBytes = BitConverter.GetBytes(victim.netId.Value);

            var bytesToAdd = new List<byte>()
            {
                (byte)killer.id, assisterId, victimBytes[0], victimBytes[1]
            };

            if (weaponIndex != null)
            {
                bytesToAdd.Add((byte)weaponIndex.Value);
            }

            if (Global.serverClient != null)
            {
                Global.serverClient.rpc(RPC.creditPlayerKillMaverick, bytesToAdd.ToArray());
            }
        }
    }

    public class RPCCreditPlayerKillVehicle : RPC
    {
        public RPCCreditPlayerKillVehicle()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int killerId = arguments[0];
            int assisterId = arguments[1];
            ushort victimNetId = BitConverter.ToUInt16(new byte[] { arguments[2], arguments[3] });
            int? weaponIndex = null;
            if (arguments.Length >= 5) weaponIndex = arguments[4];

            Player killer = Global.level.getPlayerById(killerId);
            Player assister = Global.level.getPlayerById(assisterId);
            Actor victim = Global.level.getActorByNetId(victimNetId);

            if (victim is RideArmor ra) ra.creditKill(killer, assister, weaponIndex);
            else if (victim is RideChaser rc) rc.creditKill(killer, assister, weaponIndex);
        }

        public void sendRpc(Player killer, Player assister, Actor victim, int? weaponIndex)
        {
            if (killer == null) return;
            if (victim?.netId == null) return;

            byte assisterId = assister == null ? byte.MaxValue : (byte)assister.id;
            var victimBytes = BitConverter.GetBytes(victim.netId.Value);

            var bytesToAdd = new List<byte>()
            {
                (byte)killer.id, assisterId, victimBytes[0], victimBytes[1]
            };

            if (weaponIndex != null)
            {
                bytesToAdd.Add((byte)weaponIndex.Value);
            }

            if (Global.serverClient != null)
            {
                Global.serverClient.rpc(creditPlayerKillVehicle, bytesToAdd.ToArray());
            }
        }
    }

    public class RPCChangeDamage : RPC
    {
        public RPCChangeDamage()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            float damage = BitConverter.ToSingle(new byte[] { arguments[2], arguments[3], arguments[4], arguments[5] }, 0);
            int flinch = (int)arguments[6];
            
            var proj = Global.level.getActorByNetId(netId) as Projectile;
            if (proj?.damager != null)
            {
                proj.damager.damage = damage;
                proj.damager.flinch = flinch;
            }
        }

        public void sendRpc(ushort netId, float damage, int flinch)
        {
            if (Global.serverClient == null) return;

            byte[] netIdBytes = BitConverter.GetBytes(netId);
            byte[] damageBytes = BitConverter.GetBytes(damage);

            Global.serverClient.rpc(RPC.changeDamage, netIdBytes[0], netIdBytes[1],
                damageBytes[0], damageBytes[1], damageBytes[2], damageBytes[3],
                (byte)flinch);
        }
    }

    public class RPCLogWeaponKills : RPC
    {
        public RPCLogWeaponKills()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            isServerMessage = true;
        }

        public void sendRpc()
        {
            Global.serverClient?.rpc(logWeaponKills);
        }
    }

    public class RPCCheckRAEnter : RPC
    {
        public RPCCheckRAEnter()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            ushort raNetId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
            int neutralId = arguments[3];
            int raNum = arguments[4];

            Player player = Global.level.getPlayerById(playerId);
            if (player == null) return;
            RideArmor ra = Global.level.getActorByNetId(raNetId) as RideArmor;
            if (ra == null) return;

            if (ra.isNeutral && ra.ownedByLocalPlayer && !ra.claimed && ra.character == null)
            {
                ra.claimed = true;
                RPC.raEnter.sendRpc(player.id, ra.netId, neutralId, raNum);
            }
        }

        public void sendRpc(int playerId, ushort? raNetId, int? neutralId, int raNum)
        {
            if (raNetId == null) return;
            if (neutralId == null) return;
            byte[] netIdBytes = BitConverter.GetBytes(raNetId.Value);
            Global.serverClient?.rpc(this, (byte)playerId, netIdBytes[0], netIdBytes[1], (byte)neutralId, (byte)raNum);
        }
    }

    public class RPCRAEnter : RPC
    {
        public RPCRAEnter()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];

            Player player = Global.level.getPlayerById(playerId);
            if (player == null) return;

            ushort oldRaNetId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
            int neutralId = arguments[3];
            int raNum = arguments[4];

            if (player.ownedByLocalPlayer && player.character != null)
            {
                var oldRa = Global.level.getActorByNetId(oldRaNetId) as RideArmor;
                var pos = player.character.pos;
                float oldRaHealth = oldRa.health;
                if (oldRa != null)
                {
                    pos = oldRa.pos;

                    oldRa.destroySelf(doRpcEvenIfNotOwned: true);
                }
                var ra = new RideArmor(player, pos, raNum, neutralId, player.getNextActorNetId(), true, sendRpc: true);
                ra.health = oldRaHealth;
                ra.putCharInRideArmor(player.character);
            }
        }

        public void sendRpc(int playerId, ushort? oldRaNetId, int neutralId, int raNum)
        {
            if (oldRaNetId == null) return;
            byte[] netIdBytes = BitConverter.GetBytes(oldRaNetId.Value);
            Global.serverClient?.rpc(this, (byte)playerId, netIdBytes[0], netIdBytes[1], (byte)neutralId, (byte)raNum);
        }
    }

    public class RPCCheckRCEnter : RPC
    {
        public RPCCheckRCEnter()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            ushort rcNetId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
            int neutralId = arguments[3];

            Player player = Global.level.getPlayerById(playerId);
            if (player == null) return;
            RideChaser rc = Global.level.getActorByNetId(rcNetId) as RideChaser;
            if (rc == null) return;

            if (rc.ownedByLocalPlayer && !rc.claimed && rc.character == null)
            {
                rc.claimed = true;
                RPC.rcEnter.sendRpc(player.id, rc.netId, neutralId);
            }
        }

        public void sendRpc(int playerId, ushort? rcNetId, int? neutralId)
        {
            if (rcNetId == null) return;
            if (neutralId == null) return;
            byte[] netIdBytes = BitConverter.GetBytes(rcNetId.Value);
            Global.serverClient?.rpc(this, (byte)playerId, netIdBytes[0], netIdBytes[1], (byte)neutralId);
        }
    }

    public class RPCRCEnter : RPC
    {
        public RPCRCEnter()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];

            Player player = Global.level.getPlayerById(playerId);
            if (player == null) return;

            ushort oldRcNetId = BitConverter.ToUInt16(new byte[] { arguments[1], arguments[2] }, 0);
            int neutralId = arguments[3];

            if (player.ownedByLocalPlayer && player.character != null)
            {
                var oldRc = Global.level.getActorByNetId(oldRcNetId) as RideChaser;
                var pos = player.character.pos;
                float oldRcHealth = oldRc.health;
                if (oldRc != null)
                {
                    pos = oldRc.pos;
                    oldRc.destroySelf(doRpcEvenIfNotOwned: true);
                }
                var ra = new RideChaser(player, pos, neutralId, player.getNextActorNetId(), true, sendRpc: true);
                ra.health = oldRcHealth;
                ra.putCharInRideChaser(player.character);
            }
        }

        public void sendRpc(int playerId, ushort? oldRaNetId, int neutralId)
        {
            if (oldRaNetId == null) return;
            byte[] netIdBytes = BitConverter.GetBytes(oldRaNetId.Value);
            Global.serverClient?.rpc(this, (byte)playerId, netIdBytes[0], netIdBytes[1], (byte)neutralId);
        }
    }

    public class RPCUseSubTank : RPC
    {
        public RPCUseSubTank()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            int subtankHealAmount = arguments[2];

            var actor = Global.level.getActorByNetId(netId);
            if (actor == null) return;
            if (actor is Character chr) chr.netSubtankHealAmount = subtankHealAmount;
            if (actor is Maverick mvk) mvk.netSubtankHealAmount = subtankHealAmount;
        }

        public void sendRpc(ushort? netId, int subtankHealAmount)
        {
            if (netId == null) return;
            byte[] netIdBytes = BitConverter.GetBytes(netId.Value);
            Global.serverClient?.rpc(this, netIdBytes[0], netIdBytes[1], (byte)subtankHealAmount);
        }
    }

    public class RPCPossess : RPC
    {
        public RPCPossess()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int possesserPlayerId = arguments[0];
            int victimPlayerId = arguments[1];
            bool isUnpossess = arguments[2] == 1;

            var possesser = Global.level.getPlayerById(possesserPlayerId);
            var victim = Global.level.getPlayerById(victimPlayerId);
            
            if (isUnpossess)
            {
                victim?.unpossess();
            }
            else if (possesser != null)
            {
                victim?.startPossess(possesser);
            }
        }

        public void sendRpc(int possesserPlayerId, int victimPlayerId, bool isUnpossess)
        {
            Global.serverClient?.rpc(this, (byte)possesserPlayerId, (byte)victimPlayerId, isUnpossess ? (byte)1 : (byte)0);
        }
    }

    public class RPCSyncPossessInput : RPC
    {
        public RPCSyncPossessInput()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int playerId = arguments[0];
            var player = Global.level.getPlayerById(playerId);
            if (player == null) return;
            if (!player.isPossessed()) return;

            bool[] inputHeldArray = Helpers.byteToBoolArray(arguments[1]);
            bool[] inputPressedArray = Helpers.byteToBoolArray(arguments[2]);

            player.input.possessedControlHeld[Control.Left] = inputHeldArray[0];
            player.input.possessedControlHeld[Control.Right] = inputHeldArray[1];
            player.input.possessedControlHeld[Control.Up] = inputHeldArray[2];
            player.input.possessedControlHeld[Control.Down] = inputHeldArray[3];
            player.input.possessedControlHeld[Control.Jump] = inputHeldArray[4];
            player.input.possessedControlHeld[Control.Dash] = inputHeldArray[5];
            player.input.possessedControlHeld[Control.Taunt] = inputHeldArray[6];

            player.input.possessedControlPressed[Control.Left] = inputPressedArray[0];
            player.input.possessedControlPressed[Control.Right] = inputPressedArray[1];
            player.input.possessedControlPressed[Control.Up] = inputPressedArray[2];
            player.input.possessedControlPressed[Control.Down] = inputPressedArray[3];
            player.input.possessedControlPressed[Control.Jump] = inputPressedArray[4];
            player.input.possessedControlPressed[Control.Dash] = inputPressedArray[5];
            player.input.possessedControlPressed[Control.Taunt] = inputPressedArray[6];
        }

        public void sendRpc(int playerId, byte inputHeldByte, byte inputPressedByte)
        {
            Global.serverClient?.rpc(this, (byte)playerId, inputHeldByte, inputPressedByte);
        }
    }

    public class RPCFeedWheelGator : RPC
    {
        public RPCFeedWheelGator()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            float damage = BitConverter.ToSingle(new byte[] { arguments[2], arguments[3], arguments[4], arguments[5] }, 0);

            var wheelGator = Global.level.getActorByNetId(netId) as WheelGator;
            if (wheelGator != null)
            {
                wheelGator.feedWheelGator(damage);
            }
        }

        public void sendRpc(WheelGator wheelGator, float damage)
        {
            if (wheelGator?.netId == null || Global.serverClient == null) return;

            byte[] netIdBytes = BitConverter.GetBytes(wheelGator.netId.Value);
            byte[] damageBytes = BitConverter.GetBytes(damage);

            Global.serverClient.rpc(RPC.feedWheelGator, netIdBytes[0], netIdBytes[1],
                damageBytes[0], damageBytes[1], damageBytes[2], damageBytes[3]);
        }
    }

    public class RPCHealDoppler : RPC
    {
        public RPCHealDoppler()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort netId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            float damage = BitConverter.ToSingle(new byte[] { arguments[2], arguments[3], arguments[4], arguments[5] }, 0);
            int attackerPlayerId = arguments[6];

            var player = Global.level.getPlayerById(attackerPlayerId);
            var drDoppler = Global.level.getActorByNetId(netId) as DrDoppler;
            if (drDoppler != null)
            {
                drDoppler.healDrDoppler(player, damage);
            }
        }

        public void sendRpc(DrDoppler drDoppler, float damage, Player attacker)
        {
            if (drDoppler.netId == null || Global.serverClient == null) return;

            byte[] netIdBytes = BitConverter.GetBytes(drDoppler.netId.Value);
            byte[] damageBytes = BitConverter.GetBytes(damage);

            Global.serverClient.rpc(RPC.healDoppler, netIdBytes[0], netIdBytes[1],
                damageBytes[0], damageBytes[1], damageBytes[2], damageBytes[3], (byte)attacker.id);
        }
    }

    public class RPCResetFlag : RPC
    {
        public RPCResetFlag()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            if (!Global.isHost)
            {
                if (Global.level?.redFlag?.ownedByLocalPlayer == true || Global.level?.blueFlag?.ownedByLocalPlayer == true)
                {
                    Logger.logException(new Exception("A non-host owned the flags. Removing ownership"), false);
                    Global.level.redFlag.ownedByLocalPlayer = false;
                    Global.level.redFlag.frameSpeed = 0;
                    Global.level.blueFlag.ownedByLocalPlayer = false;
                    Global.level.blueFlag.frameSpeed = 0;
                    return;
                }
            }
            else
            {
                Global.level?.resetFlags();
            }
        }

        public void sendRpc()
        {
            Global.serverClient?.rpc(RPC.resetFlags);
        }
    }
}
