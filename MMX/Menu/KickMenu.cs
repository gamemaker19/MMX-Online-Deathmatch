using Newtonsoft.Json;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class KickMenu : IMainMenu
    {
        public int selectArrowPosY;
        public IMainMenu previous;
        public bool listenForKey = false;
        public int kickReasonIndex;
        public List<string> kickReasons = new List<string>()
        {
            "(None specified)",
            "AFK player",
            "Toxicity",
            "Buggy/laggy player",
            "Cheater/exploiter",
            "Reserved match"
        };
        public int kickDuration = 1;
        Player player;

        public List<Point> optionPoses = new List<Point>()
        {
            new Point(30, 100),
            new Point(30, 120)
        };
        public int ySep = 10;

        public KickMenu(IMainMenu mainMenu, Player player)
        {
            previous = mainMenu;
            this.player = player;
        }

        public void update()
        {
            Helpers.menuUpDown(ref selectArrowPosY, 0, optionPoses.Count - 1);
            if (selectArrowPosY == 0)
            {
                if (Global.input.isPressedMenu(Control.MenuLeft))
                {
                    kickReasonIndex--;
                    if (kickReasonIndex < 0) kickReasonIndex = kickReasons.Count - 1;
                }
                else if (Global.input.isPressedMenu(Control.MenuRight))
                {
                    kickReasonIndex++;
                    if (kickReasonIndex > kickReasons.Count - 1) kickReasonIndex = 0;
                }
            }
            if (selectArrowPosY == 1)
            {
                if (Global.input.isPressedMenu(Control.MenuLeft))
                {
                    kickDuration--;
                    if (kickDuration < 0) kickDuration = 1000;
                }
                else if (Global.input.isPressedMenu(Control.MenuRight))
                {
                    kickDuration++;
                    if (kickDuration > 1000) kickDuration = 0;
                }
            }

            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                string voteKickPrefix = "";
                if (!hasDirectKickPower()) voteKickPrefix = "Start Vote ";
                string kickMsg = string.Format(voteKickPrefix + "Kick player {0}\nfor {1} minutes?", player.name, kickDuration);
                Menu.change(new ConfirmLeaveMenu(this, kickMsg, () =>
                {
                    if (hasDirectKickPower())
                    {
                        var kickPlayerObj = new RPCKickPlayerJson(VoteType.Kick, player.name, player.serverPlayer.deviceId, kickDuration, kickReasons[kickReasonIndex]);
                        Global.serverClient?.rpc(RPC.kickPlayerRequest, JsonConvert.SerializeObject(kickPlayerObj));
                        Menu.exit();
                    }
                    else
                    {
                        VoteKick.initiate(player, VoteType.Kick, kickDuration, kickReasons[kickReasonIndex]);
                        Menu.exit();
                    }
                }));
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(previous);
            }
        }

        public void render()
        {
            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
            Global.sprites["cursor"].drawToHUD(0, 15, optionPoses[(int)selectArrowPosY].y + 5);

            string voteKickPrefix = "";
            if (!hasDirectKickPower()) voteKickPrefix = "Vote ";

            Helpers.drawTextStd(voteKickPrefix + "Kick Player " + player.name, Global.halfScreenW, 15, fontSize: 24, alignment: Alignment.Center);

            uint fontSize = 24;

            Helpers.drawTextStd("Kick reason: " + kickReasons[kickReasonIndex], optionPoses[0].x, optionPoses[0].y, fontSize: fontSize, color: Color.White);
            Helpers.drawTextStd("Kick duration: " + kickDuration + " min", optionPoses[1].x, optionPoses[1].y, fontSize: fontSize);

            Helpers.drawTextStd(Helpers.menuControlText("Left/Right: Change, [X]: Kick, [Z]: Back"), Global.screenW * 0.5f, 200, Alignment.Center, fontSize: 21);
        }

        public static bool hasDirectKickPower()
        {
            return Global.isHost && Global.level.server.hidden;
        }
    }
}
