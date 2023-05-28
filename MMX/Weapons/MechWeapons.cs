using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum VileMechMenuType
    {
        None = -1,
        All,
    }

    public class MechMenuWeapon : Weapon
    {
        public bool isMenuOpened;
        public const int weight = 16;
        public MechMenuWeapon(VileMechMenuType type) : base()
        {
            ammo = 0;
            index = (int)WeaponIds.MechMenuWeapon;
            weaponSlotIndex = 46;
            this.type = (int)type;

            if (type == VileMechMenuType.None)
            {
                displayName = "None";
                description = new string[] { "Do not equip Ride Armors." };
                killFeedIndex = 126;
            }
            else if (type == VileMechMenuType.All)
            {
                displayName = "All";
                description = new string[] { "Vile has all 4 Ride Armors available", "to call down on the battlefield." };
                killFeedIndex = 178;
                vileWeight = weight;
            }
            /*
            else if (type == VileMechMenuType.N)
            {
                displayName = "N";
                description = new string[] { "Vile can call down the", "Neutral Ride Armor." };
                killFeedIndex = 0;
            }
            else if (type == VileMechMenuType.K)
            {
                displayName = "K";
                description = new string[] { "Vile can call down the", "Kangaroo Ride Armor." };
                killFeedIndex = 0;
            }
            else if (type == VileMechMenuType.H)
            {
                displayName = "H";
                description = new string[] { "Vile can call down the", "Hawk Ride Armor." };
                killFeedIndex = 0;
            }
            else if (type == VileMechMenuType.F)
            {
                displayName = "F";
                description = new string[] { "Vile can call down the", "Frog Ride Armor." };
                killFeedIndex = 0;
            }
            */
        }
    }

    public class MechPunchWeapon : Weapon
    {
        public MechPunchWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.5f);
            ammo = 0;
            index = (int)WeaponIds.MechPunch;
            killFeedIndex = 18;
        }
    }

    public class MechKangarooPunchWeapon : Weapon
    {
        public MechKangarooPunchWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            ammo = 0;
            index = (int)WeaponIds.MechKangarooPunch;
            killFeedIndex = 49;
        }
    }

    public class MechGoliathPunchWeapon : Weapon
    {
        public MechGoliathPunchWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            ammo = 0;
            index = (int)WeaponIds.MechGoliathPunch;
            killFeedIndex = 57;
        }
    }

    public class MechDevilBearPunchWeapon : Weapon
    {
        public MechDevilBearPunchWeapon(Player player) : base()
        {
            damager = new Damager(player, 2, Global.defFlinch, 0.25f);
            ammo = 0;
            index = (int)WeaponIds.MechDevilBearPunch;
            killFeedIndex = 176;
        }
    }

    public class MechStompWeapon : Weapon
    {
        public MechStompWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.75f);
            ammo = 0;
            index = (int)WeaponIds.MechStomp;
            killFeedIndex = 19;
        }
    }

    public class MechKangarooStompWeapon : Weapon
    {
        public MechKangarooStompWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.75f);
            ammo = 0;
            index = (int)WeaponIds.MechKangarooStomp;
            killFeedIndex = 58;
        }
    }

    public class MechFrogStompWeapon : Weapon
    {
        public MechFrogStompWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.5f);
            ammo = 0;
            index = (int)WeaponIds.MechFrogStomp;
            killFeedIndex = 51;
        }
    }

    public class MechFrogStompShockwave : Projectile
    {
        public MechFrogStompShockwave(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "groundpound_explosion", 0, 1f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.75f;
            projId = (int)ProjIds.MechFrogStompShockwave;
            yScale = 0.5f;
            destroyOnHit = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;
            
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
            shakeCamera();
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (isAnimOver())
            {
                destroySelf();
            }
        }
    }

    public class MechHawkStompWeapon : Weapon
    {
        public MechHawkStompWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.75f);
            ammo = 0;
            index = (int)WeaponIds.MechHawkStomp;
            killFeedIndex = 59;
        }
    }

    public class MechGoliathStompWeapon : Weapon
    {
        public MechGoliathStompWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.75f);
            ammo = 0;
            index = (int)WeaponIds.MechGoliathStomp;
            killFeedIndex = 60;
        }
    }

    public class MechDevilBearStompWeapon : Weapon
    {
        public MechDevilBearStompWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.75f);
            ammo = 0;
            index = (int)WeaponIds.MechDevilBearStomp;
            killFeedIndex = 177;
        }
    }

    public class MechChainChargeWeapon : Weapon
    {
        public MechChainChargeWeapon(Player player) : base()
        {
            damager = new Damager(player, 1, Global.defFlinch, 0.1f);
            ammo = 0;
            index = (int)WeaponIds.MechChainCharge;
            killFeedIndex = 49;
        }
    }

    public class MechChainWeapon : Weapon
    {
        public MechChainWeapon(Player player) : base()
        {
            damager = new Damager(player, 1, Global.defFlinch, 0.1f);
            ammo = 0;
            index = (int)WeaponIds.MechChain;
            killFeedIndex = 49;
        }
    }

    public class MechMissileWeapon : Weapon
    {
        public MechMissileWeapon(Player player) : base()
        {
            damager = new Damager(player, 1, Global.defFlinch, 0.1f);
            ammo = 0;
            index = (int)WeaponIds.MechMissile;
            killFeedIndex = 50;
        }
    }

    public class MechMissileProj : Projectile, IDamagable
    {
        public Character target;
        public float smokeTime = 0;
        public bool isDown;
        public MechMissileProj(Weapon weapon, Point pos, int xDir, bool isDown, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 400, 2, player, "hawk_missile", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MechMissile;
            maxTime = 0.5f;
            fadeOnAutoDestroy = true;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            reflectable2 = true;
            this.isDown = isDown;
            if (isDown)
            {
                this.xDir = 1;
                angle = 90;
                vel.x = 0;
                vel.y = 400;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            updateProjectileCooldown();

            smokeTime += Global.spf;
            if (smokeTime > 0.2)
            {
                smokeTime = 0;
                new Anim(pos, "torpedo_smoke", 1, null, true);
            }
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (damage > 0)
            {
                destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return damager.owner.alliance != damagerAlliance;
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
        }

        public bool canBeHealed(int healerAlliance)
        {
            return false;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
        }
    }

    public class MechTorpedoWeapon : Weapon
    {
        public MechTorpedoWeapon(Player player) : base()
        {
            damager = new Damager(player, 1, Global.defFlinch, 0.1f);
            ammo = 0;
            index = (int)WeaponIds.MechTorpedo;
            weaponBarBaseIndex = 52;
            weaponBarIndex = 52;
            killFeedIndex = 52;
        }
    }

    public class MechChainProj : Projectile
    {
        public MechChainProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "kangaroo_chain_proj", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MechChain;
            
            destroyOnHit = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;

            removeRenderEffect(RenderEffectType.RedShadow);
            removeRenderEffect(RenderEffectType.BlueShadow);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            if (!ownedByLocalPlayer) return;
            base.update();
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

    public class MechBusterWeapon : Weapon
    {
        public MechBusterWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.5f);
            ammo = 0;
            index = (int)WeaponIds.MechBuster;
            weaponBarBaseIndex = 53;
            weaponBarIndex = 53;
            killFeedIndex = 53;
        }
    }

    public class MechBusterProj : Projectile
    {
        public MechBusterProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 200, 4, player, "goliath_proj", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MechBuster;
            maxTime = 0.75f;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class MechBusterProj2 : Projectile
    {
        int type = 0;
        float startY;
        public MechBusterProj2(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 200, 4, player, "goliath_proj2", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.75f;
            projId = (int)ProjIds.MechBuster;
            startY = pos.y;
            this.type = type;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            if (!ownedByLocalPlayer) return;
            base.update();
            float offsetY;
            if (type == 0)
            {
                offsetY = 25 * MathF.Sin(Global.time * 10);
            }
            else
            {
                offsetY = 25 * MathF.Sin(MathF.PI + Global.time * 10);
            }
            changePos(new Point(pos.x, startY + offsetY));
        }
    }
}
