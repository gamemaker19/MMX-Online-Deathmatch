using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class DevConsole
    {
        public static bool showConsole = false;
        private static bool showLogOnly = false;
        public static List<string> consoleLog = new List<string>();
        public static void log(string message, bool showConsole = false)
        {
            if (consoleLog.Count > 16)
            {
                consoleLog.RemoveAt(0);
            }
            string prefix = Global.frameCount.ToString() + ": ";
            if (!Global.debug) prefix = "";
            consoleLog.Add(prefix + message);
            if (showConsole)
            {
                showLogOnly = true;
            }
        }

        public static void toggleShow()
        {
            if (showConsole) hide();
            else show();
        }

        public static void show()
        {
            showConsole = true;
            Menu.chatMenu.openChat();
        }

        public static void hide()
        {
            showConsole = false;
            showLogOnly = false;
            Menu.chatMenu.closeChat();
        }

        public static void toggleShowLogOnly()
        {
            hide();
            showLogOnly = true;
            consoleLog.Clear();
        }

        public static void drawConsole()
        {
            if (showConsole || showLogOnly)
            {
                DrawWrappers.DrawRect(10, 10, Global.screenW - 10, Global.screenH - 10, true, new Color(32, 32, 32, 128), 0, ZIndex.HUD + 200, false);
                for (int i = 0; i < consoleLog.Count; i++)
                {
                    string line = consoleLog[i];
                    Helpers.drawTextStd(line, 20, 20 + (i * 12), fontSize: 24, color: Color.White);
                }
            }
        }

        public static void aiSwitch(string[] args)
        {
            int slot = int.Parse(args[0]);
            Global.level.otherPlayer.changeWeaponSlot(slot - 1);
            if (args.Contains("a"))
            {
                AI.trainingBehavior = AITrainingBehavior.Attack;
            }
        }

        public static void scrapCommand(string[] args)
        {
            if (args[0] == "max") args[0] = "9999";
            int scrap = int.Parse(args[0]);
            Global.level.mainPlayer.scrap = scrap;
        }

        public static void setHealth(string[] args)
        {
            if (Global.level.mainPlayer.currentMaverick != null)
            {
                Global.level.mainPlayer.currentMaverick.health = int.Parse(args[0]);
            }
            Global.level.mainPlayer.health = int.Parse(args[0]);
        }

        public static void setMusicNearEnd()
        {
            Global.music?.setNearEnd();
        }

        public static void printChecksum()
        {
            if (Global.level?.levelData?.isCustomMap == true)
            {
                log(Global.level.levelData.checksum);
            }
        }

        public static void addDnaCore(string[] args)
        {
            int count = 10;
            if (args.Length > 0)
            {
                count = int.Parse(args[0]);
            }
            for (int i = 0; i < count; i++)
            {
                var chr = Global.level.players.FirstOrDefault(p => p != Global.level.mainPlayer).character;
                if (chr != null)
                {
                    Global.level.mainPlayer.weapons.Add(new DNACore(chr));
                }
            }
        }

        public static void fillSubtank(string[] args)
        {
            if (Global.level.mainPlayer.subtanks.Count < 1)
            {
                Global.level.mainPlayer.subtanks.Add(new SubTank());
            }
            Global.level.mainPlayer.fillSubtank(12);
        }

        public static void showOrHideHitboxes(string[] args)
        {
            Global.showHitboxes = !Global.showHitboxes;
        }

        public static void becomeMoth()
        {
            var mmc = Global.level?.mainPlayer?.currentMaverick as MorphMothCocoon;
            if (mmc != null)
            {
                mmc.selfDestructTime = Global.spf;
            }
        }

        public static void win()
        {
            if (Global.level.gameMode is FFADeathMatch)
            {
                Global.level.mainPlayer.kills = Global.level.gameMode.playingTo;
            }
            else if (Global.level.gameMode is TeamDeathMatch)
            {
                Global.level.gameMode.bluePoints = Global.level.gameMode.playingTo;
            }
        }

        public static void lose()
        {
            if (Global.level.gameMode is FFADeathMatch)
            {
                Global.level.otherPlayer.kills = Global.level.gameMode.playingTo;
            }
            else if (Global.level.gameMode is TeamDeathMatch)
            {
                Global.level.gameMode.redPoints = Global.level.gameMode.playingTo;
            }
        }

        public static void aiRevive()
        {
            if (Global.debug)
            {
                Global.shouldAiAutoRevive = true;
                Global.level.otherPlayer.character?.applyDamage(null, null, Damager.envKillDamage, null);
            }
        }

        public static void aiMash(string[] args)
        {
            int mashType = 0;
            if (args.Length > 0)
            {
                mashType = int.Parse(args[0]);
            }
            mashType = Helpers.clamp(mashType, 0, 2);
            Global.level.otherPlayer.character.ai.mashType = mashType;
        }

        public static void spawnRideChaser()
        {
            var mp = Global.level.mainPlayer;
            if (mp != null) new RideChaser(mp, mp.character.pos, 0, null, true);
        }

        public static void toggleFTD()
        {
            if (Global.level.server.netcodeModel == NetcodeModel.FavorAttacker)
            {
                Global.level.server.netcodeModel = NetcodeModel.FavorDefender;
            }
            else
            {
                Global.level.server.netcodeModel = NetcodeModel.FavorAttacker;
            }
        }

        public static void toggleInvulnFrames(int time)
        {
            var mc = Global.level.mainPlayer.character;
            mc.invulnTime = time;
        }

        public static void changeTeam()
        {
            if (!Global.level.gameMode.isTeamMode) return;

            int team = Global.level.mainPlayer.alliance == GameMode.redAlliance ? GameMode.blueAlliance : GameMode.redAlliance;
            Global.serverClient?.rpc(RPC.switchTeam, RPCSwitchTeam.getSendMessage(Global.level.mainPlayer.id, team));
            Global.level.mainPlayer.newAlliance = team;
            Global.level.mainPlayer.forceKill();
            Menu.exit();
        }

        public static void aiDebug(bool changeToSpec)
        {
            Global.showAIDebug = true;
            if (changeToSpec)
            {
                Global.level.setMainPlayerSpectate();
            }
        }

        public static void aiGiga()
        {
            Global.level.otherPlayer.weapons.Add(new GigaCrush());
            Global.level.otherPlayer.character.changeState(new GigaCrushCharState(), true);
        }

        public static List<Command> commands = new List<Command>()
        {
            // Offline only, undocumented
            new Command("log", (args) => toggleShowLogOnly()),
            new Command("moth", (args) => becomeMoth()),
            new Command("airevive", (args) => aiRevive()),
            new Command("aigiga", (args) => aiGiga()),
            new Command("rc", (args) => spawnRideChaser()),
            new Command("aidebug", (args) => aiDebug(false)),
            new Command("aispec", (args) => aiDebug(true)),
            // Offline only
            new Command("hitbox", (args) => showOrHideHitboxes(args)),
            new Command("dumpnetids", (args) => Helpers.WriteToFile("netIdDump.txt", Global.level.getNetIdDump())),
            new Command("dumpkillfeed", (args) => Helpers.WriteToFile("killFeedDump.txt", string.Join(Environment.NewLine, Global.level.gameMode.killFeedHistory))),
            new Command("invuln", (args) => Global.level.mainPlayer.character.invulnTime = 60),
            new Command("ult", (args) => Global.level.mainPlayer.setUltimateArmor(true)),
            new Command("health", (args) => setHealth(args)),
            new Command("freeze", (args) => Global.level.mainPlayer.character.freeze(2)),
            new Command("hurt", (args) => Global.level.mainPlayer.character.setHurt(-1, Global.defFlinch, 0, false)),
            new Command("trhealth", (args) => Global.spawnTrainingHealth = !Global.spawnTrainingHealth),
            new Command("checksum", (args) => printChecksum()),
            new Command("dna", (args) => addDnaCore(args)),
            new Command("timeleft", (args) => Global.level.gameMode.remainingTime = 5),
            new Command("subtank", (args) => fillSubtank(args)),
            new Command("subtest", (args) => { fillSubtank(args); setHealth(new string[] { "1" }); }),
            new Command("aiattack", (args) => AI.trainingBehavior = AITrainingBehavior.Attack),
            new Command("aijump", (args) => AI.trainingBehavior = AITrainingBehavior.Jump),
            new Command("aiguard", (args) => AI.trainingBehavior = AITrainingBehavior.Guard),
            new Command("aicrouch", (args) => AI.trainingBehavior = AITrainingBehavior.Crouch),
            new Command("aistop", (args) => AI.trainingBehavior = AITrainingBehavior.Idle),
            new Command("aikill", (args) => Global.level.otherPlayer?.forceKill()),
            new Command("aiswitch", aiSwitch),
            new Command("aimash", (args) => aiMash(args)),
            new Command("scrap", scrapCommand),
            new Command("die", (args) => Global.level.mainPlayer.forceKill()),
            new Command("raflight", (args) => Global.level.rideArmorFlight = !Global.level.rideArmorFlight),
            // Online
            new Command("diagnostics", (args) => Global.showDiagnostics = !Global.showDiagnostics, offlineOnly: false),
            new Command("clear", (args) => consoleLog.Clear(), offlineOnly: false),
            new Command("musicend", (args) => setMusicNearEnd()),
            // GMTODO remove
            new Command("dumpflagdata", (args) => Helpers.WriteToFile("flagDataDump.txt", Global.level.getFlagDataDump()), offlineOnly: false),
#if DEBUG
            new Command("autofire", (args) => Global.autoFire = !Global.autoFire),
            new Command("breakpoint", (args) => Global.breakpoint = !Global.breakpoint),
            new Command("r", (args) => Global.level.mainPlayer.kills = Global.level.gameMode.playingTo - 1, offlineOnly: false),
            new Command("1morekill", (args) => Global.level.mainPlayer.kills = Global.level.gameMode.playingTo - 1),
            new Command("win", (args) => win()),
            new Command("lose", (args) => lose()),
            new Command("changeteam", (args) => changeTeam()),
            new Command("ftd", (args) => toggleFTD()),
            new Command("invuln", (args) => toggleInvulnFrames(10)),
#endif
        };

        public static void runCommand(string commandStr)
        {
            List<string> pieces = commandStr.Split(' ').ToList();
            string command = pieces[0];
            var args = new List<string>();
            try
            {
                args = pieces.GetRange(1, pieces.Count - 1);
            }
            catch { }

            var commandObj = commands.FirstOrDefault(c => c.name == command);
            if (commandObj != null)
            {
                if (!commandObj.offlineOnly || Global.serverClient == null || Global.debug)
                {
                    try
                    {
                        log("Ran command \"" + command + "\"");
                        commandObj.action.Invoke(args.ToArray());
                        if (args.Contains("q"))
                        {
                            hide();
                        }
                    }
                    catch
                    {
                        log("Command \"" + command + "\" failed");
                    }
                }
                else
                {
                    log("Command \"" + command + "\" is only available offline");
                }
            }
            else
            {
                log("Command \"" + command + "\" does not exist.");
            }
        }
    }

    public class Command
    {
        public string name;
        public bool offlineOnly;
        public Action<string[]> action;

        public Command(string name, Action<string[]> action, bool offlineOnly = true)
        {
            this.name = name;
            this.offlineOnly = offlineOnly;
            this.action = action;
        }
    }
}
