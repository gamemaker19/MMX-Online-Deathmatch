using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class CrystalHunter : Weapon
    {
        public CrystalHunter() : base()
        {
            shootSounds = new List<string>() { "crystalHunter", "crystalHunter", "crystalHunter", "crystalHunterCharged" };
            rateOfFire = 1.25f;
            index = (int)WeaponIds.CrystalHunter;
            weaponBarBaseIndex = 9;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 9;
            killFeedIndex = 20;
            weaknessIndex = 15;
            switchCooldown = 0.5f;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel != 3) return 2;
            return 8;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                new CrystalHunterProj(this, pos, xDir, player, 0, netProjId);
            }
            else
            {
                int amount = Global.level.chargedCrystalHunters.Count(c => c.owner == player);
                if (amount >= 1)
                {
                    for (int i = Global.level.chargedCrystalHunters.Count - 1; i >= 0; i--)
                    {
                        var cch = Global.level.chargedCrystalHunters[i];
                        if (cch.owner == player)
                        {
                            cch.destroySelf();
                            amount--;
                            if (amount < 2) break;
                        }
                    }
                }

                new CrystalHunterCharged(pos, player, netProjId, player.ownedByLocalPlayer);
            }
        }
    }

    public class CrystalHunterProj : Projectile
    {
        public CrystalHunterProj(Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId, Point? vel = null, bool rpc = false) : base(weapon, pos, xDir, 250, 0, player, "crystalhunter_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.6f;
            useGravity = true;
            destroyOnHit = true;
            reflectable = true;
            gravityModifier = 0.4f;
            projId = (int)ProjIds.CrystalHunter;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
        }
    }

    public class CrystalHunterCharged : Actor
    {
        public float time;
        public Player owner;
        public ShaderWrapper timeSlowShader;
        public const int radius = 120;
        public float drawRadius = 120;
        public float drawAlpha = 64;
        public bool isSnails;
        float maxTime = 4;
        float soundTime;
        public CrystalHunterCharged(Point pos, Player owner, ushort? netId, bool ownedByLocalPlayer, float? overrideTime = null, bool sendRpc = false) : 
            base("empty", pos, netId, ownedByLocalPlayer, false)
        {
            useGravity = false;
            this.owner = owner;
            isSnails = overrideTime != null;

            if (Options.main.enablePostProcessing)
            {
                timeSlowShader = Helpers.cloneShaderSafe("timeslow");
            }
            
            Global.level.chargedCrystalHunters.Add(this);

            if (isSnails)
            {
                maxTime = overrideTime.Value;
                netOwner = owner;
                netActorCreateId = NetActorCreateId.CrystalHunterCharged;
                if (sendRpc)
                {
                    createActorRpc(owner.id);
                }
            }
        }

        public override void update()
        {
            base.update();

            var screenCoords = new Point(pos.x - Global.level.camX, pos.y - Global.level.camY);
            var normalizedCoords = new Point(screenCoords.x / Global.viewScreenW, 1 - screenCoords.y / Global.viewScreenH);

            if (isSnails)
            {
                Helpers.decrementTime(ref soundTime);
                if (soundTime == 0)
                {
                    playSound("csnailSlowLoop");
                    soundTime = 1.09f;
                }
            }

            if (timeSlowShader != null)
            {
                timeSlowShader.SetUniform("x", normalizedCoords.x);
                timeSlowShader.SetUniform("y", normalizedCoords.y);
                timeSlowShader.SetUniform("t", Global.time);
                if (Global.viewSize == 2) timeSlowShader.SetUniform("r", 0.25f);
                else timeSlowShader.SetUniform("r", 0.5f);
            }

            if (timeSlowShader == null)
            {
                drawRadius = 120 + 0.5f * MathF.Sin(Global.time * 10);
                drawAlpha = 64f + 32f * MathF.Sin(Global.time * 10);
            }

            if (!ownedByLocalPlayer) return;

            time += Global.spf;
            if (time > maxTime)
            {
                destroySelf();
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            Global.level.chargedCrystalHunters.Remove(this);
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (timeSlowShader == null)
            {
                var fillColor = new Color(96, 80, 240, Helpers.toByte(drawAlpha));
                var lineColor = new Color(208, 200, 240, Helpers.toByte(drawAlpha));
                if (owner.alliance == GameMode.redAlliance && Global.level?.gameMode?.isTeamMode == true)
                {
                    fillColor = new Color(240, 80, 96, Helpers.toByte(drawAlpha));
                }

                //if (Global.isOnFrameCycle(20))
                {
                    DrawWrappers.DrawCircle(pos.x, pos.y, drawRadius, true, fillColor, 0, ZIndex.Character - 1, pointCount: 50u);
                    //DrawWrappers.DrawCircle(pos.x, pos.y, drawRadius, false, new Color(208, 200, 240, Helpers.toByte(drawAlpha)), 1f, ZIndex.Character - 1, pointCount: 50u);
                }

                float randY = Helpers.randomRange(-1f, 1f);
                float xLen = MathF.Sqrt(1 - MathF.Pow(randY, 2)) * drawRadius;
                float randThickness = Helpers.randomRange(0.5f, 2f);
                DrawWrappers.DrawLine(pos.x - xLen, pos.y + randY * drawRadius, pos.x + xLen, pos.y + randY * drawRadius, lineColor, randThickness, ZIndex.Character - 1);
            }
        }
    }
}
