using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
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
    
    public float stunCount = .25f;

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

    public static bool operator ==(WorldState_Mine worldState1, WorldState_Mine worldState2)
    {
        return worldState1.mWorldStateMask == worldState2.mWorldStateMask && worldState1.stamina == worldState2.stamina &&
               worldState1.playerHealth == worldState2.playerHealth && worldState1.monsterCurrentHealth == worldState2.monsterCurrentHealth;
    }

    public static bool operator !=(WorldState_Mine worldState1, WorldState_Mine worldState2)
    {
        return worldState1.mWorldStateMask != worldState2.mWorldStateMask || worldState1.stamina != worldState2.stamina ||
               worldState1.playerHealth != worldState2.playerHealth || worldState1.monsterCurrentHealth != worldState2.monsterCurrentHealth;
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
               stamina >= worldStateDestination.stamina && playerHealth >= worldStateDestination.playerHealth
               && monsterCurrentHealth <= worldStateDestination.monsterCurrentHealth;
    }
    
    // WS & NP == 0
    private bool negativePreconditionsMet(WorldState_Mine worldStateDestination)
    {
        // Check the masks
        // If there is not enough stamina
        // If there is not enough health
        return (mWorldStateMask & worldStateDestination.mWorldStateMask) == 0 &&
               stamina >= worldStateDestination.stamina && playerHealth >= worldStateDestination.playerHealth
               && monsterCurrentHealth <= worldStateDestination.monsterCurrentHealth;
    }
    
    public bool checkPreconditions(WorldState_Mine worldStateDestination, WorldState_Mine worldStateNegativeDestination)
    {
        return preconditionsMet(worldStateDestination) && negativePreconditionsMet(worldStateNegativeDestination);
    }

    // Apply the effects of an action to the current WorldState_Mine
    public WorldState_Mine applyEffects(WorldState_Mine effects, WorldState_Mine negativeEffects)
    {
        // Apply effects and negative effects
        // Mask effects
        // Change stamina
        // Change health
        // Return the new WorldState_Mine
        WorldState_Mine newWorldState = new WorldState_Mine(mWorldStateMask);
        // If ACTION_TYPE_ATTACK (enum flagged) has been set
        if ((mActionType & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK) == ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK)
        {
            int dmg = weapon.Attack(mActionType, mWorldStateMask);
            newWorldState.monsterHealth -= dmg;
        }
        
        RandomThrows();
        
        newWorldState.mWorldStateMask |= effects.mWorldStateMask;
        newWorldState.mWorldStateMask &= ~negativeEffects.mWorldStateMask;
        newWorldState.stamina = stamina + effects.stamina + negativeEffects.stamina;
        newWorldState.playerHealth = playerHealth + effects.playerHealth + negativeEffects.playerHealth;
        newWorldState.monsterHealth = monsterHealth + effects.monsterHealth + negativeEffects.monsterHealth;
        return newWorldState;
    }

    private void RandomThrows()
    {
        // If the monster's health is lower than 
    }
}

[Flags]
public enum WorldState_Mask
{
    WS_NONE = 0,
    WS_MONSTER_DEAD = 0b1,
    WS_MONSTER_FLEEING = 0b10,
    WS_MONSTER_ATTACK = 0x4,
    WS_MONSTER_INJURED = 0x8,
    WS_MONSTER_PART_SEVERED = 0x10,
    WS_MONSTER_PART_BROKEN = 1 << 5,
    WS_MONSTER_IN_FOV = 1 << 6,
    WS_MONSTER_IN_RANGE = 1 << 7,
    WS_MONSTER_FLYING = 1 << 8,
    WS_MONSTER_AGGRESSIVE = 1 << 9,
    WS_MONSTER_SLEEPING = 1 << 10,
    WS_MONSTER_STUNNED = 1 << 11,
    WS_MONSTER_CHARGING = 1 << 12,
    WS_MONSTER_SUPER = 1 << 13,
    
    WS_WEAPON_EQUIPPED = 1 << 14,
    // 00 -> Longsword (bit 15 and 16 are 0)
    // 01 -> Hammer (bit 15 is 1 and bit 16 is 0)
    // 10 -> Lance (bit 15 is 0 and bit 16 is 1)
    // 11 -> Sword (bit 15 and 16 are 1)
    WS_WEAPON_TYPE_LONGSWORD = ~(1 << 15 | 1 << 16),
    WS_WEAPON_TYPE_HAMMER = 1 << 15 & ~(1 << 16),
    WS_WEAPON_TYPE_LANCE = ~(1 << 15) & 1 << 16,
    WS_WEAPON_TYPE_SWORD = 1 << 15 & 1 << 16,
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
        // attackMask is the attack type
        // NORMAL
        // CHARGING
        // SUPER
        // Check the flag to see which
        if ((attackMask & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL) == ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL)
        {
            // Normal attack
            return AttackType(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL, worldStateMask) * 
                   AttackPlace(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL_POINT, worldStateMask);
        }

        if ((attackMask & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING) == ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING)
        {
            // Charging attack
            return AttackType(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING, worldStateMask) * 
                   AttackPlace(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WEAK_POINT, worldStateMask);
        }

        if ((attackMask & ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER) == ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER)
        {
            // Super attack
            return AttackType(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER, worldStateMask) * 
                   AttackPlace(ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_BREAKABLE_PART, worldStateMask);
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
}