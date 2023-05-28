using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum VulcanType
    {
        None = -1,
        CherryBlast,
        DistanceNeedler,
        BuckshotDance
    }

    public class Vulcan : Weapon
    {
        public float vileAmmoUsage;
        public string muzzleSprite;
        public string projSprite;

        public Vulcan(VulcanType vulcanType) : base()
        {
            index = (int)WeaponIds.Vulcan;
            weaponBarBaseIndex = 26;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 62;
            weaponSlotIndex = 44;
            type = (int)vulcanType;

            if (vulcanType == VulcanType.None)
            {
                displayName = "None";
                description = new string[] { "Do not equip a Vulcan." };
                killFeedIndex = 126;
            }
            else if (vulcanType == VulcanType.CherryBlast)
            {
                rateOfFire = 0.1f;
                displayName = "Cherry Blast";
                vileAmmoUsage = 0.25f;
                muzzleSprite = "vulcan_muzzle";
                projSprite = "vulcan_proj";
                description = new string[] { "With a range of approximately 20 feet,", "this vulcan is easy to use." };
                vileWeight = 2;
            }
            else if (vulcanType == VulcanType.DistanceNeedler)
            {
                rateOfFire = 0.25f;
                displayName = "Distance Needler";
                vileAmmoUsage = 6f;
                muzzleSprite = "vulcan_dn_muzzle";
                projSprite = "vulcan_dn_proj";
                killFeedIndex = 88;
                weaponSlotIndex = 59;
                description = new string[] { "This vulcan has good range and speed,", "but cannot fire rapidly." };
                vileWeight = 2;
            }
            else if (vulcanType == VulcanType.BuckshotDance)
            {
                rateOfFire = 0.12f;
                displayName = "Buckshot Dance";
                vileAmmoUsage = 0.3f;
                muzzleSprite = "vulcan_bd_muzzle";
                projSprite = "vulcan_bd_proj";
                killFeedIndex = 89;
                weaponSlotIndex = 60;
                description = new string[] { "The scattering power of this vulcan", "results in less than perfect aiming." };
                vileWeight = 4;
            }
        }

        public override void vileShoot(WeaponIds weaponInput, Character character)
        {
            if (type == (int)VulcanType.DistanceNeedler && shootTime > 0) return;
            if (string.IsNullOrEmpty(character.charState.shootSprite)) return;

            Player player = character.player;
            if (character.tryUseVileAmmo(vileAmmoUsage))
            {
                if (character.charState is LadderClimb)
                {
                    if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
                    if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
                }
                character.changeSpriteFromName(character.charState.shootSprite, false);
                shootVulcan(character);
            }
        }

        public void shootVulcan(Character character)
        {
            Player player = character.player;
            if (shootTime <= 0)
            {
                character.vulcanLingerTime = 0f;
                new VulcanMuzzleAnim(this, character.getShootPos(), character.getShootXDir(), character, player.getNextActorNetId(), true, true);
                new VulcanProj(this, character.getShootPos(), character.getShootXDir(), player, player.getNextActorNetId(), rpc: true);
                if (type == (int)VulcanType.BuckshotDance && Global.isOnFrame(3))
                {
                    new VulcanProj(this, character.getShootPos(), character.getShootXDir(), player, player.getNextActorNetId(), rpc: true);
                }
                character.playSound("vulcan", sendRpc: true);
                character.vileLadderShootCooldown = rateOfFire;
                shootTime = rateOfFire;
            }
        }
    }

    public class VulcanProj : Projectile
    {
        public VulcanProj(Vulcan weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, weapon.type == (int)VulcanType.DistanceNeedler ? 600 : 500, 1, player, weapon.projSprite, 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Vulcan;
            maxTime = 0.25f;
            destroyOnHit = true;
            reflectable = true;

            if (weapon.type == (int)VulcanType.DistanceNeedler)
            {
                maxTime = 0.3f;
                destroyOnHit = false;
                damager.hitCooldown = 0.2f;
                damager.damage = 2;
                projId = (int)ProjIds.DistanceNeedler;
            }
            else if (weapon.type == (int)VulcanType.BuckshotDance)
            {
                //this.xDir = 1;
                //pixelPerfectRotation = true;
                int rand = 0;
                if (player?.character != null)
                {
                    rand = player.character.buckshotDanceNum % 3;
                    player.character.buckshotDanceNum++;
                }
                float angle = 0;
                if (rand == 0) angle = 0;
                if (rand == 1) angle = -20;
                if (rand == 2) angle = 20;
                if (xDir == -1) angle += 180;
                vel = Point.createFromAngle(angle).times(speed);
                projId = (int)ProjIds.BuckshotDance;
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

    public class VulcanMuzzleAnim : Anim
    {
        Character chr;
        public VulcanMuzzleAnim(Vulcan weapon, Point pos, int xDir, Character chr, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
            base(pos, weapon.muzzleSprite, xDir, netId, true, sendRpc, ownedByLocalPlayer)
        {
            this.chr = chr;
        }

        public override void postUpdate()
        {
            if (chr.currentFrame.getBusterOffset() != null)
            {
                changePos(chr.getShootPos());
            }
        }
    }

    public class VulcanCharState : CharState
    {
        bool isCrouch;
        public VulcanCharState(bool isCrouch) : base(isCrouch ? "crouch_shoot" : "idle_shoot", "", "", "")
        {
            this.isCrouch = isCrouch;
        }

        public override void update()
        {
            base.update();

            if (isCrouch && !player.input.isHeld(Control.Down, player))
            {
                character.changeState(new Idle(), true);
                return;
            }

            if (!player.input.isHeld(Control.Shoot, player) || !(player.weapon is Vulcan))
            {
                if (isCrouch) character.changeState(new Crouch(transitionSprite = ""), true);
                else character.changeState(new Idle(), true);
                return;
            }

            if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
            if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
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
}
