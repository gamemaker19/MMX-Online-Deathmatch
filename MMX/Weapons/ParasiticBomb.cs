using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ParasiticBomb : Weapon
    {
        public static float carryRange = 120;
        public static float beeRange = 120;

        public ParasiticBomb() : base()
        {
            shootSounds = new List<string>() { "", "", "", "" };
            rateOfFire = 1f;
            index = (int)WeaponIds.ParasiticBomb;
            weaponBarBaseIndex = 18;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 18;
            killFeedIndex = 41;
            weaknessIndex = (int)WeaponIds.GravityWell;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                player.character.playSound("buster");
                new ParasiticBombProj(this, pos, xDir, player, netProjId);
            }
            else
            {
                if (player.character.ownedByLocalPlayer && player.character.beeSwarm == null)
                {
                    player.character.beeSwarm = new BeeSwarm(player.character);
                }
            }
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel != 3) return 1;
            else return 2;
        }
    }

    public class ParasiticBombProj : Projectile
    {
        public Character host;
        public ParasiticBombProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 200, 0, player, "parasitebomb", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            this.weapon = weapon;
            maxTime = 0.6f;
            projId = (int)ProjIds.ParasiticBomb;
            destroyOnHit = true;
            shouldShieldBlock = true;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            Global.sprites["parasitebomb_light"].draw(MathF.Round(Global.frameCount * 0.25f) % 4, pos.x + x, pos.y + y, 1, 1, null, 1, 1, 1, zIndex);
        }
    }

    public class ParasiteCarry : CharState
    {
        bool flinch;
        bool isDone;
        Character otherChar;
        float moveAmount;
        float maxMoveAmount;
        public ParasiteCarry(Character otherChar, bool flinch) : base(flinch ? "hurt" : "fall", flinch ? "" : "fall_shoot", flinch ? "" : "fall_attack")
        {
            this.flinch = flinch;
            this.otherChar = otherChar;
        }

        public override bool canEnter(Character character)
        {
            if (!character.ownedByLocalPlayer) return false;
            if (!base.canEnter(character)) return false;
            if (character.isCCImmune()) return false;
            return !character.charState.invincible;
        }

        public override bool canExit(Character character, CharState newState)
        {
            if (newState is Hurt || newState is Die) return true;
            return isDone;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.grounded = false;
            character.vel.y = 0;
            maxMoveAmount = character.getCenterPos().distanceTo(otherChar.getCenterPos()) * 1.5f;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            player.character.useGravity = true;
        }

        public override void update()
        {
            base.update();

            if (!character.hasParasite || character.parasiteDamager == null)
            {
                isDone = true;
                character.changeState(new Fall(), true);
                return;
            }

            Point amount = character.getCenterPos().directionToNorm(otherChar.getCenterPos()).times(250);

            /*
            var hit = Global.level.checkCollisionActor(character, amount.x * Global.spf, amount.y * Global.spf);
            if (hit?.gameObject is Wall)
            {
                character.parasiteDamager.applyDamage(character, player.weapon is FrostShield, new ParasiticBomb(), otherChar, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
                character.removeParasite(false, true);
                return;
            }
            */

            character.move(amount);

            moveAmount += amount.magnitude * Global.spf;
            if (character.getCenterPos().distanceTo(otherChar.getCenterPos()) < 5)
            {
                var pd = character.parasiteDamager;
                pd.applyDamage(character, player.weapon is FrostShield, new ParasiticBomb(), otherChar, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
                pd.applyDamage(otherChar, player.weapon is FrostShield, new ParasiticBomb(), character, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
                character.removeParasite(false, true);
                return;
            }
            else if (moveAmount > maxMoveAmount)
            {
                character.removeParasite(false, false);
                return;
            }
        }
    }

    public class BeeSwarm
    {
        public Character character;
        public List<BeeCursorAnim> beeCursors = new List<BeeCursorAnim>();
        int currentIndex;
        float currentTime = 0f;
        const float beeCooldown = 1f;

        public BeeSwarm(Character character)
        {
            this.character = character;

            beeCursors = new List<BeeCursorAnim>()
            {
                new BeeCursorAnim(getCursorStartPos(0), character),
                new BeeCursorAnim(getCursorStartPos(1), character),
                new BeeCursorAnim(getCursorStartPos(2), character),
                new BeeCursorAnim(getCursorStartPos(3), character),
            };
        }

        public Point getCursorStartPos(int index)
        {
            Point cPos = character.getCenterPos();
            if (index == 0) return cPos.addxy(-15, -17);
            else if(index == 1) return cPos.addxy(15, -17);
            else if(index == 2) return cPos.addxy(-15, 17);
            else return cPos.addxy(15, 17);
        }

        public Actor getAvailableTarget()
        {
            Point centerPos = character.getCenterPos();
            var targets = Global.level.getTargets(centerPos, character.player.alliance, true, ParasiticBomb.beeRange);
            
            foreach (var target in targets)
            {
                if (beeCursors.Any(b => b.target == target))
                {
                    continue;
                }
                return target;
            }

            return null;
        }

        public void update()
        {
            currentTime -= Global.spf;
            if (currentTime <= 0)
            {
                var target = getAvailableTarget();
                if (target != null)
                {
                    beeCursors[currentIndex].target = target;
                    currentTime = beeCooldown;
                    currentIndex++;
                    if (currentIndex > 3) currentIndex = 0;
                }
            }

            for (int i = 0; i < beeCursors.Count; i++)
            {
                if (beeCursors[i].state < 2)
                {
                    beeCursors[i].pos = getCursorStartPos(i);
                }
                if (beeCursors[i].state == 4)
                {
                    beeCursors[i] = new BeeCursorAnim(getCursorStartPos(i), character);
                }
            }

            if (shouldDestroy())
            {
                destroy();
            }
        }

        public void reset(bool isMiniFlinch)
        {
            currentTime = 1;
            if (!isMiniFlinch)
            {
                foreach (var beeCursor in beeCursors)
                {
                    beeCursor.reset();
                }
            }
        }

        public bool shouldDestroy()
        {
            if (!character.player.input.isHeld(Control.Shoot, character.player)) return true;
            if (character.player.weapon is not ParasiticBomb) return true;
            var pb = character.player.weapon as ParasiticBomb;
            if (pb.ammo <= 0) return true;
            return false;
        }

        public void destroy()
        {
            foreach (var beeCursor in beeCursors)
            {
                beeCursor.destroySelf();
            }
            beeCursors.Clear();
            character.beeSwarm = null;
        }
    }

    public class BeeCursorAnim : Anim
    {
        public int state = 0;
        Character character;
        public Actor target;
        public BeeCursorAnim(Point pos, Character character) 
            : base(pos, "parasite_cursor_start", 1, character.player.getNextActorNetId(), false, true, character.ownedByLocalPlayer)
        {
            this.character = character;
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (state == 0)
            {
                if (sprite.name == "parasite_cursor_start" && sprite.isAnimOver())
                {
                    changeSprite("parasite_cursor", true);
                    state = 1;
                    time = 0;
                }
            }
            else if (state == 1)
            {
                if (target != null)
                {
                    state = 2;
                }
            }
            else if (state == 2)
            {
                if (target.destroyed)
                {
                    state = 3;
                    return;
                }
                move(pos.directionToNorm(target.getCenterPos()).times(350));
                if (pos.distanceTo(target.getCenterPos()) < 5)
                {
                    state = 3;
                    changeSprite("parasite_cursor_lockon", true);
                }
            }
            else if (state == 3)
            {
                pos = target.getCenterPos();
                if (isAnimOver())
                {
                    state = 4;
                    destroySelf();
                    if (!target.destroyed)
                    {
                        character.chargeTime = Character.charge3Time;
                        character.shoot(true);
                        character.chargeTime = 0;
                        new ParasiticBombProjCharged(new ParasiticBomb(), character.getShootPos(), character.pos.x - target.getCenterPos().x < 0 ? 1 : -1, character.player, character.player.getNextActorNetId(), target, rpc: true);
                    }
                }
            }
        }

        public void reset()
        {
            state = 0;
            changeSpriteIfDifferent("parasite_cursor_start", true);
            target = null;
        }
    }

    public class ParasiticBombProjCharged : Projectile, IDamagable
    {
        public Actor host;
        public Point lastMoveAmount;
        const float maxSpeed = 150;
        public ParasiticBombProjCharged(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, Actor host, bool rpc = false) :
            base(weapon, pos, xDir, 0, 4, player, "parasitebomb_bee", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            this.weapon = weapon;
            this.host = host;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            maxTime = 3f;
            projId = (int)ProjIds.ParasiticBombCharged;
            destroyOnHit = true;
            shouldShieldBlock = true;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();
            if (!ownedByLocalPlayer) return;

            if (!host.destroyed)
            {
                Point amount = pos.directionToNorm(host.getCenterPos()).times(150);
                vel = Point.lerp(vel, amount, Global.spf * 4);
                if (vel.magnitude > maxSpeed) vel = vel.normalize().times(maxSpeed);
            }
            else
            {
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
}
