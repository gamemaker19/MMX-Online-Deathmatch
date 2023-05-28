namespace MMXOnline
{
    /*
    public class MaverickTemplate : Maverick
    {
        public Weapon meleeWeapon;
        public MaverickTemplate(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(MaverickAbbrSpecialState), new MaverickStateCooldown(false, true, 0.75f));
    
            weapon = new Weapon(WeaponIds.MaverickAbbrGeneric, 0);
            meleeWeapon = new Weapon(WeaponIds.MaverickAbbrGeneric, 0);

            awardWeaponId = WeaponIds.???;
            weakWeaponId = WeaponIds.???;
            weakMaverickWeaponId = WeaponIds.???;

            netActorCreateId = NetActorCreateId.MaverickTemplate;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();
            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new MaverickAbbrSpecialState());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new MaverickAbbrMeleeState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "maverickAbbr";
        }
    
        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new MaverickAbbrShootState(),
                new MaverickAbbrMeleeState(),
                new MaverickAbbrSpecialState(),
            };
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                playSound("???", sendRpc: true);
                new FakeZeroBusterProj(weapon, pos, xDir, player, player.getNextActorNetId(), rpc: true);
            }, null);
            if (isAI)
            {
                mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
            }
            return mshoot;
        }
    }

    public class MaverickAbbrRangedProj : Projectile
    {
        public MaverickAbbrRangedProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 250, 3, player, "maverickabbr_proj", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MaverickAbbrRanged;
            maxTime = 0.75f;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }
    }

    public class MaverickAbbrShootState : MaverickState
    {
        bool shotOnce;
        public MaverickAbbrShootState() : base("shoot", "")
        {
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                maverick.playSound("???", sendRpc: true);
                new MaverickAbbrRangedProj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), sendRpc: true);
            }
        }
    }

    public class MaverickAbbrMeleeState : MaverickState
    {
        public MaverickAbbrMeleeState() : base("melee", "")
        {
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();
        }
    }

    public class MaverickAbbrSpecialState : MaverickState
    {
        public MaverickAbbrSpecialState() : base("special", "")
        {
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();
        }
    }
    */
}
