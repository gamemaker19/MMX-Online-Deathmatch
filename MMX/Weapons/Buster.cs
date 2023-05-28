using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Buster : Weapon
    {
        public List<BusterProj> lemonsOnField = new List<BusterProj>();
        public bool isUnpoBuster;
        public void setUnpoBuster()
        {
            isUnpoBuster = true;
            rateOfFire = 0.75f;
            weaponBarBaseIndex = 70;
            weaponBarIndex = 59;
            weaponSlotIndex = 121;
            killFeedIndex = 180;
        }

        public static bool isNormalBuster(Weapon weapon)
        {
            return weapon is Buster buster && !buster.isUnpoBuster;
        }

        public static bool isWeaponUnpoBuster(Weapon weapon)
        {
            return weapon is Buster buster && buster.isUnpoBuster;
        }

        public Buster() : base()
        {
            index = (int)WeaponIds.Buster;
            killFeedIndex = 0;
            weaponBarBaseIndex = 0;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 0;
            shootSounds = new List<string>() { "", "", "", "" };
            rateOfFire = 0.15f;
        }

        public override bool canShoot(int chargeLevel, Player player)
        {
            if (!base.canShoot(chargeLevel, player)) return false;
            if (chargeLevel > 1)
            {
                return true;
            }
            for (int i = lemonsOnField.Count - 1; i >= 0; i--)
            {
                if (lemonsOnField[i].destroyed)
                {
                    lemonsOnField.RemoveAt(i);
                    continue;
                }
            }
            if (player.character?.isHyperX == true) return true;
            return lemonsOnField.Count < 3;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            string shootSound = "buster";
            if (chargeLevel >= 1) shootSound = "buster2";
            if (chargeLevel >= 2) shootSound = "buster3";
            if (chargeLevel >= 3) shootSound = "buster4";

            bool hasUltArmor = player.character.hasUltimateArmorBS.getValue();
            if (player.character?.isHyperX == true && chargeLevel > 0)
            {
                new BusterUnpoProj(this, pos, xDir, player, netProjId);
                new Anim(pos, "buster_unpo_muzzle", xDir, null, true);
                shootSound = "buster2";
            }
            else if (player.character.stockedCharge)
            {
                player.character.changeState(new X2ChargeShot(1), true);
            }
            else if (chargeLevel == 0)
            {
                lemonsOnField.Add(new BusterProj(this, pos, xDir, 0, player, netProjId));
            }
            else if (chargeLevel == 1)
            {
                new Buster2Proj(this, pos, xDir, player, netProjId);
            }
            else if (chargeLevel == 2)
            {
                new Buster3Proj(this, pos, xDir, 0, player, netProjId);
            }
            else if (chargeLevel == 3)
            {
                if (hasUltArmor)
                {
                    if (player.hasArmArmor(2))
                    {
                        player.character.changeState(new X2ChargeShot(2), true);
                    }
                    else
                    {
                        new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
                        new BusterPlasmaProj(this, pos, xDir, player, netProjId);
                    }
                    shootSound = "plasmaShot";
                }
                else if (player.hasArmArmor(0) || player.hasArmArmor(1))
                {
                    new Anim(pos.clone(), "buster4_muzzle_flash", xDir, null, true);
                    //Create the buster effect
                    var xOff = -50 * xDir;
                    player.setNextActorNetId(netProjId);
                    createBuster4Line(pos.x + xOff, pos.y, xDir, player, 0);
                    createBuster4Line(pos.x + xOff + 15 * xDir, pos.y, xDir, player, 1);
                    createBuster4Line(pos.x + xOff + 30 * xDir, pos.y, xDir, player, 2);
                }
                else if (player.hasArmArmor(2))
                {
                    player.character.changeState(new X2ChargeShot(0), true);
                }
                else if (player.hasArmArmor(3))
                {
                    player.character.changeState(new X3ChargeShot(null), true);
                }
            }

            if (player?.character?.ownedByLocalPlayer == true)
            {
                player.character.playSound(shootSound, sendRpc: true);
            }
        }
    }

    public class BusterProj : Projectile
    {
        public BusterProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 350, 1, player, "buster1", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "buster1_fade";
            reflectable = true;
            maxTime = 0.5f;
            if (type == 0) projId = (int)ProjIds.Buster;
            else if (type == 1) projId = (int)ProjIds.ZBuster;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class Buster2Proj : Projectile
    {
        public Buster2Proj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) : base(weapon, pos, xDir, 350, 2, player, "buster2", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "buster2_fade";
            reflectable = true;
            maxTime = 0.5f;
            projId = (int)ProjIds.Buster2;
            var busterWeapon = weapon as Buster;
            if (busterWeapon != null)
            {
                damager.damage = busterWeapon.getDamage(damager.damage);
            }
        }
    }

    public class BusterUnpoProj : Projectile
    {
        public BusterUnpoProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) : 
            base(weapon, pos, xDir, 350, 3, player, "buster_unpo", Global.defFlinch, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "buster3_fade";
            reflectable = true;
            maxTime = 0.5f;
            projId = (int)ProjIds.BusterUnpo;
        }
    }

    public class Buster3Proj : Projectile
    {
        public int type;
        public List<Sprite> spriteMids = new List<Sprite>();
        public List<Sprite> spriteMids2 = new List<Sprite>();
        float partTime;
        public Buster3Proj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 350, 3, player, "buster3", Global.defFlinch, 0f, netProjId, player.ownedByLocalPlayer)
        {
            this.type = type;
            maxTime = 0.5f;
            fadeSprite = "buster3_fade";
            // Regular yellow charge
            if (type == 0)
            {
                damager.flinch = Global.halfFlinch;
            }
            // Double buster part 1
            if (type == 1)
            {
                damager.damage = 4;
                changeSprite("buster3_x2", true);
            }
            // Double buster part 2
            if (type == 2)
            {
                damager.damage = 4;
                changeSprite("buster4_x2", true);
                fadeSprite = "buster4_x2_fade";
                for (int i = 0; i < 4; i++)
                {
                    var midSprite = Global.sprites["buster4_x2_orbit"].clone();
                    spriteMids.Add(midSprite);
                    midSprite = Global.sprites["buster4_x2_orbit"].clone();
                    spriteMids2.Add(midSprite);
                }
            }
            // X3 buster part 1
            if (type == 3)
            {
                damager.damage = 4;
                changeSprite("buster4_x3", true);
                fadeSprite = "buster4_x2_fade";
                vel.x = 0;
                maxTime = 0.75f;
            }

            reflectable = true;
            projId = (int)ProjIds.Buster3;
            var busterWeapon = weapon as Buster;
            if (busterWeapon != null)
            {
                damager.damage = busterWeapon.getDamage(damager.damage);
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir, (byte)type);
            }
        }

        public override void update()
        {
            base.update();
            if (type == 3)
            {
                vel.x += Global.spf * xDir * 550;
                if (MathF.Abs(vel.x) > 300) vel.x = 300 * xDir;
                partTime += Global.spf;
                if (partTime > 0.05f)
                {
                    partTime = 0;
                    new Anim(pos.addRand(0, 16), "buster4_x3_part", 1, null, true) { acc = new Point(-vel.x * 3f, 0) };
                }
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (type == 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float t = Global.time - (i * 0.05f);
                    float sinX = 10 * MathF.Sin(Global.time * 15);
                    float sinY = 15 * MathF.Sin(t * 20);
                    spriteMids[i].draw(spriteMids[i].frameIndex, pos.x + x + sinX - (i * xDir * 3), pos.y + y + sinY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
                    spriteMids[i].update();
                }
                for (int i = 0; i < 4; i++)
                {
                    float t = Global.time - 1 - (i * 0.05f);
                    float sinX = 10 * MathF.Sin((Global.time - 1) * 15);
                    float sinY = 15 * MathF.Sin(t * 20);
                    spriteMids2[i].draw(spriteMids2[i].frameIndex, pos.x + x + sinX - (i * xDir * 3), pos.y + y + sinY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
                    spriteMids2[i].update();
                }
            }
        }
    }

    public class Buster4Proj : Projectile
    {
        public int type = 0;
        public float num = 0;
        public float offsetTime = 0;
        public float initY = 0;

        public Buster4Proj(Weapon weapon, Point pos, int xDir, Player player, int type, float num, float offsetTime, ushort netProjId) : base(weapon, pos, xDir, 400, 4, player, "buster4", Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "buster4_fade";
            this.type = type;
            //this.vel.x = 0;
            initY = this.pos.y;
            this.offsetTime = offsetTime;
            this.num = num;
            reflectable = true;
            maxTime = 0.5f;
            projId = (int)ProjIds.Buster4;
            var busterWeapon = weapon as Buster;
            if (busterWeapon != null)
            {
                damager.damage = busterWeapon.getDamage(damager.damage);
            }
        }

        public override void update()
        {
            base.update();

            frameIndex = type;
            var y = initY + MathF.Sin(Global.level.time * 18 - num * 0.5f + offsetTime * 2.09f) * 15;
            changePos(new Point(pos.x, y));
        }
    }

    public class X2ChargeShot : CharState
    {
        bool fired;
        int type;
        public X2ChargeShot(int type) : base("x2_shot", "", "", "")
        {
            this.type = type;
        }

        public override void update()
        {
            base.update();
            if (!character.grounded)
            {
                airCode();
                if (player.input.isHeld(Control.Dash, player))
                {
                    character.isDashing = true;
                }
            }
            if (!fired && character.currentFrame.getBusterOffset() != null)
            {
                fired = true;
                if (type == 0)
                {
                    new Buster3Proj(player.weapon, character.getShootPos(), character.getShootXDir(), 1, player, player.getNextActorNetId(), rpc: true);
                }
                else if (type == 1)
                {
                    new Buster3Proj(player.weapon, character.getShootPos(), character.getShootXDir(), 2, player, player.getNextActorNetId(), rpc: true);
                }
                else if (type == 2)
                {
                    new Anim(character.getShootPos(), "buster4_muzzle_flash", character.getShootXDir(), player.getNextActorNetId(), true, sendRpc: true);
                    new BusterPlasmaProj(player.weapon, character.getShootPos(), character.getShootXDir(), player, player.getNextActorNetId(), rpc: true);
                }
            }
            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            bool air = !character.grounded;
            if (type == 0 && !air)
            {
                character.changeSpriteFromName("x2_shot", true);
            }
            if (type == 1 && !air)
            {
                character.changeSpriteFromName("x2_shot2", true);
            }
            if (type == 0 && air)
            {
                character.changeSpriteFromName("x2_air_shot", true);
                landSprite = "x2_shot";
            }
            if (type == 1 && air)
            {
                character.changeSpriteFromName("x2_air_shot2", true);
                landSprite = "x2_shot2";
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class X3ChargeShot : CharState
    {
        bool fired;
        int state = 0;
        public HyperBuster hyperBusterWeapon;
        public X3ChargeShot(HyperBuster hyperBusterWeapon) : base("x3_shot", "", "", "")
        {
            this.hyperBusterWeapon = hyperBusterWeapon;
        }

        public override void update()
        {
            base.update();
            if (!character.ownedByLocalPlayer) return;

            if (!character.grounded)
            {
                airCode();
                if (player.input.isHeld(Control.Dash, player))
                {
                    character.isDashing = true;
                }
            }
            else
            {
                character.turnToInput(player.input, player);
            }

            if (!fired && character.currentFrame.getBusterOffset() != null)
            {
                fired = true;
                if (state == 0)
                {
                    new Anim(character.getShootPos(), "buster4_x3_muzzle", character.getShootXDir(), player.getNextActorNetId(), true, sendRpc: true);
                    new Buster3Proj(player.weapon, character.getShootPos(), character.getShootXDir(), 3, player, player.getNextActorNetId(), rpc: true);
                }
                else
                {
                    if (hyperBusterWeapon != null)
                    {
                        hyperBusterWeapon.ammo -= hyperBusterWeapon.getChipFactoredAmmoUsage(player);
                    }
                    character.playSound("buster4", sendRpc: true);
                    new BusterX3Proj2(player.weapon, character.getShootPos(), character.getShootXDir(), 0, player, player.getNextActorNetId(), rpc: true);
                    new BusterX3Proj2(player.weapon, character.getShootPos(), character.getShootXDir(), 1, player, player.getNextActorNetId(), rpc: true);
                    new BusterX3Proj2(player.weapon, character.getShootPos(), character.getShootXDir(), 2, player, player.getNextActorNetId(), rpc: true);
                    new BusterX3Proj2(player.weapon, character.getShootPos(), character.getShootXDir(), 3, player, player.getNextActorNetId(), rpc: true);
                }
            }
            if (character.isAnimOver())
            {
                if (state == 0)
                {
                    if (hyperBusterWeapon != null)
                    {
                        if (hyperBusterWeapon.ammo < hyperBusterWeapon.getChipFactoredAmmoUsage(player))
                        {
                            if (character.grounded) character.changeState(new Idle(), true);
                            else character.changeState(new Fall(), true);
                            return;
                        }
                    }

                    if (character.grounded) character.changeSpriteFromName("x3_shot2", true);
                    else character.changeSpriteFromName("x3_air_shot2", true);
                    landSprite = "x3_shot2";
                    state = 1;
                    fired = false;
                }
                else
                {
                    if (character.grounded) character.changeState(new Idle(), true);
                    else character.changeState(new Fall(), true);
                }
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (character.grounded)
            {
                sprite = "x3_shot";
                character.changeSpriteFromName(sprite, true);
            }
            else
            {
                sprite = "x3_air_shot";
                landSprite = "x3_shot";
                character.changeSpriteFromName(sprite, true);
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class BusterX3Proj2 : Projectile
    {
        public int type = 0;
        public List<Point> lastPositions = new List<Point>();
        public BusterX3Proj2(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 400, 1, player, type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "buster4_fade";
            this.type = type;
            reflectable = true;
            maxTime = 0.675f;
            projId = (int)ProjIds.BusterX3Proj2;
            if (type == 0) vel = new Point(-200 * xDir, -100);
            if (type == 1) vel = new Point(-150 * xDir, -50);
            if (type == 2) vel = new Point(-150 * xDir, 50);
            if (type == 3) vel = new Point(-200 * xDir, 100);
            frameSpeed = 0;
            frameIndex = 0;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir, (byte)type);
            }
        }

        public override void update()
        {
            base.update();
            float maxSpeed = 600;
            vel.inc(new Point(Global.spf * 1500 * xDir, 0));
            if (MathF.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
            lastPositions.Add(pos);
            if (lastPositions.Count > 4) lastPositions.RemoveAt(0);
        }

        public override void render(float x, float y)
        {
            string spriteName = type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2";
            //if (lastPositions.Count > 3) Global.sprites[spriteName].draw(1, lastPositions[3].x + x, lastPositions[3].y + y, 1, 1, null, 1, 1, 1, zIndex);
            if (lastPositions.Count > 2) Global.sprites[spriteName].draw(2, lastPositions[2].x + x, lastPositions[2].y + y, 1, 1, null, 1, 1, 1, zIndex);
            if (lastPositions.Count > 1) Global.sprites[spriteName].draw(3, lastPositions[1].x + x, lastPositions[1].y + y, 1, 1, null, 1, 1, 1, zIndex);
            base.render(x, y);
        }
    }

    public class BusterPlasmaProj : Projectile
    {
        public HashSet<IDamagable> hitDamagables = new HashSet<IDamagable>();
        public BusterPlasmaProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 400, 4, player, "buster_plasma", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.5f;
            projId = (int)ProjIds.BusterX3Plasma;
            destroyOnHit = false;
            
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (ownedByLocalPlayer && hitDamagables.Count < 1)
            {
                if (!hitDamagables.Contains(damagable))
                {
                    hitDamagables.Add(damagable);
                    float xThreshold = 10;
                    Point targetPos = damagable.actor().getCenterPos();
                    float distToTarget = MathF.Abs(pos.x - targetPos.x);
                    Point spawnPoint = pos;
                    if (distToTarget > xThreshold) spawnPoint = new Point(targetPos.x + xThreshold * Math.Sign(pos.x - targetPos.x), pos.y);
                    new BusterPlasmaHitProj(weapon, spawnPoint, xDir, owner, owner.getNextActorNetId(), rpc: true);
                }
            }
        }
    }

    public class BusterPlasmaHitProj : Projectile
    {
        public BusterPlasmaHitProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "buster_plasma_hit", 0, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 2f;
            projId = (int)ProjIds.BusterX3PlasmaHit;
            destroyOnHit = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }
}
