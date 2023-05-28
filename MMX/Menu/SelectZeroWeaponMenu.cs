using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class SelectZeroWeaponMenu : IMainMenu
    {
        public List<WeaponCursor> cursors;
        public int selCursorIndex;
        public bool inGame;
        public string error = "";

        public static List<Weapon> meleeWeapons = new List<Weapon>()
        {
            new ZSaber(null),
            new KKnuckleWeapon(null),
            new ZeroBuster(),
        };

        public static List<Weapon> groundSpecialWeapons = new List<Weapon>()
        {
            new RaijingekiWeapon(null),
            new SuiretsusenWeapon(null),
            new TBreakerWeapon(null)
        };

        public static List<Weapon> airSpecialWeapons = new List<Weapon>()
        {
            new KuuenbuWeapon(null),
            new FSplasherWeapon(null),
            new HyorogaWeapon(null)
        };

        public static List<Weapon> uppercutWeapons = new List<Weapon>()
        {
            new RyuenjinWeapon(null),
            new EBladeWeapon(null),
            new RisingWeapon(null)
        };

        public static List<Weapon> downThrustWeapons = new List<Weapon>()
        {
            new HyouretsuzanWeapon(null),
            new RakukojinWeapon(null),
            new QuakeBlazerWeapon(null)
        };

        public static List<Weapon> gigaAttackWeapons = new List<Weapon>()
        {
            new RakuhouhaWeapon(null),
            new CFlasher(null),
            new RekkohaWeapon(null)
        };

        public static List<Tuple<string, List<Weapon>>> zeroWeaponCategories = new List<Tuple<string, List<Weapon>>>()
        {
            Tuple.Create("Ground Atk", meleeWeapons),
            Tuple.Create("Ground Spc", groundSpecialWeapons),
            Tuple.Create("Air Spc", airSpecialWeapons),
            Tuple.Create("Uppercut(Spc)", uppercutWeapons),
            Tuple.Create("Uppercut(Atk)", uppercutWeapons),
            Tuple.Create("Down thrust(Spc)", downThrustWeapons),
            Tuple.Create("Down thrust(Atk)", downThrustWeapons),
            Tuple.Create("Giga attack", gigaAttackWeapons),
        };

        public IMainMenu prevMenu;

        public SelectZeroWeaponMenu(IMainMenu prevMenu, bool inGame)
        {
            this.prevMenu = prevMenu;
            this.inGame = inGame;

            cursors = new List<WeaponCursor>();

            cursors.Add(new WeaponCursor(zeroWeaponCategories[0].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.melee)));
            cursors.Add(new WeaponCursor(zeroWeaponCategories[1].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.groundSpecial)));
            cursors.Add(new WeaponCursor(zeroWeaponCategories[2].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.airSpecial)));
            cursors.Add(new WeaponCursor(zeroWeaponCategories[3].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.uppercutS)));
            cursors.Add(new WeaponCursor(zeroWeaponCategories[4].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.uppercutA)));
            cursors.Add(new WeaponCursor(zeroWeaponCategories[5].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.downThrustS)));
            cursors.Add(new WeaponCursor(zeroWeaponCategories[6].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.downThrustA)));
            cursors.Add(new WeaponCursor(zeroWeaponCategories[7].Item2.FindIndex(w => w.type == Options.main.zeroLoadout.gigaAttack)));
            cursors.Add(new WeaponCursor(Options.main.zeroLoadout.hyperMode));
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

            int maxCatCount = 3;
            if (selCursorIndex < 8)
            {
                maxCatCount = zeroWeaponCategories[selCursorIndex].Item2.Count;
            }

            if (!isIndexDisabled(selCursorIndex))
            {
                Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, maxCatCount - 1, wrap: true, playSound: true);
            }

            Helpers.menuUpDown(ref selCursorIndex, 0, 8);

            bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
            bool selectPressed = Global.input.isPressedMenu(Control.MenuSelectPrimary) || (backPressed && !inGame);
            if (selectPressed)
            {
                if (duplicateTechniques())
                {
                    error = "Cannot select same technique in two slots!";
                    return;
                }

                int[] oldArray = { Options.main.zeroLoadout.uppercutS, Options.main.zeroLoadout.uppercutA, Options.main.zeroLoadout.downThrustS, Options.main.zeroLoadout.downThrustA, Options.main.zeroLoadout.gigaAttack, Options.main.zeroLoadout.hyperMode, Options.main.zeroLoadout.melee, Options.main.zeroLoadout.groundSpecial, Options.main.zeroLoadout.airSpecial };

                Options.main.zeroLoadout.melee = zeroWeaponCategories[0].Item2[cursors[0].index].type;
                Options.main.zeroLoadout.groundSpecial = zeroWeaponCategories[1].Item2[cursors[1].index].type;
                Options.main.zeroLoadout.airSpecial = zeroWeaponCategories[2].Item2[cursors[2].index].type;
                Options.main.zeroLoadout.uppercutS = zeroWeaponCategories[3].Item2[cursors[3].index].type;
                Options.main.zeroLoadout.uppercutA = zeroWeaponCategories[4].Item2[cursors[4].index].type;
                Options.main.zeroLoadout.downThrustS = zeroWeaponCategories[5].Item2[cursors[5].index].type;
                Options.main.zeroLoadout.downThrustA = zeroWeaponCategories[6].Item2[cursors[6].index].type;
                Options.main.zeroLoadout.gigaAttack = zeroWeaponCategories[7].Item2[cursors[7].index].type;
                Options.main.zeroLoadout.hyperMode = cursors[8].index;
                int[] newArray = { Options.main.zeroLoadout.uppercutS, Options.main.zeroLoadout.uppercutA, Options.main.zeroLoadout.downThrustS, Options.main.zeroLoadout.downThrustA, Options.main.zeroLoadout.gigaAttack, Options.main.zeroLoadout.hyperMode, Options.main.zeroLoadout.melee, Options.main.zeroLoadout.groundSpecial, Options.main.zeroLoadout.airSpecial };

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

        public bool duplicateTechniques()
        {
            return zeroWeaponCategories[3].Item2[cursors[3].index].type == zeroWeaponCategories[4].Item2[cursors[4].index].type ||
                zeroWeaponCategories[5].Item2[cursors[5].index].type == zeroWeaponCategories[6].Item2[cursors[6].index].type;
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

            Helpers.drawTextStd(TCat.Title, "Zero Loadout", Global.screenW * 0.5f, 12, Alignment.Center, fontSize: 48);
            var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
            float botOffY = inGame ? 0 : -2;

            int startY = 40;
            int startX = 30;
            int wepH = 15;

            float wepPosX = 155;
            float wepTextX = 167;

            Global.sprites["cursor"].drawToHUD(0, startX, startY + (selCursorIndex * wepH) - 2);
            Color color;
            float alpha;
            for (int i = 0; i < 8; i++)
            {
                color = isIndexDisabled(i) ? Helpers.Gray : Color.White;
                alpha = isIndexDisabled(i) ? 0.5f : 1f;
                float yPos = startY - 6 + (i * wepH);
                Helpers.drawTextStd(TCat.Option, zeroWeaponCategories[i].Item1 + ": ", 40, yPos, color: color, fontSize: 24, selected: selCursorIndex == i);
                var weapon = zeroWeaponCategories[i].Item2[cursors[i].index];
                Helpers.drawTextStd(TCat.Option, weapon.displayName, wepTextX, yPos, color: color, fontSize: 24, selected: selCursorIndex == i);
                Global.sprites["hud_killfeed_weapon"].drawToHUD(weapon.killFeedIndex, wepPosX, yPos + 3, alpha);
            }

            color = isIndexDisabled(8) ? Helpers.Gray : Color.White;
            alpha = isIndexDisabled(8) ? 0.5f : 1f;

            float hyperModeYPos = startY - 6 + (wepH * 8);
            Helpers.drawTextStd(TCat.Option, "Hyper Mode:", 40, hyperModeYPos, color: color, fontSize: 24, selected: selCursorIndex == 8);
            if (cursors[8].index == 0)
            {
                Helpers.drawTextStd(TCat.Option, "Black Zero", wepTextX, hyperModeYPos, color: color, fontSize: 24, selected: selCursorIndex == 8);
                Global.sprites["hud_killfeed_weapon"].drawToHUD(122, wepPosX, hyperModeYPos + 3, alpha);
            }
            else if (cursors[8].index == 1)
            {
                Helpers.drawTextStd(TCat.Option, "Awakened Zero", wepTextX, hyperModeYPos, color: color, fontSize: 24, selected: selCursorIndex == 8);
                Global.sprites["hud_killfeed_weapon"].drawToHUD(87, wepPosX, hyperModeYPos + 3, alpha);
            }
            else if (cursors[8].index == 2)
            {
                Helpers.drawTextStd(TCat.Option, "Nightmare Zero", wepTextX, hyperModeYPos, color: color, fontSize: 24, selected: selCursorIndex == 8);
                Global.sprites["hud_killfeed_weapon"].drawToHUD(173, wepPosX, hyperModeYPos + 3, alpha);
            }

            int wsy = 167;
            DrawWrappers.DrawRect(25, wsy + 2, Global.screenW - 30, wsy + 28, true, new Color(0, 0, 0, 100), 0.5f, ZIndex.HUD, false, outlineColor: outlineColor);

            if (selCursorIndex < 8)
            {
                var wep = zeroWeaponCategories[selCursorIndex].Item2[cursors[selCursorIndex].index];
                if (wep.description?.Length == 1) Helpers.drawTextStd(wep.description[0], 40, wsy + 12, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                else if (wep.description?.Length > 0) Helpers.drawTextStd(wep.description[0], 40, wsy + 8, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                if (wep.description?.Length > 1) Helpers.drawTextStd(wep.description[1], 40, wsy + 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
            }
            else
            {
                if (cursors[8].index == 0)
                {
                    Helpers.drawTextStd("This hyper form increases speed and damage.", 40, wsy + 8, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Lasts 12 seconds.", 40, wsy + 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
                else if (cursors[8].index == 1)
                {
                    Helpers.drawTextStd("This hyper form grants powerful ranged attacks.", 40, wsy + 8, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Lasts until scrap is depleted.", 40, wsy + 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
                else if (cursors[8].index == 2)
                {
                    Helpers.drawTextStd("This hyper form infects and disrupts foes on each hit.", 40, wsy + 8, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                    Helpers.drawTextStd("Lasts until death.", 40, wsy + 17, Alignment.Left, style: Text.Styles.Italic, fontSize: 18);
                }
            }

            Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Technique", Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 18);
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

        public bool isIndexDisabled(int index)
        {
            if (cursors[0].index == 1)
            {
                return index >= 1 && index < 7;
            }
            if (cursors[0].index == 2)
            {
                return index >= 1 && index < 9;
            }
            return false;
        }
    }
}
