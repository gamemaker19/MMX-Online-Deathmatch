using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum NetCharBoolStateNum
    {
        One,
        Two
    }

    public class NetCharBoolState
    {
        private Character character;
        private int byteIndex;
        private Func<Character, bool> getBSValue;
        public NetCharBoolStateNum netCharStateNum;

        public NetCharBoolState(Character character, int byteIndex, NetCharBoolStateNum netCharStateNum, Func<Character, bool> getBSValue)
        {
            this.character = character;
            this.byteIndex = byteIndex;
            this.getBSValue = getBSValue;
            this.netCharStateNum = netCharStateNum;
        }

        public bool getValue()
        {
            if (character.ownedByLocalPlayer)
            {
                return getBSValue(character);
            }
            if (netCharStateNum == NetCharBoolStateNum.One)
            {
                return Helpers.getByteValue(character.netCharState1, byteIndex);
            }
            else
            {
                return Helpers.getByteValue(character.netCharState2, byteIndex);
            }
        }

        public void updateValue()
        {
            if (netCharStateNum == NetCharBoolStateNum.One)
            {
                Helpers.setByteValue(ref character.netCharState1, byteIndex, getValue());
            }
            else
            {
                Helpers.setByteValue(ref character.netCharState2, byteIndex, getValue());
            }
        }
    }

    public partial class Character
    {
        // NET CHAR STATE 1 SECTION
        public byte netCharState1;

        public NetCharBoolState isFrozenCastleActiveBS;
        public NetCharBoolState isStrikeChainHookedBS;
        public NetCharBoolState shouldDrawArmBS;
        public NetCharBoolState isAwakenedZeroBS;
        public NetCharBoolState isAwakenedGenmuZeroBS;
        public NetCharBoolState isInvisibleBS;
        public NetCharBoolState isHyperXBS;
        public NetCharBoolState isHyperSigmaBS;

        public void initNetCharState1()
        {
            isFrozenCastleActiveBS = new NetCharBoolState(this, 0, NetCharBoolStateNum.One, (character) => { return character.hasFrozenCastleBarrier(); });
            isStrikeChainHookedBS = new NetCharBoolState(this, 1, NetCharBoolStateNum.One, (character) => { return character.charState is StrikeChainHooked; });
            shouldDrawArmBS = new NetCharBoolState(this, 2, NetCharBoolStateNum.One,(character) => { return character.shouldDrawArm(); });
            isAwakenedZeroBS = new NetCharBoolState(this, 3, NetCharBoolStateNum.One,(character) => { return character.isAwakenedZero(); });
            isAwakenedGenmuZeroBS = new NetCharBoolState(this, 4, NetCharBoolStateNum.One,(character) => { return character.isAwakenedGenmuZero(); });
            isInvisibleBS = new NetCharBoolState(this, 5, NetCharBoolStateNum.One,(character) => { return character.isInvisible(); });
            isHyperXBS = new NetCharBoolState(this, 6, NetCharBoolStateNum.One,(character) => { return character.isHyperX; });
            isHyperSigmaBS = new NetCharBoolState(this, 7, NetCharBoolStateNum.One,(character) => { return character.isHyperSigma; });
        }

        public byte updateAndGetNetCharState1()
        {
            isFrozenCastleActiveBS.updateValue();
            isStrikeChainHookedBS.updateValue();
            shouldDrawArmBS.updateValue();
            isAwakenedZeroBS.updateValue();
            isAwakenedGenmuZeroBS.updateValue();
            isInvisibleBS.updateValue();
            isHyperXBS.updateValue();
            isHyperSigmaBS.updateValue();
            return netCharState1;
        }

        // NET CHAR STATE 2 SECTION
        public byte netCharState2;

        public NetCharBoolState isHyperChargeActiveBS;
        public NetCharBoolState isSpeedDevilActiveBS;
        public NetCharBoolState isInvulnBS;
        public NetCharBoolState hasUltimateArmorBS;
        public NetCharBoolState isDefenderFavoredBS;
        public NetCharBoolState hasSubtankCapacityBS;
        public NetCharBoolState isNightmareZeroBS;
        public NetCharBoolState isDarkHoldBS;

        public void initNetCharState2()
        {
            isHyperChargeActiveBS = new NetCharBoolState(this, 0, NetCharBoolStateNum.Two, (character) => { return character.player.showHyperBusterCharge(); });
            isSpeedDevilActiveBS = new NetCharBoolState(this, 1, NetCharBoolStateNum.Two, (character) => { return character.player.speedDevil; });
            isInvulnBS = new NetCharBoolState(this, 2, NetCharBoolStateNum.Two, (character) => { return character.invulnTime > 0; });
            hasUltimateArmorBS = new NetCharBoolState(this, 3, NetCharBoolStateNum.Two, (character) => { return character.player.hasUltimateArmor(); });
            isDefenderFavoredBS = new NetCharBoolState(this, 4, NetCharBoolStateNum.Two, (character) => { return character.player.isDefenderFavored; });
            hasSubtankCapacityBS = new NetCharBoolState(this, 5, NetCharBoolStateNum.Two, (character) => { return character.player.hasSubtankCapacity(); });
            isNightmareZeroBS = new NetCharBoolState(this, 6, NetCharBoolStateNum.Two, (character) => { return character.isNightmareZero; });
            isDarkHoldBS = new NetCharBoolState(this, 7, NetCharBoolStateNum.Two, (character) => { return character.charState is DarkHoldState; });
        }

        public byte updateAndGetNetCharState2()
        {
            isHyperChargeActiveBS.updateValue();
            isSpeedDevilActiveBS.updateValue();
            isInvulnBS.updateValue();
            hasUltimateArmorBS.updateValue();
            isDefenderFavoredBS.updateValue();
            hasSubtankCapacityBS.updateValue();
            isNightmareZeroBS.updateValue();
            isDarkHoldBS.updateValue();
            return netCharState2;
        }
    }
}
