using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum NapalmType
    {
        NoneBall = -1,
        RumblingBang,
        FireGrenade,
        SplashHit,
        NoneFlamethrower,
    }

    public class Napalm : Weapon
    {
        public float vileAmmoUsage;
        public Napalm(NapalmType napalmType) : base()
        {
            index = (int)WeaponIds.Napalm;
            weaponBarBaseIndex = 0;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 30;
            type = (int)napalmType;

            if (napalmType == NapalmType.NoneBall)
            {
                displayName = "None(BALL)";
                description = new string[] { "Do not equip a Napalm.", "BALL will be used instead." };
                killFeedIndex = 126;
            }
            else if (napalmType == NapalmType.NoneFlamethrower)
            {
                displayName = "None(FLAMETHROWER)";
                description = new string[] { "Do not equip a Napalm.", "FLAMETHROWER will be used instead." };
                killFeedIndex = 126;
            }
            else if (napalmType == NapalmType.RumblingBang)
            {
                displayName = "Rumbling Bang";
                vileAmmoUsage = 8;
                rateOfFire = 2f;
                description = new string[] { "This napalm sports a wide horizontal", "range but cannot attack upward." };
                vileWeight = 3;
            }
            if (napalmType == NapalmType.FireGrenade)
            {
                displayName = "Fire Grenade";
                vileAmmoUsage = 16;
                rateOfFire = 4f;
                description = new string[] { "This napalm travels along the", "ground, laying a path of fire." };
                killFeedIndex = 54;
                vileWeight = 3;
            }
            if (napalmType == NapalmType.SplashHit)
            {
                displayName = "Splash Hit";
                vileAmmoUsage = 16;
                rateOfFire = 3f;
                description = new string[] { "This napalm can attack foes above,", "but has a narrow horizontal range." };
                killFeedIndex = 79;
                vileWeight = 3;
            }
        }

        public override void vileShoot(WeaponIds weaponInput, Character character)
        {
            if (type == (int)NapalmType.NoneBall || type == (int)NapalmType.NoneFlamethrower) return;
            if (shootTime == 0)
            {
                if (weaponInput == WeaponIds.Napalm)
                {
                    if (character.tryUseVileAmmo(vileAmmoUsage))
                    {
                        character.changeState(new NapalmAttack(NapalmAttackType.Napalm), true);
                    }
                }
                else if (weaponInput == WeaponIds.VileFlamethrower)
                {
                    var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
                    if (ground == null)
                    {
                        if (character.tryUseVileAmmo(vileAmmoUsage))
                        {
                            character.setVileShootTime(this);
                            character.changeState(new AirBombAttack(true), true);
                        }
                    }
                }
                else if (weaponInput == WeaponIds.VileBomb)
                {
                    var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
                    if (ground == null)
                    {
                        if (character.player.vileAmmo >= vileAmmoUsage)
                        {
                            character.setVileShootTime(this);
                            character.changeState(new AirBombAttack(true), true);
                        }
                    }
                }
            }
        }
    }

    public class NapalmGrenadeProj : Projectile
    {
        bool exploded;
        public NapalmGrenadeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 150, 2, player, "napalm_grenade", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.NapalmGrenade;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
            this.vel = new Point(speed * xDir, -200);
            useGravity = true;
            collider.wallOnly = true;
            fadeSound = "explosion";
            fadeSprite = "explosion";
            shouldShieldBlock = false;
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (grounded)
            {
                explode();
            }
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            xDir *= -1;
            explode();
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (ownedByLocalPlayer) explode();
        }

        public void explode()
        {
            if (exploded) return;
            exploded = true;
            for (int i = -3; i <= 3; i++)
            {
                new NapalmPartProj(weapon, pos.addxy(0, 0), 1, owner, owner.getNextActorNetId(), false, i * 10, rpc: true);
                new NapalmPartProj(weapon, pos.addxy(0, 0), 1, owner, owner.getNextActorNetId(), true, i * 10, rpc: true);
            }
            destroySelf();
        }
    }

    public class NapalmPartProj : Projectile
    {
        int times;
        float xDist;
        float maxXDist;
        float napalmTime;
        float timeOffset;
        float napalmPeriod = 0.5f;
        public NapalmPartProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool isTimeOffset, float xDist, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "napalm_part", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Napalm;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir, isTimeOffset ? (byte)1 : (byte)0);
            }
            useGravity = true;
            collider.wallOnly = true;
            destroyOnHit = false;
            shouldShieldBlock = false;
            gravityModifier = 0.25f;
            frameIndex = Helpers.randomRange(0, sprite.frames.Count - 1);
            if (isTimeOffset)
            {
                timeOffset = napalmPeriod * 0.5f;
            }
            maxXDist = xDist;
            visible = false;
        }

        public override void update()
        {
            base.update();

            if (isUnderwater())
            {
                destroySelf();
                return;
            }

            if (time < timeOffset) return;
            else visible = true;

            napalmTime += Global.spf;

            if (!Options.main.lowQualityParticles())
            {
                alpha = 2 * (napalmPeriod - napalmTime);
                xScale = 1 + (napalmTime * 2);
                yScale = 1 + (napalmTime * 2);
            }

            if (ownedByLocalPlayer)
            {
                if (xDist < MathF.Abs(maxXDist))
                {
                    xDist += MathF.Abs(maxXDist * 0.25f);
                    move(new Point(maxXDist * 0.25f, 0), useDeltaTime: false);
                }
            }
            
            if (napalmTime > napalmPeriod)
            {
                napalmTime = 0;
                times++;
                if (ownedByLocalPlayer && times >= 8)
                {
                    destroySelf();
                }
            }
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
        }
    }

    public enum NapalmAttackType
    {
        Napalm,
        Ball,
        Flamethrower,
    }

    public class NapalmAttack : CharState
    {
        bool shot;
        NapalmAttackType napalmAttackType;
        float shootTime;
        int shootCount;

        public NapalmAttack(NapalmAttackType napalmAttackType, string transitionSprite = "") : 
            base(getSprite(napalmAttackType), "", "", transitionSprite)
        {
            this.napalmAttackType = napalmAttackType;
        }

        public static string getSprite(NapalmAttackType napalmAttackType)
        {
            return napalmAttackType == NapalmAttackType.Flamethrower ? "crouch_flamethrower" : "crouch_nade";
        }

        public override void update()
        {
            base.update();

            if (napalmAttackType == NapalmAttackType.Napalm)
            {
                if (!shot && character.sprite.frameIndex == 2)
                {
                    shot = true;
                    character.setVileShootTime(player.vileNapalmWeapon);
                    var poi = character.sprite.getCurrentFrame().POIs[0];
                    poi.x *= character.xDir;

                    Projectile proj;
                    if (napalmAttackType == NapalmAttackType.Napalm)
                    {
                        if (player.vileNapalmWeapon.type == (int)NapalmType.RumblingBang)
                        {
                            proj = new NapalmGrenadeProj(player.vileNapalmWeapon, character.pos.add(poi), character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
                        }
                        else if (player.vileNapalmWeapon.type == (int)NapalmType.FireGrenade)
                        {
                            proj = new MK2NapalmGrenadeProj(player.vileNapalmWeapon, character.pos.add(poi), character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
                        }
                        else if (player.vileNapalmWeapon.type == (int)NapalmType.SplashHit)
                        {
                            proj = new SplashHitGrenadeProj(player.vileNapalmWeapon, character.pos.add(poi), character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
                        }
                    }
                }
            }
            else if (napalmAttackType == NapalmAttackType.Ball)
            {
                if (player.vileBallWeapon.type == (int)VileBallType.AirBombs)
                {
                    if (shootCount < 3 && character.sprite.frameIndex == 2)
                    {
                        if (!character.tryUseVileAmmo(player.vileBallWeapon.vileAmmoUsage))
                        {
                            character.changeState(new Crouch(""), true);
                            return;
                        }
                        shootCount++;
                        character.setVileShootTime(player.vileBallWeapon);
                        var poi = character.sprite.getCurrentFrame().POIs[0];
                        poi.x *= character.xDir;
                        Projectile proj = new VileBombProj(player.vileBallWeapon, character.pos.add(poi), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
                        proj.vel = new Point(character.xDir * 150, -200);
                        proj.maxTime = 0.6f;
                        character.sprite.frameIndex = 0;
                    }
                }
                else if (player.vileBallWeapon.type == (int)VileBallType.StunBalls)
                {
                    shootTime += Global.spf;
                    var poi = character.getFirstPOI();
                    if (shootTime > 0.06f && poi != null && shootCount <= 4)
                    {
                        if (!character.tryUseVileAmmo(player.vileBallWeapon.vileAmmoUsage))
                        {
                            character.changeState(new Crouch(""), true);
                            return;
                        }
                        shootTime = 0;
                        character.sprite.frameIndex = 1;
                        Point shootDir = Point.createFromAngle(-45).times(150);
                        if (shootCount == 1) shootDir = Point.createFromAngle(-22.5f).times(150);
                        if (shootCount == 2) shootDir = Point.createFromAngle(0).times(150);
                        if (shootCount == 3) shootDir = Point.createFromAngle(22.5f).times(150);
                        if (shootCount == 4) shootDir = Point.createFromAngle(45f).times(150);
                        new StunShotProj(player.vileBallWeapon, poi.Value, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(shootDir.x * character.xDir, shootDir.y), rpc: true);
                        shootCount++;
                    }
                }
                else if (player.vileBallWeapon.type == (int)VileBallType.PeaceOutRoller)
                {
                    if (!shot && character.sprite.frameIndex == 2)
                    {
                        if (!character.tryUseVileAmmo(player.vileBallWeapon.vileAmmoUsage))
                        {
                            character.changeState(new Crouch(""), true);
                            return;
                        }
                        shot = true;
                        character.setVileShootTime(player.vileBallWeapon);
                        var poi = character.sprite.getCurrentFrame().POIs[0];
                        poi.x *= character.xDir;
                        Projectile proj = new PeaceOutRollerProj(player.vileBallWeapon, character.pos.add(poi), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
                        proj.vel = new Point(character.xDir * 150, -200);
                        proj.gravityModifier = 1;
                    }
                }
            }
            else
            {
                shootTime += Global.spf;
                var poi = character.getFirstPOI();
                if (shootTime > 0.06f && poi != null)
                {
                    if (!character.tryUseVileAmmo(2))
                    {
                        character.changeState(new Crouch(""), true);
                        return;
                    }
                    shootTime = 0;
                    character.playSound("flamethrower");
                    new FlamethrowerProj(player.vileFlamethrowerWeapon, poi.Value, character.xDir, true, player, player.getNextActorNetId(), sendRpc: true);
                }

                if (character.loopCount > 4)
                {
                    character.changeState(new Crouch(""), true);
                    return;
                }
            }

            if (character.isAnimOver())
            {
                character.changeState(new Crouch(""), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class MK2NapalmGrenadeProj : Projectile
    {
        public MK2NapalmGrenadeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
            base(weapon, pos, xDir, 150, 1, player, "napalm_grenade2", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.NapalmGrenade2;
            this.vel = new Point(speed * xDir, -200);
            useGravity = true;
            collider.wallOnly = true;
            fadeSound = "explosion";
            fadeSprite = "explosion";

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (grounded)
            {
                destroySelf();
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            Point destroyPos = other?.hitData?.hitPoint ?? pos;
            changePos(destroyPos);
            destroySelf();
        }

        public override void onDestroy()
        {
            if (!ownedByLocalPlayer) return;
            new MK2NapalmProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
        }
    }

    public class MK2NapalmProj : Projectile
    {
        float flameCreateTime = 1;
        public MK2NapalmProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 100, 1f, player, "napalm2_proj", 0, 1f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 2;
            projId = (int)ProjIds.Napalm2;
            useGravity = true;
            collider.wallOnly = true;
            destroyOnHit = false;
            shouldShieldBlock = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            flameCreateTime += Global.spf;
            if (flameCreateTime > 0.1f)
            {
                flameCreateTime = 0;
                new MK2NapalmFlame(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
            }

            var hit = Global.level.checkCollisionActor(this, vel.x * Global.spf, 0, null);
            if (hit?.gameObject is Wall && hit?.hitData?.normal != null && !(hit.hitData.normal.Value.isAngled()))
            {
                new MK2NapalmWallProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
                destroySelf();
                return;
            }
            if (isUnderwater()) destroySelf();
        }
    }


    public class MK2NapalmFlame : Projectile
    {
        public MK2NapalmFlame(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "napalm2_flame", 0, 1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Napalm2Flame;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
            useGravity = true;
            collider.wallOnly = true;
            destroyOnHit = true;
            shouldShieldBlock = false;
            gravityModifier = 0.25f;
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (isUnderwater())
            {
                destroySelf();
                return;
            }

            if (loopCount > 8)
            {
                destroySelf();
                return;
            }
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
        }
    }

    public class MK2NapalmWallProj : Projectile
    {
        public MK2NapalmWallProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "napalm2_wall", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 1f;
            projId = (int)ProjIds.Napalm2Wall;
            vel = new Point(0, -200);
            destroyOnHit = false;
            shouldShieldBlock = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (isUnderwater()) destroySelf();
        }
    }


    public class SplashHitGrenadeProj : Projectile
    {
        bool exploded;
        public SplashHitGrenadeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 150, 2, player, "napalm_sh_grenade", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.NapalmGrenadeSplashHit;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
            this.vel = new Point(speed * xDir, -200);
            useGravity = true;
            collider.wallOnly = true;
            fadeSound = "explosion";
            fadeSprite = "explosion";
            shouldShieldBlock = false;
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (grounded)
            {
                explode();
            }
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            explode();
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (ownedByLocalPlayer) explode();
        }

        public void explode()
        {
            if (exploded) return;
            exploded = true;
            var hit = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, 100), new List<Type>() { typeof(Wall) });
            new SplashHitProj(weapon, hit?.getHitPointSafe() ?? pos, xDir, owner, owner.getNextActorNetId(), sendRpc: true);
            destroySelf();
        }
    }

    public class SplashHitProj : Projectile
    {
        Player player;
        public SplashHitProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 0, 1, player, "napalm_sh_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.NapalmSplashHit;
            shouldShieldBlock = false;
            shouldVortexSuck = false;
            destroyOnHit = false;
            maxTime = 1.5f;
            this.player = player;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
        }

        public override bool shouldDealDamage(IDamagable damagable)
        {
            if (damagable is Actor actor && MathF.Abs(pos.x - actor.pos.x) > 40)
            {
                return false;
            }
            return true;
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (damagable is Character chr)
            {
                float modifier = 1;
                if (chr.isUnderwater()) modifier = 2;
                if (chr.isImmuneToKnockback()) return;
                float xMoveVel = MathF.Sign(pos.x - chr.pos.x);
                chr.move(new Point(xMoveVel * 50 * modifier, 0));
            }
        }
    }
}