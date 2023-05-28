using Newtonsoft.Json;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline
{
    public enum HUDHealthPosition
    {
        Left,
        Right,
        TopLeft,
        TopRight,
        BotLeft,
        BotRight
    }

    public class GameMode
    {
        public const string Deathmatch = "deathmatch";
        public const string TeamDeathmatch = "team deathmatch";
        public const string CTF = "ctf";
        public const string ControlPoint = "control point";
        public const string Elimination = "elimination";
        public const string TeamElimination = "team elimination";
        public const string KingOfTheHill = "king of the hill";
        public const string Race = "race";
        public static List<string> allGameModes = new List<string>() { Deathmatch, TeamDeathmatch, CTF, KingOfTheHill, ControlPoint, Elimination, TeamElimination };

        public static bool isStringTeamMode(string selectedGameMode)
        {
            if (selectedGameMode == CTF || selectedGameMode == TeamDeathmatch || selectedGameMode == ControlPoint || selectedGameMode == TeamElimination || selectedGameMode == KingOfTheHill)
            {
                return true;
            }
            return false;
        }

        public static string abbreviatedMode(string mode)
        {
            if (mode == TeamDeathmatch) return "tdm";
            else if (mode == CTF) return "ctf";
            else if (mode == ControlPoint) return "cp";
            else if (mode == Elimination) return "elim";
            else if (mode == TeamElimination) return "t.elim";
            else if (mode == KingOfTheHill) return "koth";
            else if (mode == Race) return "race";
            else return "dm";
        }

        public bool useTeamSpawns()
        {
            return (this is CTF) || (this is ControlPoints) || (this is KingOfTheHill);
        }

        public float getAmmoModifier()
        {
            /*
            if (level.is1v1())
            {
                if (Global.level.server.playTo == 1) return 0.25f;
                if (Global.level.server.playTo == 2) return 0.5f;
            }
            return 1;
            */
            return 1;
        }

        public const int blueAlliance = 0;
        public const int redAlliance = 1;
        public const int neutralAlliance = 2;

        public bool isTeamMode = false;
        public float overTime = 0;
        public float secondsBeforeLeave = 7;
        public float? setupTime;
        public float? remainingTime;
        public float? startTimeLimit;
        public int playingTo;
        public bool drawingScoreboard;

        public bool noContest;

        public int redPoints;
        public int bluePoints;

        public VoteKick currentVoteKick;
        public float voteKickCooldown;

        public string dpsString;
        public Level level;
        public float eliminationTime;
        public float localElimTimeInc;  // Used to "client side predict" the elimination time increase.
        public byte virusStarted;
        public byte safeZoneSpawnIndex;
        public Point safeZonePoint
        {
            get
            {
                return level.spawnPoints[safeZoneSpawnIndex].pos;
            }
        }
        public Rect safeZoneRect
        {
            get
            {
                if (virusStarted == 0)
                {
                    return new Rect(0, 0, level.width, level.height);
                }
                else if (virusStarted == 1)
                {
                    float t = eliminationTime - startTimeLimit.Value;
                    if (t < 0) t = 0;
                    float timePct = t / 60;
                    return new Rect(
                        timePct * (safeZonePoint.x - 150),
                        timePct * (safeZonePoint.y - 112),
                        level.width - (timePct * (level.width - (safeZonePoint.x + 150))),
                        level.height - (timePct * (level.height - (safeZonePoint.y + 112)))
                    );
                }
                else if (virusStarted == 2)
                {
                    float t = eliminationTime - startTimeLimit.Value - 60;
                    if (t < 0) t = 0;
                    float timePct = t / 300;
                    return new Rect(
                        (safeZonePoint.x - 150) + (timePct * 150),
                        (safeZonePoint.y - 112) + (timePct * 112),
                        (safeZonePoint.x + 150) - (timePct * 150),
                        (safeZonePoint.y + 112) - (timePct * 112)
                    );
                }
                else
                {
                    return new Rect(safeZonePoint.x, safeZonePoint.y, safeZonePoint.x, safeZonePoint.y);
                }
            }
        }

        public RPCMatchOverResponse matchOverResponse;
        public bool isOver { get { return matchOverResponse != null; } }

        public int lastTimeInt;
        public int lastSetupTimeInt;
        public float periodicHostSyncTime;
        public float syncValueTime;

        bool changedEndMenuOnce;
        bool changedEndMenuOnceHost;

        public ChatMenu chatMenu;

        public static void getAllianceCounts(List<Player> players, out int redCount, out int blueCount)
        {
            redCount = players.Count(p => p.alliance == redAlliance && !p.isSpectator);
            blueCount = players.Count(p => p.alliance == blueAlliance && !p.isSpectator);
        }

        public static void getAllianceCounts(List<ServerPlayer> players, out int redCount, out int blueCount)
        {
            redCount = players.Count(p => p.alliance == redAlliance && !p.isSpectator);
            blueCount = players.Count(p => p.alliance == blueAlliance && !p.isSpectator);
        }

        public Player mainPlayer { get { return level?.mainPlayer; } }

        public GameMode(Level level, int? timeLimit)
        {
            this.level = level;
            if (timeLimit != null)
            {
                remainingTime = timeLimit.Value * 60;
                startTimeLimit = remainingTime;
            }
            chatMenu = new ChatMenu();
        }

        public List<KillFeedEntry> killFeed = new List<KillFeedEntry>();
        public List<string> killFeedHistory = new List<string>();
        static List<ChatEntry> getTestChatHistory()
        {
            var test = new List<ChatEntry>();
            for (int i = 0; i < 30; i++)
            {
                test.Add(new ChatEntry("chat entry " + i.ToString(), "gm19", null, false));
            }
            return test;
        }

        bool removedGates;
        public void removeAllGates()
        {
            if (!removedGates) removedGates = true;
            else return;

            for (int i = Global.level.gates.Count - 1; i >= 0; i--)
            {
                Global.level.removeGameObject(Global.level.gates[i]);
                Global.level.gates.RemoveAt(i);
            }
            if (Global.level.isRace())
            {
                foreach (var player in Global.level.players)
                {
                    if (player.character != null && player.character.ownedByLocalPlayer)
                    {
                        player.character.invulnTime = 1;
                    }
                }
            }
        }

        public HostMenu nextMatchHostMenu;
        public virtual void update()
        {
            Helpers.decrementTime(ref hudErrorMsgTime);

            if (Global.isHost)
            {
                if (level.isNon1v1Elimination() && remainingTime.Value <= 0)
                {
                    if (virusStarted < 3)
                    {
                        virusStarted++;
                        if (virusStarted == 1) remainingTime = 60;
                        else if (virusStarted == 2) remainingTime = 300;
                    }
                }
            }
            else
            {
                if (level.isNon1v1Elimination())
                {
                    if (localElimTimeInc < 1)
                    {
                        eliminationTime += Global.spf;
                        localElimTimeInc += Global.spf;
                    }

                    float phase1Time = startTimeLimit.Value;
                    float phase2Time = startTimeLimit.Value + 60;

                    if (eliminationTime <= phase1Time) virusStarted = 0;
                    else if (eliminationTime >= phase1Time && eliminationTime < phase2Time) virusStarted = 1;
                    else if (eliminationTime >= phase2Time) virusStarted = 2;
                }
            }

            if (currentVoteKick != null)
            {
                currentVoteKick.update();
            }
            if (voteKickCooldown > 0)
            {
                voteKickCooldown -= Global.spf;
                if (voteKickCooldown < 0) voteKickCooldown = 0;
            }

            if (level.mainPlayer.isSpectator && !Menu.inMenu)
            {
                if (Global.input.isPressedMenu(Control.Left))
                {
                    level.specPlayer = level.getNextSpecPlayer(-1);
                }
                else if (Global.input.isPressedMenu(Control.Right))
                {
                    level.specPlayer = level.getNextSpecPlayer(1);
                }
            }

            for (var i = this.killFeed.Count - 1; i >= 0; i--)
            {
                var killFeed = this.killFeed[i];
                killFeed.time += Global.spf;
                if (killFeed.time > 8)
                {
                    this.killFeed.Remove(killFeed);
                }
            }

            checkIfWin();
            
            if (Global.isHost && Global.serverClient != null)
            {
                periodicHostSyncTime += Global.spf;
                if (periodicHostSyncTime >= 0.5f)
                {
                    periodicHostSyncTime = 0;
                    RPC.periodicHostSync.sendRpc();
                }
                
                if (Global.level.movingPlatforms.Count > 0)
                {
                    syncValueTime += Global.spf;
                    if (syncValueTime > 0.06f)
                    {
                        syncValueTime = 0;
                        RPC.syncValue.sendRpc(Global.level.syncValue);
                    }
                }
            }

            if ((Global.level.mainPlayer.isAxl || Global.level.mainPlayer.isDisguisedAxl) && Options.main.useMouseAim && overTime < secondsBeforeLeave && !Menu.inMenu && !Global.level.mainPlayer.isSpectator)
            {
                Global.window.SetMouseCursorVisible(false);
                Global.window.SetMouseCursorGrabbed(true);
                Global.isMouseLocked = true;
            }
            else
            {
                Global.window.SetMouseCursorVisible(true);
                Global.window.SetMouseCursorGrabbed(false);
                Global.isMouseLocked = false;
            }

            if (!isOver)
            {
                if (setupTime == 0 && Global.isHost)
                {
                    // Just in case packets were dropped, keep syncing "0" time
                    if (Global.frameCount % 30 == 0)
                    {
                        Global.serverClient?.rpc(RPC.syncSetupTime, 0, 0);
                    }
                }

                if (setupTime > 0 && Global.isHost)
                {
                    int time = MathF.Round(setupTime.Value);
                    byte[] timeBytes = BitConverter.GetBytes((ushort)time);
                    if (setupTime > 0)
                    {
                        setupTime -= Global.spf;
                        if (setupTime <= 0)
                        {
                            setupTime = 0;
                            removeAllGates();
                        }
                    }
                    if (setupTime.Value < lastSetupTimeInt)
                    {
                        Global.serverClient?.rpc(RPC.syncSetupTime, timeBytes);
                    }
                    lastSetupTimeInt = MathF.Floor(setupTime.Value);
                }
                else if (remainingTime != null && Global.isHost)
                {
                    int time = MathF.Round(remainingTime.Value);
                    byte[] timeBytes = BitConverter.GetBytes((ushort)time);
                    int elimTime = MathF.Round(eliminationTime);
                    byte[] elimTimeBytes = BitConverter.GetBytes((ushort)elimTime);

                    if (remainingTime > 0)
                    {
                        remainingTime -= Global.spf;
                        eliminationTime += Global.spf;
                        if (remainingTime <= 0)
                        {
                            remainingTime = 0;
                            if (elimTime > 0) Global.serverClient?.rpc(RPC.syncGameTime, 0, 0, elimTimeBytes[0], elimTimeBytes[1]);
                            else Global.serverClient?.rpc(RPC.syncGameTime, 0, 0);
                        }
                    }

                    if (remainingTime.Value < lastTimeInt)
                    {
                        if (remainingTime.Value <= 10) Global.playSound("tick");
                        if (elimTime > 0) Global.serverClient?.rpc(RPC.syncGameTime, timeBytes[0], timeBytes[1], elimTimeBytes[0], elimTimeBytes[1]);
                        else Global.serverClient?.rpc(RPC.syncGameTime, timeBytes[0], timeBytes[1]);
                    }

                    lastTimeInt = MathF.Floor(remainingTime.Value);
                }
                else if (level.isNon1v1Elimination() && !Global.isHost)
                {
                    remainingTime -= Global.spf;
                }
            }

            bool isWarpIn = level.mainPlayer.character != null && level.mainPlayer.character.isWarpIn();

            Helpers.decrementTime(ref UpgradeMenu.subtankDelay);

            if (!isOver)
            {
                if (!Menu.inMenu && ((level.mainPlayer.warpedIn && !isWarpIn) || Global.level.mainPlayer.isSpectator) && Global.input.isPressedMenu(Control.MenuEnter) && !chatMenu.recentlyExited)
                {
                    level.mainPlayer.character?.resetToggle();
                    Menu.change(new InGameMainMenu());
                }
                else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuEnter) && !isBindingControl())
                {
                    Menu.exit();
                }
            }
            else if (Global.serverClient != null)
            {
                if (!Global.isHost && !level.is1v1())
                {
                    if (!Menu.inMenu && Global.input.isPressedMenu(Control.MenuEnter) && !chatMenu.recentlyExited)
                    {
                        level.mainPlayer.character?.resetToggle();
                        Menu.change(new InGameMainMenu());
                    }
                    else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuEnter) && !isBindingControl())
                    {
                        Menu.exit();
                    }
                }

                if (overTime <= secondsBeforeLeave)
                {
                    
                }
                else
                {
                    if (Global.isHost)
                    {
                        if ((Menu.mainMenu is HostMenu || Menu.mainMenu is SelectCharacterMenu) && Global.input.isPressedMenu(Control.MenuEnter) && !chatMenu.recentlyExited)
                        {
                            level.mainPlayer.character?.resetToggle();
                            Menu.change(new InGameMainMenu());
                        }
                        else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuEnter) && !chatMenu.recentlyExited)
                        {
                            if (nextMatchHostMenu != null) Menu.change(nextMatchHostMenu);
                        }
                        if (!Menu.inMenu)
                        {
                            if (nextMatchHostMenu != null) Menu.change(nextMatchHostMenu);
                        }
                    }
                    else
                    {
                        if (!Menu.inMenu && level.is1v1())
                        {
                            Menu.change(new SelectCharacterMenu(null, level.is1v1(), false, true, true, level.gameMode.isTeamMode, Global.isHost, () => { }));
                        }
                    }
                }
            }
        }

        private bool isBindingControl()
        {
            if (Menu.mainMenu is ControlMenu cm)
            {
                return cm.isBindingControl();
            }
            return false;
        }

        public void checkIfWin()
        {
            if (!isOver)
            {
                if (Global.isHost)
                {
                    checkIfWinLogic();

                    if (noContest)
                    {
                        matchOverResponse = new RPCMatchOverResponse()
                        {
                            winningAlliances = new HashSet<int>() { },
                            winMessage = "No contest!",
                            loseMessage = "No contest!",
                            loseMessage2 = "Host ended match."
                        };
                    }

                    if (isOver)
                    {
                        onMatchOver();
                        Global.serverClient?.rpc(RPC.matchOver, JsonConvert.SerializeObject(matchOverResponse));
                    }
                }
            }
            else
            {
                overTime += Global.spf;
                if (overTime > secondsBeforeLeave)
                {
                    if (Global.serverClient != null)
                    {
                        if (Global.isHost)
                        {
                            if (!changedEndMenuOnceHost)
                            {
                                changedEndMenuOnceHost = true;
                                nextMatchHostMenu = new HostMenu(null, level.server, false, level.server.isLAN);
                                Menu.change(nextMatchHostMenu);
                            }
                        }
                        else
                        {
                            if (!changedEndMenuOnce)
                            {
                                changedEndMenuOnce = true;
                                if (level.is1v1())
                                {
                                    Menu.change(new SelectCharacterMenu(null, level.is1v1(), false, true, true, level.gameMode.isTeamMode, Global.isHost, () => { }));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Global.input.isPressedMenu(Control.MenuEnter))
                        {
                            Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.MatchOver, null, null);
                        }
                    }
                }
            }
        }

        public virtual void checkIfWinLogic()
        {
        }

        public void checkIfWinLogicTeams()
        {
            bool redWins = false;
            bool blueWins = false;
            bool stalemate = false;
            if (remainingTime <= 0)
            {
                if (this is CTF)
                {
                    if (level.redFlag.pickedUpOnce && (redPoints == bluePoints || redPoints == bluePoints - 1))
                    {
                        return;
                    }
                    if (level.blueFlag.pickedUpOnce && (bluePoints == redPoints || bluePoints == redPoints - 1))
                    {
                        return;
                    }
                }

                if (redPoints > bluePoints)
                {
                    redWins = true;
                }
                else if (redPoints < bluePoints)
                {
                    blueWins = true;
                }
                else
                {
                    stalemate = true;
                }
            }

            if (redPoints >= playingTo)
            {
                redWins = true;
            }
            else if (bluePoints >= playingTo)
            {
                blueWins = true;
            }

            if (redWins)
            {
                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { redAlliance },
                    winMessage = "Victory!",
                    winMessage2 = "Red team wins",
                    loseMessage = "You lost!",
                    loseMessage2 = "Red team wins"
                };
            }
            else if (blueWins)
            {
                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { blueAlliance },
                    winMessage = "Victory!",
                    winMessage2 = "Blue team wins",
                    loseMessage = "You lost!",
                    loseMessage2 = "Blue team wins"
                };
            }
            else if (stalemate)
            {
                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { },
                    winMessage = "Stalemate!",
                    loseMessage = "Stalemate!"
                };
            }
        }

        float flashTime;
        float flashCooldown;
        public virtual void render()
        {
            if (level.mainPlayer == null) return;

            Character c = level.mainPlayer.character;
            if (c != null)
            {
                Player p = c.player;
                if (c.isZooming() && !c.isZoomOutPhase1Done)
                {
                    Point charPos = c.getCenterPos();

                    float xOff = p.axlScopeCursorWorldPos.x - level.camCenterX;
                    float yOff = p.axlScopeCursorWorldPos.y - level.camCenterY;

                    Point bulletPos = c.getAxlBulletPos();
                    Point scopePos = c.getAxlScopePos();
                    Point hitPos = c.getCorrectedCursorPos();
                    //Point hitPos = bulletPos.add(c.getAxlBulletDir().times(Global.level.adjustedZoomRange));
                    var hitData = c.getFirstHitPos(p.adjustedZoomRange, ignoreDamagables: true);
                    Point hitPos2 = hitData.hitPos;
                    if (hitPos2.distanceTo(charPos) < hitPos.distanceTo(charPos)) hitPos = hitPos2;
                    if (!c.isZoomingOut && !c.isZoomingIn)
                    {
                        Color laserColor = new Color(255, 0, 0, 160);
                        DrawWrappers.DrawLine(scopePos.x, scopePos.y, hitPos.x, hitPos.y, laserColor, 2, ZIndex.HUD);
                        DrawWrappers.DrawCircle(hitPos.x, hitPos.y, 2f, true, laserColor, 1, ZIndex.HUD);
                        if (c.ownedByLocalPlayer && Global.level.isSendMessageFrame())
                        {
                            RPC.syncAxlScopePos.sendRpc(p.id, true, scopePos, hitPos);
                        }
                    }

                    Point cursorPos = new Point(Global.halfScreenW + (xOff / Global.viewSize), Global.halfScreenH + (yOff / Global.viewSize));
                    string scopeSprite = "scope";
                    if (c.hasScopedTarget()) scopeSprite = "scope2";
                    Global.sprites[scopeSprite].drawToHUD(0, cursorPos.x, cursorPos.y);
                    float w = 298;
                    float h = 224;
                    float hw = 149;
                    float hh = 112;
                    DrawWrappers.DrawRect(cursorPos.x - w, cursorPos.y - h, cursorPos.x + w, cursorPos.y - hh, true, Color.Black, 1, ZIndex.HUD, false, outlineColor: Color.Black);
                    DrawWrappers.DrawRect(cursorPos.x - w, cursorPos.y + hh, cursorPos.x + w, cursorPos.y + h, true, Color.Black, 1, ZIndex.HUD, false, outlineColor: Color.Black);
                    DrawWrappers.DrawRect(cursorPos.x - w, cursorPos.y - hh, cursorPos.x - hw, cursorPos.y + hh, true, Color.Black, 1, ZIndex.HUD, false, outlineColor: Color.Black);
                    DrawWrappers.DrawRect(cursorPos.x + hw, cursorPos.y - hh, cursorPos.x + w, cursorPos.y + hh, true, Color.Black, 1, ZIndex.HUD, false, outlineColor: Color.Black);

                    DrawWrappers.DrawCircle(charPos.x, charPos.y, p.zoomRange, false, Color.Red, 1f, ZIndex.HUD, outlineColor: Color.Red, pointCount: 250);

                    if (!c.isZoomingIn && !c.isZoomingOut)
                    {
                        int zoomChargePercent = MathF.Round(c.zoomCharge * 100);
                        DrawWrappers.DrawText(zoomChargePercent.ToString() + "%", cursorPos.x + 5, cursorPos.y + 5, Alignment.Left, true, 0.75f, Color.White, Color.Black, Text.Styles.Regular, 1, false, ZIndex.HUD);
                    }

                    Helpers.decrementTime(ref flashCooldown);
                    if (c.renderEffects.ContainsKey(RenderEffectType.Hit) && flashTime == 0 && flashCooldown == 0)
                    {
                        flashTime = 0.075f;
                    }
                    if (flashTime > 0)
                    {
                        float th = 2;
                        DrawWrappers.DrawRect(th, th, Global.screenW - th, Global.screenH - th, false, Color.Red, th, ZIndex.HUD, false, outlineColor: Color.Red);
                        flashTime -= Global.spf;
                        if (flashTime < 0)
                        {
                            flashTime = 0;
                            flashCooldown = 0.15f;
                        }
                    }
                }
                else
                {
                    if (c.isAnyZoom() && Global.level.isSendMessageFrame())
                    {
                        RPC.syncAxlScopePos.sendRpc(p.id, false, new Point(), new Point());
                    }
                }
            }
            
            if (!Global.level.mainPlayer.isSpectator)
            {
                renderHealthAndWeapons();

                // Scrap
                if (!Global.level.is1v1())
                {
                    Global.sprites["hud_scrap"].drawToHUD(0, 4, 138);
                    Helpers.drawTextStd(TCat.HUD, "x" + Global.level.mainPlayer.scrap.ToString(), 17, 140, Alignment.Left, fontSize: 24);
                }

                if (mainPlayer.character != null && mainPlayer.character.unpoShotCount > 0)
                {
                    int x = 10, y = 156;
                    int count = mainPlayer.character.unpoShotCount;
                    if (count >= 1) Global.sprites["hud_killfeed_weapon"].drawToHUD(180, x, y);
                    if (count >= 2) Global.sprites["hud_killfeed_weapon"].drawToHUD(180, x + 13, y);
                    if (count >= 3) Global.sprites["hud_killfeed_weapon"].drawToHUD(180, x, y + 11);
                    if (count >= 4) Global.sprites["hud_killfeed_weapon"].drawToHUD(180, x + 13, y + 11);
                }

                if (level.mainPlayer.weapons.Count > 1)
                {
                    drawWeaponSwitchHUD();
                }
                else if (level.mainPlayer.weapons.Count == 1 && level.mainPlayer.weapons[0] is MechMenuWeapon mmw)
                {
                    drawWeaponSwitchHUD();
                }
            }

            if (!Global.level.is1v1())
            {
                drawKillFeed();
            }

            drawTopHUD();

            if (Global.level.isTraining())
            {
                drawDpsIfSet(40);
            }

            if (isOver)
            {
                drawWinScreen();
            }
            else
            {
                int startY = Options.main.showFPS ? 201 : 208;
                if (!Menu.inMenu && !Global.hideMouse && Options.main.showInGameMenuHUD)
                {
                    if (!shouldDrawRadar())
                    {
                        Helpers.drawTextStd(TCat.HUD, Helpers.controlText("[ESC]: Menu"), Global.screenW - 5, startY, Alignment.Right, fontSize: 18);
                        Helpers.drawTextStd(TCat.HUD, Helpers.controlText("[TAB]: Score"), Global.screenW - 5, startY + 7, Alignment.Right, fontSize: 18);
                    }
                }

                drawRespawnHUD();
            }

            drawingScoreboard = false;
            if (!Menu.inControlMenu && level.mainPlayer.input.isHeldMenu(Control.Scoreboard))
            {
                drawingScoreboard = true;
                drawScoreboard();
            }

            if (level.isAfkWarn())
            {
                Helpers.drawTextStd(TCat.HUD, "Warning: Time before AFK Kick: " + Global.level.afkWarnTimeAmount(), Global.halfScreenW - 2, 50, Alignment.Center, fontSize: 24);
            }
            else if (Global.serverClient != null && Global.serverClient.isLagging() && hudErrorMsgTime == 0)
            {
                Helpers.drawTextStd(TCat.HUD, Helpers.controlText("Connectivity issues detected."), Global.halfScreenW - 2, 50, Alignment.Center, fontSize: 24);
            }
            else if (mainPlayer?.character?.possessTarget != null)
            {
                Helpers.drawTextStd(TCat.HUD, Helpers.controlText($"Hold [JUMP] to possess {mainPlayer.character.possessTarget.player.name}"), Global.halfScreenW - 2, 50, Alignment.Center, fontSize: 24);
            }
            else if (hudErrorMsgTime > 0)
            {
                Helpers.drawTextStd(TCat.HUD, hudErrorMsg, Global.halfScreenW - 2, 50, Alignment.Center, fontSize: 24);
            }
            else if (mainPlayer?.isKaiserViralSigma() == true)
            {
                string msg = "";
                if (mainPlayer.character.canKaiserSpawn(out _)) msg += "[JUMP]: Relocate";
                if (msg != "") Helpers.drawTextStd(TCat.HUD, Helpers.controlText(msg), Global.halfScreenW - 2, 50, Alignment.Center, fontSize: 24);
            }
            else if (mainPlayer?.character?.charState is ViralSigmaPossess vsp && vsp.target != null)
            {
                Helpers.drawTextStd(TCat.HUD, $"Controlling possessed player {vsp.target.player.name}", Global.halfScreenW - 2, 50, Alignment.Center, fontSize: 24);
            }
            else if (mainPlayer?.isPossessed() == true && mainPlayer.possesser != null)
            {
                Helpers.drawTextStd(TCat.HUD, $"{mainPlayer.possesser.name} is possessing you!", Global.halfScreenW - 2, 50, Alignment.Center, fontSize: 24);
            }

            if (currentVoteKick != null)
            {
                currentVoteKick.render();
            }
            else if (level.mainPlayer.isSpectator && !Menu.inMenu)
            {
                if (level.specPlayer == null)
                {
                    Helpers.drawTextStd(TCat.HUD, "Now spectating: (No player to spectate)", Global.halfScreenW, 190, Alignment.Center, fontSize: 24);
                }
                else
                {
                    string deadMsg = level.specPlayer.character == null ? " (dead)" : "";
                    Helpers.drawTextStd(TCat.HUD, "Now spectating: " + level.specPlayer.name + deadMsg, Global.halfScreenW, 180, Alignment.Center, fontSize: 24);
                    Helpers.drawTextStd(TCat.HUD, "Left/Right: Change Spectated Player", Global.halfScreenW, 190, Alignment.Center, fontSize: 24);
                }
            }
            else if (level.mainPlayer.aiTakeover)
            {
                Helpers.drawTextStd(TCat.HUD, "AI Takeover active. Press F12 to stop.", Global.halfScreenW, 190, Alignment.Center, fontSize: 24);
            }
            else if (level.mainPlayer.isDisguisedAxl)
            {
                Helpers.drawTextStd(TCat.HUD, "Disguised as " + level.mainPlayer.disguise.targetName, Global.halfScreenW, 190, Alignment.Center, fontSize: 24);
            }
            else if (level.mainPlayer.isPuppeteer() && level.mainPlayer.currentMaverick != null && level.mainPlayer.weapon is MaverickWeapon mw)
            {
                if (level.mainPlayer.currentMaverick.isPuppeteerTooFar())
                {
                    Helpers.drawTextStd(TCat.HUD, mw.displayName + " too far to control", Global.halfScreenW, 190, Alignment.Center, fontSize: 24);
                }
                else
                {
                    Helpers.drawTextStd(TCat.HUD, "Controlling " + mw.displayName, Global.halfScreenW, 190, Alignment.Center, fontSize: 24);
                }
            }
            /*
            else if (level.mainPlayer.character?.isVileMK5Linked() == true)
            {
                string rideArmorName = level.mainPlayer.character.vileStartRideArmor?.getRaTypeFriendlyName() ?? "Ride Armor";
                Helpers.drawTextStd(TCat.HUD, "Controlling " + rideArmorName, Global.halfScreenW, 190, Alignment.Center, fontSize: 24);
            }
            */

            drawDiagnostics();

            if (Global.level.mainPlayer.isAxl && Global.level.mainPlayer.character != null)
            {
                //Global.sprites["axl_cursor"].drawImmediate(0, Global.level.mainPlayer.character.axlCursorPos.x, Global.level.mainPlayer.character.axlCursorPos.y);
            }

            if (level.mainPlayer.isX && level.mainPlayer.hasHelmetArmor(2))
            {
                Player mostRecentlyScanned = null;
                foreach (var player in level.players)
                {
                    if (player.tagged && player.character != null)
                    {
                        mostRecentlyScanned = player;
                        break;
                    }
                }
                if (mostRecentlyScanned != null)
                {
                    drawObjectiveNavpoint(mostRecentlyScanned.name, mostRecentlyScanned.character.getCenterPos());
                }
            }

            if (level.isNon1v1Elimination() && virusStarted > 0)
            {
                drawObjectiveNavpoint("Safe Zone", safeZonePoint);
            }

            if (shouldDrawRadar() && !Menu.inMenu)
            {
                drawRadar();
            }

            if (level.mainPlayer.isX && level.mainPlayer.character?.charState is XReviveStart xrs)
            {
                Character chr = level.mainPlayer.character;

                float boxHeight = xrs.boxHeight;
                float boxEndY = Global.screenH - 5;
                float boxStartY = boxEndY - boxHeight;

                if (chr.pos.y - level.camCenterY > 0)
                {
                    boxStartY = 5;
                    boxEndY = 5 + boxHeight;
                    boxStartY += xrs.boxOffset;
                    boxEndY += xrs.boxOffset;
                }
                else
                {
                    boxStartY -= xrs.boxOffset;
                    boxEndY -= xrs.boxOffset;
                }

                DrawWrappers.DrawRect(5, boxStartY, Global.screenW - 5, boxEndY, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);

                Helpers.drawTextStd(TCat.HUD, xrs.dialogLine1, 55, boxStartY + boxHeight * 0.3f, alignment: Alignment.Left, fontSize: 22);
                Helpers.drawTextStd(TCat.HUD, xrs.dialogLine2, 55, boxStartY + boxHeight * 0.55f, alignment: Alignment.Left, fontSize: 22);

                if (xrs.dialogLine1.Length > 0)
                {
                    int index = 0;
                    if (xrs.state == 1 || xrs.state == 3)
                    {
                        index = Global.isOnFrameCycle(15) ? 1 : 0;
                    }
                    Global.sprites["drlight_portrait"].drawToHUD(index, 15, boxStartY + boxHeight * 0.5f);
                }
            }
        }

        public float hudErrorMsgTime;
        string hudErrorMsg;
        public void setHUDErrorMessage(Player player, string message, bool playSound = true, bool resetCooldown = false)
        {
            if (player != level.mainPlayer) return;
            if (resetCooldown) hudErrorMsgTime = 0;
            if (hudErrorMsgTime == 0)
            {
                hudErrorMsg = message;
                hudErrorMsgTime = 2;
                if (playSound)
                {
                    Global.playSound("error");
                }
            }
        }

        public bool shouldDrawRadar()
        {
            if (Global.level.isRace()) return true;
            if (level.is1v1()) return false;
            if (level.mainPlayer == null) return false;
            if (level.mainPlayer.isX && level.mainPlayer.hasHelmetArmor(3))
            {
                return true;
            }
            if (level.mainPlayer.isAxl && level.boundBlasterAltProjs.Any(b => b.state == 1))
            {
                return true;
            }
            if (level.mainPlayer.isSigma && level.mainPlayer.currentMaverick == null)
            {
                if (level.mainPlayer.isPuppeteer() || level.mainPlayer.isSummoner())
                {
                    return level.mainPlayer.mavericks.Count > 0;
                }
            }
            if (level.mainPlayer.isSigma && level.mainPlayer.currentMaverick != null)
            {
                if (level.mainPlayer.isPuppeteer())
                {
                    return level.mainPlayer.health > 0;
                }
                else if (level.mainPlayer.isSummoner())
                {
                    return level.mainPlayer.mavericks.Count > 1;
                }
            }
            return false;
        }

        void drawRadar()
        {
            List<Point> revealedSpots = new List<Point>();
            float revealedRadius;

            if (level.mainPlayer.isX)
            {
                revealedSpots.Add(new Point(level.camX + Global.viewScreenW / 2, level.camY + Global.viewScreenH / 2));
                revealedRadius = Global.viewScreenW * 1.5f;
            }
            else if (level.mainPlayer.isSigma)
            {
                foreach (var maverick in level.mainPlayer.mavericks)
                {
                    if (maverick == level.mainPlayer.currentMaverick && !level.mainPlayer.isAlivePuppeteer()) continue;
                    revealedSpots.Add(maverick.pos);
                }
                revealedRadius = Global.viewScreenW * 0.5f;
            }
            else
            {
                foreach (var bbAltProj in level.boundBlasterAltProjs)
                {
                    revealedSpots.Add(bbAltProj.pos);
                }
                revealedRadius = Global.viewScreenW;
            }

            float borderThickness = 1;
            float dotRadius = 0.75f;
            if (Global.level.isRace())
            {
                revealedSpots.Add(new Point(level.camX, level.camY));
                revealedRadius = float.MaxValue;
                borderThickness = 0.5f;
                dotRadius = 0.75f;
            }

            Global.radarRenderTexture.Clear(Color.Transparent);
            RenderStates states = new RenderStates(Global.radarRenderTexture.Texture);
            states.BlendMode = new BlendMode(BlendMode.Factor.SrcColor, BlendMode.Factor.Zero, BlendMode.Equation.Add);

            float scaleW = level.scaleW;
            float scaleH = level.scaleH;
            float scaledW = level.scaledW;
            float scaledH = level.scaledH;

            float radarX = Global.screenW - 6 - scaledW;
            float radarY = Global.screenH - 6 - scaledH;

            // The "fog of war" rect
            RectangleShape rect = new RectangleShape(new Vector2f(scaledW * 4, scaledH * 4));
            rect.Position = new Vector2f(0, 0);
            rect.FillColor = new Color(0, 0, 0, 224);
            Global.radarRenderTexture.Draw(rect, states);

            float camStartX = level.camX * scaleW;
            float camStartY = level.camY * scaleH;
            float camEndX = (level.camX + Global.viewScreenW) * scaleW;
            float camEndY = (level.camY + Global.viewScreenH) * scaleH;

            // The visible area circles
            foreach (var spot in revealedSpots)
            {
                float pxPos = spot.x * scaleW * 4;
                float pyPos = spot.y * scaleH * 4;
                float radius = revealedRadius * scaleW * 4;
                CircleShape circle1 = new CircleShape(radius);
                circle1.FillColor = new Color(128, 128, 128, 192);
                circle1.Position = new Vector2f(pxPos - radius, pyPos - radius);
                Global.radarRenderTexture.Draw(circle1, states);
            }

            Global.radarRenderTexture.Display();
            var sprite = new SFML.Graphics.Sprite(Global.radarRenderTexture.Texture);
            sprite.Position = new Vector2f(radarX, radarY);
            sprite.Scale = new Vector2f(0.25f, 0.25f);
            Global.window.SetView(DrawWrappers.hudView);
            Global.window.Draw(sprite);

            if (level.mainPlayer.isSigma)
            {
                foreach (Maverick maverick in level.mainPlayer.mavericks)
                {
                    if (maverick == level.mainPlayer.currentMaverick && !level.mainPlayer.isAlivePuppeteer()) continue;
                    float xPos = maverick.pos.x * scaleW;
                    float yPos = maverick.pos.y * scaleH;
                    DrawWrappers.DrawCircle(radarX + xPos, radarY + yPos, dotRadius, true, new Color(255, 128, 0), 0, ZIndex.HUD, isWorldPos: false);
                }
            }

            if (level.isRace())
            {
                float xPos = level.goal.pos.x * scaleW;
                float yPos = level.goal.pos.y * scaleH;
                DrawWrappers.DrawCircle(radarX + xPos, radarY + yPos, dotRadius, true, Color.White, 0, ZIndex.HUD, isWorldPos: false);
            }

            foreach (var player in level.nonSpecPlayers())
            {
                if (player.character == null) continue;
                if (player.character.isStealthy(level.mainPlayer.alliance)) continue;
                if (player.isMainPlayer && player.isDead) continue;
                
                float xPos = player.character.pos.x * scaleW;
                float yPos = player.character.pos.y * scaleH;

                Color color;
                if (player.isMainPlayer)
                {
                    color = Color.Green;
                }
                else if (player.alliance == level.mainPlayer.alliance) color = Color.Yellow;
                else color = Color.Red;

                if (xPos < 0 || xPos > scaledW || yPos < 0 || yPos > scaledH) continue;

                foreach (var spot in revealedSpots)
                {
                    if (player.isMainPlayer || new Point(xPos, yPos).distanceTo(new Point(spot.x * scaleW, spot.y * scaleH)) < revealedRadius * scaleW)
                    {
                        DrawWrappers.DrawCircle(radarX + xPos, radarY + yPos, dotRadius, true, color, 0, ZIndex.HUD, isWorldPos: false);
                        break;
                    }
                }
            }

            // Radar rectangle itself (with border)
            DrawWrappers.DrawRect(radarX, radarY, Global.screenW - 6, Global.screenH - 6, true, Color.Transparent, borderThickness, ZIndex.HUD, isWorldPos: false, outlineColor: Color.White);

            // Camera
            DrawWrappers.DrawRect(radarX + camStartX, radarY + camStartY, radarX + camEndX, radarY + camEndY, true, new Color(0, 0, 0, 0), borderThickness * 0.5f, ZIndex.HUD, isWorldPos: false, outlineColor: new Color(255, 255, 255));
        }

        public Player hudTopLeftPlayer;
        public Player hudTopRightPlayer;
        public Player hudLeftPlayer;
        public Player hudRightPlayer;
        public Player hudBotLeftPlayer;
        public Player hudBotRightPlayer;

        public void draw1v1PlayerTopHUD(Player player, HUDHealthPosition position)
        {
            if (player == null) return;

            Color outlineColor = isTeamMode ? Helpers.getAllianceColor(player) : Helpers.DarkBlue;

            bool isLeft = position == HUDHealthPosition.Left || position == HUDHealthPosition.TopLeft || position == HUDHealthPosition.BotLeft;
            bool isTop = position != HUDHealthPosition.BotLeft && position != HUDHealthPosition.BotRight;

            float lifeX = (isLeft ? 10 : Global.screenW - 10);
            float lifeY = (isTop ? 10 : Global.screenH - 10);

            float nameX = (isLeft ? 20 : Global.screenW - 20);
            float nameY = (isTop ? 5 : Global.screenH - 15);

            float deathX = (isLeft ? 11 : Global.screenW - 9);
            float deathY = (isTop ? 18 : Global.screenH - 26);

            Global.sprites["hud_life"].drawToHUD(player.getHudLifeSpriteIndex(), lifeX, lifeY);
            Helpers.drawTextStd(TCat.HUD, player.name, nameX, nameY, (isLeft ? Alignment.Left : Alignment.Right), fontSize: 24, outlineColor: outlineColor);
            Helpers.drawTextStd(TCat.HUD, player.getDeathScore(), deathX, deathY, Alignment.Center, fontSize: 24, outlineColor: outlineColor);
        }

        public void draw1v1TopHUD()
        {
            draw1v1PlayerTopHUD(hudTopLeftPlayer, HUDHealthPosition.TopLeft);
            draw1v1PlayerTopHUD(hudTopRightPlayer, HUDHealthPosition.TopRight);
            draw1v1PlayerTopHUD(hudLeftPlayer, HUDHealthPosition.Left);
            draw1v1PlayerTopHUD(hudRightPlayer, HUDHealthPosition.Right);
            draw1v1PlayerTopHUD(hudBotLeftPlayer, HUDHealthPosition.BotLeft);
            draw1v1PlayerTopHUD(hudBotRightPlayer, HUDHealthPosition.BotRight);

            if (remainingTime != null)
            {
                var timespan = new TimeSpan(0, 0, MathF.Ceiling(remainingTime.Value));
                string timeStr = timespan.ToString(@"m\:ss");
                Helpers.drawTextStd(TCat.HUD, timeStr, Global.halfScreenW, 5, Alignment.Center, fontSize: (uint)32, color: getTimeColor());
            }
        }

        public void assignPlayerHUDPositions()
        {
            var nonSpecPlayers = level.players.FindAll(p => p.is1v1Combatant && p != mainPlayer);
            if (mainPlayer != null)
            {
                nonSpecPlayers.Insert(0, mainPlayer);
            }

            // Two player case: just arrange left and right trivially
            if (nonSpecPlayers.Count <= 2)
            {
                hudLeftPlayer = nonSpecPlayers.ElementAtOrDefault(0);
                hudRightPlayer = nonSpecPlayers.ElementAtOrDefault(1);
            }
            // Three player case with mainPlayer
            else if (nonSpecPlayers.Count == 3 && mainPlayer != null)
            {
                // Not a team mode: put main player on left, others on right
                if (!isTeamMode)
                {
                    hudLeftPlayer = nonSpecPlayers[0];
                    hudTopRightPlayer = nonSpecPlayers[1];
                    hudBotRightPlayer = nonSpecPlayers[2];
                }
                // If team mode, group main player on left with first ally.
                else
                {
                    int mainPlayerAlliance = mainPlayer.alliance;
                    var mainPlayerAllies = nonSpecPlayers.FindAll(p => p != mainPlayer && p.alliance == mainPlayer.alliance);
                    if (mainPlayerAllies.Count == 0)
                    {
                        hudLeftPlayer = nonSpecPlayers[0];
                        hudTopRightPlayer = nonSpecPlayers[1];
                        hudBotRightPlayer = nonSpecPlayers[2];
                    }
                    else
                    {
                        hudTopLeftPlayer = nonSpecPlayers[0];
                        hudBotLeftPlayer = mainPlayerAllies[0];
                        hudRightPlayer = nonSpecPlayers.FirstOrDefault(p => p != nonSpecPlayers[0] && p != mainPlayerAllies[0]);
                    }
                }
            }
            else
            {
                // Four players with main player and team mode: group main player with any allies on left if they exist
                if (nonSpecPlayers.Count == 4 && mainPlayer != null && isTeamMode)
                {
                    int allyIndex = nonSpecPlayers.FindIndex(p => p != mainPlayer && p.alliance == mainPlayer.alliance);
                    if (allyIndex != -1)
                    {
                        var temp = nonSpecPlayers[2];
                        nonSpecPlayers[2] = nonSpecPlayers[allyIndex];
                        nonSpecPlayers[allyIndex] = temp;
                    }
                }

                hudTopLeftPlayer = nonSpecPlayers.ElementAtOrDefault(0);
                hudTopRightPlayer = nonSpecPlayers.ElementAtOrDefault(1);
                hudBotLeftPlayer = nonSpecPlayers.ElementAtOrDefault(2);
                hudBotRightPlayer = nonSpecPlayers.ElementAtOrDefault(3);
            }
        }

        bool hudPositionsAssigned;
        public void renderHealthAndWeapons()
        {
            bool is1v1OrTraining = level.is1v1() || level.levelData.isTraining();
            if (!is1v1OrTraining)
            {
                renderHealthAndWeapon(level.mainPlayer, HUDHealthPosition.Left);
            }
            else
            {
                if (!hudPositionsAssigned)
                {
                    assignPlayerHUDPositions();
                    hudPositionsAssigned = true;
                }

                renderHealthAndWeapon(hudTopLeftPlayer, HUDHealthPosition.TopLeft);
                renderHealthAndWeapon(hudTopRightPlayer, HUDHealthPosition.TopRight);
                renderHealthAndWeapon(hudLeftPlayer, HUDHealthPosition.Left);
                renderHealthAndWeapon(hudRightPlayer, HUDHealthPosition.Right);
                renderHealthAndWeapon(hudBotLeftPlayer, HUDHealthPosition.BotLeft);
                renderHealthAndWeapon(hudBotRightPlayer, HUDHealthPosition.BotRight);
            }
        }

        public void renderHealthAndWeapon(Player player, HUDHealthPosition position)
        {
            if (player == null) return;
            if (level.is1v1() && player.deaths >= playingTo) return;

            //Health
            renderHealth(player, position, false);
            bool mechBarExists = renderHealth(player, position, true);

            //Weapon
            if (!mechBarExists) renderWeapon(player, position);
        }

        public Point getHUDHealthPosition(HUDHealthPosition position, bool isHealth)
        {
            float x = 0;
            if (position == HUDHealthPosition.Left || position == HUDHealthPosition.TopLeft || position == HUDHealthPosition.BotLeft)
            {
                x = isHealth ? 10 : 25;
            }
            else
            {
                x = isHealth ? 288 : 273;
            }
            float y = Global.screenH / 2;
            if (position == HUDHealthPosition.TopLeft || position == HUDHealthPosition.TopRight)
            {
                y -= 27;
            }
            else if (position == HUDHealthPosition.BotLeft || position == HUDHealthPosition.BotRight)
            {
                y += 61;
            }

            return new Point(x, y);
        }

        public bool renderHealth(Player player, HUDHealthPosition position, bool isMech)
        {
            bool mechBarExists = false;

            string spriteName = "hud_health_base";
            float health = player.health;
            float maxHealth = player.maxHealth;

            if (player.currentMaverick != null)
            {
                health = player.currentMaverick.health;
                maxHealth = player.currentMaverick.maxHealth;
            }

            int frameIndex = player.charNum;
            if (player.isDisguisedAxl) frameIndex = 3;

            var hudHealthPosition = getHUDHealthPosition(position, true);
            float baseX = hudHealthPosition.x;
            float baseY = hudHealthPosition.y;

            float twoLayerHealth = 0;
            if (isMech && player.character?.rideArmor != null)
            {
                spriteName = "hud_health_base_mech";
                health = player.character.rideArmor.health;
                maxHealth = player.character.rideArmor.maxHealth;
                twoLayerHealth = player.character.rideArmor.goliathHealth;
                frameIndex = player.character.rideArmor.raNum;
                baseX = getHUDHealthPosition(position, false).x;
                mechBarExists = true;
            }

            if (isMech && player.character?.rideChaser != null)
            {
                spriteName = "hud_health_base_bike";
                health = player.character.rideChaser.health;
                maxHealth = player.character.rideChaser.maxHealth;
                frameIndex = 0;
                baseX = getHUDHealthPosition(position, false).x;
                mechBarExists = true;
            }

            maxHealth /= player.getHealthModifier();
            health /= player.getHealthModifier();

            baseY += 25;
            var healthBaseSprite = spriteName;
            Global.sprites[healthBaseSprite].drawToHUD(frameIndex, baseX, baseY);
            baseY -= 16;
            for (var i = 0; i < MathF.Ceiling(maxHealth); i++)
            {
                if (i < MathF.Ceiling(health))
                {
                    int barIndex = 0;
                    bool isHyperX = player.character?.isHyperX == true || player.character?.charState is XRevive;
                    if (isHyperX)
                    {
                        if (player.character.unpoDamageMaxCooldown >= 2) barIndex = 1;
                        else if (player.character.unpoDamageMaxCooldown >= 1) barIndex = 3;
                        else if (player.character.unpoDamageMaxCooldown >= 0.5f) barIndex = 4;
                        else barIndex = 5;
                    }
                    Global.sprites["hud_health_full"].drawToHUD(barIndex, baseX, baseY);
                }
                else
                {
                    Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
                }

                if (twoLayerHealth > 0 && i < MathF.Ceiling(twoLayerHealth))
                {
                    Global.sprites["hud_health_full"].drawToHUD(2, baseX, baseY);
                }

                baseY -= 2;
            }
            Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);

            // 2-layer health

            return mechBarExists;
        }

        const int grayAmmoIndex = 30;
        public void renderAmmo(float baseX, ref float baseY, int baseIndex, int barIndex, float ammo, float grayAmmo = 0)
        {
            baseY += 25;
            Global.sprites["hud_weapon_base"].drawToHUD(baseIndex, baseX, baseY);
            baseY -= 16;
            for (var i = 0; i < MathF.Ceiling(32); i++)
            {
                if (i < Math.Ceiling(ammo))
                {
                    if (ammo < grayAmmo) Global.sprites["hud_weapon_full"].drawToHUD(grayAmmoIndex, baseX, baseY);
                    else Global.sprites["hud_weapon_full"].drawToHUD(barIndex, baseX, baseY);
                }
                else
                {
                    Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
                }
                baseY -= 2;
            }
            Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
        }

        public bool shouldDrawWeaponAmmo(Player player, Weapon weapon)
        {
            if (weapon == null) return false;
            if (weapon.index == 0 && weapon is not Buster) return false;
            if (weapon is AbsorbWeapon) return false;
            if (weapon is DNACore) return false;
            if (weapon is AssassinBullet) return false;
            if (weapon is UndisguiseWeapon) return false;
            if (weapon is NovaStrike && level.isHyper1v1()) return false;
            if (weapon is Buster buster) return false;

            return true;
        }

        public void renderWeapon(Player player, HUDHealthPosition position)
        {
            var hudHealthPosition = getHUDHealthPosition(position, false);
            float baseX = hudHealthPosition.x;
            float baseY = hudHealthPosition.y;

            if (player.isSigma)
            {
                if (player.character == null || (player.isSigma3() && player.currentMaverick == null) || (player.isSigma1() && player.character.isHyperSigmaBS.getValue()))
                {
                    return;
                }

                if (player.isMainPlayer && player.currentMaverick is StormEagle se && se.ammo < 32)
                {
                    renderAmmo(baseX, ref baseY, 44, 38, player.currentMaverick.ammo);
                }
                else if (player.isMainPlayer && player.currentMaverick is MorphMoth me && player.currentMaverick.ammo < 32)
                {
                    renderAmmo(baseX, ref baseY, 53, 42, player.currentMaverick.ammo);
                }
                else if (player.isMainPlayer && player.currentMaverick is MorphMothCocoon mmc)
                {
                    renderAmmo(baseX, ref baseY, 52, 41, player.currentMaverick.ammo);
                }
                else if (player.isMainPlayer && player.currentMaverick is CrystalSnail cs && player.currentMaverick.ammo < 32 && !cs.noShell)
                {
                    renderAmmo(baseX, ref baseY, 54, 43, player.currentMaverick.ammo);
                }
                /*
                else if (player.isMainPlayer && player.currentMaverick is SparkMandrill)
                {
                    renderAmmo(baseX, ref baseY, 55, 44, player.currentMaverick.ammo, grayAmmo: 31);
                }
                */
                else if (player.isMainPlayer && player.currentMaverick is ArmoredArmadillo)
                {
                    renderAmmo(baseX, ref baseY, 56, 45, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is StingChameleon)
                {
                    renderAmmo(baseX, ref baseY, 57, 46, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is BoomerKuwanger)
                {
                    renderAmmo(baseX, ref baseY, 58, 47, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is MagnaCentipede)
                {
                    renderAmmo(baseX, ref baseY, 59, 48, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is FakeZero)
                {
                    renderAmmo(baseX, ref baseY, 60, 49, player.currentMaverick.ammo, grayAmmo: 2);
                }
                if (player.isSigma2() && player.character.isHyperSigmaBS.getValue() && player.currentMaverick == null)
                {
                    renderAmmo(baseX, ref baseY, 61, 50, player.sigmaAmmo, grayAmmo: player.weapon.getAmmoUsage(0));
                }
                else if (player.isMainPlayer && player.currentMaverick is BubbleCrab)
                {
                    renderAmmo(baseX, ref baseY, 62, 51, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is VoltCatfish)
                {
                    renderAmmo(baseX, ref baseY, 65, 54, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is DrDoppler)
                {
                    renderAmmo(baseX, ref baseY, 66, 55, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is ToxicSeahorse)
                {
                    renderAmmo(baseX, ref baseY, 67, 56, player.currentMaverick.ammo, grayAmmo: 8);
                }
                else if (player.isMainPlayer && player.currentMaverick is BlastHornet bh && bh.ammo < 32)
                {
                    renderAmmo(baseX, ref baseY, 68, 57, player.currentMaverick.ammo);
                }
                else if (player.isMainPlayer && player.currentMaverick is LaunchOctopus lo)
                {
                    renderAmmo(baseX, ref baseY, 71, 60, player.currentMaverick.ammo);
                }
                else if (player.isMainPlayer && player.currentMaverick == null)
                {
                    int hudWeaponBaseIndex = 50;
                    int hudWeaponFullIndex = 39;
                    int floorOrCeil = MathF.Ceiling(player.sigmaMaxAmmo);
                    if (player.isSigma2())
                    {
                        hudWeaponBaseIndex = 51;
                        hudWeaponFullIndex = player.sigmaAmmo < 16 ? 30 : 40;
                        floorOrCeil = MathF.Floor(player.sigmaMaxAmmo);
                    }
                    baseY += 25;
                    Global.sprites["hud_weapon_base"].drawToHUD(hudWeaponBaseIndex, baseX, baseY);
                    baseY -= 16;
                    for (var i = 0; i < floorOrCeil; i++)
                    {
                        if (i < Math.Ceiling(player.sigmaAmmo))
                        {
                            Global.sprites["hud_weapon_full"].drawToHUD(hudWeaponFullIndex, baseX, baseY);
                        }
                        else
                        {
                            Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
                        }
                        baseY -= 2;
                    }
                    Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
                    return;
                }
                return;
            }

            if (player.isVile)
            {
                baseY += 25;
                Global.sprites["hud_weapon_base"].drawToHUD(39, baseX, baseY);
                baseY -= 16;
                for (var i = 0; i < MathF.Ceiling(player.vileMaxAmmo); i++)
                {
                    if (i < Math.Ceiling(player.vileAmmo))
                    {
                        Global.sprites["hud_weapon_full"].drawToHUD(32, baseX, baseY);
                    }
                    else
                    {
                        Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
                    }
                    baseY -= 2;
                }
                Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
                return;
            }

            Weapon weapon = player.weapon;
            if (player.isZero && !player.isZBusterZero())
            {
                weapon = player.zeroGigaAttackWeapon;
            }

            if (shouldDrawWeaponAmmo(player, weapon))
            {
                baseY += 25;
                Global.sprites["hud_weapon_base"].drawToHUD(weapon.weaponBarBaseIndex, baseX, baseY);
                baseY -= 16;
                for (var i = 0; i < MathF.Ceiling(weapon.maxAmmo); i++)
                {
                    var floorOrCeiling = Math.Ceiling(weapon.ammo);
                    // Weapons that cost the whole bar go here, so they don't show up as full but still grayed out
                    if (weapon is RekkohaWeapon || weapon is GigaCrush)
                    {
                        floorOrCeiling = Math.Floor(weapon.ammo);
                    }
                    if (i < floorOrCeiling)
                    {
                        int spriteIndex = weapon.weaponBarIndex;
                        if ((weapon is RakuhouhaWeapon && weapon.ammo < 16) ||
                            (weapon is RekkohaWeapon && weapon.ammo < 32) ||
                            (weapon is CFlasher && weapon.ammo < 8) ||
                            (weapon is ShinMessenkou && weapon.ammo < 16) ||
                            (weapon is DarkHoldWeapon && weapon.ammo < 16) ||
                            (weapon is GigaCrush && !weapon.canShoot(0, player)) ||
                            (weapon is NovaStrike && !weapon.canShoot(0, player)) ||
                            (weapon is HyperBuster hb && !hb.canShootIncludeCooldown(level.mainPlayer)))
                        {
                            spriteIndex = grayAmmoIndex;
                        }
                        if (spriteIndex >= Global.sprites["hud_weapon_full"].frames.Count)
                        {
                            spriteIndex = 0;
                        }
                        Global.sprites["hud_weapon_full"].drawToHUD(spriteIndex, baseX, baseY);
                    }
                    else
                    {
                        Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
                    }
                    baseY -= 2;
                }
                Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
            }
        }

        public void addKillFeedEntry(KillFeedEntry killFeed, bool sendRpc = false)
        {
            killFeedHistory.Add(killFeed.rawString());
            this.killFeed.Insert(0, killFeed);
            if (this.killFeed.Count > 4) this.killFeed.Pop();
            if (sendRpc)
            {
                killFeed.sendRpc();
            }
        }

        public void drawKillFeed()
        {
            var fromRight = Global.screenW - 10;
            var fromTop = 10;
            var yDist = 12;
            for (var i = 0; i < this.killFeed.Count; i++)
            {
                var killFeed = this.killFeed[i];

                string victimName = killFeed.victim?.name ?? "";
                if (killFeed.maverickKillFeedIndex != null)
                {
                    victimName = " (" + killFeed.victim.name + ")";
                }

                var msg = "";
                var killersMsg = "";
                if (killFeed.killer != null)
                {
                    var killerMessage = "";
                    if (killFeed.killer != killFeed.victim)
                    {
                        killerMessage = killFeed.killer.name;
                    }
                    var assisterMsg = "";
                    if (killFeed.assister != null && killFeed.assister != killFeed.victim)
                    {
                        assisterMsg = killFeed.assister.name;
                    }

                    var killerAndAssister = new List<string>();
                    if (!string.IsNullOrEmpty(killerMessage)) killerAndAssister.Add(killerMessage);
                    if (!string.IsNullOrEmpty(assisterMsg)) killerAndAssister.Add(assisterMsg);

                    killersMsg = string.Join(" & ", killerAndAssister) + "    ";

                    msg = killersMsg + victimName;
                    
                }
                else if (killFeed.victim != null && killFeed.customMessage == null)
                {
                    if (killFeed.maverickKillFeedIndex != null)
                    {
                        msg = killFeed.victim.name + "'s Maverick died";
                    }
                    else
                    {
                        msg = victimName + " died";
                    }
                }
                else
                {
                    msg = killFeed.customMessage;
                }

                if (killFeed.killer == level.mainPlayer || killFeed.victim == level.mainPlayer || killFeed.assister == level.mainPlayer)
                {
                    var msgLen = Helpers.measureTextStd(TCat.HUD, msg, fontSize: 24).x;
                    var msgHeight = 10;
                    DrawWrappers.DrawRect(fromRight - msgLen - 2, fromTop - 2 + (i * yDist) - msgHeight / 2, fromRight + 2, fromTop - 1 + msgHeight / 2 + (i * yDist), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, isWorldPos: false, outlineColor: Color.White);
                }

                var isKillerRed = killFeed.killer != null && killFeed.killer.alliance == redAlliance && isTeamMode;
                var isVictimRed = killFeed.victim != null && killFeed.victim.alliance == redAlliance && isTeamMode;
                if (killFeed.victim == null) isVictimRed = killFeed.customMessageAlliance == blueAlliance ? false : true;

                if (killFeed.killer != null)
                {
                    var nameLen = Helpers.measureTextStd(TCat.HUD, victimName, fontSize: 24).x;
                    Helpers.drawTextStd(TCat.HUDColored, victimName, fromRight, fromTop + (i * yDist) - 5, Alignment.Right, fontSize: 24, outlineColor: isVictimRed ? Helpers.DarkRed : Helpers.DarkBlue);
                    var victimNameWidth = Helpers.measureTextStd(TCat.Default, victimName, fontSize: 24).x;
                    Helpers.drawTextStd(TCat.HUDColored, killersMsg, fromRight - victimNameWidth, fromTop + (i * yDist) - 5, Alignment.Right, fontSize: 24, outlineColor: isKillerRed ? Helpers.DarkRed : Helpers.DarkBlue);
                    int weaponIndex = (int)killFeed.weaponIndex;
                    weaponIndex = weaponIndex < Global.sprites["hud_killfeed_weapon"].frames.Count ? weaponIndex : 0;
                    Global.sprites["hud_killfeed_weapon"].drawToHUD(weaponIndex, fromRight - nameLen - 13, fromTop + (i * yDist) - 2);
                    if (killFeed.maverickKillFeedIndex != null)
                    {
                        Global.sprites["hud_killfeed_weapon"].drawToHUD(killFeed.maverickKillFeedIndex.Value, fromRight - nameLen + 3, fromTop + (i * yDist) - 2);
                    }
                }
                else
                {
                    Helpers.drawTextStd(TCat.HUDColored, msg, fromRight, fromTop + (i * yDist) - 5, Alignment.Right, fontSize: 24, outlineColor: killFeed.customMessageAlliance == GameMode.blueAlliance ? Helpers.DarkBlue : Helpers.DarkRed);
                }
            }
        }

        public void drawSpectators()
        {
            var spectatorNames = level.players.Where(p => p.isSpectator).Select((p) =>
            {
                bool isHost = p.serverPlayer?.isHost ?? false;
                return p.name + (isHost ? " (Host)" : "");
            });
            string spectatorStr = string.Join(",", spectatorNames);
            if (!string.IsNullOrEmpty(spectatorStr))
            {
                Helpers.drawTextStd(TCat.HUD, "Spectators: " + spectatorStr, 15, 200, fontSize: 20);
            }
        }

        int currentLineH;
        public void drawDiagnostics()
        {
            if (Global.showDiagnostics)
            {
                double? downloadedBytes = 0;
                double? uploadedBytes = 0;

                if (Global.serverClient?.client?.ServerConnection?.Statistics != null)
                {
                    downloadedBytes = Global.serverClient.client.ServerConnection.Statistics.ReceivedBytes;
                    uploadedBytes = Global.serverClient.client.ServerConnection.Statistics.SentBytes;
                }

                int topLeftX = 10;
                int topLeftY = 35;
                int w = 120;
                int lineHeight = 4;
                uint fontSize = 12;

                DrawWrappers.DrawRect(topLeftX - 5, topLeftY - 5, topLeftX + w, topLeftY + 6 + currentLineH, true, Helpers.MenuBgColor, 1, ZIndex.HUD - 10, isWorldPos: false);

                currentLineH = -6;

                bool showNetStats = Global.debug;
                if (showNetStats)
                {
                    if (downloadedBytes != null)
                    {
                        string downloadMb = (downloadedBytes.Value / 1000000.0).ToString("0.00");
                        string downloadKb = (downloadedBytes.Value / 1000.0).ToString("0.00");
                        Helpers.drawTextStd("Bytes received: " + downloadMb + " mb" + " (" + downloadKb + " kb)", topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);
                    }
                    if (uploadedBytes != null)
                    {
                        string uploadMb = (uploadedBytes.Value / 1000000.0).ToString("0.00");
                        string uploadKb = (uploadedBytes.Value / 1000.0).ToString("0.00");
                        Helpers.drawTextStd("Bytes sent: " + uploadMb + " mb" + " (" + uploadKb + " kb)", topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);
                    }

                    double avgPacketIncrease = Global.lastFramePacketIncreases.Count == 0 ? 0 : Global.lastFramePacketIncreases.Average();
                    Helpers.drawTextStd("Packet rate: " + (avgPacketIncrease * 60f).ToString("0") + " bytes/second", topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);
                    Helpers.drawTextStd("Packet rate: " + avgPacketIncrease.ToString("0") + " bytes/frame", topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);
                }

                double avgPacketsReceived = Global.last10SecondsPacketsReceived.Count == 0 ? 0 : Global.last10SecondsPacketsReceived.Average();
                Helpers.drawTextStd("Ping Packets / sec: " + avgPacketsReceived.ToString("0.0"), topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);

                Helpers.drawTextStd("Start GameObject Count: " + level.startGoCount, topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);
                Helpers.drawTextStd("Current GameObject Count: " + level.gameObjects.Count, topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);

                Helpers.drawTextStd("Start GridItem Count: " + level.startGridCount, topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);
                Helpers.drawTextStd("Current GridItem Count: " + level.getGridCount(), topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);

                Helpers.drawTextStd("Sound Count: " + Global.sounds.Count, topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);

                Helpers.drawTextStd("List Counts: " + Global.level.getListCounts(), topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);

                float avgFrameProcessTime = Global.lastFrameProcessTimes.Count == 0 ? 0 : Global.lastFrameProcessTimes.Average();

                Helpers.drawTextStd("Avg frame process time: " + avgFrameProcessTime.ToString("0.00") + " ms", topLeftX, topLeftY + (currentLineH += lineHeight), alignment: Alignment.Left, fontSize: fontSize, outlineColor: Color.Black);
                //float graphYHeight = 20;
                //drawDiagnosticsGraph(Global.lastFrameProcessTimes, topLeftX, topLeftY + (currentLineH += lineHeight) + graphYHeight, 1);

            }
        }

        public void drawDiagnosticsGraph(List<float> values, float startX, float startY, float yScale)
        {
            for (int i = 1; i < values.Count; i++)
            {
                DrawWrappers.DrawLine(startX + i - 1, startY + (values[i - 1] * yScale), startX + i, startY + (values[i] * yScale), Color.Green, 0.5f, ZIndex.HUD, false);
            }
        }

        public void drawWeaponSwitchHUD()
        {
            if (level.mainPlayer.isZero && !level.mainPlayer.isDisguisedAxl) return;

            if (level.mainPlayer.isSelectingRA())
            {
                drawRideArmorIcons();
            }

            if (mainPlayer.isVile && mainPlayer.character != null && mainPlayer.character.rideArmor != null && mainPlayer.character.rideArmor == mainPlayer.character.vileStartRideArmor
                && mainPlayer.character.rideArmor.raNum == 2)
            {
                int x = 10, y = 155;
                int napalmNum = mainPlayer.loadout.vileLoadout.napalm;
                if (napalmNum < 0) napalmNum = 0;
                if (napalmNum > 2) napalmNum = 0;
                Global.sprites["hud_hawk_bombs"].drawToHUD(napalmNum, x, y, alpha: mainPlayer.vileNapalmWeapon.shootTime == 0 ? 1 : 0.5f);
                Helpers.drawTextStd(TCat.HUD, "x" + mainPlayer.character.rideArmor.hawkBombCount.ToString(), x + 10, y - 4, fontSize: 24, color: Color.White);
            }

            if (level.mainPlayer.character?.rideArmor != null || level.mainPlayer.character?.rideChaser != null)
            {
                return;
            }

            var iconW = 8;
            var iconH = 8;
            var width = 20;

            var startX = getWeaponSlotStartX(ref iconW, ref iconH, ref width);
            var startY = Global.screenH - 12;

            if (mainPlayer.character != null && mainPlayer.character.hasFgMoveEquipped() && mainPlayer.character.canAffordFgMove())
            {
                int x = 10, y = 159;
                Global.sprites["hud_weapon_icon"].drawToHUD(mainPlayer.character.hasHadoukenEquipped() ? 112 : 113, x, y);
                float cooldown = Helpers.progress(mainPlayer.fgMoveAmmo, 32f);
                drawWeaponSlotCooldown(x, y, cooldown);
            }

            if (mainPlayer.isAxl && mainPlayer.weapons[0].type > 0)
            {
                int x = 10, y = 156;
                int index = 0;
                if (mainPlayer.weapons[0].type == (int)AxlBulletWeaponType.MetteurCrash) index = 0;
                if (mainPlayer.weapons[0].type == (int)AxlBulletWeaponType.BeastKiller) index = 1;
                if (mainPlayer.weapons[0].type == (int)AxlBulletWeaponType.MachineBullets) index = 2;
                if (mainPlayer.weapons[0].type == (int)AxlBulletWeaponType.DoubleBullets) index = 3;
                if (mainPlayer.weapons[0].type == (int)AxlBulletWeaponType.RevolverBarrel) index = 4;
                if (mainPlayer.weapons[0].type == (int)AxlBulletWeaponType.AncientGun) index = 5;
                Global.sprites["hud_axl_ammo"].drawToHUD(index, x, y);
                int currentAmmo = MathF.Ceiling(mainPlayer.weapons[0].ammo);
                int totalAmmo = MathF.Ceiling(mainPlayer.axlBulletTypeAmmo[mainPlayer.weapons[0].type]);
                Helpers.drawTextStd(TCat.HUD, totalAmmo.ToString(), x + 10, y - 4, fontSize: 24, color: Color.White);
            }

            if (level.mainPlayer.isGridModeEnabled())
            {
                if (level.mainPlayer.gridModeHeld == true)
                {
                    var gridPoints = level.mainPlayer.gridModePoints();
                    for (var i = 0; i < level.mainPlayer.weapons.Count && i < 9; i++)
                    {
                        Point pos = gridPoints[i];
                        var weapon = level.mainPlayer.weapons[i];
                        var x = Global.halfScreenW + (pos.x * 20);
                        var y = Global.screenH - 30 + pos.y * 20;

                        drawWeaponSlot(weapon, x, y);
                    }
                }

                /*
                // Draw giga crush/hyper buster
                if (level.mainPlayer.weapons.Count == 10)
                {
                    int x = 10, y = 146;
                    Weapon weapon = level.mainPlayer.weapons[9];

                    drawWeaponSlot(weapon, x, y);

                    //Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, x, y);
                    //DrawWrappers.DrawRectWH(x - 8, y - 8, 16, 16 - MathF.Floor(16 * (weapon.ammo / weapon.maxAmmo)), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
                }
                */

                return;
            }

            for (var i = 0; i < level.mainPlayer.weapons.Count; i++)
            {
                var weapon = level.mainPlayer.weapons[i];
                var x = startX + (i * width);
                var y = startY;

                if (weapon is HyperBuster hb)
                {
                    bool canShootHyperBuster = hb.canShootIncludeCooldown(level.mainPlayer);
                    Color lineColor = canShootHyperBuster ? Color.White : Helpers.Gray;

                    float slotPosX = startX + (level.mainPlayer.hyperChargeSlot * width);
                    int yOff = -1;

                    // Stretch black
                    DrawWrappers.DrawRect(slotPosX, y - 9 + yOff, x, y - 12 + yOff, true, Color.Black, 1, ZIndex.HUD, false);

                    // Right
                    DrawWrappers.DrawRect(x - 1, y - 7, x + 2, y - 12 + yOff, true, Color.Black, 1, ZIndex.HUD, false);
                    DrawWrappers.DrawRect(x, y - 8, x + 1, y - 11 + yOff, true, lineColor, 1, ZIndex.HUD, false);

                    // Left
                    DrawWrappers.DrawRect(slotPosX - 1, y - 7, slotPosX + 2, y - 12 + yOff, true, Color.Black, 1, ZIndex.HUD, false);
                    DrawWrappers.DrawRect(slotPosX, y - 8, slotPosX + 1, y - 11 + yOff, true, lineColor, 1, ZIndex.HUD, false);

                    // Stretch white
                    DrawWrappers.DrawRect(slotPosX, y - 10 + yOff, x, y - 11 + yOff, true, lineColor, 1, ZIndex.HUD, false);

                    break;
                }
            }

            for (var i = 0; i < level.mainPlayer.weapons.Count; i++)
            {
                var weapon = level.mainPlayer.weapons[i];
                var x = startX + (i * width);
                var y = startY;

                drawWeaponSlot(weapon, x, y);
            }

            if (level.mainPlayer.isSelectingCommand())
            {
                drawMaverickCommandIcons();
            }
        }

        public void drawWeaponSlot(Weapon weapon, float x, float y)
        {
            if (weapon is MechMenuWeapon && level.mainPlayer.character?.vileStartRideArmor != null)
            {
                int index = 37 + level.mainPlayer.character.vileStartRideArmor.raNum;
                if (index == 42) index = 119;
                Global.sprites["hud_weapon_icon"].drawToHUD(index, x, y);
            }
            else if (weapon is MechMenuWeapon && level.mainPlayer.isSelectingRA())
            {
                return;
            }
            else if (weapon is not AbsorbWeapon)
            {
                Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, x, y);
            }

            if (weapon.ammo < weapon.maxAmmo && weapon is not UndisguiseWeapon && weapon is not MechMenuWeapon)
            {
                drawWeaponSlotAmmo(x, y, weapon.ammo / weapon.maxAmmo);
            }

            if (weapon is MechaniloidWeapon mew)
            {
                if (mew.mechaniloidType == MechaniloidType.Tank && level.mainPlayer.tankMechaniloidCount() > 0)
                {
                    drawWeaponText(x, y, level.mainPlayer.tankMechaniloidCount().ToString());
                }
                else if (mew.mechaniloidType == MechaniloidType.Hopper && level.mainPlayer.hopperMechaniloidCount() > 0)
                {
                    drawWeaponText(x, y, level.mainPlayer.hopperMechaniloidCount().ToString());
                }
                else if (mew.mechaniloidType == MechaniloidType.Bird && level.mainPlayer.birdMechaniloidCount() > 0)
                {
                    drawWeaponText(x, y, level.mainPlayer.birdMechaniloidCount().ToString());
                }
                else if (mew.mechaniloidType == MechaniloidType.Fish && level.mainPlayer.fishMechaniloidCount() > 0)
                {
                    drawWeaponText(x, y, level.mainPlayer.fishMechaniloidCount().ToString());
                }
            }

            if (weapon is MagnetMine && level.mainPlayer.magnetMines.Count > 0)
            {
                drawWeaponText(x, y, level.mainPlayer.magnetMines.Count.ToString());
            }

            if (weapon is RaySplasher && level.mainPlayer.turrets.Count > 0)
            {
                // drawWeaponText(x, y, level.mainPlayer.turrets.Count.ToString());
            }

            if (weapon is GLauncher && level.mainPlayer.axlLoadout.blastLauncherAlt == 1 && level.mainPlayer.grenades.Count > 0)
            {
                drawWeaponText(x, y, level.mainPlayer.grenades.Count.ToString());
            }

            if (weapon is DNACore dnaCore && level.mainPlayer.weapon == weapon && level.mainPlayer.input.isHeld(Control.Special1, level.mainPlayer))
            {
                drawTransformPreviewInfo(dnaCore, x, y);
            }

            if (weapon is HyperBuster && level.mainPlayer.weapons[level.mainPlayer.hyperChargeSlot].ammo == 0)
            {
                drawWeaponSlotAmmo(x, y, 0);
            }
            else if (weapon is HyperBuster hb)
            {
                drawWeaponSlotCooldown(x, y, hb.shootTime / hb.getRateOfFire(level.mainPlayer));
            }
            else if (weapon is NovaStrike ns)
            {
                drawWeaponSlotCooldown(x, y, ns.shootTime / ns.rateOfFire);
            }
            else if (weapon is SigmaMenuWeapon)
            {
                drawWeaponSlotCooldown(x, y, weapon.shootTime / 4);
            }
            
            if (Global.debug && Global.quickStart && weapon is AxlWeapon aw2 && weapon is not DNACore)
            {
                drawWeaponSlotCooldownBar(x, y, aw2.shootTime / aw2.rateOfFire);
                drawWeaponSlotCooldownBar(x, y, aw2.altShootTime / aw2.altFireCooldown, true);
            }

            MaverickWeapon mw = weapon as MaverickWeapon;
            if (mw != null)
            {
                float maxHealth = level.mainPlayer.getMaverickMaxHp();
                if (level.mainPlayer.isSummoner())
                {
                    float mHealth = mw.maverick?.health ?? mw.lastHealth;
                    float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
                    if (!mw.summonedOnce) mHealth = 0;
                    drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
                    drawWeaponSlotCooldown(x, y, mw.shootTime / MaverickWeapon.summonerCooldown);
                }
                else if (level.mainPlayer.isPuppeteer())
                {
                    float mHealth = mw.maverick?.health ?? mw.lastHealth;
                    float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
                    if (!mw.summonedOnce) mHealth = 0;
                    drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
                }
                else if (level.mainPlayer.isStriker())
                {
                    float mHealth = mw.maverick?.health ?? mw.lastHealth;
                    float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
                    if (level.mainPlayer.isStriker() && level.mainPlayer.mavericks.Count > 0 && mw.maverick == null)
                    {
                        mHealth = 0;
                    }

                    drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
                    drawWeaponSlotCooldown(x, y, mw.cooldown / MaverickWeapon.strikerCooldown);
                }
                else if (level.mainPlayer.isTagTeam())
                {
                    float mHealth = mw.maverick?.health ?? mw.lastHealth;
                    float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
                    if (!mw.summonedOnce) mHealth = 0;
                    drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
                    drawWeaponSlotCooldown(x, y, mw.cooldown / MaverickWeapon.tagTeamCooldown);
                }

                if (mw is ChillPenguinWeapon)
                {
                    for (int i = 0; i < mainPlayer.iceStatues.Count; i++)
                    {
                        Global.sprites["hud_ice_statue"].drawToHUD(0, x - 3 + (i * 6), y + 10);
                    }
                }

                if (mw is DrDopplerWeapon ddw && ddw.ballType == 1)
                {
                    Global.sprites["hud_doppler_weapon"].drawToHUD(ddw.ballType, x + 4, y + 4);
                }

                if (mw is WireSpongeWeapon && level.mainPlayer.seeds.Count > 0)
                {
                    drawWeaponText(x, y, level.mainPlayer.seeds.Count.ToString());
                }

                if (mw is BubbleCrabWeapon && mw.maverick is BubbleCrab bc && bc.crabs.Count > 0)
                {
                    drawWeaponText(x, y, bc.crabs.Count.ToString());
                }
            }

            if (level.mainPlayer.weapon == weapon && !level.mainPlayer.isSelectingCommand())
            {
                drawWeaponSlotSelected(x, y);
            }

            if (weapon is AxlWeapon && Options.main.axlLoadout.altFireArray[Weapon.wiToFi(weapon.index)] == 1)
            {
                Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "B");
            }

            if (weapon is SigmaMenuWeapon)
            {
                if ((level.mainPlayer.isPuppeteer() || level.mainPlayer.isSummoner()) && level.mainPlayer.currentMaverickCommand == MaverickAIBehavior.Follow)
                {
                    Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "F");
                }
                
                /*
                string commandModeSymbol = null;
                //if (level.mainPlayer.isSummoner()) commandModeSymbol = "SUM";
                if (level.mainPlayer.isPuppeteer()) commandModeSymbol = "PUP";
                if (level.mainPlayer.isStriker()) commandModeSymbol = "STK";
                if (level.mainPlayer.isTagTeam()) commandModeSymbol = "TAG";
                if (commandModeSymbol != null)
                {
                    Helpers.drawTextStd(commandModeSymbol, x - 7, y + 4, Alignment.Left, fontSize: 12);
                }
                */
            }

            if (mw != null)
            {
                if (mw.scrapHUDAnimTime > 0)
                {
                    float animProgress = mw.scrapHUDAnimTime / MaverickWeapon.scrapHUDMaxAnimTime;
                    float yOff = animProgress * 20;
                    float alpha = Helpers.clamp01(1 - animProgress);
                    Global.sprites["hud_scrap"].drawToHUD(0, x - 6, y - yOff - 10, alpha);
                    //DrawWrappers.DrawText("+1", x - 6, y - yOff - 10, Alignment.Center, )
                    Color color = new Color(0, 255, 0, (byte)(int)(alpha * 255));
                    Color outlineColor = new Color(0, 0, 0, (byte)(int)(alpha * 255));
                    Helpers.drawTextStd("+1", x - 4, y - yOff - 15, Alignment.Left, fontSize: 18, color: color, outlineColor: outlineColor);
                }
            }

            if (weapon is AbsorbWeapon aw)
            {
                var sprite = Global.sprites[aw.absorbedProj.sprite.name];

                float w = sprite.getCurrentFrame().rect.w();
                float h = sprite.getCurrentFrame().rect.h();

                float scaleX = Helpers.clampMax(10f / w, 1);
                float scaleY = Helpers.clampMax(10f / h, 1);
                
                Global.sprites["hud_weapon_icon"].draw(weapon.weaponSlotIndex, Global.level.camX + x, Global.level.camY + y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
                Global.sprites[aw.absorbedProj.sprite.name].draw(0, Global.level.camX + x, Global.level.camY + y, 1, 1, null, 1, scaleX, scaleY, ZIndex.HUD);
            }
        }

        private void drawWeaponText(float x, float y, string text)
        {
            Helpers.drawTextStd(TCat.Default, text, x + 1, y + 8, Alignment.Center, fontSize: 18);
        }

        private void drawWeaponSlotSelected(float x, float y)
        {
            DrawWrappers.DrawRectWH(x - 7, y - 7, 14, 14, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
        }

        private void drawWeaponSlotAmmo(float x, float y, float val)
        {
            DrawWrappers.DrawRectWH(x - 8, y - 8, 16, 16 - MathF.Floor(16 * val), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
        }

        public static void drawWeaponSlotCooldownBar(float x, float y, float val, bool isAlt = false)
        {
            if (val <= 0) return;
            val = Helpers.clamp01(val);

            float yPos = -8.5f;
            if (isAlt) yPos = 8.5f;
            DrawWrappers.DrawLine(x - 8, y + yPos, x + 8, y + yPos, Color.Black, 1, ZIndex.HUD, false);
            DrawWrappers.DrawLine(x - 8, y + yPos, x - 8 + (val * 16), y + yPos, Color.Yellow, 1, ZIndex.HUD, false);
        }

        public static void drawWeaponSlotCooldown(float x, float y, float val)
        {
            if (val <= 0) return;
            val = Helpers.clamp01(val);

            int sliceStep = 1;
            if (Options.main.particleQuality == 0) sliceStep = 4;
            if (Options.main.particleQuality == 1) sliceStep = 2;

            int gridLen = 16 / sliceStep;
            List<Point> points = new List<Point>(gridLen * 4);

            int startX = 0;
            int startY = -8;

            int xDir = -1;
            int yDir = 0;

            for (int i = 0; i < gridLen * 4; i++)
            {
                points.Add(new Point(x + startX, y + startY));
                startX += sliceStep * xDir;
                startY += sliceStep * yDir;

                if (xDir == -1 && startX == -8)
                {
                    xDir = 0;
                    yDir = 1;
                }
                if (yDir == 1 && startY == 8)
                {
                    yDir = 0;
                    xDir = 1;
                }
                if (xDir == 1 && startX == 8)
                {
                    xDir = 0;
                    yDir = -1;
                }
                if (yDir == -1 && startY == -8)
                {
                    xDir = -1;
                    yDir = 0;
                }
            }

            var slices = new List<List<Point>>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                Point nextPoint = i + 1 >= points.Count ? points[0] : points[i + 1];
                slices.Add(new List<Point>() { new Point(x, y), points[i], nextPoint });
            }

            for (int i = 0; i < (int)(val * slices.Count); i++)
            {
                DrawWrappers.DrawPolygon(slices[i], new Color(0, 0, 0, 164), true, ZIndex.HUD, false);
            }
        }

        public void drawTransformPreviewInfo(DNACore dnaCore, float x, float y)
        {
            float sx = x - 50;
            float sy = y - 100;

            float leftX = sx + 15;

            DrawWrappers.DrawRect(sx, sy, x + 50, y - 18, true, new Color(0, 0, 0, 224), 1, ZIndex.HUD, false);
            Global.sprites["cursorchar"].drawToHUD(0, x, y - 13);
            int sigmaForm = dnaCore.loadout?.sigmaLoadout?.sigmaForm ?? 0;

            sy += 5;
            Helpers.drawTextStd(dnaCore.name, x, sy, fontSize: 24, alignment: Alignment.Center);
            sy += 30;
            if (dnaCore.charNum == 0)
            {
                if (dnaCore.ultimateArmor)
                {
                    Global.sprites["menu_megaman"].drawToHUD(5, x, sy + 4);
                }
                else if (dnaCore.armorFlag == ushort.MaxValue)
                {
                    Global.sprites["menu_megaman"].drawToHUD(4, x, sy + 4);
                }
                else
                {
                    Global.sprites["menu_megaman_armors"].drawToHUD(0, x, sy + 4);
                    int boots = Player.getArmorNum(dnaCore.armorFlag, 0, true);
                    int body = Player.getArmorNum(dnaCore.armorFlag, 1, true);
                    int helmet = Player.getArmorNum(dnaCore.armorFlag, 2, true);
                    int arm = Player.getArmorNum(dnaCore.armorFlag, 3, true);

                    if (helmet == 1) Global.sprites["menu_megaman_armors"].drawToHUD(1, x, sy + 4);
                    if (helmet == 2) Global.sprites["menu_megaman_armors"].drawToHUD(2, x, sy + 4);
                    if (helmet >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(3, x, sy + 4);

                    if (body == 1) Global.sprites["menu_megaman_armors"].drawToHUD(4, x, sy + 4);
                    if (body == 2) Global.sprites["menu_megaman_armors"].drawToHUD(5, x, sy + 4);
                    if (body >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(6, x, sy + 4);

                    if (arm == 1) Global.sprites["menu_megaman_armors"].drawToHUD(7, x, sy + 4);
                    if (arm == 2) Global.sprites["menu_megaman_armors"].drawToHUD(8, x, sy + 4);
                    if (arm >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(9, x, sy + 4);

                    if (boots == 1) Global.sprites["menu_megaman_armors"].drawToHUD(10, x, sy + 4);
                    if (boots == 2) Global.sprites["menu_megaman_armors"].drawToHUD(11, x, sy + 4);
                    if (boots >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(12, x, sy + 4);

                    if (helmet == 15) Global.sprites["menu_chip"].drawToHUD(0, x, sy - 16 + 4);
                    if (body == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 2, sy - 5 + 4);
                    if (arm == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 9, sy - 2 + 4);
                    if (boots == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 12, sy + 10);
                }
            }
            else if (dnaCore.charNum == 1)
            {
                int index = 0;
                if (dnaCore.hyperMode == DNACoreHyperMode.BlackZero) index = 1;
                if (dnaCore.hyperMode == DNACoreHyperMode.AwakenedZero) index = 2;
                if (dnaCore.hyperMode == DNACoreHyperMode.NightmareZero) index = 3;
                Global.sprites["menu_zero"].drawToHUD(index, x, sy + 1);
            }
            else if (dnaCore.charNum == 2)
            {
                int index = 0;
                if (dnaCore.hyperMode == DNACoreHyperMode.VileMK2) index = 1;
                if (dnaCore.hyperMode == DNACoreHyperMode.VileMK5) index = 2;
                Global.sprites["menu_vile"].drawToHUD(index, x, sy + 2);
                if (dnaCore.frozenCastle)
                {
                    Helpers.drawTextStd("F", x - 25, sy, fontSize: 24, color: new Color(23, 232, 255));
                }
                if (dnaCore.speedDevil)
                {
                    Helpers.drawTextStd("S", x + 20, sy, fontSize: 24, color: new Color(213, 154, 245));
                }
            }
            else if (dnaCore.charNum == 3)
            {
                Global.sprites["menu_axl"].drawToHUD(dnaCore.hyperMode == DNACoreHyperMode.WhiteAxl ? 1 : 0, x, sy + 4);
            }
            else if (dnaCore.charNum == 4)
            {
                Global.sprites["menu_sigma"].drawToHUD(sigmaForm, x, sy + 10);
            }

            sy += 35;

            var weapons = new List<Weapon>();
            for (int i = 0; i < dnaCore.weapons.Count && i < 6; i++)
            {
                weapons.Add(dnaCore.weapons[i]);
            }
            if (dnaCore.charNum == 1 && dnaCore.loadout.zeroLoadout.melee != 2)
            {
                if (dnaCore.hyperMode == DNACoreHyperMode.NightmareZero)
                {
                    weapons.Add(new DarkHoldWeapon() { ammo = dnaCore.rakuhouhaAmmo });
                }
                else
                {
                    weapons.Add(RakuhouhaWeapon.getWeaponFromIndex(null, dnaCore.loadout.zeroLoadout.gigaAttack));
                }
            }
            if (dnaCore.charNum == 4)
            {
                if (sigmaForm == 0) weapons.Add(new Weapon()
                {
                    weaponSlotIndex = 111,
                    ammo = dnaCore.rakuhouhaAmmo,
                    maxAmmo = 32,
                });
                if (sigmaForm == 1) weapons.Add(new Weapon()
                {
                    weaponSlotIndex = 110,
                    ammo = dnaCore.rakuhouhaAmmo,
                    maxAmmo = 32,
                });
            }
            int counter = 0;
            float wx = 1 + x - ((weapons.Count - 1) * 8);
            foreach (var weapon in weapons)
            {
                float slotX = wx + (counter * 15);
                float slotY = sy;
                Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, slotX, slotY);
                float ammo = weapon.ammo;
                if (weapon is RakuhouhaWeapon || weapon is RekkohaWeapon || weapon is CFlasher || weapon is DarkHoldWeapon) ammo = dnaCore.rakuhouhaAmmo;
                if (weapon is not MechMenuWeapon)
                {
                    DrawWrappers.DrawRectWH(slotX - 8, slotY - 8, 16, 16 - MathF.Floor(16 * (ammo / weapon.maxAmmo)), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
                }
                counter++;
            }
        }

        public float getWeaponSlotStartX(ref int iconW, ref int iconH, ref int width)
        {
            int weaponsOver3 = level.mainPlayer.weapons.Count - 3;
            int overboard = 9;
            if (weaponsOver3 > overboard)
            {
                width = weaponsOver3 > overboard ? 10 : 20;
                iconW = 7;
                iconH = 7;
            }

            var startX = MathF.Round(Global.screenW * 0.43f) - (weaponsOver3 * width * 0.5f);

            return startX;
        }

        public void drawMaverickCommandIcons()
        {
            int mwIndex = level.mainPlayer.weapons.IndexOf(level.mainPlayer.weapon);
            float height = 15;
            int width = 20;
            var iconW = 8;
            var iconH = 8;

            float startX = getWeaponSlotStartX(ref iconW, ref iconH, ref width) + (mwIndex * 20);
            float startY = Global.screenH - 12;

            for (int i = 0; i < MaverickWeapon.maxCommandIndex; i++)
            {
                float x = startX;
                float y = startY - ((i + 1) * height);
                int index = i;
                Global.sprites["hud_maverick_command"].drawToHUD(index, x, y);
                /*
                if (i == 1)
                {
                    Global.sprites["hud_maverick_command"].drawToHUD(3, x - height, y);
                    Global.sprites["hud_maverick_command"].drawToHUD(4, x + height, y);
                }
                */
            }

            for (int i = 0; i < MaverickWeapon.maxCommandIndex + 1; i++)
            {
                float x = startX;
                float y = startY - (i * height);
                if (level.mainPlayer.maverickWeapon.selCommandIndex == i && level.mainPlayer.maverickWeapon.selCommandIndexX == 1)
                {
                    DrawWrappers.DrawRectWH(x - iconW, y - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
                }
            }

            /*
            if (level.mainPlayer.maverickWeapon.selCommandIndexX == 0)
            {
                DrawWrappers.DrawRectWH(startX - height - iconW, startY - (height * 2) - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
            }
            if (level.mainPlayer.maverickWeapon.selCommandIndexX == 2)
            {
                DrawWrappers.DrawRectWH(startX + height - iconW, startY - (height * 2) - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
            }
            */
        }

        public void drawRideArmorIcons()
        {
            int raIndex = mainPlayer.weapons.FindIndex(w => w is MechMenuWeapon);

            float startX = 168;
            if (raIndex == 0) startX = 148;
            if (raIndex == 1) startX = 158;

            float startY = Global.screenH - 12;
            float height = 15;
            bool isMK2 = level.mainPlayer?.character?.isVileMK2 == true;
            bool isMK5 = level.mainPlayer?.character?.isVileMK5 == true;
            bool isMK2Or5 = isMK2 || isMK5;
            int maxIndex = isMK2Or5 ? 5 : 4;

            for (int i = 0; i < maxIndex; i++)
            {
                float x = startX;
                float y = startY - (i * height);
                int iconIndex = 37 + i;
                if (i == 4 && isMK5) iconIndex = 119;
                Global.sprites["hud_weapon_icon"].drawToHUD(iconIndex, x, y);
            }

            for (int i = 0; i < maxIndex; i++)
            {
                float x = startX;
                float y = startY - (i * height);
                if (i == 4 && (!isMK2Or5 || level.mainPlayer.scrap < 10))
                {
                    DrawWrappers.DrawRectWH(x - 8, y - 8, 16, 16, true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
                }
            }

            for (int i = 0; i < maxIndex; i++)
            {
                float x = startX;
                float y = startY - (i * height);
                if (level.mainPlayer.selectedRAIndex == i)
                {
                    DrawWrappers.DrawRectWH(x - 7, y - 7, 14, 14, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
                }
            }
        }

        public Color getPingColor(Player player)
        {
            Color pingColor = Helpers.getPingColor(player.getPingOrStartPing(), level.server.netcodeModel == NetcodeModel.FavorAttacker ? level.server.netcodeModelPing : Global.defaultThresholdPing);
            if (pingColor == Color.Green) pingColor = Color.White;
            return pingColor;
        }

        public void drawNetcodeData()
        {
            int top2 = -3;
            Helpers.drawTextStd(TCat.HUD, Global.level.server.region.name, Global.screenW - 12, top2 + 12, Alignment.Right, fontSize: 24, style: Text.Styles.Italic);

            string netcodePingStr = "";
            int iconXPos = 280;
            if (level.server.netcodeModel == NetcodeModel.FavorAttacker)
            {
                netcodePingStr = "<" + level.server.netcodeModelPing.ToString();
                if (level.server.netcodeModelPing < 100) iconXPos = 260;
                else iconXPos = 253;
            }
            Helpers.drawTextStd(TCat.HUD, netcodePingStr, Global.screenW - 12, top2 + 22, Alignment.Right, fontSize: 24, style: Text.Styles.Italic);
            Global.sprites["hud_netcode"].drawToHUD((int)level.server.netcodeModel, iconXPos, top2 + 26);
            if (Global.level.server.isLAN)
            {
                Helpers.drawTextStd(TCat.HUD, "IP: " + Global.level.server.ip, Global.screenW - 12, top2 + 32, Alignment.Right, fontSize: 24, style: Text.Styles.Italic);
            }
        }

        public virtual void drawScoreboard()
        {
            var padding = 5;
            var top = padding + 2;
            var fontSize = 32;
            var col1x = padding + 10;
            var col2x = Global.screenW * 0.33f;
            var col3x = Global.screenW * 0.475f;
            var col4x = Global.screenW * 0.65f;
            var col5x = Global.screenW * 0.85f;
            var lineY = padding + 35;
            var labelY = lineY - 1;
            var labelTextY = labelY + 2;
            var line2Y = labelY + 12;
            var topPlayerY = line2Y + 2;
            DrawWrappers.DrawRect(padding, padding, Global.screenW - padding, Global.screenH - padding, true, Helpers.MenuBgColor, 0, ZIndex.HUD, false);

            if (this is FFADeathMatch)
            {
                Helpers.drawTextStd(TCat.HUD, string.Format("Mode: Deathmatch(to {0})", playingTo.ToString()), padding + 10, top, Alignment.Left, fontSize: (uint)fontSize);
            }
            else if (this is Elimination)
            {
                Helpers.drawTextStd(TCat.HUD, "Mode: Elimination", padding + 10, top, Alignment.Left, fontSize: (uint)fontSize);
            }
            else if (this is Race)
            {
                Helpers.drawTextStd(TCat.HUD, "Mode: Race", padding + 10, top, Alignment.Left, fontSize: (uint)fontSize);
            }

            drawMapName(padding + 10, top + 10, (uint)fontSize);

            if (Global.serverClient != null)
            {
                Helpers.drawTextStd(TCat.HUD, "Match: " + Global.level.server.name, padding + 10, top + 20, Alignment.Left, fontSize: (uint)fontSize);
                drawNetcodeData();
            }

            DrawWrappers.DrawLine(padding + 10, lineY, Global.screenW - padding - 10, lineY, Color.White, 1, ZIndex.HUD, false);
            Helpers.drawTextStd(TCat.HUD, "Player", col1x, labelTextY, Alignment.Left, fontSize: (uint)fontSize);
            Helpers.drawTextStd(TCat.HUD, "Char", col2x, labelTextY, Alignment.Left, fontSize: (uint)fontSize);
            Helpers.drawTextStd(TCat.HUD, "Kills", col3x, labelTextY, Alignment.Left, fontSize: (uint)fontSize);
            Helpers.drawTextStd(TCat.HUD, this is Elimination ? "Lives" : "Deaths", col4x, labelTextY, Alignment.Left, fontSize: (uint)fontSize);

            if (Global.serverClient != null) Helpers.drawTextStd(TCat.HUD, "Ping", col5x, labelTextY, Alignment.Left, fontSize: (uint)fontSize);

            DrawWrappers.DrawLine(padding + 10, line2Y, Global.screenW - padding - 10, line2Y, Color.White, 1, ZIndex.HUD, false);
            var rowH = 10;
            var players = level.players.Where(p => !p.isSpectator).ToList();
            if (this is Race race)
            {
                players = race.getSortedPlayers();
            }

            for (var i = 0; i < 12; i++)
            {
                if (i < players.Count)
                {
                    var player = players[i];
                    var color = getCharColor(player);
                    var alpha = getCharAlpha(player);

                    if (Global.serverClient != null && player.serverPlayer.isHost)
                    {
                        Helpers.drawTextStd(TCat.HUD, "H", col1x - 8, 3 + topPlayerY + i * rowH, Alignment.Left, style: Text.Styles.Italic, fontSize: 20, color: Color.Yellow);
                    }
                    else if (Global.serverClient != null && player.serverPlayer.isBot)
                    {
                        Helpers.drawTextStd(TCat.HUD, "B", col1x - 8, 3 + topPlayerY + i * rowH, Alignment.Left, style: Text.Styles.Italic, fontSize: 20, color: Helpers.Gray);
                    }

                    Helpers.drawTextStd(TCat.HUD, player.name, col1x, topPlayerY + (i) * rowH, Alignment.Left, fontSize: (uint)fontSize, color: color, alpha: alpha);
                    Helpers.drawTextStd(TCat.HUD, player.kills.ToString(), col3x, topPlayerY + (i) * rowH, Alignment.Left, fontSize: (uint)fontSize, color: color, alpha: alpha);
                    Helpers.drawTextStd(TCat.HUD, player.getDeathScore(), col4x, topPlayerY + (i) * rowH, Alignment.Left, fontSize: (uint)fontSize, color: color, alpha: alpha);

                    if (Global.serverClient != null)
                    {
                        Helpers.drawTextStd(TCat.HUD, player.getDisplayPing(), col5x, topPlayerY + (i) * rowH, Alignment.Left, fontSize: (uint)fontSize, color: getPingColor(player), alpha: alpha);
                    }

                    Global.sprites[getCharIcon(player)].drawToHUD(player.realCharNum, col2x + 4, topPlayerY + i * rowH, alpha: alpha);

                    if (player.eliminated())
                    {
                        //int elimLineY = 5 + topPlayerY + i * rowH;
                        //DrawWrappers.DrawLine(col1x, elimLineY, Global.screenW - padding - 10, elimLineY, Color.Red, 1, ZIndex.HUD, false);
                    }
                }
            }

            drawSpectators();
        }

        public void drawTeamScoreboard()
        {
            var padding = 5;
            var top = padding + 2;
            var hPadding = padding + 5;
            uint fontSize = 32u;
            var col1x = padding + 5;
            var playerNameX = padding + 15;
            var col2x = col1x - 11;
            var col3x = Global.screenW * 0.28f;
            var col4x = Global.screenW * 0.35f;
            var col5x = Global.screenW * 0.4225f;
            var teamLabelY = padding + 35;
            var lineY = teamLabelY + 10;
            var labelY = lineY + 5;
            var line2Y = labelY + 10;
            var topPlayerY = line2Y + 5;
            var halfwayX = Global.halfScreenW - 2;

            DrawWrappers.DrawRect(padding, padding, Global.screenW - padding, Global.screenH - padding, true, Helpers.MenuBgColor, 0, ZIndex.HUD, false);

            if (this is CTF)
            {
                Helpers.drawTextStd(TCat.HUD, string.Format("Mode: CTF(to {0})", playingTo.ToString()), padding + 5, top, Alignment.Left, fontSize: fontSize);
            }
            else if (this is TeamDeathMatch)
            {
                Helpers.drawTextStd(TCat.HUD, string.Format("Mode: Team Deathmatch(to {0})", playingTo.ToString()), padding + 5, top, Alignment.Left, fontSize: fontSize);
            }
            else if (this is TeamElimination)
            {
                Helpers.drawTextStd(TCat.HUD, string.Format("Mode: Team Elimination", playingTo.ToString()), padding + 5, top, Alignment.Left, fontSize: fontSize);
            }
            else
            {
                Helpers.drawTextStd(TCat.HUD, string.Format("Mode: {0}", Global.level.server.gameMode), padding + 5, top, Alignment.Left, fontSize: fontSize);
            }

            drawMapName(padding + 5, top + 10, fontSize);

            if (Global.serverClient != null)
            {
                Helpers.drawTextStd(TCat.HUD, "Match: " + Global.level.server.name, padding + 5, top + 20, Alignment.Left, fontSize: fontSize);
                drawNetcodeData();
            }

            int redPlayersStillAlive = 0;
            int bluePlayersStillAlive = 0;
            if (this is TeamElimination)
            {
                redPlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == redAlliance).Count();
                bluePlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == blueAlliance).Count();
            }

            bool isTE = this is TeamElimination;

            //Blue
            string blueText = "Blue: " + bluePoints.ToString();
            if (this is ControlPoints) blueText = "Blue: Attack";
            if (this is KingOfTheHill) blueText = "Blue";
            if (isTE) blueText = "Alive: " + bluePlayersStillAlive.ToString();
            Helpers.drawTextStd(TCat.HUDColored, blueText, col1x, teamLabelY, fontSize: 40u, outlineColor: Helpers.DarkBlue, style: Text.Styles.Bold);
            Helpers.drawTextStd(TCat.HUDColored, "Player", col1x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkBlue);
            Helpers.drawTextStd(TCat.HUDColored, "K", col3x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkBlue);
            Helpers.drawTextStd(TCat.HUDColored, isTE ? "L" : "D", col4x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkBlue);
            if (Global.serverClient != null) Helpers.drawTextStd(TCat.HUDColored, "P", col5x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkBlue);

            //Red
            string redText = "Red: " + redPoints.ToString();
            if (this is ControlPoints) redText = "Red: Defend";
            if (this is KingOfTheHill) redText = "Red";
            if (this is TeamElimination) redText = "Alive: " + redPlayersStillAlive.ToString();
            Helpers.drawTextStd(TCat.HUDColored, redText, halfwayX + col1x, teamLabelY, fontSize: 40u, outlineColor: Helpers.DarkRed, style: Text.Styles.Bold);
            Helpers.drawTextStd(TCat.HUDColored, "Player", halfwayX + col1x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkRed);
            Helpers.drawTextStd(TCat.HUDColored, "K", halfwayX + col3x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkRed);
            Helpers.drawTextStd(TCat.HUDColored, isTE ? "L" : "D", halfwayX + col4x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkRed);
            if (Global.serverClient != null) Helpers.drawTextStd(TCat.HUDColored, "P", halfwayX + col5x, labelY, fontSize: fontSize, outlineColor: Helpers.DarkRed);

            DrawWrappers.DrawLine(hPadding, line2Y, Global.screenW - hPadding, line2Y, Color.White, 1, ZIndex.HUD, false);

            var redPlayers = level.players.Where(p => p.alliance == redAlliance && !p.isSpectator).ToList();
            var bluePlayers = level.players.Where(p => p.alliance == blueAlliance && !p.isSpectator).ToList();

            //this.blueScoreText.text = "Blue: " + String(blueKills);
            //this.redScoreText.text = "Red: " + String(redKills);
            for (var i = 0; i < 12; i++)
            {
                //Blue
                if (i < bluePlayers.Count)
                {
                    var player = bluePlayers[i];
                    Color color = getCharColor(player);
                    float alpha = getCharAlpha(player);

                    if (Global.serverClient != null && player.serverPlayer.isHost)
                    {
                        Helpers.drawTextStd(TCat.HUDColored, "H", playerNameX - 19, 1 + topPlayerY + (i) * 10, Alignment.Left, style: Text.Styles.Italic, fontSize: 20, color: Color.Yellow);
                    }
                    else if (Global.serverClient != null && player.serverPlayer.isBot)
                    {
                        Helpers.drawTextStd(TCat.HUDColored, "B", playerNameX - 19, 1 + topPlayerY + (i) * 10, Alignment.Left, style: Text.Styles.Italic, fontSize: 20, color: Helpers.Gray);
                    }

                    Helpers.drawTextStd(TCat.HUDColored, player.name, playerNameX, topPlayerY + (i) * 10, fontSize: 24, color: color, outlineColor: Helpers.DarkBlue, alpha: alpha);
                    Helpers.drawTextStd(TCat.HUDColored, player.kills.ToString(), col3x, topPlayerY + (i) * 10, fontSize: 24, color: color, outlineColor: Helpers.DarkBlue, alpha: alpha);
                    Helpers.drawTextStd(TCat.HUDColored, player.getDeathScore(), col4x, topPlayerY + (i) * 10, fontSize: 24, color: color, outlineColor: Helpers.DarkBlue, alpha: alpha);
                    if (Global.serverClient != null) Helpers.drawTextStd(TCat.HUDColored, player.getDisplayPing(), col5x, topPlayerY + (i) * 10, fontSize: 24, color: getPingColor(player), outlineColor: Helpers.DarkBlue, alpha: alpha);
                    Global.sprites[getCharIcon(player)].drawToHUD(player.realCharNum, playerNameX - 8, -1 + topPlayerY + (i) * 10, alpha: alpha);
                }

                //Red
                if (i < redPlayers.Count)
                {
                    var player = redPlayers[i];
                    var color = getCharColor(player);
                    float alpha = getCharAlpha(player);

                    if (Global.serverClient != null && player.serverPlayer.isHost)
                    {
                        Helpers.drawTextStd(TCat.HUDColored, "H", halfwayX + playerNameX - 19, 1 + topPlayerY + (i) * 10, Alignment.Left, style: Text.Styles.Italic, fontSize: 20, color: Color.Yellow);
                    }
                    else if (Global.serverClient != null && player.serverPlayer.isBot)
                    {
                        Helpers.drawTextStd(TCat.HUDColored, "B", halfwayX + playerNameX - 19, 1 + topPlayerY + (i) * 10, Alignment.Left, style: Text.Styles.Italic, fontSize: 20, color: Helpers.Gray);
                    }

                    Helpers.drawTextStd(TCat.HUDColored, player.name, halfwayX + playerNameX, topPlayerY + (i) * 10, fontSize: 24, color: color, outlineColor: Helpers.DarkRed, alpha: alpha);
                    Helpers.drawTextStd(TCat.HUDColored, player.kills.ToString(), halfwayX + col3x, topPlayerY + (i) * 10, fontSize: 24, color: color, outlineColor: Helpers.DarkRed, alpha: alpha);
                    Helpers.drawTextStd(TCat.HUDColored, player.getDeathScore(), halfwayX + col4x, topPlayerY + (i) * 10, fontSize: 24, color: color, outlineColor: Helpers.DarkRed, alpha: alpha);
                    if (Global.serverClient != null) Helpers.drawTextStd(TCat.HUDColored, player.getDisplayPing(), halfwayX + col5x, topPlayerY + (i) * 10, fontSize: 24, color: getPingColor(player), outlineColor: Helpers.DarkRed, alpha: alpha);
                    Global.sprites[getCharIcon(player)].drawToHUD(player.realCharNum, halfwayX + playerNameX - 8, -1 + topPlayerY + (i) * 10, alpha: alpha);
                }
            }

            drawSpectators();
        }

        private void drawMapName(int x, int y, uint fontSize)
        {
            string displayName = "Map: " + level.levelData.displayName.Replace("_mirrored", "");
            Helpers.drawTextStd(TCat.HUD, displayName, x, y, Alignment.Left, fontSize: fontSize);
            if (level.levelData.isMirrored)
            {
                var size = Helpers.measureTextStd(TCat.HUD, displayName, fontSize: fontSize);
                Global.sprites["hud_mirror_icon"].drawToHUD(0, x + size.x + 9, y + 5);
            }
        }

        public Color getCharColor(Player player)
        {
            if (player == level.mainPlayer) return Color.Green;
            return Color.White;
        }

        public float getCharAlpha(Player player)
        {
            if (player.isDead && !isOver)
            {
                return 0.5f;
            }
            else if (player.eliminated())
            {
                return 0.5f;
            }
            return 1;
        }

        public string getCharIcon(Player player)
        {
            return "char_icon";
            //if (isOver) return "char_icon";
            //return player.isDead ? "char_icon_dead" : "char_icon";
        }

        public static string getTeamName(int alliance)
        {
            if (alliance == redAlliance) return "red";
            else return "blue";
        }

        public Color getTimeColor()
        {
            if (remainingTime <= 10)
            {
                return Color.Red;
            }
            return Color.White;
        }

        float goTime;
        public void drawTimeIfSet(int yPos)
        {
            if (setupTime > 0)
            {
                var timespan = new TimeSpan(0, 0, MathF.Ceiling(setupTime.Value));
                string timeStr = timespan.ToString(@"m\:ss");
                Helpers.drawTextStd(TCat.HUD, timeStr, 5, yPos, Alignment.Left, fontSize: (uint)32, color: getTimeColor());
            }
            else if (setupTime == 0 && goTime < 1)
            {
                goTime += Global.spf;
                var timespan = new TimeSpan(0, 0, MathF.Ceiling(setupTime.Value));
                Helpers.drawTextStd(TCat.HUD, "GO!", 5, yPos, Alignment.Left, fontSize: (uint)32, color: getTimeColor());
            }
            else if (remainingTime != null)
            {
                var timespan = new TimeSpan(0, 0, MathF.Ceiling(remainingTime.Value));
                string timeStr = timespan.ToString(@"m\:ss");
                if (!level.isNon1v1Elimination() || virusStarted >= 2) timeStr += " Left";
                if (isOvertime()) timeStr = "Overtime!";
                Helpers.drawTextStd(TCat.HUD, timeStr, 5, yPos, Alignment.Left, fontSize: (uint)32, color: getTimeColor());
            }
        }

        public bool isOvertime()
        {
            return (this is ControlPoints || this is KingOfTheHill || this is CTF) && remainingTime != null && remainingTime.Value == 0 && !isOver;
        }

        public void drawDpsIfSet(int yPos)
        {
            if (!string.IsNullOrEmpty(dpsString))
            {
                Helpers.drawTextStd(TCat.HUD, dpsString, 5, yPos, Alignment.Left, fontSize: (uint)32, color: getTimeColor());
            }
        }

        public void drawVirusTime(int yPos)
        {
            var timespan = new TimeSpan(0, 0, MathF.Ceiling(remainingTime.Value));
            string timeStr = "Sigma Virus: " + timespan.ToString(@"m\:ss");
            Helpers.drawTextStd(TCat.HUD, timeStr, 5, yPos, Alignment.Left, fontSize: (uint)32, color: getTimeColor());
        }

        public void drawWinScreen()
        {
            string text = "";
            string subtitle = "";

            if (playerWon(level.mainPlayer))
            {
                text = matchOverResponse.winMessage;
                subtitle = matchOverResponse.winMessage2;
            }
            else
            {
                text = matchOverResponse.loseMessage;
                subtitle = matchOverResponse.loseMessage2;
            }

            float titleY = Global.halfScreenH;
            float subtitleY = titleY + 20;

            // Title
            var titleMeasurement = Helpers.measureTextStd(TCat.HUD, " " + text.ToUpperInvariant(), true, 64);
            float hw = (titleMeasurement.x / 2) + 5;
            float hh = (titleMeasurement.y / 2) + 5;
            float topOffY = 0;
             
            // Subtitle
            var subtitleMeasurement = Helpers.measureTextStd(TCat.HUD, (subtitle ?? "").ToUpperInvariant(), true, 32);
            float hw2 = (subtitleMeasurement.x / 2) + 5;
            float hh2 = (subtitleMeasurement.y / 2) + 5;
            if (string.IsNullOrEmpty(subtitle))
            {
                hh2 = hh;
                subtitleY = titleY;
                topOffY = 2;
            }

            // Box
            if (Options.main.fontType == 0)
            {
                //DrawWrappers.DrawRect(Global.halfScreenW - Math.Max(hw, hw2), titleY - hh + topOffY, Global.halfScreenW + Math.Max(hw, hw2), subtitleY + hh2, true, new Color(0, 0, 0, 192), 0f, ZIndex.HUD, isWorldPos: false, outlineColor: Color.White);
                DrawWrappers.DrawRect(Global.halfScreenW - 149, titleY - hh + topOffY, Global.halfScreenW + 149, subtitleY + hh2, true, new Color(0, 0, 0, 192), 0f, ZIndex.HUD, isWorldPos: false, outlineColor: Color.White);
            }

            // Title
            Helpers.drawTextStd(TCat.HUD, text.ToUpperInvariant(), Global.halfScreenW, titleY, Alignment.Center, fontSize: 64u, outlineThickness: 6, outlineColor: Helpers.getAllianceColor(), vAlignment: VAlignment.Center);

            // Subtitle
            Helpers.drawTextStd(TCat.HUD, subtitle, Global.halfScreenW, subtitleY, Alignment.Center, fontSize: 32, outlineThickness: 5, outlineColor: Helpers.getAllianceColor(), vAlignment: VAlignment.Center);

            if (overTime >= secondsBeforeLeave)
            {
                if (Global.serverClient == null)
                {
                    Helpers.drawTextStd(TCat.HUD, Helpers.controlText("Press [ESC] to return to menu"), Global.halfScreenW, Global.halfScreenH + 50, Alignment.Center, fontSize: 28);
                }
            }
        }

        public virtual void drawTopHUD()
        {

        }

        public void drawRespawnHUD()
        {
            if (level.mainPlayer.character != null && level.mainPlayer.readyTextOver && level.mainPlayer.canReviveX())
            {
                Helpers.drawTextStd(TCat.HUD, Helpers.controlText("[D]: Activate Unlimited Potential"), Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center, fontSize: 21);
            }

            if (level.mainPlayer.randomTip == null) return;
            if (level.mainPlayer.isSpectator) return;

            if (level.mainPlayer.character == null && level.mainPlayer.readyTextOver)
            {
                string respawnStr = (level.mainPlayer.respawnTime > 0) ? "Respawn in " + Math.Round(level.mainPlayer.respawnTime).ToString() :
                    Helpers.controlText("Press [X] to respawn");

                if (level.mainPlayer.eliminated())
                {
                    Helpers.drawTextStd(TCat.HUD, "You were eliminated!", Global.screenW / 2, -15 + Global.screenH / 2, Alignment.Center);
                    Helpers.drawTextStd(TCat.HUD, "Spectating in " + Math.Round(level.mainPlayer.respawnTime).ToString(), Global.screenW / 2, Global.screenH / 2, Alignment.Center);
                }
                else if (level.mainPlayer.canReviveVile())
                {
                    if (level.mainPlayer.lastDeathWasVileMK2)
                    {
                        Helpers.drawTextStd(TCat.HUD, respawnStr, Global.screenW / 2, -10 + Global.screenH / 2, Alignment.Center);
                        string reviveText = Helpers.controlText("[D]: Revive as Vile V (5 scrap)");
                        Helpers.drawTextStd(TCat.HUD, reviveText, Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center, fontSize: 24);
                    }
                    else
                    {
                        Helpers.drawTextStd(TCat.HUD, respawnStr, Global.screenW / 2, -10 + Global.screenH / 2, Alignment.Center);
                        string reviveText = Helpers.controlText("[D]: Revive as MK-II (5 scrap)");
                        Helpers.drawTextStd(TCat.HUD, reviveText, Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center, fontSize: 24);
                        string reviveText2 = Helpers.controlText("[C]: Revive as MK-V (5 scrap)");
                        Helpers.drawTextStd(TCat.HUD, reviveText2, Global.screenW / 2, 22 + Global.screenH / 2, Alignment.Center, fontSize: 24);
                    }
                }
                else if (level.mainPlayer.canReviveSigma(out _))
                {
                    Helpers.drawTextStd(TCat.HUD, respawnStr, Global.screenW / 2, -10 + Global.screenH / 2, Alignment.Center);
                    string hyperType = "Wolf";
                    if (level.mainPlayer.isSigma2()) hyperType = "Viral";
                    if (level.mainPlayer.isSigma3()) hyperType = "Kaiser";
                    string reviveText = Helpers.controlText($"[D]: Revive as {hyperType} Sigma (" + Player.reviveSigmaScrapCost.ToString() + " scrap)");
                    Helpers.drawTextStd(TCat.HUD, reviveText, Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center, fontSize: 24);
                }
                else
                {
                    Helpers.drawTextStd(TCat.HUD, respawnStr, Global.screenW / 2, Global.screenH / 2, Alignment.Center);
                }

                if (!Menu.inMenu)
                {
                    DrawWrappers.DrawRect(0, Global.halfScreenH + 40, Global.screenW, Global.halfScreenH + 40 + (14 * level.mainPlayer.randomTip.Length), true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
                    for (int i = 0; i < level.mainPlayer.randomTip.Length; i++)
                    {
                        var line = level.mainPlayer.randomTip[i];
                        if (i == 0) line = "Tip: " + line;
                        Helpers.drawTextStd(TCat.HUD, line, Global.screenW / 2, (Global.screenH / 2) + 45 + (12 * i), Alignment.Center, fontSize: 18);
                    }
                }
            }
        }

        public bool playerWon(Player player)
        {
            if (!isOver) return false;
            if (matchOverResponse.winningAlliances == null) return false;
            return matchOverResponse.winningAlliances.Contains(player.alliance);
        }

        public void onMatchOver()
        {
            if (level.mainPlayer != null && playerWon(level.mainPlayer))
            {
                Global.changeMusic(Global.level.levelData.getWinTheme());
            }
            else if (level.mainPlayer != null && !playerWon(level.mainPlayer))
            {
                Global.changeMusic("lose");
            }
            if (Menu.inMenu)
            {
                Menu.exit();
            }
            logStats();
        }

        public void matchOverRpc(RPCMatchOverResponse matchOverResponse)
        {
            if (this.matchOverResponse == null)
            {
                this.matchOverResponse = matchOverResponse;
                onMatchOver();
            }
        }

        bool loggedStatsOnce;
        public void logStats()
        {
            if (loggedStatsOnce) return;
            loggedStatsOnce = true;

            if (Global.serverClient == null) return;
            if (level.isTraining()) return;
            bool is1v1 = level.is1v1();
            var nonSpecPlayers = Global.level.nonSpecPlayers();
            int botCount = nonSpecPlayers.Count(p => p.isBot);
            int nonBotCount = nonSpecPlayers.Count(p => !p.isBot);
            if (botCount >= nonBotCount) return;
            Player mainPlayer = level.mainPlayer;
            string mainPlayerCharName = getLoggingCharNum(mainPlayer, is1v1);
            
            if (this is FFADeathMatch && !mainPlayer.isSpectator && isFairDeathmatch(mainPlayer))
            {
                long val = playerWon(mainPlayer) ? 100 : 0;
                Logger.logEvent("dm_win_rate", mainPlayerCharName, val, forceLog: true);
                Logger.logEvent("dm_unique_win_rate_" + mainPlayerCharName, Global.deviceId + "_" + mainPlayer.name, val, forceLog: true);
            }

            if (is1v1 && !mainPlayer.isSpectator && !isMirrorMatchup())
            {
                long val = playerWon(mainPlayer) ? 100 : 0;
                Logger.logEvent("1v1_win_rate", mainPlayerCharName, val, forceLog: true);
            }

            if (!is1v1 && (mainPlayer.kills > 0 || mainPlayer.deaths > 0 || mainPlayer.assists > 0))
            {
                Logger.logEvent("kill_stats_v2", mainPlayerCharName + ":kills", mainPlayer.kills, forceLog: true);
                Logger.logEvent("kill_stats_v2", mainPlayerCharName + ":deaths", mainPlayer.deaths, forceLog: true);
                Logger.logEvent("kill_stats_v2", mainPlayerCharName + ":assists", mainPlayer.assists, forceLog: true);
            }

            if (!is1v1 && Global.isHost)
            {
                RPC.logWeaponKills.sendRpc();
                if (isTeamMode && !level.levelData.isMirrored && (this is CTF || this is ControlPoints))
                {
                    long val;
                    if (matchOverResponse.winningAlliances.Contains(blueAlliance)) val = 100;
                    else if (matchOverResponse.winningAlliances.Contains(redAlliance)) val = 0;
                    else
                    {
                        Logger.logEvent("map_stalemate_rates", level.levelData.name + ":" + level.server.gameMode, 100, false, true);
                        return;
                    }
                    Logger.logEvent("map_win_rates", level.levelData.name + ":" + level.server.gameMode, val, false, true);
                    Logger.logEvent("map_stalemate_rates", level.levelData.name + ":" + level.server.gameMode, 0, false, true);
                }
            }
        }

        public bool isMirrorMatchup()
        {
            var nonSpecPlayers = Global.level.nonSpecPlayers();
            if (nonSpecPlayers.Count != 2) return false;
            if (nonSpecPlayers[0].charNum != nonSpecPlayers[1].charNum)
            {
                return true;
            }
            else
            {
                if (nonSpecPlayers[0].charNum == 0 && nonSpecPlayers[0].armorFlag != nonSpecPlayers[1].armorFlag)
                {
                    return true;
                }
                return false;
            }
        }

        public bool isFairDeathmatch(Player mainPlayer)
        {
            int kills = mainPlayer.charNumToKills[mainPlayer.realCharNum];
            if (kills < mainPlayer.kills / 2) return false;
            if (kills < 10) return false;
            return true;
        }

        public string getLoggingCharNum(Player player, bool is1v1)
        {
            int charNum = player.realCharNum;
            string charName;
            if (charNum == 0)
            {
                charName = "X";
                if (is1v1)
                {
                    if (player.bootsArmorNum == 1) charName += "1";
                    else if (player.bootsArmorNum == 2) charName += "2";
                    else if (player.bootsArmorNum == 3) charName += "3";
                }
            }
            else if (charNum == 1) charName = "Zero";
            else if (charNum == 2) charName = "Vile";
            else if (charNum == 3)
            {
                if (Options.main.axlAimMode == 2) charName = "AxlCursor";
                else if (Options.main.axlAimMode == 1) charName = "AxlAngular";
                else charName = "AxlDirectional";
            }
            else if (charNum == 4)
            {
                if (Options.main.sigmaLoadout.commandMode == 0) charName = "SigmaSummoner";
                else if (Options.main.sigmaLoadout.commandMode == 1) charName = "SigmaPuppeteer";
                else if (Options.main.sigmaLoadout.commandMode == 2) charName = "SigmaStriker";
                else charName = "SigmaTagTeam";
            }
            else charName = null;

            return charName;
        }

        public void drawTeamTopHUD()
        {
            var redText = "Red: " + redPoints.ToString();
            var blueText = "Blue: " + bluePoints.ToString();

            if (redPoints >= bluePoints)
            {
                Helpers.drawTextStd(TCat.HUDColored, redText, 5, 2, Alignment.Left, fontSize: (uint)32, outlineColor: Helpers.DarkRed);
                Helpers.drawTextStd(TCat.HUDColored, blueText, 5, 12, Alignment.Left, fontSize: (uint)32, outlineColor: Helpers.DarkBlue);
            }
            else
            {
                Helpers.drawTextStd(TCat.HUDColored, blueText, 5, 2, Alignment.Left, fontSize: (uint)32, outlineColor: Helpers.DarkBlue);
                Helpers.drawTextStd(TCat.HUDColored, redText, 5, 12, Alignment.Left, fontSize: (uint)32, outlineColor: Helpers.DarkRed);
            }

            drawTimeIfSet(27);
        }

        public void drawObjectiveNavpoint(string label, Point objPos)
        {
            if (level.mainPlayer.character == null) return;
            if (!string.IsNullOrEmpty(label)) label += ":";

            Point playerPos = level.mainPlayer.character.pos;

            var line = new Line(playerPos, objPos);
            var camRect = new Rect(level.camX, level.camY, level.camX + Global.viewScreenW, level.camY + Global.viewScreenH);

            var intersectionPoints = camRect.getShape().getLineIntersectCollisions(line);
            if (intersectionPoints.Count > 0)
            {
                var intersectPoint = intersectionPoints[0].hitData.hitPoint.Value;
                var dirTo = playerPos.directionTo(objPos).normalize();

                //a = arrow, l = length, m = minus
                int al = 10 / Global.viewSize;
                int alm1 = 9 / Global.viewSize;
                int alm2 = 8 / Global.viewSize;
                int alm3 = 7 / Global.viewSize;
                int alm4 = 5 / Global.viewSize;

                intersectPoint.inc(dirTo.times(-10));
                var posX = intersectPoint.x - Global.level.camX;
                var posY = intersectPoint.y - Global.level.camY;

                posX /= Global.viewSize;
                posY /= Global.viewSize;

                DrawWrappers.DrawLine(posX, posY, posX + dirTo.x * al, posY + dirTo.y * al, Helpers.getAllianceColor(), 1, ZIndex.HUD, false);
                DrawWrappers.DrawLine(posX + dirTo.x * alm4, posY + dirTo.y * alm4, posX + dirTo.x * alm3, posY + dirTo.y * alm3, Helpers.getAllianceColor(), 4, ZIndex.HUD, false);
                DrawWrappers.DrawLine(posX + dirTo.x * alm3, posY + dirTo.y * alm3, posX + dirTo.x * alm2, posY + dirTo.y * alm2, Helpers.getAllianceColor(), 3, ZIndex.HUD, false);
                DrawWrappers.DrawLine(posX + dirTo.x * alm2, posY + dirTo.y * alm2, posX + dirTo.x * alm1, posY + dirTo.y * alm1, Helpers.getAllianceColor(), 2, ZIndex.HUD, false);

                float distInMeters = objPos.distanceTo(playerPos) * 0.044f;
                bool isLeft = posX < Global.viewScreenW / 2;
                Helpers.drawTextStd(TCat.HUD, label + MathF.Round(distInMeters).ToString() + "m", posX, posY, isLeft ? Alignment.Left : Alignment.Right, fontSize: (uint)MathF.Round(18f / Global.viewSize));
            }
        }

        public void syncTeamScores()
        {
            if (Global.isHost)
            {
                Global.serverClient?.rpc(RPC.syncTeamScores, new byte[] { (byte)redPoints, (byte)bluePoints });
            }
        }
    }
}