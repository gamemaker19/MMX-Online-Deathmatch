using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class XUPParry : Weapon
    {
        public XUPParry() : base()
        {
            rateOfFire = 0.75f;
            index = (int)WeaponIds.UPParry;
            killFeedIndex = 168;
        }
    }

    // If fixing parry code also fix kknuckle parry
    public class XUPParryStartState : CharState
    {
        public XUPParryStartState() : base("unpo_parry_start", "", "", "")
        {
        }

        public override void update()
        {
            base.update();

            if (stateTime < 0.1f)
            {
                character.turnToInput(player.input, player);
            }

            if (character.isAnimOver())
            {
                character.changeToIdleOrFall();
            }
        }

        public void counterAttack(Player damagingPlayer, Actor damagingActor, float damage)
        {
            Actor counterAttackTarget = null;
            Projectile absorbedProj = null;
            if (damagingActor is GenericMeleeProj gmp)
            {
                counterAttackTarget = gmp.owningActor;
            }
            else if (damagingActor is Projectile proj)
            {
                if (!proj.canBeParried() && proj.shouldVortexSuck)
                {
                    absorbedProj = proj;
                    absorbedProj.destroySelfNoEffect(doRpcEvenIfNotOwned: true);
                }
            }

            if (absorbedProj != null)
            {
                if (character.ownedByLocalPlayer)
                {
                    bool shootProj = false;
                    bool absorbThenShoot = false;
                    character.playSound("upParryAbsorb", sendRpc: true);
                    if (!player.input.isWeaponLeftOrRightHeld(player))
                    {
                        character.unpoAbsorbedProj = absorbedProj;
                        //character.player.weapons.Add(new AbsorbWeapon(absorbedProj));
                    }
                    else
                    {
                        shootProj = true;
                        absorbThenShoot = true;
                    }
                    character.refillUnpoBuster();
                    character.changeState(new XUPParryProjState(absorbedProj, shootProj, absorbThenShoot), true);
                }

                return;
            }

            if (counterAttackTarget == null)
            {
                counterAttackTarget = damagingPlayer?.character ?? damagingActor;
            }

            if (counterAttackTarget != null && character.pos.distanceTo(counterAttackTarget.pos) < 75 && counterAttackTarget is Character chr)
            {
                if (!chr.ownedByLocalPlayer)
                {
                    RPC.actorToggle.sendRpc(chr.netId, RPCActorToggleType.ChangeToParriedState);
                }
                else
                {
                    chr.changeState(new ParriedState(), true);
                }
            }
            character.refillUnpoBuster();
            character.playSound("upParry", sendRpc: true);
            character.changeState(new XUPParryMeleeState(counterAttackTarget, damage), true);
        }

        public bool canParry()
        {
            return character.frameIndex == 0;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.parryCooldown = character.maxParryCooldown;
        }
    }

    public class ParriedState : CharState
    {
        public ParriedState() : base("grabbed", "", "", "")
        {
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            if (character.charState is ParriedState) return false;
            if (character.isInvulnerable()) return false;
            if (character.charState.invincible) return false;
            return true;
        }

        public override void update()
        {
            base.update();
            if (stateTime > 0.5f)
            {
                character.changeToIdleOrFall();
            }
        }
    }

    public class UPParryMeleeProj : Projectile
    {
        public UPParryMeleeProj(Weapon weapon, Point pos, int xDir, float damage, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, damage, player, "mmx_unpo_parry_proj", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.UPParryMelee;
            setIndestructableProperties();
            maxTime = 0.25f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
            new Anim(pos.addxy(-10, 0), "explosion", xDir, null, true);
            new Anim(pos.addxy(10, 0), "explosion", xDir, null, true);
        }
    }

    public class XUPParryMeleeState : CharState
    {
        Actor counterAttackTarget;
        float damage;
        public XUPParryMeleeState(Actor counterAttackTarget, float damage) : base("unpo_parry_attack", "", "", "")
        {
            invincible = true;
            this.counterAttackTarget = counterAttackTarget;
            this.damage = damage;
        }

        public override void update()
        {
            base.update();

            if (counterAttackTarget != null)
            {
                character.turnToPos(counterAttackTarget.pos);

                float dist = character.pos.distanceTo(counterAttackTarget.pos);
                if (dist < 150)
                {
                    if (character.frameIndex >= 4 && !once)
                    {
                        if (character.pos.distanceTo(counterAttackTarget.pos) > 10)
                        {
                            character.moveToPos(counterAttackTarget.pos, 350);
                        }
                    }
                }
            }

            Point? shootPos = character.getFirstPOI("melee");
            if (!once && shootPos != null)
            {
                once = true;
                new UPParryMeleeProj(new XUPParry(), shootPos.Value, character.xDir, damage, player, player.getNextActorNetId(), rpc: true);
                character.playSound("upParryAttack", sendRpc: true);
                character.shakeCamera(sendRpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeToIdleOrFall();
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            //character.frameIndex = 2;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.parryCooldown = character.maxParryCooldown;
        }
    }

    public class AbsorbWeapon : Weapon
    {
        public Projectile absorbedProj;
        public AbsorbWeapon(Projectile otherProj)
        {
            index = (int)WeaponIds.UPParry;
            weaponSlotIndex = 118;
            killFeedIndex = 168;
            this.absorbedProj = otherProj;
        }
    }

    public class UPParryRangedProj : Projectile
    {
        public UPParryRangedProj(Weapon weapon, Point pos, int xDir, string sprite, float damage, int flinch, float hitCooldown, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 350, damage, player, sprite, flinch, hitCooldown, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.UPParryProj;
            maxDistance = 150;

            if (rpc)
            {
                byte[] hitCooldownBytes = BitConverter.GetBytes(hitCooldown);
                rpcCreate(pos, player, netProjId, xDir, hitCooldownBytes[0], hitCooldownBytes[1], hitCooldownBytes[2], hitCooldownBytes[3]);
            }
        }
    }

    public class XUPParryProjState : CharState
    {
        Projectile otherProj;
        Anim absorbAnim;
        bool shootProj;
        bool absorbThenShoot;
        public XUPParryProjState(Projectile otherProj, bool shootProj, bool absorbThenShoot) : base("unpo_parry_attack", "", "", "")
        {
            this.otherProj = otherProj;
            invincible = true;
            this.shootProj = shootProj;
            this.absorbThenShoot = absorbThenShoot;
        }

        public override void update()
        {
            base.update();

            if (!shootProj && character.sprite.frameIndex >= 1)
            {
                character.sprite.frameIndex = 1;
                character.sprite.frameSpeed = 0;
            }

            if (absorbAnim != null)
            {
                absorbAnim.moveToPos(character.getFirstPOIOrDefault(), 350);
                absorbAnim.xScale -= Global.spf * 5;
                absorbAnim.yScale -= Global.spf * 5;
                if (absorbAnim.xScale <= 0)
                {
                    absorbAnim.destroySelf();
                    absorbAnim = null;
                    if (!shootProj)
                    {
                        character.changeToIdleOrFall();
                        return;
                    }
                }
            }

            Point? shootPos = character.getFirstPOI("proj");
            if (!once && shootPos != null)
            {
                once = true;
                float damage = Math.Max(otherProj.damager.damage * 2, 4);
                //int flinch = otherProj.damager.flinch;
                int flinch = Global.defFlinch;
                float hitCooldown = otherProj.damager.hitCooldown;
                new UPParryRangedProj(new XUPParry(), shootPos.Value, character.xDir, otherProj.sprite.name, damage, flinch, hitCooldown, player, player.getNextActorNetId(), rpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeToIdleOrFall();
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (!shootProj || absorbThenShoot)
            {
                absorbAnim = new Anim(otherProj.pos, otherProj.sprite.name, otherProj.xDir, player.getNextActorNetId(), false, sendRpc: true);
                absorbAnim.syncScale = true;
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            absorbAnim?.destroySelf();
            character.parryCooldown = character.maxParryCooldown;
        }
    }

    public class XUPPunch : Weapon
    {
        public XUPPunch(Player player) : base()
        {
            rateOfFire = 0.75f;
            index = (int)WeaponIds.UPPunch;
            killFeedIndex = 167;
            damager = new Damager(player, 3, Global.defFlinch, 0.5f);
        }
    }

    public class XUPPunchState : CharState
    {
        float slideVelX;
        bool isGrounded;
        public XUPPunchState(bool isGrounded) : base(isGrounded ? "unpo_punch" : "unpo_air_punch", "", "", "")
        {
            this.isGrounded = isGrounded;
        }

        public override void update()
        {
            base.update();
            character.move(new Point(slideVelX, 0));
            slideVelX = Helpers.lerp(slideVelX, 0, Global.spf * 5);
            if (!character.grounded)
            {
                airCode();
            }

            if (character.isAnimOver() || (!isGrounded && character.grounded))
            {
                character.changeToIdleOrFall();
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (oldState is Dash)
            {
                slideVelX = character.xDir * 250;
            }
        }
    }

    public class XUPGrab : Weapon
    {
        public XUPGrab() : base()
        {
            rateOfFire = 0.75f;
            index = (int)WeaponIds.UPGrab;
            killFeedIndex = 92;
        }
    }

    public class XUPGrabState : CharState
    {
        public Character victim;
        float leechTime = 1;
        public bool victimWasGrabbedSpriteOnce;
        float timeWaiting;
        public XUPGrabState(Character victim) : base("unpo_grab", "", "", "")
        {
            this.victim = victim;
            grabTime = UPGrabbed.maxGrabTime;
        }

        public override void update()
        {
            base.update();
            grabTime -= Global.spf;
            leechTime += Global.spf;

            if (victimWasGrabbedSpriteOnce && !victim.sprite.name.EndsWith("_grabbed"))
            {
                character.changeState(new Idle(), true);
                return;
            }

            if (victim.sprite.name.EndsWith("_grabbed") || victim.sprite.name.EndsWith("_die"))
            {
                // Consider a max timer of 0.5-1 second here before the move just shorts out. Same with other command grabs
                victimWasGrabbedSpriteOnce = true;
            }
            if (!victimWasGrabbedSpriteOnce)
            {
                timeWaiting += Global.spf;
                if (timeWaiting > 1)
                {
                    victimWasGrabbedSpriteOnce = true;
                }
                if (character.isDefenderFavored())
                {
                    if (leechTime > 0.33f)
                    {
                        leechTime = 0;
                        character.addHealth(1);
                    }
                    return;
                }
            }

            if (character.sprite.name.Contains("unpo_grab"))
            {
                Point enemyHeadPos = victim.getHeadPos() ?? victim.getCenterPos().addxy(0, -10);
                Point poi = character.getFirstPOIOffsetOnly() ?? new Point();

                Point snapPos = enemyHeadPos.addxy(-poi.x * character.xDir, -poi.y);

                character.changePos(Point.lerp(character.pos, snapPos, 0.25f));

                if (!character.grounded && !character.sprite.name.EndsWith("2"))
                {
                    character.changeSpriteFromName("unpo_grab2", true);
                }
            }

            if (leechTime > 0.33f)
            {
                leechTime = 0;
                character.addHealth(1);
                var damager = new Damager(player, 1, 0, 0);
                damager.applyDamage(victim, false, new XUPGrab(), character, (int)ProjIds.UPGrab);
            }

            if (player.input.isPressed(Control.Special1, player))
            {
                character.changeState(new Idle(), true);
                return;
            }

            if (grabTime <= 0)
            {
                character.changeState(new Idle(), true);
                return;
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            character.grabCooldown = 1;
            victim.grabInvulnTime = 2;
            victim?.releaseGrab(character);
        }
    }

    public class UPGrabbed : CharState
    {
        public const float maxGrabTime = 4;
        public Character grabber;
        public long savedZIndex;
        public UPGrabbed(Character grabber) : base("grabbed")
        {
            this.grabber = grabber;
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            return !character.isInvulnerable() && !character.charState.invincible;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.stopMoving();
            character.stopCharge();
            savedZIndex = character.zIndex;
            character.setzIndex(grabber.zIndex - 100);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.grabInvulnTime = 2;
            character.setzIndex(savedZIndex);
        }

        public override void update()
        {
            base.update();

            grabTime -= player.mashValue();
            if (grabTime <= 0)
            {
                character.changeToIdleOrFall();
            }
        }
    }

    public class XReviveStart : CharState
    {
        public float boxHeight;
        public float boxOffset = 30;
        const float boxSpeed = 90;
        public string dialogLine1Content = "X, I gave you the power to grow";
        public string dialogLine2Content = "stronger than you could have ever imagined.";
        public string dialogLine3Content = "Now, activate your unlimited";
        public string dialogLine4Content = "evolutionary potential!";
        public int dialogType;
        public float dialogTime;

        public string dialogLine1 = "";
        public string dialogLine2 = "";
        int frameCount;
        public int state;
        int dialogIndex;
        float subStateTime;
        Anim drLightAnim;
        public XReviveStart() : base("revive_start")
        {
            invincible = true;
            dialogType = Helpers.randomRange(0, 1);
            if (dialogType == 1)
            {
                dialogLine3Content = "You are the world's one true hope,";
                dialogLine4Content = "X...";
            }
        }

        public bool cancellable()
        {
            if (stateTime > 1.8f) return true;
            //if (state >= 2) return true;
            //if (state == 2 && subStateTime > 0.5f) return true;
            return false;
        }

        public override void update()
        {
            base.update();

            if (cancellable() && Global.input.isPressed(Control.Special1, player))
            {
                character.changeState(new XRevive(), true);
                return;
            }

            if (drLightAnim != null && drLightAnim.isAnimOver() && drLightAnim.sprite.name == "drlight")
            {
                drLightAnim.changeSprite("drlight_talk", true);
            }

            if (state == 0)
            {
                boxHeight += Global.spf * boxSpeed;
                boxOffset -= Global.spf * boxSpeed * 0.5f;
                if (boxHeight > 60)
                {
                    state = 1;
                    frameCount = 0;
                    boxOffset = 0;
                    boxHeight = 60;
                }
            }
            else if (state == 1)
            {
                drLightAnim.frameSpeed = 1;
                dialogTime += Global.spf;

                float dialogWaitTime = 0.03f;
                if (dialogIndex == 2)
                {
                    dialogWaitTime = 0.325f;
                }
                else
                {
                    if (frameCount % 5 == 0) Global.playSound("text");
                }
                frameCount++;

                if (dialogTime > dialogWaitTime)
                {
                    dialogTime = 0;
                    if (dialogLine1.Length < dialogLine1Content.Length)
                    {
                        dialogLine1 += dialogLine1Content[dialogIndex];
                    }
                    else if (dialogLine2.Length < dialogLine2Content.Length)
                    {
                        dialogLine2 += dialogLine2Content[dialogIndex - dialogLine1Content.Length];
                    }
                    else
                    {
                        state = 2;
                        return;
                    }
                    dialogIndex++;
                }
            }
            else if (state == 2)
            {
                drLightAnim.frameSpeed = 0;
                drLightAnim.frameIndex = 0;
                subStateTime += Global.spf;
                if (subStateTime > 1.75f)
                {
                    state = 3;
                    dialogLine1 = "";
                    dialogLine2 = "";
                    dialogIndex = 0;
                    subStateTime = 0;
                }
            }
            else if (state == 3)
            {
                drLightAnim.frameSpeed = 1;
                dialogTime += Global.spf;

                float dialogWaitTime = 0;
                if (dialogType == 0)
                {
                    dialogWaitTime = 0;
                    if (dialogIndex < 4)
                    {
                        dialogWaitTime = 0.03f;
                        if (Global.frameCount % 5 == 0) Global.playSound("text");
                    }
                    else if (dialogIndex == 4)
                    {
                        dialogWaitTime = 0.4f;
                    }
                    else
                    {
                        if (Global.frameCount % 5 == 0) Global.playSound("text");
                    }
                }
                else
                {
                    dialogWaitTime = 0.03f;
                    if (dialogIndex == dialogLine3Content.Length)
                    {
                        dialogWaitTime = 0.4f;
                    }
                    else
                    {
                        if (Global.frameCount % 5 == 0) Global.playSound("text");
                    }
                }

                if (dialogTime > dialogWaitTime)
                {
                    dialogTime = 0;
                    if (dialogLine1.Length < dialogLine3Content.Length)
                    {
                        dialogLine1 += dialogLine3Content[dialogIndex];
                    }
                    else if (dialogLine2.Length < dialogLine4Content.Length)
                    {
                        dialogLine2 += dialogLine4Content[dialogIndex - dialogLine3Content.Length];
                    }
                    else
                    {
                        state = 4;
                        return;
                    }
                    dialogIndex++;
                }
            }
            else if (state == 4)
            {
                drLightAnim.frameSpeed = 0;
                drLightAnim.frameIndex = 0;
            }

            if (stateTime > 2) character.unpoShotCount = Math.Max(character.unpoShotCount, 1);
            if (stateTime > 3.75f) character.unpoShotCount = Math.Max(character.unpoShotCount, 2);
            if (stateTime > 5.75f) character.unpoShotCount = Math.Max(character.unpoShotCount, 3);

            if (stateTime > 7.75f)
            {
                character.unpoShotCount = Math.Max(character.unpoShotCount, 4);
                character.changeState(new XRevive(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            drLightAnim = new Anim(character.pos.addxy(30 * character.xDir, -15), "drlight", -character.xDir, player.getNextActorNetId(), false, sendRpc: true);
            drLightAnim.blink = true;
            int busterIndex = player.weapons.FindIndex(w => w is Buster);
            if (busterIndex >= 0)
            {
                player.changeWeaponSlot(busterIndex);
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            drLightAnim.destroySelf();
        }
    }

    public class XRevive : CharState
    {
        public float radius = 200;
        XReviveAnim reviveAnim;
        public XRevive() : base("revive_shake")
        {
            invincible = true;
        }

        public override void update()
        {
            base.update();
            if (!character.ownedByLocalPlayer) return;

            if (!once && character.frameIndex >= 1 && sprite == "revive")
            {
                character.playSound("ching", sendRpc: true);
                player.health = 1;
                character.addHealth(player.maxHealth);

                player.weapons.RemoveAll(w => w is not Buster);
                if (player.weapons.Count == 0)
                {
                    player.weapons.Add(new Buster());
                }
                var busterWeapon = player.weapons.FirstOrDefault(w => w is Buster) as Buster;
                if (busterWeapon != null)
                {
                    busterWeapon.setUnpoBuster();
                }
                player.weaponSlot = 0;

                once = true;
                var flash = new Anim(character.pos.addxy(0, -33), "up_flash", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
                flash.grow = true;
            }
            
            if (character.isAnimOver())
            {
                character.isHyperX = true;
                if (character.grounded) character.changeState(new Idle(), true);
                else character.changeState(new Fall(), true);
            }

            if (sprite == "revive_shake" && character.loopCount > 6)
            {
                sprite = "revive_shake2";
                character.changeSpriteFromName(sprite, true);
            }
            else if (sprite == "revive_shake2" && character.loopCount > 6)
            {
                sprite = "revive";
                character.changeSpriteFromName(sprite, true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            reviveAnim = new XReviveAnim(character.getCenterPos(), player.getNextActorNetId(), sendRpc: true);
            character.playSound("xRevive", sendRpc: true);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            character.isHyperX = true;
            Global.level.addGameObjectToGrid(character);
            if (character != null)
            {
                character.invulnTime = character.maxParryCooldown;
            }
        }
    }

    public class XReviveAnim : Anim
    {
        public float startRadius = 150;
        public XReviveAnim(Point pos, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
            base(pos, "empty", 1, netId, false, sendRpc, ownedByLocalPlayer)
        {
            ttl = 1f;
        }

        public override void update()
        {
            base.update();
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            DrawWrappers.DrawCircle(pos.x + x, pos.y + y, startRadius * (1 - (time / ttl.Value)), false, Color.White, 5, zIndex + 1, true, Color.White);
        }
    }
}
