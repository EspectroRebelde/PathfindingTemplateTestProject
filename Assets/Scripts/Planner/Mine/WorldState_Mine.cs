﻿using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Vector3 = System.Numerics.Vector3;

[Serializable]
public class WorldState_Mine
{
    public WorldState_Mask mWorldStateMask;
    public ActionPlanning_Mine.ActionType mActionType;
    public int stamina;
    public int playerHealth;
    public int monsterHealth;
    public int monsterCurrentHealth;
    public Weapon weapon;
    
    public float2 stunCount = new (.25f, 2);
    public float2 fleeCount = new (.75f, 2);
    public float2 aggresiveCount = new (.5f, 1.5f);
    
    public int2 flyingCounter = new (0, 2);
    public int2 chargingCounter = new (0, 2);
    public int2 superAttackCounter = new (0, 2);
    public int2 stunCounter = new (0, 2);
    
    public float attackPercentage = 20f;
    public float flyPercentage = 15f;
    public float chargePercentage = 10f;
    public float superAttackPercentage = 5f;

    public WorldState_Mine(WorldState_Mask worldStateMask, 
        int stamina = 0, int playerHealth = 0, int monsterCurrentHealth = 0,
        Weapon weaponRef = null,
        ActionPlanning_Mine.ActionType actionType = ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, 
        int monsterHealth = 0)
    {
        mWorldStateMask = worldStateMask;
        this.mActionType = actionType;
        this.stamina = stamina;
        this.playerHealth = playerHealth;
        this.monsterHealth = monsterCurrentHealth > monsterHealth ? monsterCurrentHealth : monsterHealth;
        this.monsterCurrentHealth = monsterCurrentHealth;
        this.weapon = weaponRef;
    }

    // ACTIVATE to handle similar states (stamina wise) [Idle, Move Slow, Move Fast] [Dodge, Block, Take Cover]
    public static bool operator ==(WorldState_Mine worldState1, WorldState_Mine worldState2)
    {
        // Check if the masks are the same
        // Check if the monster health is the same
        // Check if the weapon is the same
        return worldState1.mWorldStateMask == worldState2.mWorldStateMask && 
               worldState1.monsterCurrentHealth == worldState2.monsterCurrentHealth &&
                worldState1.weapon == worldState2.weapon;
    }

    public static bool operator !=(WorldState_Mine worldState1, WorldState_Mine worldState2)
    {
        return worldState1.mWorldStateMask != worldState2.mWorldStateMask || worldState1.monsterCurrentHealth != worldState2.monsterCurrentHealth ||
               worldState1.weapon != worldState2.weapon;
    }

    public static bool FinalStateCheck(WorldState_Mine worldStateCurrent, WorldState_Mine worldStateDestination)
    {
        // If states of the destination are active in the current state
        // If the stamina is greater than the destination
        // If the health is greater than the destination
        return (worldStateCurrent.mWorldStateMask & worldStateDestination.mWorldStateMask) == worldStateDestination.mWorldStateMask &&
               worldStateCurrent.stamina >= worldStateDestination.stamina && worldStateCurrent.playerHealth >= worldStateDestination.playerHealth
               && worldStateCurrent.monsterCurrentHealth <= worldStateDestination.monsterCurrentHealth;
    }

    // Compare two WorldState_Mine to check the preconditions
    private bool preconditionsMet(WorldState_Mine worldStateDestination)
    {
        // Check the masks
        // If there is enough stamina
        // If there is enough health
        return (mWorldStateMask & worldStateDestination.mWorldStateMask) == worldStateDestination.mWorldStateMask &&
               stamina >= worldStateDestination.stamina && playerHealth >= worldStateDestination.playerHealth;
    }
    
    // WS & NP == 0
    private bool negativePreconditionsMet(WorldState_Mine worldStateDestination)
    {
        // Check the masks, worldStateDestination.mWorldStateMask should be 0 in mWorldStateMask
        return (mWorldStateMask & worldStateDestination.mWorldStateMask) == 0
               &&
               stamina >= worldStateDestination.stamina && playerHealth >= worldStateDestination.playerHealth;
    }
    
