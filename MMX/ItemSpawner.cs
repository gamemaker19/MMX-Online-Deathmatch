using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ItemSpawner
    {
        public Point pos;
        public float respawnTime;
        public float time;
        public Type itemType;
        public GameObject currentItem;
        public int id;
        public static int autoIncId = 1;
        public int rideArmorType;
        public int xDir;
        public bool isStacked;
        public int stackAssignedPlayerId;
        public ItemSpawner(Point pos, Type itemType, int rideArmorType, float respawnTime, int xDir)
        {
            this.pos = pos;
            this.itemType = itemType;
            this.respawnTime = respawnTime;
            this.rideArmorType = rideArmorType;
            this.xDir = xDir;
            id = autoIncId++;
            time = respawnTime + 0.1f;
        }

        public void raUpdate()
        {
            if (!Global.isHost) return;

            // Check if the vehicle still exists
            RideArmor myRideArmor = null;
            foreach (var actor in Global.level.gameObjects)
            {
                if (actor is RideArmor ra2 && ra2.neutralId == id)
                {
                    myRideArmor = ra2;
                    break;
                }
            }
            if (myRideArmor != null && !myRideArmor.destroyed)
            {
                time = 0;
                return;
            }
            
            time += Global.spf;
            if (Global.level.levelData.isTraining()) respawnTime = 1;
            if (time > respawnTime)
            {
                time = 0;
                var ra = new RideArmor(Global.level.mainPlayer, pos.clone(), rideArmorType, id, Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
                ra.xDir = xDir;
            }
        }

        public void rcUpdate()
        {
            if (!Global.isHost) return;

            // Check if the vehicle still exists
            RideChaser myRideChaser = null;
            foreach (var actor in Global.level.gameObjects)
            {
                if (actor is RideChaser rc && rc.neutralId == id)
                {
                    myRideChaser = rc;
                    break;
                }
            }
            if (myRideChaser != null && !myRideChaser.destroyed)
            {
                time = 0;
                return;
            }

            time += Global.spf;
            if (Global.level.levelData.isTraining()) respawnTime = 1;
            if (Global.level.isRace()) respawnTime = 1;
            if (time > respawnTime)
            {
                if (!isStacked)
                {
                    spawnRc();
                }
                else
                {
                    foreach (var player in Global.level.players)
                    {
                        if (player.id == stackAssignedPlayerId && player.character != null && player.character.rideChaser == null && player.character.pos.distanceTo(pos) < 100)
                        {
                            spawnRc();
                        }
                    }
                }
            }
        }

        public void spawnRc()
        {
            time = 0;
            var rc = new RideChaser(Global.level.mainPlayer, pos.clone(), id, Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
            rc.xDir = xDir;
        }

        public void update()
        {
            if (itemType == typeof(RideArmor))
            {
                raUpdate();
                return;
            }

            if (itemType == typeof(RideChaser))
            {
                rcUpdate();
                return;
            }

            if (!Global.isHost) return;
            if (Global.level.isTraining() && !Global.spawnTrainingHealth &&
                (itemType == typeof(LargeAmmoPickup) || itemType == typeof(SmallAmmoPickup) || itemType == typeof(LargeHealthPickup) || itemType == typeof(SmallHealthPickup)))
            {
                return;
            }

            if (Global.level.hasGameObject(currentItem))
            {
                time = 0;
                return;
            }

            time += Global.spf;
            if (Global.level.levelData.isTraining()) respawnTime = 1;
            if (time > respawnTime)
            {
                time = 0;
                if (itemType == typeof(LargeAmmoPickup))
                {
                    currentItem = new LargeAmmoPickup(Global.level.mainPlayer, pos.clone(), Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
                }
                else if (itemType == typeof(SmallAmmoPickup))
                {
                    currentItem = new SmallAmmoPickup(Global.level.mainPlayer, pos.clone(), Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
                }
                else if (itemType == typeof(LargeHealthPickup))
                {
                    currentItem = new LargeHealthPickup(Global.level.mainPlayer, pos.clone(), Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
                }
                else if (itemType == typeof(SmallHealthPickup))
                {
                    currentItem = new SmallHealthPickup(Global.level.mainPlayer, pos.clone(), Global.level.mainPlayer.getNextActorNetId(), true, sendRpc: true);
                }
            }
        }
    }
}
