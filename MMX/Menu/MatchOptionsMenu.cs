using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class MatchOptionsMenu : IMainMenu
    {
        public int selectY;
        public IMainMenu prevMenu;

        public int playerMuteIndex;
        public int playerKickIndex;
        public int playerReportIndex;
        public int removeBotIndex;
        public List<Player> players
        {
            get { return Global.level.players.Where(p => !p.isBot && p != Global.level.mainPlayer).OrderBy(p => p.name).ToList(); }
        }
        public List<Player> playersIncludingSelf
        {
            get { return Global.level.players.Where(p => !p.isBot).OrderBy(p => p.name).ToList(); }
        }
        public List<Player> bots
        {
            get { return Global.level.players.Where(p => p.isBot).OrderBy(p => p.name).ToList(); }
        }

        public List<MenuOption> menuOptions;
        public const int startX = 90;
        public const int startY = 60;
        public const int lineH = 10;

        public float muteCooldown;

        public MatchOptionsMenu(IMainMenu prevMenu)
        {
            this.prevMenu = prevMenu;

            int lineNum = 0;

            menuOptions = new List<MenuOption>()
            {
                // Suicide
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            if (canSuicide())
                            {
                                Global.level.mainPlayer.forceKill();
                                Menu.exit();
                            }
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Suicide", pos.x, pos.y, color: canSuicide() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
                // Change team
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            if (canChangeToTeam())
                            {
                                int team = enemyTeam();
                                Global.serverClient?.rpc(RPC.switchTeam, RPCSwitchTeam.getSendMessage(Global.level.mainPlayer.id, team));
                                Global.level.mainPlayer.newAlliance = team;
                                Global.level.mainPlayer.forceKill();
                                Menu.exit();
                            }
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Change Team", pos.x, pos.y, color: canChangeToTeam() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
                // Spectate
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (!canSpectate()) return;

                        var otherPlayers = Global.level.spectatablePlayers();

                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            Global.level.setMainPlayerSpectate();
                            Menu.exit();
                        }
                    },
                    (Point pos, int index) =>
                    {
                        var otherPlayers = Global.level.spectatablePlayers();
                        string spectate = Global.level.mainPlayer.isSpectator ? "Stop spectating" : "Spectate";
                        Helpers.drawTextStd(TCat.Option, spectate, pos.x, pos.y, color: canSpectate() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
                // Add bot
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (!canAddBot()) return;
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            Menu.change(new AddBotMenu(this));
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Add Bot", pos.x, pos.y, color: canAddBot() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
                // Remove bot
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (!canRemoveBot()) return;
                        if (botToRemove == null) return;

                        if (Global.input.isPressedMenu(Control.MenuLeft))
                        {
                            removeBotIndex--;
                            if (removeBotIndex < 0) removeBotIndex = bots.Count - 1;
                        }
                        else if (Global.input.isPressedMenu(Control.MenuRight))
                        {
                            removeBotIndex++;
                            if (removeBotIndex >= bots.Count) removeBotIndex = 0;
                        }
                        else if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            // Don't allow removing a bot if it would unbalance the teams
                            if (isPublicMatch() && Global.level.gameMode.isTeamMode)
                            {
                                GameMode.getAllianceCounts(Global.level.nonSpecPlayers(), out int redCount, out int blueCount);
                                if ((botToRemove.alliance == GameMode.redAlliance && redCount < blueCount) || (botToRemove.alliance == GameMode.blueAlliance && blueCount < redCount))
                                {
                                    Menu.change(new ErrorMenu("This would unbalance the teams.", this, true));
                                    return;
                                }
                            }

                            if (Global.serverClient != null)
                            {
                                RPC.removeBot.sendRpc(botToRemove.id);
                            }
                            else
                            {
                                Global.level.removePlayer(botToRemove);
                            }
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Remove Bot: " + (botToRemove?.name ?? "(No bots)"), pos.x, pos.y, color: canRemoveBot() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
                // Chat history
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            Menu.change(new ChatHistoryMenu(this));
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Chat History", pos.x, pos.y, color: Color.White, fontSize: 24, selected: selectY == index);
                    }),
                // Disable chat
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            Global.level.gameMode.chatMenu.chatEnabled = !Global.level.gameMode.chatMenu.chatEnabled;
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, Global.level.gameMode.chatMenu.chatEnabled ? "Disable Chat" : "Enable Chat", pos.x, pos.y, color: Color.White, fontSize: 24, selected: selectY == index);
                    }),
                // Mute
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (playerToMute == null) return;

                        if (Global.input.isPressedMenu(Control.MenuLeft))
                        {
                            playerMuteIndex--;
                            if (playerMuteIndex < 0) playerMuteIndex = players.Count - 1;
                        }
                        else if (Global.input.isPressedMenu(Control.MenuRight))
                        {
                            playerMuteIndex++;
                            if (playerMuteIndex >= players.Count) playerMuteIndex = 0;
                        }
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            if (muteCooldown == 0)
                            {
                                muteCooldown += Global.spf;
                                string muteMsg = " muted ";
                                if (!playerToMute.isMuted)
                                {
                                    playerToMute.isMuted = true;
                                }
                                else
                                {
                                    playerToMute.isMuted = false;
                                    muteMsg = " unmuted ";
                                }
                                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(Global.level.mainPlayer.name + muteMsg + playerToMute.name + ".", null, null, true));
                            }
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Mute: " + getPlayerMuteMsg(playerToMute), pos.x, pos.y, color: Color.White, fontSize: 24, selected: selectY == index);
                    }),
                // Report
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (playerToReport == null) return;

                        if (Global.input.isPressedMenu(Control.MenuLeft))
                        {
                            playerReportIndex--;
                            if (playerReportIndex < 0) playerReportIndex = playersIncludingSelf.Count - 1;
                        }
                        else if (Global.input.isPressedMenu(Control.MenuRight))
                        {
                            playerReportIndex++;
                            if (playerReportIndex >= playersIncludingSelf.Count) playerReportIndex = 0;
                        }
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            var oldMenu = this;
                            Menu.change(new ConfirmLeaveMenu(this, "Report player " + playerToReport.name + "?\nThis will generate a report_[name].txt\nfile in your game folder;\nsend it to your server admin.", () =>
                            {
                                Global.serverClient?.rpc(RPC.reportPlayerRequest, playerToReport.name);
                                Menu.change(oldMenu);
                            }));
                        }
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Report: " + (playerToReport?.name ?? "(No players)"), pos.x, pos.y, color: Color.White, fontSize: 24, selected: selectY == index);
                    }),
                // Kick/Ban
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (playerToKick == null) return;
                        if (!canKick()) return;

                        if (Global.input.isPressedMenu(Control.MenuLeft))
                        {
                            playerKickIndex--;
                            if (playerKickIndex < 0) playerKickIndex = players.Count - 1;
                        }
                        else if (Global.input.isPressedMenu(Control.MenuRight))
                        {
                            playerKickIndex++;
                            if (playerKickIndex >= players.Count) playerKickIndex = 0;
                        }
                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            Menu.change(new KickMenu(this, playerToKick));
                        }
                    },
                    (Point pos, int index) =>
                    {
                        string prefix = "";
                        if (!KickMenu.hasDirectKickPower()) prefix = "Vote ";
                        Helpers.drawTextStd(TCat.Option, prefix + "Kick: " + (playerToKick?.name ?? "(No players)"), pos.x, pos.y, color: canKick() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
                // Reset flags
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (!canResetFlags()) return;

                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            string confirmMsg = "Vote Reset Flags: are you sure?";
                            Menu.change(new ConfirmLeaveMenu(this, confirmMsg, () =>
                            {
                                VoteKick.initiate(Global.level.mainPlayer, VoteType.ResetFlags, 0, "");
                                Menu.exit();
                            }));
                        }
                    },
                    (Point pos, int index) =>
                    {
                        string msg = "Vote Reset Flags";
                        Helpers.drawTextStd(TCat.Option, msg, pos.x, pos.y, color: canResetFlags() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
                // End match
                new MenuOption(startX, startY + (lineH * lineNum++),
                    () =>
                    {
                        if (!canEndMatch()) return;

                        if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                        {
                            string confirmMsg = canEndMatchWithoutVote() ? "End Match: are you sure?" : "Vote End Match: are you sure?";
                            Menu.change(new ConfirmLeaveMenu(this, confirmMsg, () =>
                            {
                                if (canEndMatchWithoutVote())
                                {
                                    Global.level.gameMode.noContest = true;
                                }
                                else
                                {
                                    VoteKick.initiate(Global.level.mainPlayer, VoteType.EndMatch, 0, "");
                                    Menu.exit();
                                }
                                Menu.exit();
                            }));
                        }
                    },
                    (Point pos, int index) =>
                    {
                        string msg = canEndMatchWithoutVote() ? "End Match" : "Vote End Match";
                        Helpers.drawTextStd(TCat.Option, msg, pos.x, pos.y, color: canEndMatch() ? Color.White : Helpers.Gray, fontSize: 24, selected: selectY == index);
                    }),
            };
        }

        private bool canResetFlags()
        {
            if (Global.level.gameMode.isOver) return false;
            if (Global.level.gameMode.currentVoteKick != null) return false;
            if (Global.level.gameMode.voteKickCooldown > 0) return false;
            if (Global.level.gameMode is not CTF) return false;
            return true;
        }

        private bool canEndMatch()
        {
            if (Global.level.gameMode.isOver) return false;
            if (Global.level.gameMode.currentVoteKick != null) return false;
            if (Global.level.gameMode.voteKickCooldown > 0) return false;
            return true;
        }

        private bool canEndMatchWithoutVote()
        {
            if (!Global.isHost) return false;
            if (Global.level.server.hidden) return true;
            if (Global.level.time < 45) return true;
            return false;
        }

        private bool canRemoveBot()
        {
            return Global.isHost && !Global.level.isElimination();
        }

        public void update()
        {
            if (muteCooldown > 0)
            {
                muteCooldown += Global.spf;
                if (muteCooldown > 2)
                {
                    muteCooldown = 0;
                }
            }

            Helpers.menuUpDown(ref selectY, 0, menuOptions.Count - 1);

            menuOptions[selectY].update();

            if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
            }
        }

        public void render()
        {
            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
            Helpers.drawTextStd(TCat.Title, "Match Options", Global.screenW * 0.5f, 20, Alignment.Center, fontSize: 48);

            Global.sprites["cursor"].drawToHUD(0, startX - 10, startY + 3 + (selectY * lineH));

            int i = 0;
            foreach (var menuOption in menuOptions)
            {
                menuOption.render(menuOption.pos, i);
                i++;
            }

            if (selectY == 5 || selectY == 6 || selectY == 7 || selectY == 9)
            {
                Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change", Global.halfScreenW, 210, Alignment.Center, fontSize: 24);
            }
            Helpers.drawTextStd(TCat.BotHelp, "[X]: Select, [Z]: Back", Global.halfScreenW, 200, Alignment.Center, fontSize: 24);
        }

        public Player playerToMute
        {
            get
            {
                while (playerMuteIndex >= players.Count && playerMuteIndex > 0)
                {
                    playerMuteIndex--;
                }
                if (playerMuteIndex >= 0 && playerMuteIndex < players.Count)
                {
                    return players[playerMuteIndex];
                }
                return null;
            }
        }

        public Player playerToKick
        {
            get
            {
                while (playerKickIndex >= players.Count && playerKickIndex > 0)
                {
                    playerKickIndex--;
                }
                if (playerKickIndex >= 0 && playerKickIndex < players.Count)
                {
                    return players[playerKickIndex];
                }
                return null;
            }
        }

        public Player playerToReport
        {
            get
            {
                while (playerReportIndex >= playersIncludingSelf.Count && playerReportIndex > 0)
                {
                    playerReportIndex--;
                }
                if (playerReportIndex >= 0 && playerReportIndex < playersIncludingSelf.Count)
                {
                    return playersIncludingSelf[playerReportIndex];
                }
                return null;
            }
        }

        public Player botToRemove
        {
            get
            {
                while (removeBotIndex >= bots.Count && removeBotIndex > 0)
                {
                    removeBotIndex--;
                }
                if (removeBotIndex >= 0 && removeBotIndex < bots.Count)
                {
                    return bots[removeBotIndex];
                }
                return null;
            }
        }

        private string getPlayerMuteMsg(Player playerToMute)
        {
            if (playerToMute == null) return "(No players)";
            return playerToMute.name + (playerToMute.isMuted ? " (muted)" : " (not muted)");
        }

        public int enemyTeam()
        {
            return Global.level.mainPlayer.alliance == GameMode.redAlliance ? GameMode.blueAlliance : GameMode.redAlliance;
        }

        private bool canAddBot()
        {
            return Global.isHost && Global.level.players.Count < Global.level.levelData.maxPlayers && !Global.level.isElimination();
        }

        private bool canSpectate()
        {
            if (Global.level.mainPlayer.isSpectator && Global.level.players.Count(p => !p.isSpectator) >= Global.level.server.maxPlayers) return false;
            if (Global.level.is1v1()) return false;
            if (!Global.level.isElimination() && Global.level.mainPlayer.isSpectator) return true;
            if (Global.level.isElimination() && Global.level.mainPlayer.isSpectator) return false;
            if (Global.level.isRace() && Global.level.mainPlayer.isSpectator) return false;
            if (Global.serverClient == null) return true;
            if (Global.level.server != null && Global.level.server.hidden) return true;

            if (Global.level.gameMode.isTeamMode)
            {
                GameMode.getAllianceCounts(Global.level.nonSpecPlayers(), out int redCount, out int blueCount);
                if (Global.level.mainPlayer.alliance == GameMode.redAlliance && redCount < blueCount) return false;
                if (Global.level.mainPlayer.alliance == GameMode.blueAlliance && blueCount < redCount) return false;
                return true;
            }
            else
            {
                return true;
            }
        }

        private bool canKick()
        {
            return Global.level.gameMode.currentVoteKick == null && Global.level.gameMode.voteKickCooldown == 0 && !Global.isChatBanned;
        }

        public bool canSuicide()
        {
            if (Global.level.mainPlayer.isDead) return false;
            return true;
        }

        public bool canChangeToTeam()
        {
            if (Global.level.isTraining()) return true;
            if (!Global.level.gameMode.isTeamMode) return false;
            if (Global.level.mainPlayer.isSpectator) return false;
            if (Global.level.isElimination()) return false;
            if (Global.serverClient == null) return true;
            if (Global.level.server != null && Global.level.server.hidden) return true;
            return false;
        }

        private bool isPublicMatch()
        {
            return Global.serverClient != null && Global.level.server != null && !Global.level.server.hidden;
        }
    }
}
