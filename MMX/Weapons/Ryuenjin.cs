using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum RyuenjinType
    {
        Ryuenjin,
        EBlade,
        Rising,
        Shoryuken,
    }

    public class RyuenjinWeapon : Weapon
    {
        public RyuenjinWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, 0, 0.2f);
            index = (int)WeaponIds.Ryuenjin;
            rateOfFire = 0.25f;
            weaponBarBaseIndex = 23;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 11;
            type = (int)RyuenjinType.Ryuenjin;
            displayName = "Ryuenjin";
            description = new string[] { "A fiery uppercut that burns enemies." };
        }

        public static Weapon getWeaponFromIndex(Player player, int index)
        {
            if (index == (int)RyuenjinType.Ryuenjin) return new RyuenjinWeapon(player);
            else if (index == (int)RyuenjinType.EBlade) return new EBladeWeapon(player);
            else if (index == (int)RyuenjinType.Rising) return new RisingWeapon(player);
            else throw new Exception("Invalid Zero ryuenjin weapon index!");
        }
    }

    public class EBladeWeapon : Weapon
    {
        public EBladeWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.1f);
            index = (int)WeaponIds.EBlade;
            rateOfFire = 0.25f;
            weaponBarBaseIndex = 41;
            killFeedIndex = 36;
            type = (int)RyuenjinType.EBlade;
            displayName = "E-Blade";
            description = new string[] { "An electrical uppercut that flinches enemies", "and can hit multiple times." };
        }
    }

    public class RisingWeapon : Weapon
    {
        public RisingWeapon(Player player) : base()
        {
            damager = new Damager(player, 2, 0, 0.5f);
            index = (int)WeaponIds.Rising;
            rateOfFire = 0.1f;
            weaponBarBaseIndex = 41;
            killFeedIndex = 83;
            type = (int)RyuenjinType.Rising;
            displayName = "Rising";
            description = new string[] { "A fast, element-neutral uppercut.", "Can be used in the air to gain height." };
        }
    }

    public class ZeroShoryukenWeapon : Weapon
    {
        public ZeroShoryukenWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            index = (int)WeaponIds.ZeroShoryuken;
            type = (int)RyuenjinType.Shoryuken;
            killFeedIndex = 113;
        }
    }

    public class Ryuenjin : CharState
    {
        bool jumpedYet;
        float timeInWall;
        bool isUnderwater;
        public Weapon weapon;
        public bool isHeld = true;
        public float holdTime;
        public RyuenjinType type { get { return (RyuenjinType)weapon.type; } }

        public Ryuenjin(Weapon weapon, bool isUnderwater) : base(getSprite(weapon.type, isUnderwater), "", "")
        {
            this.weapon = weapon;
            this.isUnderwater = type == RyuenjinType.Ryuenjin && isUnderwater;
        }

        public static string getSprite(int type, bool isUnderwater)
        {
            if (type == (int)RyuenjinType.Ryuenjin)
            {
                return (isUnderwater ? "ryuenjin_underwater" : "ryuenjin");
            }
            if (type == (int)RyuenjinType.EBlade) return "eblade";
            if (type == (int)RyuenjinType.Shoryuken) return "shoryuken";
            return "rising";
        }

        public override void update()
        {
            base.update();

            if (character.sprite.frameIndex >= 3 && !jumpedYet)
            {
                jumpedYet = true;
                character.dashedInAir++;
                float ySpeedMod = 1.2f;
                if (type == RyuenjinType.Shoryuken) ySpeedMod = 1.5f;
                character.vel.y = -character.getJumpPower() * ySpeedMod;
                if (!isUnderwater)
                {
                    if (type == RyuenjinType.Ryuenjin) character.playSound("ryuenjin", sendRpc: true);
                    if (type == RyuenjinType.EBlade) character.playSound("raijingeki", sendRpc: true);
                    if (type == RyuenjinType.Rising) character.playSound("saber1", sendRpc: true);
                    if (type == RyuenjinType.Shoryuken) character.playSound("punch2", sendRpc: true);
                }
            }

            if (!player.input.isHeld(Control.Special1, player) && !player.input.isHeld(Control.Shoot, player))
            {
                isHeld = false;
            }

            if (character.sprite.frameIndex == 6 && type == RyuenjinType.Rising)
            {
                if (isHeld && holdTime < 0.2f)
                {
                    holdTime += Global.spf;
                    character.frameSpeed = 0;
                    character.frameIndex = 6;
                }
                else
                {
                    character.frameSpeed = 1;
                    character.frameIndex = 6;
                }
            }

            if (character.sprite.frameIndex >= 3 && character.sprite.frameIndex < 6)
            {
                float speed = 100;
                if (type == RyuenjinType.EBlade) speed = 120;
                character.move(new Point(character.xDir * speed, 0));
                if (type == RyuenjinType.Shoryuken && jumpedYet)
                {
                    character.vel.y += Physics.gravity * Global.spf * 0.5f;
                }
            }

            var wallAbove = Global.level.checkCollisionActor(character, 0, -10);
            if (wallAbove != null && wallAbove.gameObject is Wall)
            {
                timeInWall += Global.spf;
                if (timeInWall > 0.1f)
                {
                    character.changeState(new Fall());
                    return;
                }
            }

            if (canDownSpecial())
            {
                if (player.input.isPressed(Control.Shoot, player) && player.input.isHeld(Control.Down, player))
                {
                    if (!player.hasKnuckle()) character.changeState(new Hyouretsuzan(player.zeroDownThrustWeaponA), true);
                    else character.changeState(new DropKickState(), true);
                    return;
                }
                else if (player.input.isPressed(Control.Special1, player) && player.input.isHeld(Control.Down, player))
                {
                    if (!player.hasKnuckle()) character.changeState(new Hyouretsuzan(player.zeroDownThrustWeaponS), true);
                    else character.changeState(new DropKickState(), true);
                    return;
                }
            }

            if (character.isAnimOver())
            {
                character.changeState(new Fall());
            }
        }

        public bool canDownSpecial()
        {
            if (character.airAttackCooldown == 0) return false;
            int fc = character.sprite.frames.Count;
            if (type == RyuenjinType.Rising) return character.sprite.frameIndex >= fc - 1;
            return character.sprite.frameIndex >= fc - 3;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (!character.grounded)
            {
                character.sprite.frameIndex = 3;
            }
        }

        public override void onExit(CharState newState)
        {
            weapon.shootTime = weapon.rateOfFire;
            base.onExit(newState);
        }
    }
}
