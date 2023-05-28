using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public enum MaverickAIBehavior
    {
        Defend,
        Follow,
        Control,
        Attack
    }

    public class SavedMaverickData
    {
        public bool noArmor;
        public int cloakUses;
        public Dictionary<Type, MaverickStateCooldown> stateCooldowns;
        public float ammo;
        public SavedMaverickData(Maverick maverick)
        {
            //if (maverick is ArmoredArmadillo aa) noArmor = aa.noArmor;
            ammo = maverick.ammo;
            stateCooldowns = maverick.stateCooldowns;
        }

        public void applySavedMaverickData(Maverick maverick, bool isPuppeteer)
        {
            if (maverick == null) return;
            //if (maverick is ArmoredArmadillo aa) aa.noArmor = noArmor;
            if (isPuppeteer)
            {
                maverick.stateCooldowns = stateCooldowns;
            }
            maverick.ammo = ammo;
        }
    }

    public class Maverick : Actor, IDamagable
    {
        public float health;
        public float maxHealth;
        public float ammo = 32;
        public float maxAmmo = 32;
        private float healAmount = 0;
        public float healTime = 0;
        public float weaponHealAmount = 0;
        public float weaponHealTime = 0;
        public bool playHealSound;

        public float width;
        public float height;
        public float time;
        public const float maxWidth = 26;
        public MaverickState state;
        public Player player;
        public bool changedStateInFrame;
        public float dashSpeed = 1;
        public bool isHeavy;
        public Dictionary<Type, MaverickStateCooldown> stateCooldowns = new Dictionary<Type, MaverickStateCooldown>();
        public bool canFly;
        public bool canClimb;
        public bool canClimbWall;
        public Point? lastGroundedPos;
        public bool autoExit;
        public float autoExitTime;
        public float strikerTime;
        public int attackDir;
        public SubTank usedSubtank;
        public float netSubtankHealAmount;
        public float invulnTime = 0;

        public MaverickAIBehavior aiBehavior;
        public Actor target;
        public float aiCooldown;
        public float maxAICooldown = 1.25f;
        public string startMoveControl;

        public Weapon weapon;
        public WeaponIds awardWeaponId;
        public WeaponIds weakWeaponId;
        public WeaponIds weakMaverickWeaponId;

        private Input _input;
        public Input input
        {
            get
            {
                if (aiBehavior == MaverickAIBehavior.Control && !isPuppeteerTooFar() && maverickCanControl())
                {
                    return player.input;
                }
                return _input;
            }
        }

        public bool maverickCanControl()
        {
            if (this is StingChameleon sc && sc.isCloakTransition())
            {
                return false;
            }
            if (this is MorphMothCocoon mmc && (mmc.selfDestructTime > 0 || mmc.isBurned))
            {
                return false;
            }
            if (this is CrystalSnail cs && cs.sprite.name.EndsWith("shell_end"))
            {
                return false;
            }
            if (state != null && state.inTransition() && this is GravityBeetle)
            {
                return false;
            }
            return true;
        }

        public bool isPuppeteerTooFar()
        {
            return player.isSigma && player.isPuppeteer() && player.character != null && getCenterPos().distanceTo(player.character.pos) > Global.screenW * 1.25f;
        }

        public Maverick(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, MaverickState overrideState = null) : 
            base(null, pos, netId, ownedByLocalPlayer, true)
        {
            this.player = player;
            this.xDir = xDir;

            spriteToCollider.Add("enter", null);
            spriteToCollider.Add("exit", null);

            Rect idleRect = Global.sprites[getMaverickPrefix() + "_idle"].frames[0].rect;
            width = Math.Min(idleRect.w() - 20, maxWidth);
            height = Math.Min(idleRect.h(), Character.sigmaHeight);
            if (this is MorphMothCocoon) width = 20;
            if (this is MorphMoth) width = 18;
            if (this is BlizzardBuffalo) width = 30;

            int heightInt = (int)height;

            if (ownedByLocalPlayer)
            {
                // Sort mavericks by their height. Unless the maverick height is >= sigma height it should go above sigma
                zIndex = ZIndex.MainPlayer - (heightInt - (int)Character.sigmaHeight);
                if (zIndex == ZIndex.MainPlayer) zIndex = ZIndex.Character - 100;
            }
            else
            {
                zIndex = ZIndex.Character - (heightInt - (int)Character.sigmaHeight);
                if (zIndex == ZIndex.Character) zIndex = ZIndex.Character - 100;
            }

            useFrameProjs = true;
            maxHealth = player.getMaverickMaxHp();
            health = maxHealth;
            splashable = true;
            changeState(overrideState ?? new MEnter(destPos));
            _input = new Input(true);

            if (Global.level.gameMode.isTeamMode)
            {
                if (player.alliance == GameMode.blueAlliance)
                {
                    addRenderEffect(RenderEffectType.BlueShadow);
                }
                else
                {
                    addRenderEffect(RenderEffectType.RedShadow);
                }
            }

            Global.level.addGameObject(this);

            aiBehavior = player.currentMaverickCommand;
        }

        float ammoRechargeTime;
        public void rechargeAmmo(float amountPerSecond)
        {
            float ammoRechargeCooldown = 1 / amountPerSecond;
            ammoRechargeTime -= Global.spf;
            if (ammoRechargeTime <= 0)
            {
                ammoRechargeTime = ammoRechargeCooldown;
                ammo++;
                if (ammo > 32) ammo = 32;
            }
        }

        float ammoDrainTime;
        public void drainAmmo(float amountPerSecond)
        {
            float ammoDrainCooldown = 1 / amountPerSecond;
            ammoDrainTime -= Global.spf;
            if (ammoDrainTime <= 0)
            {
                ammoDrainTime = ammoDrainCooldown;
                ammo--;
                if (ammo < 0) ammo = 0;
            }
        }

        public void addHealth(float amount, bool fillSubtank)
        {
            if (health >= maxHealth && fillSubtank)
            {
                player.fillSubtank(amount);
            }
            healAmount += amount;
        }

        public virtual void setHealth(float lastHealth)
        {
            health = lastHealth;
        }

        public void addAmmo(float amount)
        {
            weaponHealAmount += amount;
        }

        public void deductAmmo(int v)
        {
            ammo -= v;
            if (ammo < 0) ammo = 0;
        }

        public override void update()
        {
            base.update();

            Helpers.decrementTime(ref invulnTime);

            if (grounded)
            {
                lastGroundedPos = pos;
                if (canFly)
                {
                    ammo += Global.spf * 10;
                    if (ammo > 32) ammo = 32;
                }
            }

            if (ammo >= maxAmmo)
            {
                weaponHealAmount = 0;
            }
            if (weaponHealAmount > 0 && health > 0)
            {
                weaponHealTime += Global.spf;
                if (weaponHealTime > 0.05)
                {
                    weaponHealTime = 0;
                    weaponHealAmount--;
                    ammo = Helpers.clampMax(ammo + 1, maxAmmo);
                    if (ammo >= maxAmmo)
                    {
                        weaponHealTime = 0;
                        weaponHealAmount = 0;
                    }
                    playSound("heal", forcePlay: true);
                }
            }

            time += Global.spf;

            if (health >= maxHealth)
            {
                healAmount = 0;
                usedSubtank = null;
            }
            if (healAmount > 0 && health > 0)
            {
                healTime += Global.spf;
                if (healTime > 0.05)
                {
                    healTime = 0;
                    healAmount--;
                    if (usedSubtank != null)
                    {
                        usedSubtank.health--;
                    }
                    health = Helpers.clampMax(health + 1, maxHealth);
                    if (player == Global.level.mainPlayer || playHealSound)
                    {
                        playSound("heal", forcePlay: true, sendRpc: true);
                    }
                }
            }

            if (usedSubtank != null && usedSubtank.health <= 0)
            {
                usedSubtank = null;
            }

            updateProjectileCooldown();

            foreach (var key in stateCooldowns.Keys)
            {
                Helpers.decrementTime(ref stateCooldowns[key].cooldown);
            }

            if (!ownedByLocalPlayer) return;

            if (pos.y > Global.level.killY && state is not MEnter && state is not MExit)
            {
                incPos(new Point(0, 50));
                applyDamage(null, null, Damager.envKillDamage, null);
            }

            if (autoExit)
            {
                autoExitTime += Global.spf;
                if (autoExitTime > 1 && state is not MExit)
                {
                    changeState(new MExit(pos, true));
                }
            }
            else if (aiBehavior != MaverickAIBehavior.Control)
            {
                aiUpdate();
            }

            if (player == null) return;

            if (player.isStriker())
            {
                strikerTime += Global.spf;
                if (strikerTime > 3)
                {
                    if (this is WireSponge || this is ToxicSeahorse)
                    {
                        if (state is MIdle)
                        {
                            changeState(new MExit(pos, true));
                        }
                    }
                    else if (state is not MExit)
                    {
                        changeState(new MExit(pos, true));
                    }
                }
            }

            state.update();
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.myCollider?.flag == (int)HitboxFlag.Hitbox || other.myCollider?.flag == (int)HitboxFlag.None) return;

            var killZone = other.gameObject as KillZone;
            if (killZone != null)
            {
                if (!killZone.killInvuln && this is StingChameleon sc && sc.isInvisible) return;
                if (state is not MEnter && state is not MExit)
                {
                    killZone.applyDamage(this);
                }
            }
        }

        public override Point getCenterPos()
        {
            return pos.addxy(0, -height / 2f);
        }

        public int getMaverickKillFeedIndex()
        {
            if (this is ChillPenguin) return 93;
            if (this is SparkMandrill) return 94;
            if (this is ArmoredArmadillo) return 95;
            if (this is LaunchOctopus) return 96;
            if (this is BoomerKuwanger) return 97;
            if (this is StingChameleon) return 98;
            if (this is StormEagle) return 99;
            if (this is FlameMammoth) return 100;
            if (this is Velguarder) return 101;

            if (this is WireSponge) return 141;
            if (this is WheelGator) return 142;
            if (this is BubbleCrab) return 143;
            if (this is FlameStag) return 144;
            if (this is MorphMothCocoon) return 145;
            if (this is MorphMoth) return 146;
            if (this is MagnaCentipede) return 147;
            if (this is CrystalSnail) return 148;
            if (this is OverdriveOstrich) return 149;
            if (this is FakeZero) return 150;

            if (this is BlizzardBuffalo) return 151;
            if (this is ToxicSeahorse) return 152;
            if (this is TunnelRhino) return 153;
            if (this is VoltCatfish) return 154;
            if (this is CrushCrawfish) return 155;
            if (this is NeonTiger) return 156;
            if (this is GravityBeetle) return 157;
            if (this is BlastHornet) return 158;
            if (this is DrDoppler) return 159;
            
            return 0;
        }

        public virtual void aiUpdate()
        {
            input.keyPressed.Clear();
            input.keyHeld.Clear();

            Helpers.decrementTime(ref aiCooldown);

            bool isSummonerOrStrikerDoppler = (player.isSummoner() || player.isStriker()) && this is DrDoppler;
            bool isSummonerCocoon = player.isSummoner() && this is MorphMothCocoon;
            bool isStrikerCocoon = player.isStriker() && this is MorphMothCocoon;
            var mmc = this as MorphMothCocoon;
            var doppler = this as DrDoppler;

            if (isSummonerOrStrikerDoppler)
            {
                var hit = Global.level.raycast(getCenterPos(), getCenterPos().addxy(xDir * 100, 0), new List<Type>() { typeof(Character) });
                if (hit?.gameObject is Character chr && chr.player.alliance == player.alliance)
                {
                    target = chr;
                    doppler.ballType = 1;
                }
                else
                {
                    target = Global.level.getClosestTarget(getCenterPos(), player.alliance, true, isRequesterAI: true);
                    doppler.ballType = 0;
                }
            }
            else if (!isSummonerCocoon && !isStrikerCocoon)
            {
                target = Global.level.getClosestTarget(getCenterPos(), player.alliance, true, isRequesterAI: true);
            }
            else
            {
                target = mmc.getHealTarget();
            }

            bool isAIState = (state is MIdle || state is MRun);
            if (canFly) isAIState = isAIState || state is MFly;

            if (target != null && (isAIState || state is MShoot))
            {
                turnToPos(target.getCenterPos());
            }

            bool doStartMoveControlIfNoTarget = !string.IsNullOrEmpty(startMoveControl);
            if (doStartMoveControlIfNoTarget && target == null && startMoveControl == Control.Shoot)
            {
                doStartMoveControlIfNoTarget = false;
            }

            if ((target != null || doStartMoveControlIfNoTarget) && isAIState && !player.isPuppeteer())
            {
                if (isSummonerCocoon)
                {
                    if (target != null)
                    {
                        mmc.changeState(new MorphMCSpinState());
                    }
                }
                else if (aiCooldown == 0 && isAIState)
                {
                    MaverickState mState = getRandomAttackState();
                    if (isSummonerOrStrikerDoppler && doppler.ballType == 1)
                    {
                        mState = aiAttackStates()[0];
                    }
                    else if (!string.IsNullOrEmpty(startMoveControl))
                    {
                        var aiAttackStateArray = aiAttackStates();
                        int mIndex = 0;

                        if (startMoveControl == Control.Shoot) mIndex = 0;
                        else if (startMoveControl == Control.Special1) mIndex = 1;
                        else if (startMoveControl == Control.Dash) mIndex = 2;

                        while (mIndex >= aiAttackStateArray.Length)
                        {
                            mIndex--;
                        }
                        mState = aiAttackStateArray[mIndex];

                        startMoveControl = null;
                    }

                    if (mState != null)
                    {
                        changeState(mState);
                    }
                }
            }
            else if (aiBehavior == MaverickAIBehavior.Follow && !player.isStriker())
            {
                Character chr = player.character;
                if (chr != null)
                {
                    float dist = chr.pos.x - pos.x;
                    float assignedDist = 40;

                    for (int i = 0; i < player.mavericks.Count; i++)
                    {
                        if (player.mavericks[i] == this)
                        {
                            assignedDist = 40 * (i + 1);
                        }
                    }

                    if (MathF.Abs(dist) > assignedDist)
                    {
                        if (dist < 0) press(Control.Left);
                        else press(Control.Right);

                        var jumpZones = Global.level.getTriggerList(this, 0, 0, null, typeof(JumpZone));
                        if (jumpZones.Count > 0)
                        {
                            press(Control.Jump);
                        }
                    }
                }
            }
            else if (aiBehavior == MaverickAIBehavior.Attack)
            {
                float raycastDist = (width / 2) + 5;
                var hit = Global.level.raycastAll(getCenterPos(), getCenterPos().addxy(attackDir * raycastDist, 0), new List<Type>() { typeof(Wall) });
                if (hit.Count == 0)
                {
                    if (attackDir < 0) press(Control.Left);
                    else press(Control.Right);
                }

                var jumpZones = Global.level.getTriggerList(this, 0, 0, null, typeof(JumpZone));
                if (jumpZones.Count > 0)
                {
                    press(Control.Jump);
                }
            }
        }
        
        public bool shootPressed()
        {
            return input.isPressed(Control.Shoot, player) && player.weapon is MaverickWeapon mw && mw.maverick == this;
        }

        public bool specialPressed()
        {
            return input.isPressed(Control.Special1, player) && player.weapon is MaverickWeapon mw && mw.maverick == this;
        }

        private void press(string inputMapping)
        {
            string keyboard = "keyboard";
            int? control = Control.controllerNameToMapping[keyboard].GetValueOrDefault(inputMapping);
            if (control == null) return;
            Key key = (Key)control;
            input.keyPressed[key] = !input.keyHeld.ContainsKey(key) || !input.keyHeld[key];
            input.keyHeld[key] = true;
        }

        public virtual string getMaverickPrefix()
        {
            return "";
        }

        public virtual MaverickState getRandomAttackState()
        {
            return null;
        }

        public virtual MaverickState[] aiAttackStates()
        {
            return new MaverickState[] { };
        }

        public override Collider getGlobalCollider()
        {
            var rect = new Rect(0, 0, width, height);
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getDashCollider(float widthPercent = 1f, float heightPercent = 0.75f)
        {
            var rect = new Rect(0, 0, width * widthPercent, height * heightPercent);
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public bool isAttacking()
        {
            return sprite.name.Contains("attack");
        }

        public override Dictionary<int, Func<Projectile>> getGlobalProjs()
        {
            var retProjs = new Dictionary<int, Func<Projectile>>();

            if (globalCollider != null && Global.level.is1v1() && player.maverick1v1 != null && (sprite.name.Contains("_jump") || sprite.name.Contains("_fall")))
            {
                retProjs[(int)ProjIds.MaverickContactDamage] = () =>
                {
                    Point centerPoint = globalCollider.shape.getRect().center();
                    float damage = 3;
                    int flinch = 0;
                    Projectile proj = new GenericMeleeProj(weapon, centerPoint, ProjIds.MaverickContactDamage, player, damage, flinch, 0.5f);
                    proj.globalCollider = globalCollider.clone();
                    return proj;
                };
            }

            return retProjs;
        }

        public void maxOutAllCooldowns(float maxAllowedCooldown)
        {
            foreach (var stateCooldown in stateCooldowns.Values)
            {
                if (stateCooldown.isGlobal)
                {
                    stateCooldown.cooldown = Math.Min(stateCooldown.maxCooldown, maxAllowedCooldown);
                }
            }
        }

        public void changeState(MaverickState newState, bool ignoreCooldown = false)
        {
            if (state != null && newState != null && !newState.canEnterSelf && state.GetType() == newState.GetType()) return;
            if (newState == null) return;
            if (state is MDie) return;
            if (!newState.canEnter(this)) return;

            MaverickStateCooldown oldStateCooldown = state == null ? null : stateCooldowns.GetValueOrDefault(state.GetType());
            MaverickStateCooldown newStateCooldown = stateCooldowns.GetValueOrDefault(newState.GetType());

            if (newStateCooldown != null && !ignoreCooldown)
            {
                if (newStateCooldown.cooldown > 0) return;
            }

            changedStateInFrame = true;
            newState.maverick = this;

            changeSpriteFromName(newState.sprite, true);

            var oldState = state;
            if (oldState != null)
            {
                oldState.onExit(newState);
                if (oldStateCooldown != null && !oldStateCooldown.startOnEnter)
                {
                    oldStateCooldown.cooldown = oldStateCooldown.maxCooldown;
                    if (oldStateCooldown.isGlobal)
                    {
                        maxOutAllCooldowns(oldStateCooldown.maxCooldown);
                    }
                }
            }
            state = newState;
            newState.onEnter(oldState);
            if (newStateCooldown != null && newStateCooldown.startOnEnter)
            {
                newStateCooldown.cooldown = newStateCooldown.maxCooldown;
                if (newStateCooldown.isGlobal)
                {
                    maxOutAllCooldowns(newStateCooldown.maxCooldown);
                }
            }
        }

        public void changeSpriteFromName(string spriteBaseName, bool resetFrame)
        {
            string spriteName = getMaverickPrefix() + "_" + spriteBaseName;
            if (this is BoomerKuwanger bk && bk.bald)
            {
                string newSpriteName = getMaverickPrefix() + "_bald_" + spriteBaseName;
                if (Global.sprites.ContainsKey(newSpriteName))
                {
                    spriteName = newSpriteName;
                }
            }
            else if (this is ArmoredArmadillo aa && aa.noArmor)
            {
                string newSpriteName = getMaverickPrefix() + "_na_" + spriteBaseName;
                if (Global.sprites.ContainsKey(newSpriteName))
                {
                    spriteName = newSpriteName;
                }
            }
            else if (this is MagnaCentipede ms && ms.noTail)
            {
                string newSpriteName = getMaverickPrefix() + "_notail_" + spriteBaseName;
                if (Global.sprites.ContainsKey(newSpriteName))
                {
                    spriteName = newSpriteName;
                }
            }
            else if (this is CrystalSnail cs && cs.noShell)
            {
                string newSpriteName = getMaverickPrefix() + "_noshell_" + spriteBaseName;
                if (Global.sprites.ContainsKey(newSpriteName))
                {
                    spriteName = newSpriteName;
                }
            }
            changeSprite(spriteName, resetFrame);
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (this is FakeZero fz && fz.state is FakeZeroGuardState)
            {
                ammo += damage;
                if (ammo > 32) ammo = 32;
                damage *= 0.75f;
            }

            health -= damage;

            if (owner != null && weaponIndex != null)
            {
                damageHistory.Add(new DamageEvent(owner, weaponIndex.Value, projId, false, Global.time));
            }

            if (ownedByLocalPlayer && damage > 0 && owner != null)
            {
                netOwner.delaySubtank();
                addDamageTextHelper(owner, damage, maxHealth, true);
            }

            if (health <= 0 && ownedByLocalPlayer)
            {
                health = 0;
                if (state is not MDie)
                {
                    changeState(new MDie(damage == Damager.envKillDamage));
                    int? assisterProjId = null;
                    int? assisterWeaponId = null;
                    Player killer = null;
                    Player assister = null;
                    getKillerAndAssister(player, ref killer, ref assister, ref weaponIndex, ref assisterProjId, ref assisterWeaponId);
                    creditMaverickKill(killer, assister, weaponIndex);
                }
            }
        }

        public void creditMaverickKill(Player killer, Player assister, int? weaponIndex)
        {
            if (killer != null && killer != player)
            {
                killer.addKill();
                if (Global.level.gameMode is TeamDeathMatch)
                {
                    if (Global.isHost)
                    {
                        if (player.alliance == GameMode.redAlliance) Global.level.gameMode.bluePoints++;
                        if (player.alliance == GameMode.blueAlliance) Global.level.gameMode.redPoints++;
                        Global.level.gameMode.syncTeamScores();
                    }
                }

                killer.awardScrap();
                awardXWeapon(killer);
            }

            if (assister != null && assister != player)
            {
                assister.addAssist();
                assister.addKill();
                assister.awardScrap();
                awardXWeapon(killer);
            }

            int maverickKillFeedIndex = getMaverickKillFeedIndex();
            Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(killer, assister, player, weaponIndex, maverickKillFeedIndex: maverickKillFeedIndex));

            if (ownedByLocalPlayer)
            {
                RPC.creditPlayerKillMaverick.sendRpc(killer, assister, this, weaponIndex);
            }
        }

        public void awardXWeapon(Player player)
        {
            if (player.isX && !player.isDisguisedAxl)
            {
                Weapon weaponToAdd = null;
                if (awardWeaponId == WeaponIds.ShotgunIce) weaponToAdd = new ShotgunIce();
                if (awardWeaponId == WeaponIds.ElectricSpark) weaponToAdd = new ElectricSpark();
                if (awardWeaponId == WeaponIds.RollingShield) weaponToAdd = new RollingShield();
                if (awardWeaponId == WeaponIds.Torpedo) weaponToAdd = new Torpedo();
                if (awardWeaponId == WeaponIds.Boomerang) weaponToAdd = new Boomerang();
                if (awardWeaponId == WeaponIds.Sting) weaponToAdd = new Sting();
                if (awardWeaponId == WeaponIds.Tornado) weaponToAdd = new Tornado();
                if (awardWeaponId == WeaponIds.FireWave) weaponToAdd = new FireWave();

                if (awardWeaponId == WeaponIds.CrystalHunter) weaponToAdd = new CrystalHunter();
                if (awardWeaponId == WeaponIds.BubbleSplash) weaponToAdd = new BubbleSplash();
                if (awardWeaponId == WeaponIds.SilkShot) weaponToAdd = new SilkShot();
                if (awardWeaponId == WeaponIds.SpinWheel) weaponToAdd = new SpinWheel();
                if (awardWeaponId == WeaponIds.SonicSlicer) weaponToAdd = new SonicSlicer();
                if (awardWeaponId == WeaponIds.StrikeChain) weaponToAdd = new StrikeChain();
                if (awardWeaponId == WeaponIds.MagnetMine) weaponToAdd = new MagnetMine();
                if (awardWeaponId == WeaponIds.SpeedBurner) weaponToAdd = new SpeedBurner(player);

                if (awardWeaponId == WeaponIds.AcidBurst) weaponToAdd = new AcidBurst();
                if (awardWeaponId == WeaponIds.ParasiticBomb) weaponToAdd = new ParasiticBomb();
                if (awardWeaponId == WeaponIds.TriadThunder) weaponToAdd = new TriadThunder();
                if (awardWeaponId == WeaponIds.SpinningBlade) weaponToAdd = new SpinningBlade();
                if (awardWeaponId == WeaponIds.RaySplasher) weaponToAdd = new RaySplasher();
                if (awardWeaponId == WeaponIds.GravityWell) weaponToAdd = new GravityWell();
                if (awardWeaponId == WeaponIds.FrostShield) weaponToAdd = new FrostShield();
                if (awardWeaponId == WeaponIds.TunnelFang) weaponToAdd = new TunnelFang();

                if (weaponToAdd != null)
                {
                    var matchingW = player.weapons.FirstOrDefault(w => w.index == weaponToAdd.index);
                    if (matchingW != null)
                    {
                        matchingW.ammo = matchingW.maxAmmo;
                    }
                    else if (player.weapons.Count >= 3 && player.weapons.Count < 9)
                    {
                        player.weapons.Insert(3, weaponToAdd);
                    }
                }
            }
        }

        public bool checkWeakness(WeaponIds weaponId, ProjIds projId, out MaverickState newState, bool isAttackerMaverick)
        {
            newState = null;
            if (player.maverick1v1 != null && isAttackerMaverick)
            {
                return false;
            }

            if ((weaponId == WeaponIds.FireWave || projId == ProjIds.FlameMFireball || projId == ProjIds.FlameMOilFire) && this is ChillPenguin)
            {
                newState = new ChillPBurnState();
                return true;
            }
            if ((weaponId == WeaponIds.ShotgunIce || projId == ProjIds.ChillPIceShot || projId == ProjIds.ChillPIceBlow || projId == ProjIds.ChillPIcePenguin) && this is SparkMandrill)
            {
                newState = new SparkMFrozenState();
                return true;
            }
            if ((weaponId == WeaponIds.ElectricSpark || projId == ProjIds.SparkMSpark) && this is ArmoredArmadillo aa)
            {
                if (!aa.noArmor)
                {
                    newState = new ArmoredAZappedState();
                }
                return true;
            }
            if ((weaponId == WeaponIds.RollingShield || projId == ProjIds.ArmoredARoll) && this is LaunchOctopus lo)
            {
                return true;
            }
            if ((weaponId == WeaponIds.Torpedo || projId == ProjIds.LaunchOMissle || projId == ProjIds.LaunchOTorpedo) && this is BoomerKuwanger bk)
            {
                return true;
            }
            if ((weaponId == WeaponIds.Boomerang || projId == ProjIds.BoomerKBoomerang) && this is StingChameleon sc)
            {
                if (sc.isInvisible && sc.ownedByLocalPlayer)
                {
                    sc.decloak();
                }
                return true;
            }
            if ((weaponId == WeaponIds.Sting || projId == ProjIds.StingCSting) && this is StormEagle se)
            {
                return true;
            }
            if ((weaponId == WeaponIds.Tornado || projId == ProjIds.StormETornado) && this is FlameMammoth fm)
            {
                return true;
            }
            if ((weaponId == WeaponIds.ShotgunIce || projId == ProjIds.ChillPIceShot || projId == ProjIds.ChillPIceBlow || projId == ProjIds.ChillPIcePenguin) && this is Velguarder vg)
            {
                return true;
            }

            // X2
            if ((weaponId == WeaponIds.SonicSlicer || projId == ProjIds.OverdriveOSonicSlicer || projId == ProjIds.OverdriveOSonicSlicerUp) && this is WireSponge)
            {
                return true;
            }
            if ((weaponId == WeaponIds.StrikeChain || projId == ProjIds.WSpongeChain || projId == ProjIds.WSpongeUpChain) && this is WheelGator)
            {
                return true;
            }
            if ((weaponId == WeaponIds.SpinWheel || projId == ProjIds.WheelGSpinWheel) && this is BubbleCrab)
            {
                return true;
            }
            if ((weaponId == WeaponIds.BubbleSplash || projId == ProjIds.BCrabBubbleSplash) && this is FlameStag)
            {
                return true;
            }
            if ((weaponId == WeaponIds.SpeedBurner || projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash || projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail) && this is MorphMothCocoon mmc)
            {
                if (ownedByLocalPlayer)
                {
                    newState = mmc.burn();
                }
                return true;
            }
            if ((weaponId == WeaponIds.SpeedBurner || projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash || projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail) && this is MorphMoth)
            {
                return true;
            }
            if ((weaponId == WeaponIds.SilkShot || projId == ProjIds.MorphMCScrap || projId == ProjIds.MorphMBeam) && this is MagnaCentipede ms)
            {
                if (ownedByLocalPlayer && !ms.noTail)
                {
                    ms.removeTail();
                }
                return true;
            }
            if ((weaponId == WeaponIds.MagnetMine || projId == ProjIds.MagnaCMagnetMine) && this is CrystalSnail cs)
            {
                if (ownedByLocalPlayer && !cs.noShell)
                {
                    newState = new CSnailWeaknessState(true);
                }
                return true;
            }
            if ((weaponId == WeaponIds.CrystalHunter || projId == ProjIds.CSnailCrystalHunter) && this is OverdriveOstrich oo)
            {
                newState = new OverdriveOCrystalizedState();
                return true;
            }
            if ((weaponId == WeaponIds.SpeedBurner || projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash || projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail) && this is FakeZero)
            {
                return true;
            }

            // x3
            if ((weaponId == WeaponIds.ParasiticBomb || projId == ProjIds.BHornetBee || projId == ProjIds.BHornetHomingBee) && this is BlizzardBuffalo)
            {
                return true;
            }
            if ((weaponId == WeaponIds.FrostShield || projId == ProjIds.BBuffaloIceProj || projId == ProjIds.BBuffaloIceProjGround) && this is ToxicSeahorse)
            {
                return true;
            }
            if ((weaponId == WeaponIds.AcidBurst || projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2) && this is TunnelRhino)
            {
                return true;
            }
            if ((weaponId == WeaponIds.TunnelFang || projId == ProjIds.TunnelRTornadoFang || projId == ProjIds.TunnelRTornadoFang2 || projId == ProjIds.TunnelRTornadoFangDiag) && this is VoltCatfish)
            {
                return true;
            }
            if ((weaponId == WeaponIds.TriadThunder || projId == ProjIds.VoltCBall || projId == ProjIds.VoltCTriadThunder || projId == ProjIds.VoltCUpBeam || projId == ProjIds.VoltCUpBeam2) && this is CrushCrawfish)
            {
                return true;
            }
            if ((weaponId == WeaponIds.SpinningBlade || projId == ProjIds.CrushCArmProj) && this is NeonTiger)
            {
                return true;
            }
            if ((weaponId == WeaponIds.RaySplasher || projId == ProjIds.NeonTRaySplasher) && this is GravityBeetle)
            {
                return true;
            }
            if ((weaponId == WeaponIds.GravityWell || projId == ProjIds.GBeetleGravityWell || projId == ProjIds.GBeetleBall) && this is BlastHornet)
            {
                return true;
            }
            if ((weaponId == WeaponIds.AcidBurst || projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2) && this is DrDoppler)
            {
                return true;
            }

            return false;
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            if (Global.level.isRace()) return false;

            if (this is BoomerKuwanger bk && bk.sprite.name.Contains("teleport"))
            {
                return false;
            }
            if (this is StingChameleon sc && sc.isInvisible && !Damager.isBoomerang(projId))
            {
                return false;
            }
            if (this is MorphMothCocoon mmc && mmc.selfDestructTime > 0)
            {
                return false;
            }
            if (sprite.name.EndsWith("enter") || sprite.name.EndsWith("exit"))
            {
                return false;
            }
            if (this is MagnaCentipede ms && ms.sprite.name.Contains("teleport"))
            {
                return false;
            }
            if (this is ToxicSeahorse ts && ts.sprite.name.Contains("teleport"))
            {
                return false;
            }
            if (sprite.name == "drdoppler_uncoat")
            {
                return false;
            }
            if (invulnTime > 0)
            {
                return false;
            }

            return damagerAlliance != player.alliance;
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            return sprite.name == "armoreda_charge" || sprite.name.Contains("_shell") || sprite.name.EndsWith("eat_loop");
        }

        public bool canBeHealed(int healerAlliance)
        {
            return healerAlliance == player.alliance && health > 0 && health < maxHealth;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
            if (!allowStacking && this.healAmount > 0) return;
            if (health < maxHealth)
            {
                playHealSound = true;
            }
            commonHealLogic(healer, healAmount, health, maxHealth, drawHealText);
            this.healAmount = healAmount;
        }

        public virtual float getAirSpeed()
        {
            return 1;
        }

        public virtual float getDashSpeed()
        {
            return dashSpeed;
        }

        public virtual float getRunSpeed()
        {
            return 100;
        }

        public virtual float getJumpPower()
        {
            return Physics.jumpPower;
        }

        public bool canDash()
        {
            return !isAttacking();
        }

        public float getLabelOffY()
        {
            if (this is MorphMothCocoon && state is MorphMCHangState)
            {
                return height * 0.5f;
            }
            if (this is BlastHornet)
            {
                return 60;
            }
            return height;
        }

        public void drawHealthBar()
        {
            float healthBarInnerWidth = 30;
            Color color = new Color();

            float healthPct = health / maxHealth;
            float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
            if (healthPct > 0.66) color = Color.Green;
            else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
            else if (healthPct < 0.33) color = Color.Red;

            float botY = pos.y + currentLabelY;
            DrawWrappers.DrawRect(pos.x - 16, botY - 5, pos.x + 16, botY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
            DrawWrappers.DrawRect(pos.x - 15, botY - 4, pos.x - 15 + width, botY - 1, true, color, 0, ZIndex.HUD - 1);

            deductLabelY(labelHealthOffY);
        }

        public void drawName(string overrideName = "", Color? overrideColor = null, Color? overrideTextColor = null)
        {
            string playerName = player.name;
            Color playerColor = Helpers.DarkBlue;
            if (Global.level.gameMode.isTeamMode)
            {
                playerColor = player.alliance == GameMode.blueAlliance ? Helpers.DarkBlue : Helpers.DarkRed;
            }

            if (!string.IsNullOrEmpty(overrideName)) playerName = overrideName;
            if (overrideColor != null) playerColor = overrideColor.Value;

            float textPosX = pos.x;
            float textPosY = pos.y + currentLabelY - 8;

            DrawWrappers.DrawText(playerName, textPosX, textPosY, Alignment.Center, true, 0.75f,
                overrideTextColor ?? Color.White, playerColor, style: overrideTextColor == null ? Text.Styles.Regular : Text.Styles.Italic, 1, true, ZIndex.HUD);

            deductLabelY(labelNameOffY);
        }

        public bool drawSubtankHealing()
        {
            if (ownedByLocalPlayer)
            {
                if (usedSubtank != null)
                {
                    drawSubtankHealingInner(usedSubtank.health);
                    return true;
                }
            }
            else
            {
                if (netSubtankHealAmount > 0)
                {
                    drawSubtankHealingInner(netSubtankHealAmount);
                    netSubtankHealAmount -= Global.spf * 20;
                    if (netSubtankHealAmount <= 0) netSubtankHealAmount = 0;
                    return true;
                }
            }

            return false;
        }

        public void drawSubtankHealingInner(float health)
        {
            Point topLeft = new Point(pos.x - 8, pos.y - 15 + currentLabelY);
            Point topLeftBar = new Point(pos.x - 2, topLeft.y + 1);
            Point botRightBar = new Point(pos.x + 2, topLeft.y + 15);

            Global.sprites["menu_subtank"].draw(1, topLeft.x, topLeft.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
            Global.sprites["menu_subtank_bar"].draw(0, topLeftBar.x, topLeftBar.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
            float yPos = 14 * (health / SubTank.maxHealth);
            DrawWrappers.DrawRect(topLeftBar.x, topLeftBar.y, botRightBar.x, botRightBar.y - yPos, true, Color.Black, 1, ZIndex.HUD);

            deductLabelY(labelSubtankOffY);
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            currentLabelY = -getLabelOffY();

            if (player == Global.level.mainPlayer && player.currentMaverick != this && health > 0)
            {
                drawHealthBar();
            }

            if (player != Global.level.mainPlayer && player.alliance == Global.level.mainPlayer.alliance)
            {
                drawHealthBar();
                drawName();
            }

            drawSubtankHealing();

            renderDamageText(35);

            if (showCursor())
            {
                Global.sprites["cursorchar"].draw(0, pos.x + x, pos.y + y + currentLabelY, 1, 1, null, 1, 1, 1, zIndex + 1);
                deductLabelY(labelCursorOffY);
            }
        }

        public bool showCursor()
        {
            if (this is StingChameleon sc && sc.isInvisible && ownedByLocalPlayer) return true;
            return player.currentMaverick == this && !player.isTagTeam();
        }

        public void changeToIdleOrFall(string transitionSprite = "")
        {
            if (grounded)
            {
                if (state is not MIdle) changeState(new MIdle(transitionSprite));
            }
            else
            {
                if (state is not MFall) changeState(new MFall(transitionSprite));
            }
        }

        public void changeToIdleRunOrFall()
        {
            if (grounded)
            {
                if (input.getInputDir(player).x != 0)
                {
                    changeState(new MRun());
                }
                if (state is not MIdle) changeState(new MIdle());
            }
            else
            {
                if (state is not MFall) changeState(new MFall());
            }
        }

        public void changeToIdleFallOrFly(string transitionSprite = "")
        {
            if (grounded)
            {
                if (state is not MIdle) changeState(new MIdle(transitionSprite));
            }
            else
            {
                if (state?.wasFlying == true) changeState(new MFly(transitionSprite));
                else if (state is not MFall) changeState(new MFall(transitionSprite));
            }
        }
    }
}
