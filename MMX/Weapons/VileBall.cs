using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum VileBallType
    {
        NoneNapalm = -1,
        AirBombs,
        StunBalls,
        PeaceOutRoller,
        NoneFlamethrower,
    }

    public class VileBall : Weapon
    {
        public float vileAmmoUsage;
        public VileBall(VileBallType vileBallType) : base()
        {
            rateOfFire = 1f;
            index = (int)WeaponIds.VileBomb;
            weaponBarBaseIndex = 27;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 15;
            type = (int)vileBallType;

            if (vileBallType == VileBallType.NoneNapalm)
            {
                displayName = "None(NAPALM)";
                description = new string[] { "Do not equip a Ball.", "NAPALM will be used instead." };
                killFeedIndex = 126;
            }
            else if (vileBallType == VileBallType.NoneFlamethrower)
            {
                displayName = "None(FLAMETHROWER)";
                description = new string[] { "Do not equip a Ball.", "FLAMETHROWER will be used instead." };
                killFeedIndex = 126;
            }
            else if (vileBallType == VileBallType.AirBombs)
            {
                displayName = "Knee Bombs";
                vileAmmoUsage = 8;
                description = new string[] { "These bombs split into two", "upon contact with the ground." };
                vileWeight = 3;
            }
            else if (vileBallType == VileBallType.StunBalls)
            {
                displayName = "Stun Balls";
                vileAmmoUsage = 5;
                description = new string[] { "Unleash a fan of energy balls", "that stun enemies in their tracks." };
                killFeedIndex = 55;
                vileWeight = 3;
            }
            else if (vileBallType == VileBallType.PeaceOutRoller)
            {
                displayName = "Peace Out Roller";
                vileAmmoUsage = 16;
                rateOfFire = 1.25f;
                description = new string[] { "This electric ball splits into two upon", "upon contact with the ground." };
                killFeedIndex = 80;
                vileWeight = 3;
            }
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return vileAmmoUsage;
        }

        public override void vileShoot(WeaponIds weaponInput, Character character)
        {
            if (type == (int)VileBallType.NoneNapalm || type == (int)VileBallType.NoneFlamethrower) return;
            if (shootTime == 0)
            {
                if (weaponInput == WeaponIds.VileBomb)
                {
                    var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
                    if (ground == null)
                    {
                        if (character.tryUseVileAmmo(vileAmmoUsage))
                        {
                            character.setVileShootTime(this);
                            character.changeState(new AirBombAttack(false), true);
                        }
                    }
                }
                else if (weaponInput == WeaponIds.Napalm)
                {
                    character.setVileShootTime(this);
                    character.changeState(new NapalmAttack(NapalmAttackType.Ball), true);
                }
                else if (weaponInput == WeaponIds.VileFlamethrower)
                {
                    var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
                    if (ground == null)
                    {
                        if (character.tryUseVileAmmo(vileAmmoUsage))
                        {
                            character.setVileShootTime(this);
                            character.changeState(new AirBombAttack(false), true);
                        }
                    }
                }
            }
        }
    }

    public class VileBombProj : Projectile
    {
        int type;
        bool split;
        public VileBombProj(Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId, Point? vel = null, bool rpc = false) : 
            base(weapon, pos, xDir, 100, 2, player, type == 0 ? "vile_bomb_air" : "vile_bomb_ground", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VileBomb;
            if (type == 0) maxTime = 0.45f;
            if (type == 1) maxTime = 0.3f;
            destroyOnHit = true;
            this.type = type;

            if (vel != null) this.vel = (Point)vel;
            if (type == 0)
            {
                fadeSprite = "explosion";
                fadeSound = "explosion";
                useGravity = true;
            }
            else
            {
                projId = (int)ProjIds.VileBombSplit;
                fadeSprite = "vile_stun_shot_fade";
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

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            if (!other.gameObject.collider.isClimbable) return;
            if (split) return;
            if (type == 0)
            {
                var normal = other?.hitData?.normal;
                if (normal != null)
                {
                    normal = normal.Value.leftNormal();
                }
                else
                {
                    normal = new Point(1, 0);
                }
                Point normal2 = (Point)normal;
                normal2.multiply(300);
                destroySelf(fadeSprite);
                split = true;
                new VileBombProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2, rpc: true);
                new VileBombProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2.times(-1), rpc: true);
                destroySelf();
            }
        }
    }

    public class PeaceOutRollerProj : Projectile
    {
        int type;
        bool split;
        public PeaceOutRollerProj(Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId, Point? vel = null, bool rpc = false) :
            base(weapon, pos, xDir, 75, 3, player, "ball_por_proj", 1, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.PeaceOutRoller;
            maxTime = 0.5f;
            if (type == 1) maxTime = 0.4f;
            destroyOnHit = false;
            this.type = type;

            xScale = 0.75f * xDir;
            yScale = 0.75f;

            if (vel != null) this.vel = (Point)vel;
            if (type == 0)
            {
                this.vel.y = 50;
                useGravity = true;
                gravityModifier = 0.5f;
            }
            else
            {
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

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            if (!other.gameObject.collider.isClimbable) return;
            if (split) return;
            if (type == 0)
            {
                var normal = other?.hitData?.normal;
                if (normal != null)
                {
                    normal = normal.Value.leftNormal();
                }
                else
                {
                    normal = new Point(1, 0);
                }
                Point normal2 = (Point)normal;
                normal2.multiply(250);
                destroySelf(fadeSprite);
                split = true;
                playSound("ballPOR", sendRpc: true);
                new PeaceOutRollerProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2, rpc: true);
                new PeaceOutRollerProj(weapon, pos.clone(), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), normal2.times(-1), rpc: true);
                destroySelf();
            }
        }
    }

    public class AirBombAttack : CharState
    {
        int bombNum;
        bool isNapalm;
        public AirBombAttack(bool isNapalm, string transitionSprite = "") : base("air_bomb_attack", "", "", transitionSprite)
        {
            this.isNapalm = isNapalm;
        }

        public override void update()
        {
            base.update();

            if (isNapalm)
            {
                var poi = character.getFirstPOI();
                if (!once && poi != null)
                {
                    once = true;
                    if (player.vileNapalmWeapon.type == (int)NapalmType.RumblingBang)
                    {
                        var proj = new NapalmGrenadeProj(player.vileNapalmWeapon, poi.Value, character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
                        proj.vel = new Point(character.xDir * 100, 0);
                    }
                    if (player.vileNapalmWeapon.type == (int)NapalmType.FireGrenade)
                    {
                        var proj = new MK2NapalmGrenadeProj(player.vileNapalmWeapon, poi.Value, character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
                        proj.vel = new Point(character.xDir * 100, 0);
                    }
                    if (player.vileNapalmWeapon.type == (int)NapalmType.SplashHit)
                    {
                        var proj = new SplashHitGrenadeProj(player.vileNapalmWeapon, poi.Value, character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
                        proj.vel = new Point(character.xDir * 100, 0);
                    }
                }

                if (stateTime > 0.25f)
                {
                    character.changeToIdleOrFall();
                }

                return;
            }

            if (player.vileBallWeapon.type == (int)VileBallType.AirBombs)
            {
                if (bombNum > 0 && player.input.isPressed(Control.Special1, player))
                {
                    character.changeState(new Fall(), true);
                    return;
                }

                var inputDir = player.input.getInputDir(player);
                if (inputDir.x == 0) inputDir.x = character.xDir;
                if (stateTime > 0f && bombNum == 0)
                {
                    bombNum++;
                    new VileBombProj(player.vileBallWeapon, character.pos, (int)inputDir.x, player, 0, character.player.getNextActorNetId(), rpc: true);
                }
                if (stateTime > 0.23f && bombNum == 1)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeState(new Fall(), true);
                        return;
                    }
                    bombNum++;
                    new VileBombProj(player.vileBallWeapon, character.pos, (int)inputDir.x, player, 0, character.player.getNextActorNetId(), rpc: true);
                }
                if (stateTime > 0.45f && bombNum == 2)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeState(new Fall(), true);
                        return;
                    }
                    bombNum++;
                    new VileBombProj(player.vileBallWeapon, character.pos, (int)inputDir.x, player, 0, character.player.getNextActorNetId(), rpc: true);
                }
                
                if (stateTime > 0.68f)
                {
                    character.changeToIdleOrFall();
                }
            }
            else if (player.vileBallWeapon.type == (int)VileBallType.StunBalls)
            {
                var ebw = new VileElectricBomb();
                if (bombNum > 0 && player.input.isPressed(Control.Special1, player))
                {
                    character.changeToIdleOrFall();
                    return;
                }

                if (stateTime > 0f && bombNum == 0)
                {
                    bombNum++;
                    new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(150 * character.xDir, 0), rpc: true);
                }
                if (stateTime > 0.1f && bombNum == 1)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeToIdleOrFall();
                        return;
                    }
                    bombNum++;
                    new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(133 * character.xDir, 75), rpc: true);
                }
                if (stateTime > 0.2f && bombNum == 2)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeToIdleOrFall();
                        return;
                    }
                    bombNum++;
                    new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(75 * character.xDir, 133), rpc: true);
                }
                if (stateTime > 0.3f && bombNum == 3)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeToIdleOrFall();
                        return;
                    }
                    bombNum++;
                    new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(0, 150), rpc: true);
                }
                if (stateTime > 0.4f && bombNum == 4)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeToIdleOrFall();
                        return;
                    }
                    bombNum++;
                    new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(-75 * character.xDir, 133), rpc: true);
                }
                if (stateTime > 0.5f && bombNum == 5)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeToIdleOrFall();
                        return;
                    }
                    bombNum++;
                    new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(-133 * character.xDir, 75), rpc: true);
                }
                if (stateTime > 0.6f && bombNum == 6)
                {
                    if (!character.tryUseVileAmmo(player.vileBallWeapon.getAmmoUsage(0)))
                    {
                        character.changeToIdleOrFall();
                        return;
                    }
                    bombNum++;
                    new StunShotProj(ebw, character.pos, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(-150 * character.xDir, 0), rpc: true);
                }

                if (stateTime > 0.66f)
                {
                    character.changeToIdleOrFall();
                }
            }
            else if (player.vileBallWeapon.type == (int)VileBallType.PeaceOutRoller)
            {
                if (stateTime > 0f && bombNum == 0)
                {
                    bombNum++;
                    new PeaceOutRollerProj(player.vileBallWeapon, character.pos, character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
                }

                if (stateTime > 0.25f)
                {
                    character.changeToIdleOrFall();
                }
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.vel = new Point();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }
    }

    public class VileElectricBomb : Weapon
    {
        public VileElectricBomb() : base()
        {
            rateOfFire = 1f;
            index = (int)WeaponIds.VileBomb;
            weaponBarBaseIndex = 55;
            weaponBarIndex = 55;
            killFeedIndex = 55;
        }
    }
}
