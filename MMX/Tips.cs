using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class Tips
    {
        public static List<string[]> xTipsPool = new List<string[]>()
        {
            new string[]
            {
                "Every special weapon has a weakness!",
                "For example, if someone is using Shotgun Ice",
                "use Fire Wave against them for double damage!"
            },
            new string[]
            {
                "Rolling shields can be broken with Electric Spark."
            },
            new string[]
            {
                "A camouflaged enemy with Chameleon Sting",
                "can be hit with Boomerang Cutter."
            },
            new string[]
            {
                "Storm Tornado is effective at keeping Zero away.",
            },
            new string[]
            {
                "When camoflauged with Chameleon Sting, you are invincible",
                "and can walk on Spikes and Lava."
            },
            new string[]
            {
                "The rolling shield does not protect you",
                "from melee attacks."
            },
            new string[]
            {
                "You earn scrap on each kill.",
                "As X, use scrap to upgrade your armor in the in-game menu."
            },
            new string[]
            {
                "A fully upgraded X is a force to be reckoned with.",
            },
            new string[]
            {
                "Use the boomerang cutter to retrieve out-of-reach pickups."
            },
            new string[]
            {
                "The foot armor parts are more useful on larger,",
                "more open maps and if there are a lot of Zeros."
            },
            new string[]
            {
                "Use Storm Tornado to blow foes off maps!",
            },
            new string[]
            {
                "Shotgun ice deals lots of damage if you use it",
                "when right next to an enemy and hit with all shards.",
            },
            new string[]
            {
                "Charge Chameleon Sting to switch armor or weapons",
                "without fear of being damaged as you decide.",
            },
            new string[]
            {
                "The fire wave can melt ice sleds.",
            },
            new string[]
            {
                "As X, use QCF + shoot to launch a Haudouken!",
                "It kills in 1 hit but requires all X1 armors + 3 scrap."
            },
            new string[]
            {
                "As X, input Dragon Punch + shoot to do a Shoryuken!",
                "It kills in 1 hit but requires all X2 armors + 3 scrap."
            },
            new string[]
            {
                "Use charged Chameleon Sting to scout",
                "off-screen areas safely."
            },
            new string[]
            {
                "While riding a charged Shotgun Ice sled,",
                "use moves like Giga Crush and charged T.Thunder",
                "to benefit from the mobility it provides."
            },
            new string[]
            {
                "Shoot Magnet Mines on objectives to defend",
                "them from enemy attack.",
            },
            new string[]
            {
                "Magnet Mines can be aimed up or down",
                "to plant them on floors or ceilings."
            },
            new string[]
            {
                "Charged Magnet Mines will destroy",
                "Crystal Hunter time rifts on contact."
            },
            new string[]
            {
                "A charged Bubble Splash grants unlimited",
                "jumps underwater."
            },
            new string[]
            {
                "A charged Bubble Splash lets X jump higher,",
                "both on land and in water."
            },
            new string[]
            {
                "Shoot Bubble Splash and Fire Wave in short,",
                "bursts to conserve ammo."
            },
            new string[]
            {
                "Charged Magnet Mines can absorb projectiles",
                "and grow in size, dealing more damage.",
            },
            new string[]
            {
                "Shoot your own charged Magnet Mine",
                "to soup it up and make it deal more damage!",
            },
            new string[]
            {
                "The Speed Burner and Fire Wave are",
                "ineffective underwater.",
            },
            new string[]
            {
                "Shoot teammates with Silk Shot to",
                "heal them!",
            },
            new string[]
            {
                "The Strike Chain can be aimed",
                "diagonally up or down."
            },
            new string[]
            {
                "The Strike Chain can yank a Vile",
                "straight out of his Ride Armor."
            },
            new string[]
            {
                "After grabbing a foe with charged Strike Chain",
                "quickly switch to another weapon for a combo.",
                "This works best with the X2 buster upgrade.",
            },
            new string[]
            {
                "The Shoryuken can be used to reach high spots.",
            },
            new string[]
            {
                "Spin Wheels travel further than most weapons.",
                "Use them from a safe distance to snipe foes.",
            },
            new string[]
            {
                "Charge Crystal Hunter to slow down foes.",
                "Pair this with abilities like Giga Crush",
                "or Shoryuken for devastating effect.",
            },
            new string[]
            {
                "Dash into crystalized foes to damage them.",
            },
            new string[]
            {
                "The Acid Burst has no effect underwater.",
                "Also, entering deep water will wash off acid."
            },
            new string[]
            {
                "The Acid Burst splashes corrosive acid on foes,",
                "dealing damage over time and increasing damage taken."
            },
            new string[]
            {
                "You can remove an attached Parasite Bomb",
                "by mashing buttons.",
            },
            new string[]
            {
                "The Parasite Bomb will drag players to nearby",
                "enemies, and can eject Ride Armor users",
                "out of the mech in this manner."
            },
            new string[]
            {
                "Press DOWN before firing Triad Thunder to",
                "invert the triangular projectile."
            },
            new string[]
            {
                "The charged Triad Thunder earthquake",
                "only deals damage to grounded enemies",
                "and enemies climbing walls."
            },
            new string[]
            {
                "Spinning Blade is great for hitting enemies",
                "behind you as you retreat."
            },
            new string[]
            {
                "A charged Spinning Blade can be spun with UP",
                "or DOWN keys, and can be retracted with SHOOT."
            },
            new string[]
            {
                "Charge up Ray Splasher to fire a turret-like",
                "orb that will shoot at enemies for you."
            },
            new string[]
            {
                "Shoot charged Ray Splasher orbs on objectives",
                "to defend them from enemy attack."
            },
            new string[]
            {
                "The charged Gravity Well can be aimed up or down",
                "to either reverse or intensify gravity in an area."
            },
            new string[]
            {
                "Charge Gravity Well to create no-fly zones where",
                "Hawk-type Ride Armors will be unable to fly."
            },
            new string[]
            {
                "Shoot Tornado Fang twice in sucession for the",
                "second shot to fire two additional drills."
            },
            new string[]
            {
                "As X, the Golden armor cannot be activated",
                "if an enhancement chip is being used."
            },
            new string[]
            {
                "As X, you can change the weapon slot that",
                "Hyper Charge fires from in Settings.",
            },
            new string[]
            {
                "As X, Hyper Charge will consume Special Weapons",
                "energy as well if it fires a Special Weapon slot.",
            },
            new string[]
            {
                "As Ultimate Armor X, you can aim the",
                "Nova Strike up or down."
            },
            new string[]
            {
                "As Ultimate Armor X, you can hover in midair by",
                "pressing JUMP again in the air.",
                "Hover backwards by holding JUMP and back."
            }
        };

        public static List<string[]> zeroTipsPool = new List<string[]>()
        {
            new string[]
            {
                "As Saber or K-Knuckle Zero, activate your Hyper Mode",
                "by charging up the Beam Saber.",
            },
            new string[]
            {
                "The Zero Buster can be fired by holding and",
                "releasing SHOOT for 1 scrap."
            },
            new string[]
            {
                "The Zero Buster is a great approach option."
            },
            new string[]
            {
                "As Zero, you can reflect projectiles",
                "with swings from your Z-Saber!"
            },
            new string[]
            {
                "As Zero, you can deflect projectiles",
                "and sword attacks with your guard."
            },
            new string[]
            {
                "If your sword hits another Zero's sword,",
                "they will clang and cancel each other out."
            },
            new string[]
            {
                "Ambush enemies as Zero to avoid getting focused",
                "down from range.",
            },
            new string[]
            {
                "As Zero, the Hyouretsuzan (down+spc btn in air)",
                "will freeze enemies it hits!",
            },
            new string[]
            {
                "As Zero, the Ryuenjin (up+spc btn on ground)",
                "damages enemies above you more easily.",
            },
            new string[]
            {
                "Zero's Raijingeki (spc btn on ground) deals",
                "massive damage, but is slow and leaves you open.",
            },
            new string[]
            {
                "Press WeaponL or WeaponR to guard as Zero",
                "and take reduced damage and knockback."
            },
            new string[]
            {
                "As Zero, your Rakuhouha (down+spc btn on ground)",
                "runs on ammo that fills as you take damage.",
            },
            new string[]
            {
                "As Zero, use your guard to fill up your",
                "Rakuhouha meter faster."
            },
            new string[]
            {
                "As Zero, you can pick up ammo capsules to",
                "fill your Rakuhouha meter faster."
            },
            new string[]
            {
                "Zero works best on small and closed maps."
            },
            new string[]
            {
                "As Zero, the Raijingeki (spc btn on ground)",
                "deals more damage to foes that are closer to you.",
            },
            new string[]
            {
                "Zero's Hyouretsuzan (down+spc btn in air)",
                "does not freeze for as long as Shotgun Ice.",
            },
            new string[]
            {
                "Zero's Rakuhouha (down+spc btn on ground) makes you",
                "invincible while it's being used.",
            },
        };

        public static List<string[]> vileTipsPool = new List<string[]>()
        {
            new string[]
            {
                "As Vile, press DOWN in the mech to hide in it,",
                "which will protect Vile's life bar.",
            },
            new string[]
            {
                "As Vile, you can be hit in your mech if foes",
                "aim at your head."
            },
            new string[]
            {
                "As Vile, your stun cannon can be aimed upward",
                "by pressing UP while shooting it."
            },
            new string[]
            {
                "As Vile, you can revive yourself as Vile MK-II",
                "after death with SPECIAL btn."
            },
            new string[]
            {
                "As Vile, the Kangaroo (K-type) Ride Armor deals",
                "more damage but punches slower."
            },
            new string[]
            {
                "As Vile, the Frog (F-type) Ride Armor can swim",
                "underwater by holding the JUMP btn."
            },
            new string[]
            {
                "As Vile, the Goliath Ride Armor can fire energy",
                "shots with the WeaponL/WeaponR buttons."
            },
        };

        public static List<string[]> axlTipsPool = new List<string[]>()
        {
            new string[]
            {
                "As Axl, switch to DNA Core transformations",
                "in your inventory and press SHOOT to transform.",
            },
            new string[]
            {
                "As Axl, try to act like the enemy team when",
                "disguised as one of them.",
            },
            new string[]
            {
                "When transformed as Axl, switch to the rightmost",
                "weapon slot and fire it to undisguise.",
            },
            new string[]
            {
                "As Axl, the Blast Launcher damages yourself.",
                "Be careful!",
            },
            new string[]
            {
                "As Axl, the Blast Launcher deals knockback to",
                "both yourself and enemies.",
            },
            new string[]
            {
                "As Axl, press and hold JUMP button in the air",
                "to hover. Let go to stop hovering."
            },
            new string[]
            {
                "As Axl, when copying an enemy team member,",
                "you will look like their ally in their screen!"
            },
            new string[]
            {
                "With the Axl Bullets equipped, press SPECIAL btn",
                "to shoot Copy Shots and gain enemy transformations",
                "in your inventory on successful kills."
            },
            new string[]
            {
                "As Axl, you can rocket or grenade jump from the",
                "blast knockback of your own explosives.",
            },
            new string[]
            {
                "As Axl, when transforming, your ammo is",
                "fully replenished."
            },
            new string[]
            {
                "You can change between 2 different aim",
                "modes for Axl in Settings. You can even",
                "do this mid-match."
            },
            new string[]
            {
                "As Axl, press the AIM KEY binding (default",
                "LSHIFT) to aim backwards. You can change the",
                "behavior of this key in Settings."
            },
            new string[]
            {
                "As Axl, you have a separate crouch binding",
                "that you can set in Controls to prevent",
                "conflict with AIMDOWN."
            },
            new string[]
            {
                "As Axl, use Dodge Roll when on fire to",
                "put it out faster."
            },
            new string[]
            {
                "As Axl, activate the White Armor with 10 scrap",
                "by charging up the Copy Shot.",
            },
            new string[]
            {
                "As Axl, press MENU SECONDARY (default C on ",
                "keyboard) when in the weapon menu to change",
                "a weapon's alt fire (SPECIAL btn) behavior."
            },
            new string[]
            {
                "As Axl, when disguised press SPECIAL button",
                "on the Assassination weapon slot to fire a very",
                "effective quick assassination shot for 2 scrap."
            },
            new string[]
            {
                "As Axl, when disguised press SPECIAL button",
                "on the Undisguise slot to be able to keep",
                "the transformation, at a cost of 2 scrap."
            },
            new string[]
            {
                "As Axl, change the alt fire of special weapons",
                "with WeaponL/WeaponR keys in the Axl weapon menu.",
                "Different alt fires have different effects."
            },
            new string[]
            {
                "As Axl, you can aim backwards in Directional aim",
                "mode (the default aim mode) by pressing the",
                "\"aim\" key (default SHIFT on keyboard).",
            },
            new string[]
            {
                "As Axl, when disguised as Zero press DOWN",
                "and WEAPON L / WEAPON R to guard.",
            },
        };

        public static List<string[]> sigmaTipsPool = new List<string[]>()
        {
            new string[]
            {
                "As Sigma, you can aim your energy balls with",
                "the arrow keys."
            },
            new string[]
            {
                "As Sigma, press DOWN to guard and deflect",
                "projectiles and beam saber attacks."
            },
            new string[]
            {
                "As Sigma, control your Mavericks in various ways",
                "by changing the Command Mode in the loadout menu.",
                "Some are more useful in certain situations."
            },
            new string[]
            {
                "As Sigma, you can play as Mavericks directly",
                "by using the \"Tag Team\" Command Mode.",
            },
            new string[]
            {
                "As Chill Penguin, activate blizzards",
                "by pressing SPECIAL when near a ceiling."
            },
            new string[]
            {
                "As Chill Penguin, destroy your own statues",
                "by holding TAUNT and pressing UP or DOWN, or",
                "LEFT / RIGHT to break left / right statue."
            },
            new string[]
            {
                "As Spark Mandrill, climb ceilings",
                "by pressing UP when near a ceiling."
            },
            new string[]
            {
                "As Armored Armadillo, hold JUMP when hitting",
                "a wall when using Rolling Shield to gain height.",
            },
            new string[]
            {
                "As Launch Octopus, the whirpool attack can only",
                "be used a minimum distance above the ground."
            },
            new string[]
            {
                "As Launch Octopus, press SHOOT twice in rapid",
                "succession to quickly fire another barrage."
            },
            new string[]
            {
                "As Boomer Kuwanger, you can teleport",
                "straight through thin walls."
            },
            new string[]
            {
                "As Storm Eagle, press UP when diving to",
                "ascend back up into the air."
            },
            new string[]
            {
                "As Flame Mammoth, your Jump Press deals",
                "more damage the longer your fall."
            },
            new string[]
            {
                "As Flame Mammoth, burn your own oil spills",
                "to create large and dangerous fires."
            },
            new string[]
            {
                "As Flame Mammoth, splash oil on foes",
                "to make them take more fire damage."
            },
            new string[]
            {
                "Wolf Sigma cannot be activated in certain spots,",
                "such as too close to a map boundary or next to",
                "another Wolf Sigma."
            },
            new string[]
            {
                "Chill Penguin is highly effective underwater",
                "as his weakness, the Fire Wave, does not work",
                "in that environment."
            },
            new string[]
            {
                "As Summoner or Striker Sigma, you can change",
                "the first attack the summoned Maverick does",
                "by pressing SPECIAL or ATTACK+SPECIAL."
            },
            new string[]
            {
                "As Viral Sigma, possess foes by holding JUMP",
                "when near them to take control of them!",
                "Note: ATTACK and SPECIAL will be disabled.",
            },
            new string[]
            {
                "As Viral Sigma, when possessing an enemy, you",
                "can stop possessing by pressing ATTACK or",
                "SPECIAL, to save yourself if they fall in a pit.",
            },
            new string[]
            {
                "As Viral Sigma, when possessing an enemy,",
                "have them run into your own Mechaniloids",
                "for maximum damage!",
            },
        };

        public static List<string[]> tipsPool = new List<string[]>()
        {
            new string[]
            {
                "When frozen, mash buttons to break out faster!",
            },
            new string[]
            {
                "Homing torpedos, both uncharged and charged",
                "can be destroyed by hitting them with most weapons.",
            },
            new string[]
            {
                "You can double-jump as Zero. Use this to reach spots",
                "inaccessible to Mega Man X."
            },
            new string[]
            {
                "Use the mid-air dash to avoid Storm Tornado salvos."
            },
            new string[]
            {
                "A Rolling Shield protective barrier can be overwhelmed",
                "with mass fire. Every hit will deplete its health."
            },
            new string[]
            {
                "Press 1-9 to switch weapons more quickly.",
                "If you have these keys bound already, it won't work."
            },
            new string[]
            {
                "You can switch weapons or characters any time",
                "in a match. Press the menu button",
                "(default: ESCAPE) to do so.",
            },
            new string[]
            {
                "Press Down to crouch and reduce your profile.",
                "You can shoot or attack when crouched.",
            },
            new string[]
            {
                "The further you are behind, the more scrap you earn.",
            },
            new string[]
            {
                "After respawning, for 2 seconds you cannot attack",
                "or be attacked.",
            },
            new string[]
            {
                "Freeze enemies over pits, lava or spikes",
                "for a quick kill!",
            },
            new string[]
            {
                "Most weapons have a limited range and won't go",
                "past one or two screens.",
            },
            new string[]
            {
                "You cannot be frozen while guarding or using",
                "a charged up rolling shield barrier."
            },
            new string[]
            {
                "In a Ride Armor, jump on enemies to damage them!",
            },
            new string[]
            {
                "You can eject from a Ride Armor to reach high spots.",
            },
            new string[]
            {
                "If a \"teammate\" is attacking you or your team,",
                "it's a disguised Axl. Attack him back!"
            },
            new string[]
            {
                "In CTF, hold the DASH key to drop the flag."
            },
        };

        public static List<string[]> raceTipsPool = new List<string[]>()
        {
            new string[]
            {
                "In RACE mode, you can push the camera left or right",
                "in a Ride Chaser by holding WEAPON L or WEAPON R.",
                "Use this to see further ahead or behind."
            },
            new string[]
            {
                "In RACE mode, you cannot exit a Ride Chaser",
                "once you have entered it."
            },
            new string[]
            {
                "In RACE mode, you can damage other Ride Chasers",
                "with your Ride Chaser gun by holding SHOOT."
            },
        };

        public static string[] getRandomTip(int charNum)
        {
            var tipsPool = new List<string[]>(Tips.tipsPool);
            if (Global.level.isRace()) tipsPool = Tips.raceTipsPool;
            else if (charNum == 0) tipsPool.AddRange(Tips.xTipsPool);
            else if (charNum == 1) tipsPool.AddRange(Tips.zeroTipsPool);
            else if (charNum == 2) tipsPool.AddRange(Tips.vileTipsPool);
            else if (charNum == 3) tipsPool.AddRange(Tips.axlTipsPool);
            else if (charNum == 4) tipsPool.AddRange(Tips.sigmaTipsPool);
            return tipsPool.GetRandomItem();
        }
    }
}
