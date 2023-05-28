using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum HyouretsuzanType
    {
        Hyouretsuzan,
        Rakukojin,
        QuakeBlazer,
        DropKick,
    }

    public class HyouretsuzanWeapon : Weapon
    {
        public HyouretsuzanWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, 12, 0.5f);
            index = (int)WeaponIds.Hyouretsuzan;
            rateOfFire = 0;
            weaponBarBaseIndex = 24;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 12;
            type = (int)HyouretsuzanType.Hyouretsuzan;
            displayName = "Hyouretsuzan";
            description = new string[] { "A dive attack that can freeze enemies." };
        }

        public static Weapon getWeaponFromIndex(Player player, int index)
        {
            if (index == (int)HyouretsuzanType.Hyouretsuzan) return new HyouretsuzanWeapon(player);
            else if (index == (int)HyouretsuzanType.Rakukojin) return new RakukojinWeapon(player);
            else if (index == (int)HyouretsuzanType.QuakeBlazer) return new QuakeBlazerWeapon(player);
            else if (index == (int)HyouretsuzanType.DropKick) return new DropKickWeapon(player);
            else throw new Exception("Invalid Zero hyouretsuzan weapon index!");
        }
    }

    public class RakukojinWeapon : Weapon
    {
        public RakukojinWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, 12, 0.5f);
            index = (int)WeaponIds.Rakukojin;
            rateOfFire = 0;
            weaponBarBaseIndex = 38;
            killFeedIndex = 37;
            type = (int)HyouretsuzanType.Rakukojin;
            displayName = "Rakukojin";
            description = new string[] { "Drop with a metal blade that deals more damage", "the faster Zero is falling." };
        }
    }

    public class QuakeBlazerWeapon : Weapon
    {
        public QuakeBlazerWeapon(Player player) : base()
        {
            damager = new Damager(player, 2, 0, 0.5f);
            index = (int)WeaponIds.QuakeBlazer;
            rateOfFire = 0.3f;
            weaponBarBaseIndex = 38;
            killFeedIndex = 82;
            type = (int)HyouretsuzanType.QuakeBlazer;
            displayName = "Quake Blazer";
            description = new string[] { "A dive attack that can burn enemies", "and knock them downwards." };
        }
    }

    public class Hyouretsuzan : CharState
    {
        public Weapon weapon;
        public HyouretsuzanType type { get { return (HyouretsuzanType)weapon.type; } }
        public bool canFreeze;
        public Hyouretsuzan(Weapon weapon) : base(getSpriteName(weapon.type) + "_fall", "", "", getSpriteName(weapon.type) + "_start")
        {
            this.weapon = weapon;
        }

        public static string getSpriteName(int type)
        {
            if (type == (int)HyouretsuzanType.Hyouretsuzan) return "hyouretsuzan";
            else if (type == (int)HyouretsuzanType.Rakukojin) return "rakukojin";
            else return "quakeblazer";
        }

        public override void update()
        {
            if (!character.ownedByLocalPlayer) return;

            if (isUnderwaterQuakeBlazer())
            {
                if (!sprite.EndsWith("_water"))
                {
                    transitionSprite += "";
                    sprite += "_water";
                    defaultSprite += "_water";
                    character.changeSpriteFromName(sprite, false);
                }
            }

            base.update();

            airCode();

            if (type == HyouretsuzanType.QuakeBlazer)
            {
                if (player.input.isHeld(Control.Left, player))
                {
                    character.xDir = -1;
                    character.move(new Point(-100, 0));
                }
                else if (player.input.isHeld(Control.Right, player))
                {
                    character.xDir = 1;
                    character.move(new Point(100, 0));
                }
            }
        }
        
        public bool isUnderwaterQuakeBlazer()
        {
            return character.isUnderwater() && type == HyouretsuzanType.QuakeBlazer;
        }

        public void quakeBlazerExplode(bool hitGround)
        {
            if (!character.ownedByLocalPlayer) return;

            if (isUnderwaterQuakeBlazer()) return;

            if (!character.sprite.name.Contains("_start") || character.frameIndex > 0)
            {
                character.playSound("circleBlazeExplosion", sendRpc: true);
                new QuakeBlazerExplosionProj(weapon, character.pos.addxy(10 * character.xDir, -10), character.xDir, player, player.getNextActorNetId(), sendRpc: true);
            }

            if (!hitGround)
            {
                if (player.input.isHeld(Control.Jump, player) && character.quakeBlazerBounces < 1)
                {
                    character.vel.y = -300;
                    character.quakeBlazerBounces++;
                    // character.airAttackCooldown = character.maxAirAttackCooldown;
                }
                else
                {
                    // weapon.shootTime = weapon.rateOfFire * 2;
                }
                character.changeState(new Fall(), true);
            }
            else
            {
                // weapon.shootTime = weapon.rateOfFire * 2;
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (character.vel.y < 0) character.vel.y = 0;
            var ground = Global.level.raycast(character.pos, character.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
            if (ground == null)
            {
                canFreeze = true;
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class QuakeBlazerExplosionProj : Projectile
    {
        public QuakeBlazerExplosionProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "quakeblazer_explosion", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            destroyOnHit = false;
            projId = (int)ProjIds.QuakeBlazer;
            shouldShieldBlock = false;
            xScale = 1f;
            yScale = 1f;
            if (sendRpc)
            {
                rpcCreate(pos, owner, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
            if (!ownedByLocalPlayer) return;

            float velMag = 75;
            new QuakeBlazerFlamePart(weapon, pos.addxy(0, -10).addRand(5, 5), xDir, new Point(-velMag, 0), owner, owner.getNextActorNetId(), rpc: true);
            new QuakeBlazerFlamePart(weapon, pos.addxy(0, -10).addRand(5, 5), xDir, new Point(velMag, 0), owner, owner.getNextActorNetId(), rpc: true);
            new QuakeBlazerFlamePart(weapon, pos.addxy(0, 0).addRand(5, 5), xDir, new Point(0, 0), owner, owner.getNextActorNetId(), rpc: true);
            new QuakeBlazerFlamePart(weapon, pos.addxy(0, 10).addRand(5, 5), xDir, new Point(-velMag, 0), owner, owner.getNextActorNetId(), rpc: true);
            new QuakeBlazerFlamePart(weapon, pos.addxy(0, 10).addRand(5, 5), xDir, new Point(velMag, 0), owner, owner.getNextActorNetId(), rpc: true);

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

    public class QuakeBlazerFlamePart : Projectile
    {
        public QuakeBlazerFlamePart(Weapon weapon, Point pos, int xDir, Point vel, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "quakeblazer_part", 0, 1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.QuakeBlazerFlame;
            useGravity = true;
            collider.wallOnly = true;
            destroyOnHit = false;
            shouldShieldBlock = false;
            this.vel = vel;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (grounded) vel = new Point();

            if (isUnderwater())
            {
                destroySelf();
                return;
            }

            if (isAnimOver())
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

    public class DropKickWeapon : Weapon
    {
        public DropKickWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, 12, 0.5f);
            index = (int)WeaponIds.DropKick;
            rateOfFire = 0;
            killFeedIndex = 112;
            type = (int)HyouretsuzanType.DropKick;
        }
    }


    public class DropKickState : CharState
    {
        float stuckTime;
        public DropKickState() : base("dropkick", "", "")
        {
        }

        public override void update()
        {
            if (!character.ownedByLocalPlayer) return;

            if (character.frameIndex >= 3)
            {
                character.vel.x = character.xDir * 300;
                character.vel.y = 450;
                if (!once)
                {
                    once = true;
                    character.playSound("punch2", sendRpc: true);
                }
            }

            base.update();
            airCode();

            var hit = Global.level.checkCollisionActor(character, character.vel.x * Global.spf, character.vel.y * Global.spf);
            if (hit?.isSideWallHit() == true)
            {
                character.changeState(new Fall(), true);
                return;
            }
            else if (hit != null)
            {
                stuckTime += Global.spf;
                if (stuckTime > 0.1f)
                {
                    character.changeState(new Fall(), true);
                    return;
                }
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.stopMoving();
            character.useGravity = false;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            character.stopMoving();
            player.zeroDownThrustWeaponA.shootTime = 1;
            player.zeroDownThrustWeaponS.shootTime = 1;
        }
    }
}