    public bool checkPreconditions(WorldState_Mine worldStateDestination, WorldState_Mine worldStateNegativeDestination)
    {
        // If the worldStateDestination has an action type
        if (worldStateDestination.mActionType != ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE)
        {
            // If the action type is not the same as the current action type
            if (worldStateDestination.mActionType != mActionType)
            {
                return false;
            }
        }
        
        return preconditionsMet(worldStateDestination) && negativePreconditionsMet(worldStateNegativeDestination);
    }

    // Apply the effects of an action to the current WorldState_Mine
    public WorldState_Mine applyEffects(WorldState_Mine effects, WorldState_Mine negativeEffects, ActionPlanning_Mine.ActionType action, int random)
    {
        // Apply effects and negative effects
        // Mask effects
        // Change stamina
        // Change health
        // Return the new WorldState_Mine
        WorldState_Mine newWorldState = new WorldState_Mine(mWorldStateMask);
        newWorldState.monsterCurrentHealth = monsterCurrentHealth;
        newWorldState.monsterHealth = monsterHealth;
        
        // If the action is an equip action
        if ((action & ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_SET) != 0)
        {
            // Create a new weapon of the type of the action
            weapon = new Weapon(Weapon.GetWeaponTypeFromAction(action));
        }
        
        // If ACTION_TYPE_ATTACK (enum flagged) is been set
        if ((action & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK) != 0)
        {
            int dmg = weapon.Attack(action, mWorldStateMask);
            newWorldState.monsterCurrentHealth -= dmg;
            
            // If the dmg is > 0, the monster cannot be asleep
            if (dmg > 0)
            {
                newWorldState.mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_SLEEPING;
            }
            // If the monsterCurrentHealth is <= 0, the monster is dead
            if (newWorldState.monsterCurrentHealth <= 0)
            {
                newWorldState.mWorldStateMask |= WorldState_Mask.WS_MONSTER_DEAD;
            }
        }
        
        newWorldState.mWorldStateMask |= effects.mWorldStateMask;
        newWorldState.mWorldStateMask &= ~negativeEffects.mWorldStateMask;
        newWorldState.stamina = stamina + effects.stamina + negativeEffects.stamina;
        newWorldState.playerHealth = playerHealth + effects.playerHealth + negativeEffects.playerHealth;
        
        // If the action isnt attacking OR ACTION_TYPE_ATTACK_SET is active
        if ((action & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK) == 0 || (action & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SET) != 0)
        {
            // ACTIVATE
            //RandomThrows(newWorldState, action, random);
        }
        return newWorldState;
    }

