using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class SelectVileWeaponMenu : IMainMenu
    {
        public List<WeaponCursor> cursors;
        public int selCursorIndex;
        public bool inGame;
        public string error = "";

        public static List<Tuple<string, List<Weapon>>> vileWeaponCategories = new List<Tuple<string, List<Weapon>>>()
        {
            Tuple.Create("Cannon", new List<Weapon>() { new VileCannon(VileCannonType.None), new VileCannon(VileCannonType.FrontRunner), new VileCannon(VileCannonType.LongshotGizmo), new VileCannon(VileCannonType.FatBoy) }),
            Tuple.Create("Vulcan", new List<Weapon>() { new Vulcan(VulcanType.None), new Vulcan(VulcanType.CherryBlast), new Vulcan(VulcanType.DistanceNeedler), new Vulcan(VulcanType.BuckshotDance) }),
            Tuple.Create("Missile", new List<Weapon>() { new VileMissile(VileMissileType.None), new VileMissile(VileMissileType.StunShot), new VileMissile(VileMissileType.HumerusCrush), new VileMissile(VileMissileType.PopcornDemon) }),
            Tuple.Create("R.Punch", new List<Weapon>() { new RocketPunch(RocketPunchType.None), new RocketPunch(RocketPunchType.GoGetterRight), new RocketPunch(RocketPunchType.SpoiledBrat), new RocketPunch(RocketPunchType.InfinityGig) }),
            Tuple.Create("Napalm", new List<Weapon>() { new Napalm(NapalmType.NoneBall), new Napalm(NapalmType.RumblingBang), new Napalm(NapalmType.FireGrenade), new Napalm(NapalmType.SplashHit), new Napalm(NapalmType.NoneFlamethrower) }),
            Tuple.Create("Ball", new List<Weapon>() { new VileBall(VileBallType.NoneNapalm), new VileBall(VileBallType.AirBombs), new VileBall(VileBallType.StunBalls), new VileBall(VileBallType.PeaceOutRoller), new VileBall(VileBallType.NoneFlamethrower) }),
            Tuple.Create("Cutter", new List<Weapon>() { new VileCutter(VileCutterType.None), new VileCutter(VileCutterType.QuickHomesick), new VileCutter(VileCutterType.ParasiteSword), new VileCutter(VileCutterType.MaroonedTomahawk) }),
            Tuple.Create("Flamethrower", new List<Weapon>() { new VileFlamethrower(VileFlamethrowerType.NoneNapalm), new VileFlamethrower(VileFlamethrowerType.WildHorseKick), new VileFlamethrower(VileFlamethrowerType.DragonsWrath), new VileFlamethrower(VileFlamethrowerType.SeaDragonRage), new VileFlamethrower(VileFlamethrowerType.NoneBall) }),
            Tuple.Create("Laser", new List<Weapon>() { new VileLaser(VileLaserType.None), new VileLaser(VileLaserType.RisingSpecter), new VileLaser(VileLaserType.NecroBurst), new VileLaser(VileLaserType.StraightNightmare) }),
        };

        public IMainMenu prevMenu;

        public SelectVileWeaponMenu(IMainMenu prevMenu, bool inGame)
        {
            this.prevMenu = prevMenu;
            this.inGame = inGame;

            cursors = new List<WeaponCursor>();

            cursors.Add(new WeaponCursor(vileWeaponCategories[0].Item2.FindIndex(w => w.type == Options.main.vileLoadout.cannon)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[1].Item2.FindIndex(w => w.type == Options.main.vileLoadout.vulcan)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[2].Item2.FindIndex(w => w.type == Options.main.vileLoadout.missile)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[3].Item2.FindIndex(w => w.type == Options.main.vileLoadout.rocketPunch)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[4].Item2.FindIndex(w => w.type == Options.main.vileLoadout.napalm)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[5].Item2.FindIndex(w => w.type == Options.main.vileLoadout.ball)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[6].Item2.FindIndex(w => w.type == Options.main.vileLoadout.cutter)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[7].Item2.FindIndex(w => w.type == Options.main.vileLoadout.flamethrower)));
            cursors.Add(new WeaponCursor(vileWeaponCategories[8].Item2.FindIndex(w => w.type == Options.main.vileLoadout.laser)));
        }

        public void update()
        {
            if (!string.IsNullOrEmpty(error))
            {
                if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                {
                    error = null;
                }
                return;
            }

            int maxCatCount = vileWeaponCategories[selCursorIndex].Item2.Count;

            int minIndex = 0;
            if (selCursorIndex == 0 || selCursorIndex == 1 || selCursorIndex == 2 || selCursorIndex == 3 || selCursorIndex == 8)
            {
                minIndex = 1;
            }

            Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, minIndex, maxCatCount - 1, wrap: true, playSound: true);

            Helpers.menuUpDown(ref selCursorIndex, 0, vileWeaponCategories.Count - 1);

            bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
            bool selectPressed = Global.input.isPressedMenu(Control.MenuSelectPrimary) || (backPressed && !inGame);
            if (selectPressed)
            {
                if (getWeightSum() > VileLoadout.maxWeight)
                {
                    error = "Cannot exceed maximum loadout weight.";
                    return;
                }

                if (vileWeaponCategories[0].Item2[cursors[0].index].type == -1 && vileWeaponCategories[1].Item2[cursors[1].index].type == -1)
                {
                    error = "Must equip either a Vulcan or Cannon.";
                    return;
                }

                int[] oldArray = { Options.main.vileLoadout.cannon, Options.main.vileLoadout.vulcan, Options.main.vileLoadout.missile, Options.main.vileLoadout.rocketPunch, Options.main.vileLoadout.napalm, Options.main.vileLoadout.ball, Options.main.vileLoadout.cutter, Options.main.vileLoadout.flamethrower, Options.main.vileLoadout.laser };
                Options.main.vileLoadout.cannon = vileWeaponCategories[0].Item2[cursors[0].index].type;
                Options.main.vileLoadout.vulcan = vileWeaponCategories[1].Item2[cursors[1].index].type;
                Options.main.vileLoadout.missile = vileWeaponCategories[2].Item2[cursors[2].index].type;
                Options.main.vileLoadout.rocketPunch = vileWeaponCategories[3].Item2[cursors[3].index].type;
                Options.main.vileLoadout.napalm = vileWeaponCategories[4].Item2[cursors[4].index].type;
                Options.main.vileLoadout.ball = vileWeaponCategories[5].Item2[cursors[5].index].type;
                Options.main.vileLoadout.cutter = vileWeaponCategories[6].Item2[cursors[6].index].type;
                Options.main.vileLoadout.flamethrower = vileWeaponCategories[7].Item2[cursors[7].index].type;
                Options.main.vileLoadout.laser = vileWeaponCategories[8].Item2[cursors[8].index].type;
                int[] newArray = { Options.main.vileLoadout.cannon, Options.main.vileLoadout.vulcan, Options.main.vileLoadout.missile, Options.main.vileLoadout.rocketPunch, Options.main.vileLoadout.napalm, Options.main.vileLoadout.ball, Options.main.vileLoadout.cutter, Options.main.vileLoadout.flamethrower, Options.main.vileLoadout.laser };

                if (!Enumerable.SequenceEqual(oldArray, newArray))
                {
                    Options.main.saveToFile();
                    if (inGame)
                    {
                        if (Options.main.killOnLoadoutChange)
                        {
                            Global.level.mainPlayer.forceKill();
                        }
                        else if (!Global.level.mainPlayer.isDead)
                        {
                            Global.level.gameMode.setHUDErrorMessage(Global.level.mainPlayer, "Change will apply on next death", playSound: false);
                        }
                    }
                }

                if (inGame) Menu.exit();
                else Menu.change(prevMenu);
            }
            else if (backPressed)
            {
                Menu.change(prevMenu);
            }
        }

        public int getWeightSum()
        {
            int total = Global.level?.mainPlayer?.getVileWeight(0) ?? 0;
            for (int i = 0; i < vileWeaponCategories.Count; i++)
            {
                total += vileWeaponCategories[i].Item2[cursors[i].index].vileWeight;
            }
            return total;
        }

        public void render()
        {
            if (!inGame)
            {
                DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
            }

            Helpers.drawTextStd(TCat.Title, "Vile Loadout", Global.screenW * 0.5f, 12, Alignment.Center, fontSize: 48);
            //Helpers.drawTextStd("Weight: ", (Global.screenW * 0.5f) - 35, 26, Alignment.Left, fontSize: 24);
            //Helpers.drawTextStd(getWeightSum() + "/" + VileLoadout.maxWeight, (Global.screenW * 0.5f) + 10, 26, Alignment.Left, fontSize: 24, color: getWeightSum() > VileLoadout.maxWeight ? Color.Red : Color.White);
            var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
            float botOffY = inGame ? 0 : -2;

            int startY = 44;
            int startX = 20;
            int wepH = 14;

            float wepPosX = 120;
            float wepTextX = 132;

            Global.sprites["cursor"].drawToHUD(0, startX, startY + (selCursorIndex * wepH) - 2);
            for (int i = 0; i < vileWeaponCategories.Count; i++)
            {
                float yPos = startY - 6 + (i * wepH);
                Helpers.drawTextStd(TCat.Option, vileWeaponCategories[i].Item1 + ": ", startX + 10, yPos, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
                var weapon = vileWeaponCategories[i].Item2[cursors[i].index];
                if (weapon.killFeedIndex != 0)
                {
                    Global.sprites["hud_killfeed_weapon"].drawToHUD(weapon.killFeedIndex, wepPosX, yPos + 3);
                    Helpers.drawTextStd(TCat.Option, weapon.displayName, wepTextX, yPos, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
                }
                else
                {
                    Helpers.drawTextStd(TCat.Option, weapon.displayName, wepPosX - 5, yPos, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
                }

                //Helpers.drawTextStd(TCat.Option, "W:" + weapon.vileWeight.ToString(), Global.screenW - 30, yPos, alignment: Alignment.Right, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
            }

            var wep = vileWeaponCategories[selCursorIndex].Item2[cursors[selCursorIndex].index];

            int wsy = 167;
            DrawWrappers.DrawRect(25, wsy + 2, Global.screenW - 30, wsy + 28, true, new Color(0, 0, 0, 100), 0.5f, ZIndex.HUD, false, outlineColor: outlineColor);

            string titleText = wep.displayName;

            string inputText = "";
            if (selCursorIndex == 0) inputText = "INPUT: Attack";
            if (selCursorIndex == 1) inputText = "INPUT: Attack";
            if (selCursorIndex == 2) inputText = "INPUT: Special(Ground)";
            if (selCursorIndex == 3) inputText = "INPUT: Side Special(Ground)";
            if (selCursorIndex == 4) inputText = "INPUT: Down Special (Ground)";
            if (selCursorIndex == 5) inputText = "INPUT: Special(Air)";
            if (selCursorIndex == 6) inputText = "INPUT: Up Special(Ground)";
            if (selCursorIndex == 7) inputText = "INPUT: Down Special(Air)";
            if (selCursorIndex == 8) inputText = "INPUT: Charge Special";

            int descLine1 = wsy + 8;
            int descLine2 = wsy + 17;
            descLine1 = wsy + 13;
            descLine2 = wsy + 20;

            Helpers.drawTextStd(TCat.Title, titleText, 40, wsy + 6, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
            Helpers.drawTextStd(TCat.Title, inputText, Global.screenW - 40, wsy + 6, Alignment.Right, style: Text.Styles.Italic, fontSize: 18);

            if (wep.description?.Length > 0)
            {
                Helpers.drawTextStd(wep.description[0], 40, wep.description.Length == 1 ? descLine1 + 3 : descLine1, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
            }
            if (wep.description?.Length > 1)
            {
                Helpers.drawTextStd(wep.description[1], 40, descLine2, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
            }

            Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Weapon", Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
            Helpers.drawTextStd(TCat.BotHelp, "Up/Down: Change Category", Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 18);
            string helpText = "[Z]: Back, [X]: Confirm";
            if (!inGame) helpText = "[Z]: Save and back";
            Helpers.drawTextStd(TCat.BotHelp, helpText, Global.screenW * 0.5f, 210 + botOffY, Alignment.Center, fontSize: 18);

            if (!string.IsNullOrEmpty(error))
            {
                float top = Global.screenH * 0.4f;
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
                Helpers.drawTextStd(error, Global.screenW / 2, top, alignment: Alignment.Center, fontSize: 24);
                Helpers.drawTextStd(TCat.BotHelp, "Press [X] to continue", Global.screenW / 2, 20 + top, alignment: Alignment.Center, fontSize: 24);
            }
        }
    }
}