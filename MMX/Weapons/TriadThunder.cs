using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class TriadThunder : Weapon
    {
        public TriadThunder() : base()
        {
            shootSounds = new List<string>() { "triadThunder", "triadThunder", "triadThunder", "" };
            rateOfFire = 2.25f;
            index = (int)WeaponIds.TriadThunder;
            weaponBarBaseIndex = 19;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 19;
            killFeedIndex = 42;
            weaknessIndex = (int)WeaponIds.TunnelFang;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel != 3) return 3;
            return 8;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                player.setNextActorNetId(netProjId);
                var triadThunder = new TriadThunderProj(this, pos, xDir, player.input.isHeld(Control.Down, player) ? -1 : 1, player, player.getNextActorNetId(true));
                // Clockwise from top
                var balls = new List<TriadThunderBall>()
                {
                    new TriadThunderBall(this, pos, xDir, player, player.getNextActorNetId(true)),
                    new TriadThunderBall(this, pos, xDir, player, player.getNextActorNetId(true)),
                    new TriadThunderBall(this, pos, xDir, player, player.getNextActorNetId(true)),
                };
                triadThunder.balls = balls;
            }
            else
            {
                if (player.character != null && player.character.ownedByLocalPlayer)
                {
                    player.character.changeState(new TriadThunderChargedState(player.character.grounded), true);
                }
            }
        }
    }

    public class TriadThunderProj : Projectile
    {
        int state;
        Character character;
        public List<TriadThunderBall> balls;
        public TriadThunderProj(Weapon weapon, Point pos, int xDir, int yDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 0, 1, player, "triadthunder_proj", 4, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.TriadThunder;
            character = player.character;
            destroyOnHit = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;
            this.yDir = yDir;

            visible = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer)
            {
                if (time > 0.125f)
                {
                    foreach (var ball in balls)
                    {
                        ball.visible = false;
                    }
                    visible = true;
                }
                return;
            }

            Point incAmount = pos.directionTo(character.getCenterPos());
            incPos(incAmount);
            foreach (var ball in balls)
            {
                ball.incPos(incAmount);
            }

            if (state == 0)
            {
                if (yDir == 1)
                {
                    balls[0].move(new Point(0, -300));
                    balls[1].move(new Point(250, 200));
                    balls[2].move(new Point(-250, 200));
                }
                else
                {
                    balls[0].move(new Point(0, 300));
                    balls[1].move(new Point(250, -200));
                    balls[2].move(new Point(-250, -200));
                }
                if (time > 0.125f)
                {
                    state = 1;
                    time = 0;
                    foreach (var ball in balls)
                    {
                        ball.visible = false;
                    }
                    visible = true;
                }
            }
            else if (state == 1)
            {
                if (time > 1.33f)
                {
                    foreach (var ball in balls)
                    {
                        ball.visible = true;
                        ball.time = 0;
                        ball.maxTime = 1;
                        ball.startDropTime = Global.spf;
                        ball.changeSprite("triadthunder_ball_drop", true);
                    }
                    if (yDir == 1)
                    {
                        new TriadThunderBeam(balls[0].pos.addxy(0, 15), 0, 1, 1, owner, ownedByLocalPlayer);
                        new TriadThunderBeam(balls[1].pos.addxy(-5, 0), 1, 1, 1, owner, ownedByLocalPlayer);
                        new TriadThunderBeam(balls[2].pos.addxy(5, 0), 1, -1, 1, owner, ownedByLocalPlayer);
                    }
                    else
                    {
                        new TriadThunderBeam(balls[0].pos.addxy(0, -15), 0, 1, -1, owner, ownedByLocalPlayer);
                        new TriadThunderBeam(balls[1].pos.addxy(-5, 0), 1, 1, -1, owner, ownedByLocalPlayer);
                        new TriadThunderBeam(balls[2].pos.addxy(5, 0), 1, -1, -1, owner, ownedByLocalPlayer);
                    }
                    destroySelf();
                }
            }
        }
    }

    public class TriadThunderBall : Projectile
    {
        public float startDropTime;
        public TriadThunderBall(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 0, 2, player, "triadthunder_ball", 4, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.TriadThunder;
            destroyOnHit = false;
            shouldShieldBlock = false;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            if (startDropTime > 0)
            {
                startDropTime += Global.spf;
                if (startDropTime > 0.075f)
                {
                    useGravity = true;
                    maxTime = 1;
                }
            }
        }
    }

    public class TriadThunderBeam : Actor
    {
        int type;
        int count;
        float time = 1;
        Player player;
        public List<TriadThunderBeamPiece> pieces = new List<TriadThunderBeamPiece>();
        public TriadThunderBeam(Point pos, int type, int xDir, int yDir, Player player, bool ownedByLocalPlayer) : base("empty", pos, null, ownedByLocalPlayer, false)
        {
            this.xDir = xDir;
            this.yDir = yDir;
            this.type = type;
            this.player = player;
            useGravity = false;
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            time += Global.spf;
            if (time > 0.03f && count < 5)
            {
                int xInc = 0;
                int yInc = -16 * yDir;
                if (type == 1)
                {
                    xInc = 12 * xDir;
                    yInc = 12 * yDir;
                }
                time = 0;
                count++;
                Point lastPos = pieces.Count > 0 ? pieces[pieces.Count - 1].pos : pos;
                pieces.Add(new TriadThunderBeamPiece(new TriadThunder(), lastPos.addxy(xInc, yInc), xDir, yDir, player, type, player.getNextActorNetId(), rpc: true));
            }
            if (count >= 5)
            {
                destroySelf();
            }
        }
    }

    public class TriadThunderBeamPiece : Projectile
    {
        int type;
        public TriadThunderBeamPiece(Weapon weapon, Point pos, int xDir, int yDir, Player player, int type, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, type == 0 ? "triadthunder_beam_up" : "triadthunder_beam_diag", 4, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            this.type = type;
            this.yDir = yDir;
            maxTime = 0.125f;
            projId = (int)ProjIds.TriadThunderBeam;
            destroyOnHit = false;
            shouldShieldBlock = false;
            if (type == 0)
            {
                vel = new Point(0, -300 * yDir);
            }
            else
            {
                vel = new Point(212 * xDir, 212 * yDir);
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

    public class TriadThunderProjCharged : Projectile
    {
        public TriadThunderProjCharged(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 4, player, "triadthunder_charged", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.TriadThunderCharged;
            if (type == 1)
            {
                maxTime = 1f;
                projId = (int)ProjIds.SparkMSpark;
                // netcodeOverride = NetcodeModel.FavorDefender;
                changeSprite("sparkm_proj_spark", true);
            }
            else if (type == 2)
            {
                projId = (int)ProjIds.VoltCBall;
                changeSprite("voltc_proj_ground_thunder", true);
                maxTime = 0.75f;
                wallCrawlSpeed = 150;
            }
            else
            {
                maxTime = 1f;
            }

            destroyOnHit = false;
            shouldShieldBlock = false;

            setupWallCrawl(new Point(xDir, 0));

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            updateWallCrawl();
        }

    }

    public class TriadThunderQuake : Projectile
    {
        public TriadThunderQuake(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 0, 3, player, "triadthunder_charged_quake", Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer)
        {
            useGravity = false;
            projId = (int)ProjIds.TriadThunderQuake;
            maxTime = 0.25f;
            destroyOnHit = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class TriadThunderChargedState : CharState
    {
        bool fired = false;
        bool groundedOnce;
        public TriadThunderChargedState(bool grounded) : base(!grounded ? "fall" : "punch_ground", "", "", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();
            if (!character.ownedByLocalPlayer) return;

            if (!groundedOnce)
            {
                if (!character.grounded)
                {
                    stateTime = 0;
                    return;
                }
                else
                {
                    groundedOnce = true;
                    sprite = "punch_ground";
                    character.changeSprite("mmx_punch_ground", true);
                }
            }

            if (character.frameIndex >= 6 && !fired)
            {
                fired = true;

                float x = character.pos.x;
                float y = character.pos.y;

                character.shakeCamera(sendRpc: true);

                var weapon = new TriadThunder();
                new TriadThunderProjCharged(weapon, new Point(x, y), -1, 0, player, player.getNextActorNetId(), rpc: true);
                new TriadThunderProjCharged(weapon, new Point(x, y), 1, 0, player, player.getNextActorNetId(), rpc: true);
                new TriadThunderQuake(weapon, new Point(x, y), 1, player, player.getNextActorNetId(), rpc: true);

                character.playSound("triadThunderCharged", sendRpc: true);
            }

            if (stateTime > 0.75f)
            {
                character.changeState(new Idle());
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (character.vel.y < 0) character.vel.y = 0;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }
}