    private void RandomThrows(WorldState_Mine newWorldState, ActionPlanning_Mine.ActionType action, int random)
    {
        #region Deactivations

        // Deactivate WS_MONSTER_FLEEING
        if ((newWorldState.mWorldStateMask & WorldState_Mask.WS_MONSTER_IN_RANGE) == 0)
        {
            newWorldState.mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_FLEEING;
            newWorldState.mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_FLYING;
        }
        
        // WS_MONSTER_ATTACK
        if ((mWorldStateMask & WorldState_Mask.WS_MONSTER_ATTACK) == WorldState_Mask.WS_MONSTER_ATTACK)
        {
            // Stop attacking
            mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_ATTACK;
            // Inflict damage if the action isnt blocking, dodging or taking cover and the player is in range
            if ((action & ActionPlanning_Mine.ActionType.ACTION_TYPE_BLOCK) == 0 && (action & ActionPlanning_Mine.ActionType.ACTION_TYPE_DODGE) == 0 &&
                (action & ActionPlanning_Mine.ActionType.ACTION_TYPE_TAKE_COVER) == 0 && 
                (mWorldStateMask & WorldState_Mask.WS_MONSTER_IN_RANGE) == WorldState_Mask.WS_MONSTER_IN_RANGE)
            {
                newWorldState.playerHealth -= 10;
            }

        }
        
        // Charging WS_MONSTER_CHARGING, add the charge time
        if ((mWorldStateMask & WorldState_Mask.WS_MONSTER_CHARGING) == WorldState_Mask.WS_MONSTER_CHARGING)
        {
            chargingCounter.x ++;
            // If the charge time is over
            if (chargingCounter.x >= chargingCounter.y)
            {
                // Stop charging
                mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_CHARGING;
                // Reset the charge time
                chargingCounter.x = 0;
                // Inflict damage if the action isnt blocking, dodging or taking cover and the player is in range
                if ((action & ActionPlanning_Mine.ActionType.ACTION_TYPE_BLOCK) == 0 && (action & ActionPlanning_Mine.ActionType.ACTION_TYPE_DODGE) == 0 &&
                    (action & ActionPlanning_Mine.ActionType.ACTION_TYPE_TAKE_COVER) == 0 && 
                    (mWorldStateMask & WorldState_Mask.WS_MONSTER_IN_RANGE) == WorldState_Mask.WS_MONSTER_IN_RANGE)
                {
                    newWorldState.playerHealth -= 20;
                }
            }
        }
        
        // Super WS_MONSTER_SUPER, add the super time
        if ((mWorldStateMask & WorldState_Mask.WS_MONSTER_SUPER) == WorldState_Mask.WS_MONSTER_SUPER)
        {
            superAttackCounter.x ++;
            // If the super time is over
            if (superAttackCounter.x >= superAttackCounter.y)
            {
                // Stop super
                mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_SUPER;
                // Activate the Injured
                mWorldStateMask |= WorldState_Mask.WS_MONSTER_INJURED;
                // Reset the super time
                superAttackCounter.x = 0;
                // Inflict damage if the action isnt taking cover and the player is in range
                if ((action & ActionPlanning_Mine.ActionType.ACTION_TYPE_TAKE_COVER) == 0 && 
                    (mWorldStateMask & WorldState_Mask.WS_MONSTER_IN_RANGE) == WorldState_Mask.WS_MONSTER_IN_RANGE)
                {
                    newWorldState.playerHealth -= 30;
                }
            }
        }
        
        // Flying WS_MONSTER_FLYING, add the fly time
        if ((mWorldStateMask & WorldState_Mask.WS_MONSTER_FLYING) == WorldState_Mask.WS_MONSTER_FLYING)
        {
            flyingCounter.x ++;
            // If the fly time is over
            if (flyingCounter.x >= flyingCounter.y)
            {
                // Stop flying
                mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_FLYING;
                // Reset the fly time
                flyingCounter.x = 0;
            }
        }
        
        // Stunned WS_MONSTER_STUNNED, add the stun time
        if ((mWorldStateMask & WorldState_Mask.WS_MONSTER_STUNNED) == WorldState_Mask.WS_MONSTER_STUNNED)
        {
            stunCounter.x ++;
            // If the stun time is over
            if (stunCounter.x >= stunCounter.y)
            {
                // Stop stunned
                mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_STUNNED;
                // Reset the stun time
                stunCounter.x = 0;
            }
        }

        #endregion
        
        #region Activations

        // If the monster's health is lower than monsterHealth * fleeCount
        // WS_MONSTER_FLEEING
        if (monsterCurrentHealth < monsterHealth * fleeCount.x)
        {
            // Activate the flee state on the wold mask
            mWorldStateMask |= WorldState_Mask.WS_MONSTER_FLEEING;
            // Deactivate aggressive
            mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_AGGRESSIVE;
            // Activate the flying state on the wold mask
            mWorldStateMask |= WorldState_Mask.WS_MONSTER_FLYING;
            // Increase the flee count
            fleeCount.x *= fleeCount.y;
        }
        
        // If the monster's health is lower than monsterHealth * stunCount
        // WS_MONSTER_STUNNED
        if (monsterCurrentHealth < monsterHealth * stunCount.x)
        {
            // Activate the stun state on the wold mask
            mWorldStateMask |= WorldState_Mask.WS_MONSTER_STUNNED;
            // Deactivate flee and aggressive
            mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_FLEEING;
            mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_AGGRESSIVE;
            // Increase the stun count
            stunCount.x *= stunCount.y;
        }
        
        // If the monster's health is lower than monsterHealth * aggresiveCount
        // WS_MONSTER_AGGRESSIVE
        if (monsterCurrentHealth < monsterHealth * aggresiveCount.x)
        {
            // Activate the aggressive state on the wold mask
            mWorldStateMask |= WorldState_Mask.WS_MONSTER_AGGRESSIVE;
            // Increase the aggressive count
            aggresiveCount.x *= aggresiveCount.y;
        }
        
        // If the monster isnt in FOV nor Range and is injured, activate the sleeping state
        // WS_MONSTER_SLEEPING
        if (((mWorldStateMask & WorldState_Mask.WS_MONSTER_IN_FOV) != WorldState_Mask.WS_MONSTER_IN_FOV) &&
            ((mWorldStateMask & WorldState_Mask.WS_MONSTER_IN_RANGE) != WorldState_Mask.WS_MONSTER_IN_RANGE) &&
            ((mWorldStateMask & WorldState_Mask.WS_MONSTER_INJURED) == WorldState_Mask.WS_MONSTER_INJURED))
        {
            // Activate the sleeping state on the wold mask
            mWorldStateMask |= WorldState_Mask.WS_MONSTER_SLEEPING;
        }

        #region Attack Activations

        if ((mWorldStateMask & WorldState_Mask.WS_MONSTER_STUNNED) != WorldState_Mask.WS_MONSTER_STUNNED &&
            (mWorldStateMask & WorldState_Mask.WS_MONSTER_FLEEING) != WorldState_Mask.WS_MONSTER_FLEEING &&
            (mWorldStateMask & WorldState_Mask.WS_MONSTER_SLEEPING) != WorldState_Mask.WS_MONSTER_SLEEPING &&
            (mWorldStateMask & WorldState_Mask.WS_MONSTER_IN_FOV) == WorldState_Mask.WS_MONSTER_IN_FOV)
        {
            // If the monster isnt Attacking, Charging, Super, Stunned or Fleeing
            if (((mWorldStateMask & WorldState_Mask.WS_MONSTER_ATTACK) != WorldState_Mask.WS_MONSTER_ATTACK) &&
                ((mWorldStateMask & WorldState_Mask.WS_MONSTER_CHARGING) != WorldState_Mask.WS_MONSTER_CHARGING) &&
                ((mWorldStateMask & WorldState_Mask.WS_MONSTER_SUPER) != WorldState_Mask.WS_MONSTER_SUPER))
            {
                // If the random number is lower than the super percentage
                if (random < superAttackPercentage)
                {
                    // Activate the super state on the wold mask
                    mWorldStateMask |= WorldState_Mask.WS_MONSTER_SUPER;
                    // Activate the aggressive state on the wold mask
                    mWorldStateMask |= WorldState_Mask.WS_MONSTER_AGGRESSIVE;
                }
                else if (random < chargePercentage + superAttackPercentage)
                {
                    // Activate the charge state on the wold mask
                    mWorldStateMask |= WorldState_Mask.WS_MONSTER_CHARGING;
                }
                else if (random < attackPercentage + chargePercentage + superAttackPercentage)
                {
                    // Activate the attack state on the wold mask
                    mWorldStateMask |= WorldState_Mask.WS_MONSTER_ATTACK;
                }

            }
        
            // If no counter is active, roll the fly counter
            if (chargingCounter.x == 0 && stunCounter.x == 0 && superAttackCounter.x == 0)
            {
                if (random < flyPercentage)
                {
                    // Activate the flying state on the wold mask
                    mWorldStateMask |= WorldState_Mask.WS_MONSTER_FLYING;
                    // Deactivate InRange
                    mWorldStateMask &= ~WorldState_Mask.WS_MONSTER_IN_RANGE;
                }
            }
        }

        #endregion
        
        // If the monster's health is lower than 0 (dead)
        // WS_MONSTER_DEAD
        if (monsterCurrentHealth <= 0)
        {
            // Activate the dead state on the wold mask
            mWorldStateMask |= WorldState_Mask.WS_MONSTER_DEAD;
        }   

        #endregion

    }
}

