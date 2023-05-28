using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public enum VoteType
    {
        Kick,
        EndMatch,
        ResetFlags
    }

    public class VoteKick
    {
        public Player player;
        public bool ownedByLocalPlayer;
        public bool voted;
        public int yesVotes;
        public int noVotes;
        public string myVote;
        public float time;
        public int playerCount;

        public VoteType type;
        public int kickDuration;
        public string kickReason;

        public VoteKick(Player player, bool ownedByLocalPlayer, VoteType type, int kickDuration, string kickReason)
        {
            this.player = player;
            this.ownedByLocalPlayer = ownedByLocalPlayer;
            this.type = type;
            this.kickDuration = kickDuration;
            this.kickReason = kickReason;
            playerCount = Global.level.players.Count(p => !p.isBot) - 1;
            yesVotes = 1;
            if (ownedByLocalPlayer)
            {
                voted = true;
                myVote = "yes";
            }
        }

        public static void initiate(Player player, VoteType type, int kickDuration, string kickReason)
        {
            if (Global.level.gameMode.voteKickCooldown > 0) return;
            Global.level.gameMode.voteKickCooldown = 180;

            if (Global.level.gameMode.currentVoteKick != null) return;
            var voteKick = new VoteKick(player, true, type, kickDuration, kickReason);
            Global.level.gameMode.currentVoteKick = voteKick;

            var voteKickObj = new RPCKickPlayerJson(type, player.name, player.serverPlayer.deviceId, kickDuration, kickReason);
            string voteKickJson = JsonConvert.SerializeObject(voteKickObj);

            string chatMsg = Global.level.mainPlayer.name + " initiated Vote Kick on " + player.name + ".";
            if (type == VoteType.EndMatch) chatMsg = Global.level.mainPlayer.name + " initiated vote to end match.";
            if (type == VoteType.ResetFlags) chatMsg = Global.level.mainPlayer.name + " initiated vote to reset flags.";

            Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(chatMsg, null, null, true), true);
            Global.serverClient?.rpc(RPC.voteKickStart, voteKickJson);
        }

        public static void sync(Player player, VoteType type, int kickDuration, string kickReason)
        {
            if (Global.level.gameMode.currentVoteKick != null) return;
            var voteKick = new VoteKick(player, false, type, kickDuration, kickReason);
            Global.level.gameMode.currentVoteKick = voteKick;
        }

        public void update()
        {
            time += Global.spf;
            if (time > 60)
            {
                Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(getGenericMessage("{0} timed out (60 sec max)"), null, null, true), false);
                Global.level.gameMode.currentVoteKick = null;
                return;
            }

            if (!voted && !isVictim())
            {
                if (Global.input.isPressed(Key.F1))
                {
                    vote(true);
                }
                else if (Global.input.isPressed(Key.F2))
                {
                    vote(false);
                }
            }

            if (ownedByLocalPlayer)
            {
                if (isVoteKicked())
                {
                    if (type == VoteType.Kick)
                    {
                        var kickPlayerObj = new RPCKickPlayerJson(type, player.name, player.serverPlayer.deviceId, kickDuration, kickReason);
                        Global.serverClient?.rpc(RPC.kickPlayerRequest, JsonConvert.SerializeObject(kickPlayerObj));
                    }
                    else if (type == VoteType.EndMatch)
                    {
                        if (Global.isHost)
                        {
                            Global.level.gameMode.noContest = true;
                        }
                        else
                        {
                            RPC.endMatchRequest.sendRpc();
                        }
                    }
                    else if (type == VoteType.ResetFlags)
                    {
                        if (Global.isHost)
                        {
                            Global.level.resetFlags();
                        }
                        else
                        {
                            RPC.resetFlags.sendRpc();
                        }
                    }

                    Global.serverClient?.rpc(RPC.voteKickEnd);
                    Global.level.gameMode.currentVoteKick = null;
                }
                else if (voteKickFailed())
                {
                    Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry(getGenericMessage("{0} failed to reach majority."), null, null, true), true);
                    Global.serverClient?.rpc(RPC.voteKickEnd);
                    Global.level.gameMode.currentVoteKick = null;
                }
            }
        }

        public string getGenericMessage(string message)
        {
            if (type == VoteType.Kick) return string.Format(message, "Vote Kick");
            if (type == VoteType.ResetFlags) return string.Format(message, "Reset Flag Vote");
            return string.Format(message, "End Match Vote");
        }

        private bool voteKickFailed()
        {
            return noVotes >= neededVotes() || (yesVotes + noVotes >= playerCount);
        }

        public int neededVotes()
        {
            return (playerCount / 2) + 1;
        }

        public void render()
        {
            if (type == VoteType.Kick)
            {
                string message = string.Format("Vote Kick player {0}? Yes: {1} No: {2} Needed: {3}", player.name, yesVotes, noVotes, neededVotes());
                string reason = string.Format("Reason: {0}, Minutes: {1}", kickReason, kickDuration);
                string instructions = getInstructions();
                Helpers.drawTextStd(TCat.HUD, message, Global.halfScreenW, 174, Alignment.Center, fontSize: 20);
                Helpers.drawTextStd(TCat.HUD, reason, Global.halfScreenW, 182, Alignment.Center, fontSize: 20);
                Helpers.drawTextStd(TCat.HUD, instructions, Global.halfScreenW, 190, Alignment.Center, fontSize: 20);
            }
            else if (type == VoteType.EndMatch)
            {
                string message = string.Format("Vote End Match? Yes: {0} No: {1} Needed: {2}", yesVotes, noVotes, neededVotes());
                string instructions = getInstructions();
                Helpers.drawTextStd(TCat.HUD, message, Global.halfScreenW, 174, Alignment.Center, fontSize: 20);
                Helpers.drawTextStd(TCat.HUD, instructions, Global.halfScreenW, 190, Alignment.Center, fontSize: 20);
            }
            else if (type == VoteType.ResetFlags)
            {
                string message = string.Format("Vote Reset Flags? Yes: {0} No: {1} Needed: {2}", yesVotes, noVotes, neededVotes());
                string instructions = getInstructions();
                Helpers.drawTextStd(TCat.HUD, message, Global.halfScreenW, 174, Alignment.Center, fontSize: 20);
                Helpers.drawTextStd(TCat.HUD, instructions, Global.halfScreenW, 190, Alignment.Center, fontSize: 20);
            }
        }

        public string getInstructions()
        {
            if (isVictim()) return "";
            if (voted) return "(You have voted " + myVote + ")";
            return "F1: Yes, F2: No";
        }

        public bool isVictim()
        {
            return Global.level.mainPlayer.name == player.name;
        }

        public bool isVoteKicked()
        {
            return yesVotes >= neededVotes();
        }

        public void vote(bool yes)
        {
            voted = true;
            byte voteByte;
            if (yes)
            {
                yesVotes++;
                myVote = "yes";
                voteByte = 0;
            }
            else
            {
                noVotes++;
                myVote = "no";
                voteByte = 1;
            }
            if (!ownedByLocalPlayer)
            {
                Global.serverClient?.rpc(RPC.voteKick, voteByte);
            }
        }
    }
}
