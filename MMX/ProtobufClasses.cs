using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    [ProtoContract]
    public class RPCMatchOverResponse
    {
        [ProtoMember(1)] public HashSet<int> winningAlliances;
        [ProtoMember(2)] public string winMessage;
        [ProtoMember(3)] public string loseMessage;
        [ProtoMember(4)] public string loseMessage2;
        [ProtoMember(5)] public string winMessage2;

        public RPCMatchOverResponse() { }
    }

    [ProtoContract]
    public class RPCAnimModel
    {
        [ProtoMember(1)] public long? zIndex;
        [ProtoMember(2)] public ushort? zIndexRelActorNetId;
        [ProtoMember(3)] public bool fadeIn;
        [ProtoMember(4)] public bool hasRaColorShader;

        public RPCAnimModel() { }
    }

    [ProtoContract]
    public class ControlPointResponseModel
    {
        [ProtoMember(1)] public int alliance;
        [ProtoMember(2)] public int num;
        [ProtoMember(3)] public bool locked;
        [ProtoMember(4)] public bool captured;
        [ProtoMember(5)] public float captureTime;
        public ControlPointResponseModel() { }
    }

    [ProtoContract]
    public class MagnetMineResponseModel
    {
        [ProtoMember(1)] public float x;
        [ProtoMember(2)] public float y;
        [ProtoMember(3)] public ushort netId;
        [ProtoMember(4)] public int playerId;
        public MagnetMineResponseModel() { }
    }

    [ProtoContract]
    public class TurretResponseModel
    {
        [ProtoMember(1)] public float x;
        [ProtoMember(2)] public float y;
        [ProtoMember(3)] public ushort netId;
        [ProtoMember(4)] public int playerId;
        public TurretResponseModel() { }
    }

    [ProtoContract]
    public class JoinLateResponseModel
    {
        [ProtoMember(1)] public List<PlayerPB> players;
        [ProtoMember(2)] public ServerPlayer newPlayer;
        [ProtoMember(3)] public List<ControlPointResponseModel> controlPoints;
        [ProtoMember(4)] public List<MagnetMineResponseModel> magnetMines;
        [ProtoMember(5)] public List<TurretResponseModel> turrets;
        public JoinLateResponseModel() { }
    }

    [ProtoContract]
    public class PeriodicServerSyncModel
    {
        [ProtoMember(1)] public List<ServerPlayer> players;
        public PeriodicServerSyncModel() { }
    }

    [ProtoContract]
    public class PeriodicHostSyncModel
    {
        [ProtoMember(1)] public RPCMatchOverResponse matchOverResponse;
        [ProtoMember(2)] public int bluePoints;
        [ProtoMember(3)] public int redPoints;
        [ProtoMember(4)] public HashSet<byte> crackedWallBytes = new HashSet<byte>();
        [ProtoMember(5)] public byte virusStarted;
        [ProtoMember(6)] public byte safeZoneSpawnIndex;
    }

    [ProtoContract]
    public class PlayerPB
    {
        [ProtoMember(1)] public int newAlliance;
        [ProtoMember(2)] public int weaponIndex;
        [ProtoMember(3)] public bool isAI;
        [ProtoMember(6)] public int newCharNum;
        [ProtoMember(7)] public ushort curMaxNetId;
        [ProtoMember(8)] public bool warpedIn = false;
        [ProtoMember(9)] public float readyTime;
        [ProtoMember(10)] public bool spawnChar = false;
        [ProtoMember(11)] public ushort armorFlag;
        [ProtoMember(12)] public LoadoutData loadoutData;
        [ProtoMember(13)] public Disguise disguise;

        [ProtoMember(14)] public ushort? charNetId;
        [ProtoMember(15)] public ushort? charRollingShieldNetId;
        [ProtoMember(16)] public float charXPos;
        [ProtoMember(17)] public float charYPos;
        [ProtoMember(18)] public int charXDir;

        [ProtoMember(20)] public ServerPlayer serverPlayer;

        public PlayerPB() { }

        public PlayerPB(Player player)
        {
            serverPlayer = player.serverPlayer;
            
            newAlliance = player.newAlliance;
            // weaponIndex = player.weaponIndex;
            newCharNum = player.newCharNum;
            curMaxNetId = player.curMaxNetId;
            warpedIn = player.warpedIn;
            readyTime = player.readyTime;
            spawnChar = player.readyTextOver;
            armorFlag = player.armorFlag;
            loadoutData = player.loadout;
            disguise = player.disguise;
            charNetId = player.charNetId;
            charRollingShieldNetId = player.charRollingShieldNetId;
            charXPos = player.charXPos;
            charYPos = player.charYPos;
            charXDir = player.charXDir;
        }
    }
}