[Flags]
public enum WorldState_Mask
{
    // Generic
    WS_NONE = 0b_0, // NADA
    WS_MONSTER_DEAD = 0b_1, // MUERTO
    WS_MONSTER_IN_FOV = 0b_10, // A VISTA
    WS_MONSTER_IN_RANGE = 0b_100, // A RANGO
    // Status
    WS_MONSTER_INJURED = 0b_1000, // HERIDO
    WS_MONSTER_PART_SEVERED = 0b_10000, // PARTE SECCIONADA
    WS_MONSTER_PART_BROKEN = 0b_100000, // PARTE ROTA
    // Managed
    WS_MONSTER_FLEEING = 0b_1000000, // Activation & Deactivation // HUYENDO
    WS_MONSTER_ATTACK = 0b_10000000, // Deactivation & Activation // ATACANDO
    WS_MONSTER_FLYING = 0b_1_00000000, // Deactivation & Activation // VOLANDO
    WS_MONSTER_AGGRESSIVE = 0b_10_00000000, // Activation & Deactivation // AGRESIVO
    WS_MONSTER_SLEEPING = 0b_100_00000000, // Activation & Deactivation // DURMIENDO
    WS_MONSTER_STUNNED = 0b_1000_00000000, // Activation & Deactivation // ATURDIDO
    WS_MONSTER_CHARGING = 0b_10000_00000000, // Deactivation & Activation // CARGANDO
    WS_MONSTER_SUPER = 0b_100000_00000000, // Deactivation & Activation // SUPER
    // Weapons
    WS_WEAPON_EQUIPPED = 0b_1000000_00000000, // EQUIPADO
    WS_WEAPON_TYPE = 0b_1_10000000_00000000,
    // 11 -> Sword (bit 15 and 16 are 1) and WS_WEAPON_EQUIPPED
    WS_WEAPON_TYPE_SWORD = 0b_1_10000000_00000000,
    // 10 -> Lance (bit 15 is 0 and bit 16 is 1) and WS_WEAPON_EQUIPPED
    WS_WEAPON_TYPE_LANCE = 0b_1_00000000_00000000,
    // 01 -> Hammer (bit 15 is 1 and bit 16 is 0) and WS_WEAPON_EQUIPPED
    WS_WEAPON_TYPE_HAMMER = 0b_10000000_00000000,
    // 00 -> Longsword (bit 15 is 0 and bit 16 is 0) and WS_WEAPON_EQUIPPED and BOTH bits 15 and 16 are 0
    WS_WEAPON_TYPE_LONGSWORD = 0b_0_00000000_00000000 | WS_WEAPON_EQUIPPED,
}

