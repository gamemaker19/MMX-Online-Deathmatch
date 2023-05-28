using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public enum PickupType
    {
        Health,
        Ammo
    }

    public enum PickupTypeRpc
    {
        LargeHealth,
        SmallHealth,
        LargeAmmo,
        SmallAmmo
    }

    public class Pickup : Actor
    {
        public float healAmount = 0;
        public PickupType pickupType;
        public Pickup(Player owner, Point pos, string sprite, ushort? netId, bool ownedByLocalPlayer, NetActorCreateId netActorCreateId, bool sendRpc = false) : 
            base(sprite, pos, netId, ownedByLocalPlayer, false)
        {
            netOwner = owner;
            collider.wallOnly = true;
            collider.isTrigger = false;

            this.netActorCreateId = netActorCreateId;
            if (sendRpc)
            {
                createActorRpc(owner.id);
            }
        }

        public override void update()
        {
            base.update();
            var leeway = 500;
            if (ownedByLocalPlayer && pos.x > Global.level.width + leeway || pos.x < -leeway || pos.y > Global.level.height + leeway || pos.y < -leeway)
            {
                destroySelf();
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.otherCollider.flag == (int)HitboxFlag.Hitbox) return;

            if (other.gameObject is Character) 
            {
                var chr = other.gameObject as Character;
                if (!chr.ownedByLocalPlayer) return;
                 if (chr.isHyperSigmaBS.getValue()) return;

                if (pickupType == PickupType.Health)
                {
                    if (chr.player.health >= chr.player.maxHealth && !chr.player.hasSubtankCapacity()) return;
                    chr.addHealth(healAmount);
                    destroySelf(doRpcEvenIfNotOwned: true);
                }
                else if (pickupType == PickupType.Ammo)
                {
                    if (chr.player.isZBusterZero()) return;
                    if (!chr.player.isZero && !chr.player.isVile && !chr.player.isSigma && (chr.player.weapon == null || chr.player.weapon.ammo >= chr.player.weapon.maxAmmo)) return;
                    if (chr.player.isVile && chr.player.vileAmmo >= chr.player.vileMaxAmmo) return;
                    if (chr.isHyperSigmaBS.getValue()) return;
                    if (chr.player.isSigma && chr.player.sigmaAmmo >= chr.player.sigmaMaxAmmo) return;
                    chr.addAmmo(healAmount);
                    destroySelf(doRpcEvenIfNotOwned: true);
                }
            }
            else if (other.gameObject is RideArmor)
            {
                var rideArmor = other.gameObject as RideArmor;
                if (!rideArmor.ownedByLocalPlayer) return;

                if (rideArmor.character != null)
                {
                    if (pickupType == PickupType.Health)
                    {
                        if (rideArmor.health >= rideArmor.maxHealth)
                        {
                            if (rideArmor.character != null && (rideArmor.character.player.health >= rideArmor.character.player.maxHealth || rideArmor.character.isVileMK5))
                            {
                                return;
                            }
                            else
                            {
                                rideArmor.character.addHealth(healAmount);
                            }
                        }
                        else
                        {
                            rideArmor.addHealth(healAmount);
                        }
                        destroySelf(doRpcEvenIfNotOwned: true);
                    }
                    else if (pickupType == PickupType.Ammo)
                    {
                        //rideArmor.character.addAmmo(this.healAmount);
                        //this.destroySelf();
                    }
                }
            }
            else if (other.gameObject is RideChaser)
            {
                var rideChaser = other.gameObject as RideChaser;
                if (!rideChaser.ownedByLocalPlayer) return;

                if (rideChaser.character != null)
                {
                    if (pickupType == PickupType.Health)
                    {
                        if (rideChaser.health >= rideChaser.maxHealth)
                        {
                            if (rideChaser.character != null && rideChaser.character.player.health >= rideChaser.character.player.maxHealth) return;
                            else rideChaser.character.addHealth(healAmount);
                        }
                        else
                        {
                            rideChaser.addHealth(healAmount);
                        }
                        destroySelf(doRpcEvenIfNotOwned: true);
                    }
                }
            }
            else if (other.gameObject is Maverick maverick && maverick.ownedByLocalPlayer)
            {
                if (pickupType == PickupType.Health && (maverick.health < maverick.maxHealth || maverick.netOwner.hasSubtankCapacity()))
                {
                    maverick.addHealth(healAmount, true);
                    destroySelf(doRpcEvenIfNotOwned: true);
                }
                else if (pickupType == PickupType.Ammo && maverick.ammo < maverick.maxAmmo)
                {
                    maverick.addAmmo(healAmount);
                    destroySelf(doRpcEvenIfNotOwned: true);
                }
            }
        }
    }

    public class LargeHealthPickup : Pickup
    {
        public LargeHealthPickup(Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) : 
            base(owner, pos, "pickup_health_large", netId, ownedByLocalPlayer, NetActorCreateId.LargeHealth, sendRpc: sendRpc)
        {
            healAmount = 8;
            pickupType = PickupType.Health;
        }
    }

    public class SmallHealthPickup : Pickup
    {
        public SmallHealthPickup(Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) : 
            base(owner, pos, "pickup_health_small", netId, ownedByLocalPlayer, NetActorCreateId.SmallHealth, sendRpc: sendRpc)
        {
            healAmount = 4;
            pickupType = PickupType.Health;
        }
    }

    public class LargeAmmoPickup : Pickup
    {
        public LargeAmmoPickup(Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) : 
            base(owner, pos, "pickup_ammo_large", netId, ownedByLocalPlayer, NetActorCreateId.LargeAmmo, sendRpc: sendRpc)
        {
            healAmount = 16;
            pickupType = PickupType.Ammo;
        }
    }

    public class SmallAmmoPickup : Pickup
    {
        public SmallAmmoPickup(Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) : 
            base(owner, pos, "pickup_ammo_small", netId, ownedByLocalPlayer, NetActorCreateId.SmallAmmo, sendRpc: sendRpc)
        {
            healAmount = 8;
            pickupType = PickupType.Ammo;
        }
    }
}
