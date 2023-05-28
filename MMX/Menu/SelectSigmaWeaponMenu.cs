using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class SigmaWeaponCursor
    {
        public int index;

        public SigmaWeaponCursor(int index)
        {
            this.index = index;
        }

        public int startOffset()
        {
            if (index < 9) return 0;
            else if (index >= 9 && index <= 17) return 9;
            else return 18;
        }

        public int numWeapons()
        {
            return 9;
        }

        public void cycleLeft()
        {
            if (index < 9) index = 18;
            else if (index >= 9 && index <= 17) index = 0;
            else if (index > 17) index = 9;
        }

        public void cycleRight()
        {
            if (index < 9) index = 9;
            else if (index >= 9 && index <= 17) index = 18;
            else if (index > 17) index = 0;
        }
    }


    public class SelectSigmaWeaponMenu : IMainMenu
    {
        public bool inGame;
        public List<SigmaWeaponCursor> cursors;
        public int selCursorIndex;
        public List<Point> weaponPositions = new List<Point>();
        public string error = "";
        public int maxRows = 1;
        public int maxCols = 9;

        public IMainMenu prevMenu;

        public SelectSigmaWeaponMenu(IMainMenu prevMenu, bool inGame)
        {
            this.prevMenu = prevMenu;
            for (int i = 0; i < 9; i++)
            {
                weaponPositions.Add(new Point(80, 42 + (i * 18)));
            }

            this.inGame = inGame;

            cursors = new List<SigmaWeaponCursor>();
            cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.maverick1));
            cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.maverick2));
            cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.sigmaForm));
            cursors.Add(new SigmaWeaponCursor(Options.main.sigmaLoadout.commandMode));
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

            if (selCursorIndex < 2)
            {
                //Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, 8, playSound: true);
                if (Global.input.isPressedMenu(Control.MenuLeft))
                {
                    cursors[selCursorIndex].index--;
                    if (cursors[selCursorIndex].index == -1) cursors[selCursorIndex].index = 26;
                    else if (cursors[selCursorIndex].index == 8) cursors[selCursorIndex].index = 8;
                    else if (cursors[selCursorIndex].index == 17) cursors[selCursorIndex].index = 17;
                    Global.playSound("menu");
                }
                else if (Global.input.isPressedMenu(Control.MenuRight))
                {
                    cursors[selCursorIndex].index++;
                    if (cursors[selCursorIndex].index == 9) cursors[selCursorIndex].index = 9;
                    else if (cursors[selCursorIndex].index == 18) cursors[selCursorIndex].index = 18;
                    else if (cursors[selCursorIndex].index == 27) cursors[selCursorIndex].index = 0;
                    Global.playSound("menu");
                }
                if (Global.input.isPressedMenu(Control.WeaponLeft))
                {
                    cursors[selCursorIndex].cycleLeft();
                }
                else if (Global.input.isPressedMenu(Control.WeaponRight))
                {
                    cursors[selCursorIndex].cycleRight();
                }
            }
            else if (selCursorIndex == 2)
            {
                Helpers.menuLeftRightInc(ref cursors[2].index, 0, 2, playSound: true);
            }
            else if (selCursorIndex == 3)
            {
                Helpers.menuLeftRightInc(ref cursors[3].index, 0, 3, playSound: true);
            }

            Helpers.menuUpDown(ref selCursorIndex, 0, 3);

            bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
            bool selectPressed = Global.input.isPressedMenu(Control.MenuSelectPrimary) || (backPressed && !inGame);
            if (selectPressed)
            {
                if (cursors[0].index == cursors[1].index)
                {
                    error = "Cannot select same maverick more than once!";
                    return;
                }

                if (cursors[0].index != Options.main.sigmaLoadout.maverick1 || 
                    cursors[1].index != Options.main.sigmaLoadout.maverick2 || 
                    cursors[2].index != Options.main.sigmaLoadout.sigmaForm ||
                    cursors[3].index != Options.main.sigmaLoadout.commandMode)
                {
                    Options.main.sigmaLoadout.maverick1 = cursors[0].index;
                    Options.main.sigmaLoadout.maverick2 = cursors[1].index;
                    Options.main.sigmaLoadout.sigmaForm = cursors[2].index;
                    Options.main.sigmaLoadout.commandMode = cursors[3].index;
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

            Helpers.drawTextStd(TCat.Title, "Sigma Loadout", Global.screenW * 0.5f, 12, Alignment.Center, fontSize: 48);

            var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
            float botOffY = inGame ? 0 : -2;

            int startY = 50;
            int startX = 30;
            int startX2 = 120;
            int wepW = 18;
            int wepH = 20;

            float rightArrowPos = Global.screenW - 20;
            float leftArrowPos = startX2 - 12;

            Global.sprites["cursor"].drawToHUD(0, startX, startY + (selCursorIndex * wepH));
            for (int i = 0; i < 4; i++)
            {
                float yPos = startY - 4 + (i * wepH);

                if (i == 2)
                {
                    Helpers.drawTextStd(TCat.Option, "Sigma Form: ", 40, yPos, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
                    string form = "Commander Sigma (X1)";
                    if (cursors[i].index == 1) form = "Neo Sigma (X2)";
                    if (cursors[i].index == 2) form = "Doppler Sigma (X3)";
                    Helpers.drawTextStd(TCat.Option, form, startX2 - 6, yPos, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
                    continue;
                }

                if (i == 3)
                {
                    Helpers.drawTextStd(TCat.Option, "Command Mode: ", 40, yPos, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
                    string commandModeStr = "Summoner";
                    if (cursors[i].index == 1) commandModeStr = "Puppeteer";
                    if (cursors[i].index == 2) commandModeStr = "Striker";
                    if (cursors[i].index == 3) commandModeStr = "Tag Team";
                    Helpers.drawTextStd(TCat.Option, commandModeStr, startX2 + 8, yPos, color: Color.White, fontSize: 24, selected: selCursorIndex == i);
                    continue;
                }

                Helpers.drawTextStd(TCat.Option, "Maverick " + (i + 1).ToString(), 40, yPos, fontSize: 24, selected: selCursorIndex == i);

                if (Global.frameCount % 60 < 30)
                {
                    Helpers.drawTextStd(TCat.Option, ">", rightArrowPos, yPos - 2, Alignment.Center, fontSize: 32, selected: selCursorIndex == i);
                    Helpers.drawTextStd(TCat.Option, "<", leftArrowPos, yPos - 2, Alignment.Center, fontSize: 32, selected: selCursorIndex == i);
                }

                for (int j = 0; j < cursors[i].numWeapons(); j++)
                {
                    int jIndex = j + cursors[i].startOffset();
                    Global.sprites["hud_weapon_icon"].drawToHUD(66 + jIndex, startX2 + (j * wepW), startY + (i * wepH));
                    //Helpers.drawTextStd((j + 1).ToString(), startX2 + (j * wepW), startY + (i * wepH) + 10, Alignment.Center, fontSize: 12);
                    if (cursors[i].index == jIndex)
                    {
                        DrawWrappers.DrawRectWH(startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14, false, Helpers.DarkGreen, 1, ZIndex.HUD, false);
                    }
                    else
                    {
                        DrawWrappers.DrawRectWH(startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14, true, Helpers.FadedIconColor, 1, ZIndex.HUD, false);
                    }
                }
            }

            int wsy = 160;
            if (selCursorIndex < 2)
            {
                wsy = 150;
                int wi = cursors[selCursorIndex].index;
                int[] strongAgainstIndices = getStrongAgainstFrameIndices(wi);
                int[] weakAgainstIndices = getWeakAgainstFrameIndices(wi);

                DrawWrappers.DrawRect(25, wsy - 22, Global.screenW - 30, wsy + 30, true, new Color(0, 0, 0, 100), 0.5f, ZIndex.HUD, false, outlineColor: outlineColor);

                Helpers.drawTextStd(TCat.Title, getMaverickName(wi), startX + 56, wsy - 17, Alignment.Left);
                Helpers.drawTextStd("Strong against: ", 199, wsy, Alignment.Right, style: Text.Styles.Italic, fontSize: 30);

                Global.sprites["hud_maverick"].drawToHUD(wi, startX - 1, wsy - 18);
                for (int i = 0; i < strongAgainstIndices.Length; i++)
                {
                    if (strongAgainstIndices[i] == 0) continue;
                    Global.sprites["hud_weapon_icon"].drawToHUD(strongAgainstIndices[i], 210 + i * 18, wsy + 5);
                }
                Helpers.drawTextStd("Weak against: ", 185, wsy + 15, Alignment.Right, style: Text.Styles.Italic, fontSize: 30);
                for (int i = 0; i < weakAgainstIndices.Length; i++)
                {
                    if (weakAgainstIndices[i] == 0) continue;
                    Global.sprites["hud_weapon_icon"].drawToHUD(weakAgainstIndices[i], 210 + i * 18, wsy + 20);
                }
            }
            else if (selCursorIndex == 2)
            {
                DrawWrappers.DrawRect(25, wsy - 5 - 17, Global.screenW - 30, wsy + 30 - 17, true, new Color(0, 0, 0, 100), 0.5f, ZIndex.HUD, false, outlineColor: outlineColor);
                if (cursors[2].index == 0)
                {
                    Helpers.drawTextStd("Balanced in melee/range and offense/defense.", 40, wsy + 5 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Hyper Mode: Wolf Sigma", 40, wsy + 15 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
                if (cursors[2].index == 1)
                {
                    Helpers.drawTextStd("Highly offensive with short range.", 40, wsy + 5 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Hyper Mode: Viral Sigma", 40, wsy + 15 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
                if (cursors[2].index == 2)
                {
                    Helpers.drawTextStd("Highly defensive with ranged attacks.", 40, wsy + 5 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Hyper Mode: Kaiser Sigma", 40, wsy + 15 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
            }
            else if (selCursorIndex == 3)
            {
                DrawWrappers.DrawRect(25, wsy - 5 - 17, Global.screenW - 30, wsy + 30 - 17, true, new Color(0, 0, 0, 100), 0.5f, ZIndex.HUD, false, outlineColor: outlineColor);
                if (cursors[3].index == 0)
                {
                    Helpers.drawTextStd("Once purchased, Mavericks will attack on their own.", 40, wsy + 5 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Mavericks cost 3 scrap in this mode.", 40, wsy + 15 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
                if (cursors[3].index == 1)
                {
                    Helpers.drawTextStd("Once purchased, Mavericks can be controlled directly.", 40, wsy + 5 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Mavericks cost 3 scrap in this mode.", 40, wsy + 15 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
                if (cursors[3].index == 2)
                {
                    Helpers.drawTextStd("Mavericks will come in, do one attack, then leave.", 40, wsy + 5 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Mavericks don't cost scrap in this mode.", 40, wsy + 15 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
                if (cursors[3].index == 3)
                {
                    Helpers.drawTextStd("Once purchased, the Maverick will swap out with Sigma.", 40, wsy + 5 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Mavericks cost 5 scrap in this mode.", 40, wsy + 15 - 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
            }

            Helpers.drawTextStd(Helpers.menuControlText("Left/Right: Change Weapon/Mode"), Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
            Helpers.drawTextStd(Helpers.menuControlText("Up/Down: Change Slot"), Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 18);
            //Helpers.drawTextStd(Helpers.menuControlText("WeaponL/WeaponR: Quick cycle X1/X2/X3 weapons"), Global.screenW * 0.5f, 205, Alignment.Center, fontSize: 18);
            string helpText = Helpers.menuControlText("[Z]: Back, [X]: Confirm");
            if (!inGame) helpText = Helpers.menuControlText("[Z]: Save and back");
            Helpers.drawTextStd(helpText, Global.screenW * 0.5f, 210 + botOffY, Alignment.Center, fontSize: 18);
            if (!string.IsNullOrEmpty(error))
            {
                float top = Global.screenH * 0.4f;
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
                Helpers.drawTextStd(error, Global.screenW / 2, top, alignment: Alignment.Center, fontSize: 24);
                Helpers.drawTextStd(Helpers.controlText("Press [X] to continue"), Global.screenW / 2, 20 + top, alignment: Alignment.Center, fontSize: 24);
            }
        }

        private string getMaverickName(int wi)
        {
            if (wi == 0) return "Chill Penguin";
            if (wi == 1) return "Spark Mandrill";
            if (wi == 2) return "Armored Armadillo";
            if (wi == 3) return "Launch Octopus";
            if (wi == 4) return "Boomer Kuwanger";
            if (wi == 5) return "Sting Chameleon";
            if (wi == 6) return "Storm Eagle";
            if (wi == 7) return "Flame Mammoth";
            if (wi == 8) return "Velguarder";
            if (wi == 9) return "Wire Sponge";
            if (wi == 10) return "Wheel Gator";
            if (wi == 11) return "Bubble Crab";
            if (wi == 12) return "Flame Stag";
            if (wi == 13) return "Morph Moth";
            if (wi == 14) return "Magna Centipede";
            if (wi == 15) return "Crystal Snail";
            if (wi == 16) return "Overdrive Ostrich";
            if (wi == 17) return "Fake Zero";
            if (wi == 18) return "Blizzard Buffalo";
            if (wi == 19) return "Toxic Seahorse";
            if (wi == 20) return "Tunnel Rhino";
            if (wi == 21) return "Volt Catfish";
            if (wi == 22) return "Crush Crawfish";
            if (wi == 23) return "Neon Tiger";
            if (wi == 24) return "Gravity Beetle";
            if (wi == 25) return "Blast Hornet";
            if (wi == 26) return "Dr. Doppler";
            return "";
        }

        private int[] getWeakAgainstFrameIndices(int wi)
        {
            if (wi == 0) return new int[] { new FlameMammothWeapon(null).weaponSlotIndex, new FireWave().weaponSlotIndex };
            if (wi == 1) return new int[] { new ChillPenguinWeapon(null).weaponSlotIndex, new ShotgunIce().weaponSlotIndex };
            if (wi == 2) return new int[] { new SparkMandrillWeapon(null).weaponSlotIndex, new ElectricSpark().weaponSlotIndex };
            if (wi == 3) return new int[] { new ArmoredArmadilloWeapon(null).weaponSlotIndex, new RollingShield().weaponSlotIndex };
            if (wi == 4) return new int[] { new LaunchOctopusWeapon(null).weaponSlotIndex, new Torpedo().weaponSlotIndex };
            if (wi == 5) return new int[] { new BoomerKuwangerWeapon(null).weaponSlotIndex, new Boomerang().weaponSlotIndex };
            if (wi == 6) return new int[] { new StingChameleonWeapon(null).weaponSlotIndex, new Sting().weaponSlotIndex };
            if (wi == 7) return new int[] { new StormEagleWeapon(null).weaponSlotIndex, new Tornado().weaponSlotIndex };
            if (wi == 8) return new int[] { new ChillPenguinWeapon(null).weaponSlotIndex, new ShotgunIce().weaponSlotIndex };

            if (wi == 9) return new int[] { new OverdriveOstrichWeapon(null).weaponSlotIndex, new SonicSlicer().weaponSlotIndex };
            if (wi == 10) return new int[] { new WireSpongeWeapon(null).weaponSlotIndex, new StrikeChain().weaponSlotIndex };
            if (wi == 11) return new int[] { new WheelGatorWeapon(null).weaponSlotIndex, new SpinWheel().weaponSlotIndex };
            if (wi == 12) return new int[] { new BubbleCrabWeapon(null).weaponSlotIndex, new BubbleSplash().weaponSlotIndex };
            if (wi == 13) return new int[] { new FlameStagWeapon(null).weaponSlotIndex, new SpeedBurner(null).weaponSlotIndex };
            if (wi == 14) return new int[] { new MorphMothWeapon(null).weaponSlotIndex, new SilkShot().weaponSlotIndex };
            if (wi == 15) return new int[] { new MagnaCentipedeWeapon(null).weaponSlotIndex, new MagnetMine().weaponSlotIndex };
            if (wi == 16) return new int[] { new CrystalSnailWeapon(null).weaponSlotIndex, new CrystalHunter().weaponSlotIndex };
            if (wi == 17) return new int[] { new FlameStagWeapon(null).weaponSlotIndex, new SpeedBurner(null).weaponSlotIndex };

            if (wi == 18) return new int[] { new BlastHornetWeapon(null).weaponSlotIndex, new ParasiticBomb().weaponSlotIndex };
            if (wi == 19) return new int[] { new BlizzardBuffaloWeapon(null).weaponSlotIndex, new FrostShield().weaponSlotIndex };
            if (wi == 20) return new int[] { new ToxicSeahorseWeapon(null).weaponSlotIndex, new AcidBurst().weaponSlotIndex };
            if (wi == 21) return new int[] { new TunnelRhinoWeapon(null).weaponSlotIndex, new TunnelFang().weaponSlotIndex };
            if (wi == 22) return new int[] { new VoltCatfishWeapon(null).weaponSlotIndex, new TriadThunder().weaponSlotIndex };
            if (wi == 23) return new int[] { new CrushCrawfishWeapon(null).weaponSlotIndex, new SpinningBlade().weaponSlotIndex };
            if (wi == 24) return new int[] { new NeonTigerWeapon(null).weaponSlotIndex, new RaySplasher().weaponSlotIndex };
            if (wi == 25) return new int[] { new GravityBeetleWeapon(null).weaponSlotIndex, new GravityWell().weaponSlotIndex };
            if (wi == 26) return new int[] { new ToxicSeahorseWeapon(null).weaponSlotIndex, new AcidBurst().weaponSlotIndex };
            return new int[] { 0 };
        }

        private int[] getStrongAgainstFrameIndices(int wi)
        {
            if (wi == 0) return new int[] { new SparkMandrillWeapon(null).weaponSlotIndex, new ElectricSpark().weaponSlotIndex, new VelguarderWeapon(null).weaponSlotIndex };
            if (wi == 1) return new int[] { new ArmoredArmadilloWeapon(null).weaponSlotIndex, new RollingShield().weaponSlotIndex };
            if (wi == 2) return new int[] { new LaunchOctopusWeapon(null).weaponSlotIndex, new Torpedo().weaponSlotIndex };
            if (wi == 3) return new int[] { new BoomerKuwangerWeapon(null).weaponSlotIndex, new Boomerang().weaponSlotIndex };
            if (wi == 4) return new int[] { new StingChameleonWeapon(null).weaponSlotIndex, new Sting().weaponSlotIndex };
            if (wi == 5) return new int[] { new StormEagleWeapon(null).weaponSlotIndex, new Tornado().weaponSlotIndex };
            if (wi == 6) return new int[] { new FlameMammothWeapon(null).weaponSlotIndex, new FireWave().weaponSlotIndex };
            if (wi == 7) return new int[] { new ChillPenguinWeapon(null).weaponSlotIndex, new ShotgunIce().weaponSlotIndex };

            if (wi == 9) return new int[] { new WheelGatorWeapon(null).weaponSlotIndex, new SpinWheel().weaponSlotIndex };
            if (wi == 10) return new int[] { new BubbleCrabWeapon(null).weaponSlotIndex, new BubbleSplash().weaponSlotIndex };
            if (wi == 11) return new int[] { new FlameStagWeapon(null).weaponSlotIndex, new SpeedBurner(null).weaponSlotIndex };
            if (wi == 12) return new int[] { new MorphMothWeapon(null).weaponSlotIndex, new SilkShot().weaponSlotIndex, new FakeZeroWeapon(null).weaponSlotIndex };
            if (wi == 13) return new int[] { new MagnaCentipedeWeapon(null).weaponSlotIndex, new MagnetMine().weaponSlotIndex };
            if (wi == 14) return new int[] { new CrystalSnailWeapon(null).weaponSlotIndex, new CrystalHunter().weaponSlotIndex };
            if (wi == 15) return new int[] { new OverdriveOstrichWeapon(null).weaponSlotIndex, new SonicSlicer().weaponSlotIndex };
            if (wi == 16) return new int[] { new WireSpongeWeapon(null).weaponSlotIndex, new StrikeChain().weaponSlotIndex };

            if (wi == 18) return new int[] { new ToxicSeahorseWeapon(null).weaponSlotIndex, new AcidBurst().weaponSlotIndex };
            if (wi == 19) return new int[] { new TunnelRhinoWeapon(null).weaponSlotIndex, new TunnelFang().weaponSlotIndex, new DrDopplerWeapon(null).weaponSlotIndex };
            if (wi == 20) return new int[] { new VoltCatfishWeapon(null).weaponSlotIndex, new TriadThunder().weaponSlotIndex };
            if (wi == 21) return new int[] { new CrushCrawfishWeapon(null).weaponSlotIndex, new SpinningBlade().weaponSlotIndex };
            if (wi == 22) return new int[] { new NeonTigerWeapon(null).weaponSlotIndex, new RaySplasher().weaponSlotIndex };
            if (wi == 23) return new int[] { new GravityBeetleWeapon(null).weaponSlotIndex, new GravityWell().weaponSlotIndex };
            if (wi == 24) return new int[] { new BlastHornetWeapon(null).weaponSlotIndex, new ParasiticBomb().weaponSlotIndex };
            if (wi == 25) return new int[] { new BlizzardBuffaloWeapon(null).weaponSlotIndex, new FrostShield().weaponSlotIndex };
            return new int[] { 0 };
        }
    }
}
