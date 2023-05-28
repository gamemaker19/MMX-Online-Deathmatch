using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class BubbleSplash : Weapon
    {
        public List<BubbleSplashProj> bubblesOnField = new List<BubbleSplashProj>();
        public List<float> bubbleAfterlifeTimes = new List<float>();
        public float hyperChargeDelay;

        public BubbleSplash() : base()
        {
            shootSounds = new List<string>() { "bubbleSplash", "bubbleSplash", "bubbleSplash", "bubbleSplashCharged" };
            rateOfFire = 0.1f;
            isStream = true;
            index = (int)WeaponIds.BubbleSplash;
            weaponBarBaseIndex = 10;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 10;
            killFeedIndex = 21;
            weaknessIndex = 12;
            maxStreams = 7;
            streamCooldown = 1;
            switchCooldown = 0.25f;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 3)
            {
                return base.getAmmoUsage(chargeLevel);
            }
            else
            {
                return 0.5f;// return Global.spf * 10;
            }
        }

        public override bool canShoot(int chargeLevel, Player player)
        {
            if (!base.canShoot(chargeLevel, player)) return false;
            if (hyperChargeDelay > 0) return false;

            return bubblesOnField.Count + bubbleAfterlifeTimes.Count < 7;
        }

        public override void update()
        {
            base.update();
            Helpers.decrementTime(ref hyperChargeDelay);

            for (int i = bubblesOnField.Count - 1; i >= 0; i--)
            {
                if (bubblesOnField[i].destroyed)
                {
                    float timeCutShort = bubblesOnField[i].maxTime - bubblesOnField[i].time;
                    bubbleAfterlifeTimes.Add(0.4f);
                    bubblesOnField.RemoveAt(i);
                    continue;
                }
            }

            for (int i = bubbleAfterlifeTimes.Count - 1; i >= 0; i--)
            {
                bubbleAfterlifeTimes[i] -= Global.spf;
                if (bubbleAfterlifeTimes[i] <= 0)
                {
                    bubbleAfterlifeTimes.RemoveAt(i);
                }
            }
        }

        // Friendly reminder that this method MUST be deterministic across all clients, i.e. don't vary it on a field that could vary locally.
        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                var proj = new BubbleSplashProj(this, pos, xDir, player, netProjId);
                bubblesOnField.Add(proj);
            }
            else if (chargeLevel == 3)
            {
                player.setNextActorNetId(netProjId);
                player.character.popAllBubbles();
                player.character.chargedBubbles.Add(new BubbleSplashProjCharged(this, pos, xDir, player, 0, player.getNextActorNetId(true)));
                player.character.chargedBubbles.Add(new BubbleSplashProjCharged(this, pos, xDir, player, 0.2f, player.getNextActorNetId(true)));
                player.character.chargedBubbles.Add(new BubbleSplashProjCharged(this, pos, xDir, player, 0.4f, player.getNextActorNetId(true)));
                player.character.chargedBubbles.Add(new BubbleSplashProjCharged(this, pos, xDir, player, 0.6f, player.getNextActorNetId(true)));
                player.character.chargedBubbles.Add(new BubbleSplashProjCharged(this, pos, xDir, player, 0.8f, player.getNextActorNetId(true)));
                player.character.chargedBubbles.Add(new BubbleSplashProjCharged(this, pos, xDir, player, 1f, player.getNextActorNetId(true)));
            }
        }
    }

    public class BubbleSplashProj : Projectile
    {
        public BubbleSplashProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 75, 1, player, "bubblesplash_proj_start", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.75f;
            useGravity = false;
            vel.y = -20 * Helpers.randomRange(0.75f, 1.25f);
            vel.x *= Helpers.randomRange(0.75f, 1.25f);

            if (!player.input.isHeld(Control.Up, player))
            {
                vel.y *= 0.5f;
                vel.x *= 1.75f;
            }
            else
            {
                vel.y *= 3;
            }

            fadeSprite = "bubblesplash_pop";
            fadeSound = "bubbleSplashPop";
            fadeOnAutoDestroy = true;
            projId = (int)ProjIds.BubbleSplash;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (sprite.name == "bubblesplash_proj_start" && isAnimOver())
            {
                int randColor = Helpers.randomRange(0, 2);
                if (randColor == 0) changeSprite("bubblesplash_proj", true);
                if (randColor == 1) changeSprite("bubblesplash_proj2", true);
                if (randColor == 2) changeSprite("bubblesplash_proj3", true);
            }
            vel.y -= Global.spf * 100;       
        }
    }

    public class BubbleSplashProjCharged : Projectile
    {
        public Character character;
        public float yPos;
        public float initTime;
        public BubbleSplashProjCharged(Weapon weapon, Point pos, int xDir, Player player, float time, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 75, 1, player, "bubblesplash_proj1", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            useGravity = false;
            fadeSprite = "bubblesplash_pop";

            int randColor = Helpers.randomRange(0, 2);
            if (randColor == 0) changeSprite("bubblesplash_proj2", true);
            if (randColor == 1) changeSprite("bubblesplash_proj3", true);
            character = player.character;
            initTime = time;
            this.time = time;
            sprite.wrapMode = "loop";
            projId = (int)ProjIds.BubbleSplashCharged;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (character == null || !Global.level.gameObjects.Contains(character) || (character.player.weapon is not BubbleSplash))
            {
                destroySelf();
                return;
            }
            time += Global.spf;
            if (time > 2) time = 0;

            float x = 20 * MathF.Sin(time * 5);
            yPos = -15 * time;
            Point newPos = character.pos.addxy(x, yPos);
            changePos(newPos);
        }

        public override void onDestroy()
        {
            if (character != null)
            {
                character.chargedBubbles.Remove(this);
            }
        }
    }
}
