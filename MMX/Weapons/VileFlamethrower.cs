using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum VileFlamethrowerType
    {
        NoneNapalm = -1,
        WildHorseKick,
        SeaDragonRage,
        DragonsWrath,
        NoneBall,
    }

    public class VileFlamethrower : Weapon
    {
        public float vileAmmoUsage;
        public string projSprite;
        public string projFadeSprite;
        public int projId;
        public VileFlamethrower(VileFlamethrowerType vileFlamethrowerType) : base()
        {
            rateOfFire = 1f;
            index = (int)WeaponIds.VileFlamethrower;
            type = (int)vileFlamethrowerType;

            if (vileFlamethrowerType == VileFlamethrowerType.NoneNapalm)
            {
                displayName = "None(NAPALM)";
                description = new string[] { "Do not equip a Flamethrower.", "NAPALM will be used instead." };
                killFeedIndex = 126;
            }
            else if (vileFlamethrowerType == VileFlamethrowerType.NoneBall)
            {
                displayName = "None(BALL)";
                description = new string[] { "Do not equip a Flamethrower.", "BALL will be used instead." };
                killFeedIndex = 126;
            }
            else if (vileFlamethrowerType == VileFlamethrowerType.WildHorseKick)
            {
                displayName = "Wild Horse Kick";
                projSprite = "flamethrower_whk";
                projFadeSprite = "flamethrower_whk_fade";
                vileAmmoUsage = 8;
                projId = (int)ProjIds.WildHorseKick;
                description = new string[] { "Shoot jets of flame from your leg.", "Strong, but not energy efficient." };
                killFeedIndex = 117;
                vileWeight = 2;

            }
            else if (vileFlamethrowerType == VileFlamethrowerType.SeaDragonRage)
            {
                displayName = "Sea Dragon's Rage";
                projSprite = "flamethrower_sdr";
                projFadeSprite = "flamethrower_sdr_fade";
                vileAmmoUsage = 5;
                projId = (int)ProjIds.SeaDragonRage;
                description = new string[] { "This powerful flamethrower can freeze", "enemies and even be used underwater." };
                killFeedIndex = 119;
                vileWeight = 4;
            }
            else if (vileFlamethrowerType == VileFlamethrowerType.DragonsWrath)
            {
                displayName = "Dragon's Wrath";
                projSprite = "flamethrower_dw";
                projFadeSprite = "flamethrower_dw_fade";
                vileAmmoUsage = 24;
                description = new string[] { "A long arching flamethrower,", "useful against faraway enemies." };
                killFeedIndex = 118;
                projId = (int)ProjIds.DragonsWrath;
                vileWeight = 3;
            }
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return vileAmmoUsage;
        }

        public override void vileShoot(WeaponIds weaponInput, Character character)
        {
            if (type == (int)VileFlamethrowerType.NoneNapalm || type == (int)VileFlamethrowerType.NoneBall) return;
            if (shootTime == 0)
            {
                if (weaponInput == WeaponIds.VileFlamethrower)
                {
                    var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
                    if (ground == null)
                    {
                        if (character.player.vileAmmo > 0)
                        {
                            character.setVileShootTime(this);
                            character.changeState(new FlamethrowerState(), true);
                        }
                    }
                }
                else if (weaponInput == WeaponIds.VileBomb)
                {
                    var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
                    if (ground == null)
                    {
                        if (character.player.vileAmmo > 0)
                        {
                            character.setVileShootTime(this);
                            character.changeState(new FlamethrowerState(), true);
                        }
                    }
                }
                else if (weaponInput == WeaponIds.Napalm)
                {
                    if (character.player.vileAmmo > 0)
                    {
                        character.changeState(new NapalmAttack(NapalmAttackType.Flamethrower), true);
                    }
                }
            }
        }
    }

    public class FlamethrowerState : CharState
    {
        public float shootTime;
        public Point shootPOI = new Point(-1, -1);
        public FlamethrowerState(string transitionSprite = "") : base("flamethrower", "", "", transitionSprite)
        {
        }

        public override void update()
        {
            base.update();

            character.turnToInput(player.input, player);

            shootTime += Global.spf;
            if (shootTime > 0.06f)
            {
                if (!character.tryUseVileAmmo(2))
                {
                    character.changeToIdleOrFall();
                    return;
                }
                shootTime = 0;
                character.playSound("flamethrower");
                new FlamethrowerProj(player.vileFlamethrowerWeapon, character.getPOIPos(shootPOI), character.xDir, false, player, player.getNextActorNetId(), sendRpc: true);
            }

            if (character.loopCount > 4 || player.input.isPressed(Control.Special1, player))
            {
                character.changeToIdleOrFall();
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

    public class FlamethrowerProj : Projectile
    {
        bool napalmInput;
        float destroyTime;
        public FlamethrowerProj(VileFlamethrower weapon, Point pos, int xDir, bool napalmInput, Player player, ushort netProjId, bool sendRpc = false) : 
            base(weapon, pos, xDir, 0, 1, player, weapon.projSprite, 0, 0.1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = weapon.projId;
            fadeSprite = weapon.projFadeSprite;
            destroyOnHit = true;
            this.napalmInput = napalmInput;

            destroyTime = 0.3f;
            if (weapon.type == (int)VileFlamethrowerType.SeaDragonRage)
            {
                destroyTime = 0.2f;
            }

            if (!napalmInput)
            {
                vel = new Point(xDir, 2f);
                vel = vel.normalize().times(350);
                if (weapon.type == (int)VileFlamethrowerType.DragonsWrath)
                {
                    this.vel.x = xDir * 350;
                    this.vel.y = 225;
                }
            }
            else
            {
                vel = new Point(xDir, -0.5f);
                vel = vel.normalize().times(350);
                if (weapon.type == (int)VileFlamethrowerType.DragonsWrath)
                {
                    this.vel.x = xDir * 350;
                    this.vel.y = -250;
                    destroyTime = 0.4f;
                }
                
            }

            angle = vel.angle;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (weapon.type != (int)VileFlamethrowerType.SeaDragonRage && isUnderwater())
            {
                destroySelf();
                return;
            }

            if (!napalmInput)
            {
                if (weapon.type == (int)VileFlamethrowerType.DragonsWrath)
                {
                    vel.y -= Global.spf * 800;
                }
                else
                {
                    vel.x *= 0.9f;
                }
            }
            else
            {
                if (weapon.type == (int)VileFlamethrowerType.DragonsWrath)
                {
                    vel.x -= xDir * Global.spf * 800;
                }
                else
                {
                    vel.y *= 0.9f;
                }
            }

            if (time > destroyTime)
            {
                destroySelf(fadeSprite);
            }

        }

        public override void onHitWall(CollideData other)
        {
            if (weapon.type != (int)VileFlamethrowerType.DragonsWrath)
            {
                destroySelf(fadeSprite);
            }
            else if (vel.y > 0)
            {
                vel.y = 0;
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (weapon.type == (int)VileFlamethrowerType.WildHorseKick)
            {
                var character = damagable as Character;
                character?.unfreezeIfFrozen();
            }
        }
    }
}