// A struct called WeaponType that contains the different types of weapons
// Implements the function Attack for each weapon type
public enum WeaponType
{
    NONE,
    LONGSWORD,
    HAMMER,
    LANCE,
    SWORD
}

[Serializable]
public class Weapon
{
    public WeaponType weaponType;
    private int damage;
    private int damageSlash;
    private int damageHit;
    private bool canBlock;
    private bool canBreak;
    private bool canSever;

    public Weapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.LONGSWORD:
                damage = 15;
                damageSlash = 20;
                damageHit = 10;
                canBlock = false;
                canBreak = false;
                canSever = true;
                break;
            case WeaponType.HAMMER:
                damage = 15;
                damageSlash = 10;
                damageHit = 20;
                canBlock = false;
                canBreak = true;
                canSever = false;
                break;
            case WeaponType.LANCE:
                damage = 15;
                damageSlash = 10;
                damageHit = 10;
                canBlock = true;
                canBreak = false;
                canSever = false;
                break;
            case WeaponType.SWORD:
                damage = 15;
                damageSlash = 15;
                damageHit = 15;
                canBlock = false;
                canBreak = false;
                canSever = false;
                break;
            case WeaponType.NONE:
            default:
                damage = 0;
                damageSlash = 0;
                damageHit = 0;
                canBlock = false;
                break;
        }

        this.weaponType = weaponType;
    }
    
    public int AttackType(ActionPlanning_Mine.ActionType attackType, WorldState_Mask worldStateMask)
    {
        switch (attackType)
        {
            case ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL:
                return damage;
            case ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING:
                return damageSlash;
            case ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER:
                return damageHit;
            default:
                return 0;
        }
    }
    
    public int AttackPlace(ActionPlanning_Mine.ActionType attackPlace, WorldState_Mask worldStateMask)
    {
        // attackPlace is the attack position
        // NORMAL_POINT
        // WEAK_POINT
        // BREAKABLE_PART
        // SEVERABLE_PART
        switch (attackPlace)
        {
            case ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL_POINT:
                return damage;
            case ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WEAK_POINT:
                return damage * 2;
            case ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_BREAKABLE_PART:
                // Is the part already broken?
                if ((worldStateMask & WorldState_Mask.WS_MONSTER_PART_BROKEN) == WorldState_Mask.WS_MONSTER_PART_BROKEN)
                {
                    return damage;
                }
                else
                {
                    return damage * 2;
                }
            case ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SEVERABLE_PART:
                // Is the part already severed?
                if ((worldStateMask & WorldState_Mask.WS_MONSTER_PART_SEVERED) == WorldState_Mask.WS_MONSTER_PART_SEVERED)
                {
                    return damage;
                }
                else
                {
                    return damage * 2;
                }
        }
        return 0;
    }

    public int Attack(ActionPlanning_Mine.ActionType attackMask, WorldState_Mask worldStateMask)
    {
        // Set Normal_Point as default
        attackMask |= ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL_POINT;
        
        // Check the flag to see which
        if ((attackMask & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL) == ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL)
        {
            // Normal attack
            return AttackType(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL, worldStateMask) + 
                   AttackPlace(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL_POINT, worldStateMask);
        }

        if ((attackMask & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING) == ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING)
        {
            // Charging attack
            return AttackType(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING, worldStateMask) + 
                   AttackPlace(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WEAK_POINT, worldStateMask);
        }

        if ((attackMask & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER) == ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER)
        {
            // Super attack
            return (AttackType(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER, worldStateMask) +
                    AttackPlace(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_BREAKABLE_PART, worldStateMask));
        }

        return 0;
    }
    
    public bool CanBlock()
    {
        return canBlock;
    }
    
    public bool CanBreak()
    {
        return canBreak;
    }
    
    public bool CanSever()
    {
        return canSever;
    }
    
    public static WeaponType GetWeaponTypeFromAction(ActionPlanning_Mine.ActionType actionType)
    {
        // Check the weapon type (sword, hammer, lance and longsword)
        if ((actionType & ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_SWORD) == ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_SWORD)
        {
            return WeaponType.SWORD;
        }

        if ((actionType & ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_HAMMER) == ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_HAMMER)
        {
            return WeaponType.HAMMER;
        }
        if ((actionType & ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_LANCE) == ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_LANCE)
        {
            return WeaponType.LANCE;
        }
        if ((actionType & ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_LONSWORD) == ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_LONSWORD)
        {
            return WeaponType.LONGSWORD;
        }
        return WeaponType.NONE;
    }
}