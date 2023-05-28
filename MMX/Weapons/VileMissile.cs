using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum VileMissileType
    {
        None = -1,
        StunShot,
        HumerusCrush,
        PopcornDemon
    }

    public class VileMissile : Weapon
    {
        public string projSprite;
        public float vileAmmo;
        public VileMissile(VileMissileType vileMissileType) : base()
        {
            index = (int)WeaponIds.StunShot;
            weaponBarBaseIndex = 26;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 42;
            killFeedIndex = 17;
            type = (int)vileMissileType;

            if (vileMissileType == VileMissileType.None)
            {
                displayName = "None";
                description = new string[] { "Do not equip a Missile." };
                vileAmmo = 8;
                killFeedIndex = 126;
            }
            else if (vileMissileType == VileMissileType.StunShot)
            {
                rateOfFire = 0.75f;
                displayName = "Stun Shot";
                vileAmmo = 8;
                description = new string[] { "Stops enemies in their tracks,", "but deals no damage." };
                vileWeight = 3;
            }
            else if (vileMissileType == VileMissileType.HumerusCrush)
            {
                rateOfFire = 0.75f;
                displayName = "Humerus Crush";
                projSprite = "missile_hc_proj";
                vileAmmo = 8;
                description = new string[] { "This missile shoots straight", "and deals decent damage." };
                killFeedIndex = 74;
                vileWeight = 3;
            }
            else if (vileMissileType == VileMissileType.PopcornDemon)
            {
                rateOfFire = 0.75f;
                displayName = "Popcorn Demon";
                projSprite = "missile_pd_proj";
                vileAmmo = 12;
                description = new string[] { "This missile splits into 3", "and can cause great damage." };
                killFeedIndex = 76;
                vileWeight = 3;
            }
        }

        public override void vileShoot(WeaponIds weaponInput, Character character)
        {
            Player player = character.player;
            if (shootTime > 0 || !player.cannonWeapon.isCooldownPercentDone(0.5f)) return;

            if (character.charState is Idle || character.charState is Run || character.charState is Crouch)
            {
                if (character.tryUseVileAmmo(vileAmmo))
                {
                    if (!character.isVileMK2)
                    {
                        character.setVileShootTime(this);
                        character.changeState(new MissileAttack(), true);
                    }
                    else if (!character.charState.isGrabbing)
                    {
                        character.setVileShootTime(this);
                        MissileAttack.mk2ShootLogic(character, player.vileMissileWeapon.type == (int)VileMissileType.StunShot);
                    }
                }
            }
            else if (character.charState is InRideArmor)
            {
                if (!character.isVileMK2)
                {
                    character.setVileShootTime(this);
                    if (player.vileMissileWeapon.type == 2 || player.vileMissileWeapon.type == 1)
                    {
                        character.playSound("vileMissile", sendRpc: true);
                        new VileMissileProj(player.vileMissileWeapon, character.getFirstPOIOrDefault(), character.getShootXDir(), 0, character.player, character.player.getNextActorNetId(), new Point(character.xDir, 0), rpc: true);
                    }
                    else
                    {
                        new StunShotProj(this, character.pos.addxy(15 * character.xDir, -10), character.getShootXDir(), 0, player, player.getNextActorNetId(), character.getVileShootVel(true), rpc: true);
                    }
                }
                else
                {
                    character.setVileShootTime(this);
                    if (player.vileMissileWeapon.type == 2 || player.vileMissileWeapon.type == 1)
                    {
                        character.playSound("mk2rocket", sendRpc: true);
                        new VileMissileProj(player.vileMissileWeapon, character.getFirstPOIOrDefault(), character.getShootXDir(), 0, character.player, character.player.getNextActorNetId(), new Point(character.xDir, 0), rpc: true);
                    }
                    else
                    {
                        MissileAttack.mk2ShootLogic(character, true);
                    }
                }
            }
        }
    }

    public class VileMissileProj : Projectile
    {
        public VileMissile missileWeapon;
        bool split;
        int type;
        public VileMissileProj(VileMissile weapon, Point pos, int xDir, int type, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
            base(weapon, pos, xDir, 200, 3, player, weapon.projSprite, 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "explosion";
            fadeSound = "explosion";
            projId = (int)ProjIds.VileMissile;
            maxTime = 0.6f;
            destroyOnHit = true;
            fadeOnAutoDestroy = true;
            missileWeapon = weapon;
            reflectable2 = true;
            this.type = type;

            if (weapon.type == (int)VileMissileType.HumerusCrush)
            {
                damager.damage = 3;
                // damager.flinch = Global.halfFlinch;
                this.vel.x = xDir * 350;
                maxTime = 0.35f;
            }
            if (weapon.type == (int)VileMissileType.PopcornDemon)
            {
                projId = (int)ProjIds.PopcornDemon;
                damager.damage = 2;
            }
            if (type == 1)
            {
                projId = (int)ProjIds.PopcornDemonSplit;
                this.xDir = 1;
                this.vel = vel.Value.times(speed);
                angle = this.vel.angle;
                damager.damage = 2;
                damager.hitCooldown = 0;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (missileWeapon.type == (int)VileMissileType.PopcornDemon && type == 0 && !split)
            {
                if (time > 0.3f || owner.input.isPressed(Control.Special1, owner))
                {
                    split = true;
                    playSound("vileMissile", sendRpc: true);
                    destroySelfNoEffect();
                    new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, -1).normalize(), rpc: true);
                    new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, 0), rpc: true);
                    new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, 1).normalize(), rpc: true);
                }
            }
        }

        /*
        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);

            if (damagable is Character character)
            {
                var victimCenter = character.getCenterPos();
                var bombCenter = pos;
                var dirTo = bombCenter.directionToNorm(victimCenter);
                character.vel.y = dirTo.y * 150;
                character.xPushVel = dirTo.x * 300;
            }
        }
        */
    }

    public class VileMK2StunShot : Weapon
    {
        public VileMK2StunShot() : base()
        {
            rateOfFire = 0.75f;
            index = (int)WeaponIds.MK2StunShot;
            killFeedIndex = 67;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            new StunShotProj(this, pos, xDir, 0, player, netProjId);
        }
    }

    public class StunShotProj : Projectile
    {
        public StunShotProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, Point? vel = null, bool rpc = false) : 
            base(weapon, pos, xDir, 150, 0, player, type == 0 ? "vile_stun_shot" : "vile_ebomb_start", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "vile_stun_shot_fade";
            projId = (int)ProjIds.StunShot;
            maxTime = 0.75f;
            destroyOnHit = true;
            
            if (vel != null)
            {
                if (type == 0)
                {
                    var norm = vel.Value.normalize();
                    this.vel.x = norm.x * speed * player.character.getShootXDir();
                    this.vel.y = norm.y * speed;
                    this.vel.x *= 1.5f;
                    this.vel.y *= 2f;
                }
                else
                {
                    this.vel = vel.Value;
                }
            }
            
            if (type == 1)
            {
                damager.damage = 1;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }
    }

    public class VileMK2StunShotProj : Projectile
    {
        public VileMK2StunShotProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
            base(weapon, pos, xDir, 150, 1, player, "vile_stun_shot2", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "vile_stun_shot_fade";
            projId = (int)ProjIds.MK2StunShot;
            maxTime = 0.75f;
            destroyOnHit = true;

            if (vel != null)
            {
                var norm = vel.Value.normalize();
                this.vel.x = norm.x * speed * player.character.getShootXDir();
                this.vel.y = norm.y * speed;
                this.vel.x *= 1.5f;
                if (player.character.charState is InRideArmor) this.vel.y *= 1.5f;
                else this.vel.y *= 2f;
                if (this.vel.y == 0) this.vel.y = 5;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }
    }

    public class MissileAttack : CharState
    {
        public MissileAttack() : base("idle_shoot", "", "", "")
        {
        }

        public override void update()
        {
            base.update();

            groundCodeWithMove();

            if (character.sprite.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }

        public static void shootLogic(Character character)
        {
            Player player = character.player;
            bool isStunShot = player.vileMissileWeapon.type == (int)VileMissileType.StunShot;
            if (character.sprite.getCurrentFrame().POIs.IsNullOrEmpty()) return;
            Point shootVel = character.getVileShootVel(isStunShot);

            Point shootPos = character.setCannonAim(shootVel);

            if (isStunShot)
            {
                new StunShotProj(player.vileMissileWeapon, shootPos, character.getShootXDir(), 0, character.player, character.player.getNextActorNetId(), shootVel, rpc: true);
            }
            else
            {
                character.playSound("vileMissile", sendRpc: true);
                new VileMissileProj(player.vileMissileWeapon, shootPos, character.getShootXDir(), 0, character.player, character.player.getNextActorNetId(), shootVel, rpc: true);
            }
        }

        public static void mk2ShootLogic(Character character, bool isStunShot)
        {
            Player player = character.player;
            Point? headPosNullable = character.getVileMK2StunShotPos();
            if (headPosNullable == null) return;

            character.playSound("mk2rocket", sendRpc: true);
            new Anim(headPosNullable.Value, "dust", 1, character.player.getNextActorNetId(), true, true);

            if (isStunShot)
            {
                new VileMK2StunShotProj(new VileMK2StunShot(), headPosNullable.Value, character.getShootXDir(), character.player, character.player.getNextActorNetId(), character.getVileShootVel(true), rpc: true);
            }
            else
            {
                new VileMissileProj(player.vileMissileWeapon, headPosNullable.Value, character.getShootXDir(), 0, character.player, character.player.getNextActorNetId(), character.getVileShootVel(false), rpc: true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            shootLogic(character);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }
}
