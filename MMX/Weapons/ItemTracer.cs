using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ItemTracer : Weapon
    {
        public ItemTracer() : base()
        {
            shootSounds = new List<string>() { "", "", "", "" };
            rateOfFire = 1f;
            index = (int)WeaponIds.ItemTracer;
            weaponBarBaseIndex = 26;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 26;
            killFeedIndex = 20 + (index - 9);
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (player?.character == null || !player.character.ownedByLocalPlayer) return;

            player.character.scannerCooldown = 1;
            ammo -= 3;
            Character target = null;
            player.character.playSound("itemTracer", sendRpc: true);
            CollideData hit = Global.level.raycast(pos, pos.addxy(150 * xDir, 0), new List<Type>() { typeof(Actor) });
            if (hit?.gameObject is Character chr && chr.player.alliance != player.alliance && !chr.player.scanned)
            {
                target = chr;
            }
            new ItemTracerProj(this, pos, xDir, player, target, netProjId, rpc: true);
        }
    }

    public class ItemTracerProj : Projectile
    {
        public Character target;
        public Character scannedChar;
        public ItemTracerProj(Weapon weapon, Point pos, int xDir, Player player, Character target, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 300, 0, player, "itemscan_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 1f;
            destroyOnHit = false;
            shouldShieldBlock = false;
            frameSpeed = 0;
            projId = (int)ProjIds.ItemTracer;
            this.target = target;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (scannedChar != null)
            {
                changePos(scannedChar.getCenterPos());
            }
            if (target != null)
            {
                vel = pos.directionTo(target.getCenterPos()).normalize().times(speed);
            }
            if (isAnimOver())
            {
                destroySelf();
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (!ownedByLocalPlayer) return;
            var chr = damagable as Character;
            if (scannedChar == null && chr != null && !chr.isStealthy(damager.owner.alliance))
            {
                scannedChar = chr;
                if (damager.owner == Global.level.mainPlayer)
                {
                    if (scannedChar.player.scanned)
                    {
                        foreach (var player in Global.level.players)
                        {
                            player.tagged = false;
                        }
                        scannedChar.player.tagged = true;
                    }
                    scannedChar.player.scanned = true;
                }
                frameSpeed = 1;
                time = 0;
            }
        }
    }
}
