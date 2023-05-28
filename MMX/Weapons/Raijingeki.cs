using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum GroundSpecialType
    {
        Raijingeki,
        Suiretsusen,
        TBreaker,
        MegaPunch
    }

    public class RaijingekiWeapon : Weapon
    {
        public RaijingekiWeapon(Player player) : base()
        {
            damager = new Damager(player, 2, Global.defFlinch, 0.06f);
            index = (int)WeaponIds.Raijingeki;
            weaponBarBaseIndex = 22;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 10;
            type = (int)GroundSpecialType.Raijingeki;
            displayName = "Raijingeki";
            description = new string[] { "Powerful lightning attack." };
        }

        public static Weapon getWeaponFromIndex(Player player, int index)
        {
            if (index == (int)GroundSpecialType.Raijingeki) return new RaijingekiWeapon(player);
            else if (index == (int)GroundSpecialType.Suiretsusen) return new SuiretsusenWeapon(player);
            else if (index == (int)GroundSpecialType.TBreaker) return new TBreakerWeapon(player);
            else if (index == (int)GroundSpecialType.MegaPunch) return new MegaPunchWeapon(player);
            else throw new Exception("Invalid Zero air special weapon index!");
        }

        public override void attack(Character character)
        {
            character.changeState(new Raijingeki(false), true);
        }

        public override void attack2(Character character)
        {
            character.changeState(new Raijingeki(true), true);
        }
    }

    public class Raijingeki2Weapon : Weapon
    {
        public Raijingeki2Weapon(Player player) : base()
        {
            damager = new Damager(player, 2, Global.defFlinch, 0.06f);
            index = (int)WeaponIds.Raijingeki2;
            weaponBarBaseIndex = 40;
            killFeedIndex = 35;
        }
    }

    public class Raijingeki : CharState
    {
        bool playedSoundYet;
        bool isAlt;
        public Raijingeki(bool isAlt) : base(isAlt ? "raijingeki2" : "raijingeki", "", "")
        {
            this.isAlt = isAlt;
        }

        public override void update()
        {
            base.update();

            if (isAlt)
            {
                if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
                else character.sprite.frameSpeed = 1;
            }

            if (character.sprite.frameIndex > 10 && !playedSoundYet)
            {
                playedSoundYet = true;
                character.playSound("raijingeki", sendRpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle());
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


    public class SuiretsusenWeapon : Weapon
    {
        public SuiretsusenWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.06f);
            index = (int)WeaponIds.Suiretsusen;
            killFeedIndex = 110;
            type = (int)GroundSpecialType.Suiretsusen;
            displayName = "Suiretsusen";
            description = new string[] { "Water element spear with good reach." };
        }

        public override void attack(Character character)
        {
            if (shootTime == 0)
            {
                shootTime = 0;
                character.changeState(new SuiretsusanState(false), true);
            }
        }

        public override void attack2(Character character)
        {
            if (shootTime == 0)
            {
                shootTime = 0;
                character.changeState(new SuiretsusanState(true), true);
            }
        }
    }

    public class SuiretsusanState : CharState
    {
        public bool isAlt;
        public SuiretsusanState(bool isAlt) : base("spear", "")
        {
            this.isAlt = isAlt;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();

            if (isAlt)
            {
                if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
                else character.sprite.frameSpeed = 1;
            }

            var pois = character.sprite.getCurrentFrame().POIs;
            if (pois != null && pois.Count > 0 && !once)
            {
                once = true;
                new SuiretsusenProj(player.raijingekiWeapon, character.getFirstPOIOrDefault(), character.xDir, player, player.getNextActorNetId(), sendRpc: true);
                character.playSound("spear", sendRpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }
    }

    public class SuiretsusenProj : Projectile
    {
        public SuiretsusenProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 200, 6, player, "spear_proj", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.SuiretsusanProj;
            destroyOnHit = false;
            shouldVortexSuck = false;
            shouldShieldBlock = false;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (isAnimOver())
            {
                destroySelf();
            }
        }
    }

    public class TBreakerWeapon : Weapon
    {
        public TBreakerWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.06f);
            index = (int)WeaponIds.TBreaker;
            killFeedIndex = 107;
            type = (int)GroundSpecialType.TBreaker;
            displayName = "T-Breaker";
            description = new string[] { "A mighty hammer that can shatter barriers." };
        }

        public override void attack(Character character)
        {
            if (shootTime == 0)
            {
                shootTime = 0f;
                character.changeState(new TBreakerState(false), true);
            }
        }

        public override void attack2(Character character)
        {
            if (shootTime == 0)
            {
                shootTime = 0f;
                character.changeState(new TBreakerState(true), true);
            }
        }
    }

    public class TBreakerState : CharState
    {
        public float dashTime = 0;
        public Projectile fSplasherProj;
        public bool isAlt;

        public TBreakerState(bool isAlt) : base("tbreaker", "")
        {
            this.isAlt = isAlt;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();

            if (isAlt)
            {
                if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
                else character.sprite.frameSpeed = 1;
            }

            var pois = character.sprite.getCurrentFrame().POIs;
            if (pois != null && pois.Count > 0 && !once)
            {
                once = true;
                Point poi = character.getFirstPOIOrDefault();
                var rect = new Rect(poi.x - 10, poi.y - 5, poi.x + 10, poi.y + 5);
                Shape shape = rect.getShape();
                var hit = Global.level.checkCollisionShape(shape, null);
                if (hit != null && hit.gameObject is Wall)
                {
                    new TBreakerProj(player.raijingekiWeapon, poi, character.xDir, player, player.getNextActorNetId(), sendRpc: true);
                }
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }
    }

    public class TBreakerProj : Projectile
    {
        public TBreakerProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "tbreaker_shockwave", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.TBreakerProj;
            destroyOnHit = false;
            shouldVortexSuck = false;
            shouldShieldBlock = false;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
            shakeCamera(sendRpc: true);
            playSound("crash", sendRpc: true);
        }

        public override void update()
        {
            base.update();
            if (isAnimOver())
            {
                destroySelf();
            }
        }
    }

    public class MegaPunchWeapon : Weapon
    {
        public MegaPunchWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.06f);
            index = (int)WeaponIds.MegaPunchWeapon;
            killFeedIndex = 106;
            type = (int)GroundSpecialType.MegaPunch;
        }

        public override void attack(Character character)
        {
            character.changeState(new MegaPunchState(false), true);
        }

        public override void attack2(Character character)
        {
            character.changeState(new MegaPunchState(true), true);
        }
    }

    public class MegaPunchState : CharState
    {
        bool isAlt;
        public MegaPunchState(bool isAlt) : base("megapunch", "")
        {
            this.isAlt = isAlt;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();

            if (isAlt)
            {
                if (character.sprite.frameIndex % 2 == 0) character.sprite.frameSpeed = 2;
                else character.sprite.frameSpeed = 1;
            }

            if (character.frameIndex >= 7 && !once)
            {
                once = true;
                character.playSound("megapunch", sendRpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }
    }

}
