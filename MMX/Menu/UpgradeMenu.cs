using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class UpgradeMenu : IMainMenu
    {
        public static int selectArrowPosY;
        public IMainMenu prevMenu;
        public static bool onUpgradeMenu = true;
        public int subtankScrapCost = 4;
        public List<Weapon> subtankTargets = new List<Weapon>();
        public static int subtankTargetIndex;
        public int startX = 25;
        public bool isFillingSubtank;
        public static float subtankDelay = 0;
        public const float maxSubtankDelay = 2;
        
        public List<Point> optionPositions = new List<Point>();

        public UpgradeMenu(IMainMenu prevMenu)
        {
            this.prevMenu = prevMenu;
            optionPositions.Add(new Point(startX, 60));
            optionPositions.Add(new Point(startX, 80));
            optionPositions.Add(new Point(startX, 100));
            optionPositions.Add(new Point(startX, 120));
            optionPositions.Add(new Point(startX, 140));

            if (selectArrowPosY >= Global.level.mainPlayer.subtanks.Count + 1)
            {
                selectArrowPosY = Global.level.mainPlayer.subtanks.Count;
            }
        }

        public int getMaxIndex()
        {
            var mainPlayer = Global.level.mainPlayer;
            return MathF.Clamp(2 + mainPlayer.subtanks.Count, 1, getMaxSubTanks() + 1);
        }

        public int getHeartTankCost()
        {
            if (Global.level.server?.customMatchSettings?.heartTankHp == 2) return 4;
            return 2;
        }

        public int getMaxHeartTanks()
        {
            return Global.level.server?.customMatchSettings?.maxHeartTanks ?? 8;
        }

        public int getMaxSubTanks()
        {
            return Global.level.server?.customMatchSettings?.maxSubTanks ?? 4;
        }

        public Player mainPlayer
        {
            get { return Global.level.mainPlayer; }
        }

        public bool canUseSubtankInMenu(bool canUseSubtank)
        {
            if (!canUseSubtank) return false;
            return subtankDelay == 0;
        }

        public void update()
        {
            if (UpgradeArmorMenu.updateHyperArmorUpgrades(mainPlayer)) return;

            subtankTargets.Clear();
            if (mainPlayer.isSigma)
            {
                if (mainPlayer.isTagTeam())
                {
                    if (mainPlayer.currentMaverick != null)
                    {
                        var currentMaverickWeapon = mainPlayer.weapons.FirstOrDefault(w => w is MaverickWeapon mw && mw.maverick == mainPlayer.currentMaverick);
                        if (currentMaverickWeapon != null)
                        {
                            subtankTargets.Add(currentMaverickWeapon);
                        }
                    }
                }
                else if (!mainPlayer.isStriker())
                {
                    subtankTargets = mainPlayer.weapons.FindAll(w => (w is MaverickWeapon mw && mw.maverick != null) || w is SigmaMenuWeapon).ToList();
                }
            }
            
            if (subtankTargets.Count > 0 && selectArrowPosY >= 1)
            {
                Helpers.menuLeftRightInc(ref subtankTargetIndex, 0, subtankTargets.Count - 1);
            }

            if (!subtankTargets.InRange(subtankTargetIndex)) subtankTargetIndex = 0;

            if (Global.input.isPressedMenu(Control.MenuLeft))
            {
                if (mainPlayer.realCharNum == 0)
                {
                    if (mainPlayer.canUpgradeXArmor())
                    {
                        UpgradeArmorMenu.xGame = 3;
                        Menu.change(new UpgradeArmorMenu(prevMenu));
                        onUpgradeMenu = false;
                        return;
                    }
                }
            }

            if (Global.input.isPressedMenu(Control.MenuRight))
            {
                if (mainPlayer.realCharNum == 0)
                {
                    if (mainPlayer.canUpgradeXArmor())
                    {
                        UpgradeArmorMenu.xGame = 1;
                        Menu.change(new UpgradeArmorMenu(prevMenu));
                        onUpgradeMenu = false;
                        return;
                    }
                }
                else if (mainPlayer.realCharNum == 2)
                {
                    Menu.change(new SelectVileArmorMenu(prevMenu));
                    onUpgradeMenu = false;
                    return;
                }
            }

            Helpers.menuUpDown(ref selectArrowPosY, 0, getMaxIndex() - 1);

            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                if (selectArrowPosY == 0)
                {
                    if (mainPlayer.heartTanks < getMaxHeartTanks() && mainPlayer.scrap >= getHeartTankCost())
                    {
                        mainPlayer.scrap -= getHeartTankCost();
                        mainPlayer.heartTanks++;
                        Global.playSound("upgrade");
                        mainPlayer.maxHealth += mainPlayer.getHeartTankModifier();
                        mainPlayer.character?.addHealth(mainPlayer.getHeartTankModifier());
                        /*
                        if (mainPlayer.isVile && mainPlayer.character?.vileStartRideArmor != null)
                        {
                            mainPlayer.character.vileStartRideArmor.addHealth(mainPlayer.getHeartTankModifier());
                        }
                        else if (mainPlayer.isSigma && mainPlayer.currentMaverick != null)
                        {
                            mainPlayer.currentMaverick.addHealth(mainPlayer.getHeartTankModifier(), false);
                            mainPlayer.currentMaverick.maxHealth += mainPlayer.getHeartTankModifier();
                        }
                        */
                    }
                }
                else if (selectArrowPosY >= 1)
                {
                    if (mainPlayer.subtanks.Count < selectArrowPosY && mainPlayer.scrap >= subtankScrapCost)
                    {
                        mainPlayer.scrap -= subtankScrapCost;
                        mainPlayer.subtanks.Add(new SubTank());
                        Global.playSound("upgrade");
                    }
                    else if (mainPlayer.subtanks.InRange(selectArrowPosY - 1))
                    {
                        bool maverickUsed = false;
                        if (subtankTargets.Count > 0)
                        {
                            var currentTarget = subtankTargets[subtankTargetIndex];
                            if (currentTarget is MaverickWeapon mw && canUseSubtankInMenu(mw.canUseSubtank(mainPlayer.subtanks[selectArrowPosY - 1])))
                            {
                                mainPlayer.subtanks[selectArrowPosY - 1].use(mw.maverick);
                                maverickUsed = true;
                            }
                        }
                        
                        if (!maverickUsed && canUseSubtankInMenu(mainPlayer.canUseSubtank(mainPlayer.subtanks[selectArrowPosY - 1])))
                        {
                            mainPlayer.subtanks[selectArrowPosY - 1].use(mainPlayer.character);
                        }
                    }
                }
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
            }
        }

        public void render()
        {
            var mainPlayer = Global.level.mainPlayer;
            var gameMode = Global.level.gameMode;

            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);

            Global.sprites["cursor"].drawToHUD(0, optionPositions[0].x - 8, optionPositions[selectArrowPosY].y + 4);

            Helpers.drawTextStd(TCat.Title, "Upgrade Menu", Global.screenW * 0.5f, 8, Alignment.Center, fontSize: 48);
            Helpers.drawTextStd("Scrap: " + mainPlayer.scrap, Global.screenW * 0.5f, 25, Alignment.Center, fontSize: 24);
            int maxHeartTanks = getMaxHeartTanks();
            for (int i = 0; i < maxHeartTanks; i++)
            {
                bool isBought = mainPlayer.heartTanks > i;
                Global.sprites["menu_hearttank"].drawToHUD(isBought ? 0 : 2, 71 + (i * 20) + ((8 - maxHeartTanks) * 10), 37);
            }

            if (Global.frameCount % 60 < 30 && mainPlayer.realCharNum == 2)
            {
                Helpers.drawTextStd(TCat.Option, ">", Global.screenW - 25, Global.halfScreenH, Alignment.Center, fontSize: 32);
                Helpers.drawTextStd(TCat.Option, "Armor", Global.screenW - 25, Global.halfScreenH + 15, Alignment.Center, fontSize: 20);
            }
            else if (Global.frameCount % 60 < 30 && mainPlayer.canUpgradeXArmor())
            {
                Helpers.drawTextStd(TCat.Option, "<", 12, Global.halfScreenH, Alignment.Center, fontSize: 32);
                Helpers.drawTextStd(TCat.Option, "X3", 12, Global.halfScreenH + 15, Alignment.Center, fontSize: 20);

                Helpers.drawTextStd(TCat.Option, ">", Global.screenW - 19, Global.halfScreenH, Alignment.Center, fontSize: 32);
                Helpers.drawTextStd(TCat.Option, "X1", Global.screenW - 19, Global.halfScreenH + 15, Alignment.Center, fontSize: 20);
            }

            bool soldOut = false;
            if (mainPlayer.heartTanks == getMaxHeartTanks()) soldOut = true;
            string heartTanksStr = soldOut ? "SOLD OUT" : "Buy Heart Tank";
            Global.sprites["menu_hearttank"].drawToHUD(heartTanksStr == "SOLD OUT" ? 1 : 0, optionPositions[0].x, optionPositions[0].y - 4);
            Point size = Helpers.measureTextStd(TCat.Option, heartTanksStr, fontSize: 24);
            Helpers.drawTextStd(TCat.Option, heartTanksStr, optionPositions[0].x + 20, optionPositions[0].y, fontSize: 24, color: soldOut ? Helpers.Gray : Color.White, selected: selectArrowPosY == 0);
            if (!soldOut)
            {
                string heartTankCostStr = string.Format(" ({0} scrap)", getHeartTankCost());
                Helpers.drawTextStd(heartTankCostStr, optionPositions[0].x + 20 + size.x, optionPositions[0].y, fontSize: 24, color: soldOut ? Helpers.Gray : (mainPlayer.scrap < getHeartTankCost() ? Color.Red : Color.Green));
            }

            for (int i = 0; i < getMaxSubTanks(); i++)
            {
                if (i > mainPlayer.subtanks.Count) continue;
                bool canUseSubtank = true;
                bool buyOrUse = mainPlayer.subtanks.Count < i + 1;
                string buyOrUseStr = buyOrUse ? "Buy Sub Tank" : "Use Sub Tank";
                var optionPos = optionPositions[i + 1];
                if (!buyOrUse)
                {
                    var subtank = mainPlayer.subtanks[i];
                    canUseSubtank = mainPlayer.canUseSubtank(subtank);
                    if (mainPlayer.currentMaverick != null && mainPlayer.isTagTeam())
                    {
                        canUseSubtank = mainPlayer.currentMaverickWeapon.canUseSubtank(subtank);
                    }

                    Global.sprites["menu_subtank"].drawToHUD(1, optionPos.x - 1, optionPos.y - 4);
                    Global.sprites["menu_subtank_bar"].drawToHUD(0, optionPos.x + 5, optionPos.y - 3);
                    float yPos = 14 * (subtank.health / SubTank.maxHealth);
                    DrawWrappers.DrawRect(optionPos.x + 5, optionPos.y - 3, optionPos.x + 9, optionPos.y + 11 - yPos, true, Color.Black, 1, ZIndex.HUD, isWorldPos: false);
                    
                    if (!canUseSubtankInMenu(canUseSubtank))
                    {
                        if (canUseSubtank)
                        {
                            GameMode.drawWeaponSlotCooldown(optionPos.x + 7, optionPos.y + 4, subtankDelay / maxSubtankDelay);
                            if (subtankTargets.Count == 0)
                            {
                                buyOrUseStr = "Cannot Use Sub Tank In Battle";
                            }
                        }
                        else
                        {
                            Global.sprites["menu_subtank"].drawToHUD(2, optionPos.x - 1, optionPos.y - 4, 0.5f);
                        }
                    }

                    if (selectArrowPosY == i + 1 && subtankTargets.Count > 0)
                    {
                        if (!subtankTargets.InRange(subtankTargetIndex)) subtankTargetIndex = 0;

                        var currentTarget = subtankTargets[subtankTargetIndex];
                        if (currentTarget is MaverickWeapon mw)
                        {
                            canUseSubtank = mw.canUseSubtank(subtank);
                        }
                        float targetXPos = 113;
                        if (subtankTargets.Count > 1)
                        {
                            Global.sprites["hud_weapon_icon"].drawToHUD(currentTarget.weaponSlotIndex, optionPos.x + targetXPos, optionPos.y + 4);
                            if (Global.frameCount % 60 < 30)
                            {
                                Helpers.drawTextStd("<", optionPos.x + targetXPos - 12, optionPos.y - 2, Alignment.Center, fontSize: 32);
                                Helpers.drawTextStd(">", optionPos.x + targetXPos + 12, optionPos.y - 2, Alignment.Center, fontSize: 32);
                            }
                        }
                    }
                }
                else
                {
                    Global.sprites["menu_subtank"].drawToHUD(0, optionPos.x - 1, optionPos.y - 4);
                }

                Point size2 = Helpers.measureTextStd(TCat.Default, buyOrUseStr, fontSize: 24);
                if (!buyOrUse)
                {
                    if (!canUseSubtank && subtankTargets.Count == 0) buyOrUseStr = "Cannot use Sub Tank Now";
                    Helpers.drawTextStd(TCat.Option, buyOrUseStr, optionPos.x + 20, optionPos.y, fontSize: 24, color: canUseSubtankInMenu(canUseSubtank) ? Color.White : Helpers.Gray, selected: selectArrowPosY == i + 1);
                }
                else
                {
                    Helpers.drawTextStd(TCat.Option, buyOrUseStr, optionPos.x + 20, optionPos.y, fontSize: 24, color: canUseSubtank ? Color.White : Helpers.Gray, selected: selectArrowPosY == i + 1);
                }
                if (buyOrUse) Helpers.drawTextStd($" ({subtankScrapCost} scrap)", optionPos.x + 20 + size2.x, optionPos.y, fontSize: 24, color: (mainPlayer.scrap < subtankScrapCost ? Color.Red : Color.Green));
            }

            if (subtankTargets.Count > 1 && selectArrowPosY > 0)
            {
                Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Heal Target", Global.halfScreenW, 202, Alignment.Center, fontSize: 16);
            }

            UpgradeArmorMenu.drawHyperArmorUpgrades(mainPlayer, 20);

            Helpers.drawTextStd(TCat.BotHelp, "Up/Down: Select Item", Global.halfScreenW, 208, Alignment.Center, fontSize: 16);
            Helpers.drawTextStd(TCat.BotHelp, "[X]: Buy/Use, [Z]: Back", Global.halfScreenW, 214, Alignment.Center, fontSize: 16);
        }
    }
}
