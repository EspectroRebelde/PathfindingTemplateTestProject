using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

public class WorldState_Mine
{
    public WorldState_Mask mWorldStateMask;
    public ActionPlanning.ActionType mActionType;
    public int stamina;
    public int playerHealth;
    public int monsterHealth;

    public WorldState_Mine(WorldState_Mask worldStateMask, int stamina = 0, int playerHealth = 0, int monsterHealth = 0,
        ActionPlanning.ActionType actionType = ActionPlanning.ActionType.ACTION_TYPE_NONE)
    {
        mWorldStateMask = worldStateMask;
        mActionType = actionType;
        this.stamina = stamina;
        this.playerHealth = playerHealth;
        this.monsterHealth = monsterHealth;
    }

    public static bool operator ==(WorldState_Mine worldState1, WorldState_Mine worldState2)
    {
        return worldState1.mWorldStateMask == worldState2.mWorldStateMask && worldState1.stamina == worldState2.stamina &&
               worldState1.playerHealth == worldState2.playerHealth && worldState1.monsterHealth == worldState2.monsterHealth;
    }

    public static bool operator !=(WorldState_Mine worldState1, WorldState_Mine worldState2)
    {
        return worldState1.mWorldStateMask != worldState2.mWorldStateMask || worldState1.stamina != worldState2.stamina ||
               worldState1.playerHealth != worldState2.playerHealth || worldState1.monsterHealth != worldState2.monsterHealth;
    }

    public static bool FinalStateCheck(WorldState_Mine worldStateCurrent, WorldState_Mine worldStateDestination)
    {
        // If states of the destination are active in the current state
        // If the stamina is greater than the destination
        // If the health is greater than the destination
        return (worldStateCurrent.mWorldStateMask & worldStateDestination.mWorldStateMask) == worldStateDestination.mWorldStateMask &&
               worldStateCurrent.stamina >= worldStateDestination.stamina && worldStateCurrent.playerHealth >= worldStateDestination.playerHealth
               && worldStateCurrent.monsterHealth <= worldStateDestination.monsterHealth;
    }

    // Compare two WorldState_Mine to check the preconditions
    public bool preconditionsMet(WorldState_Mine worldStateDestination)
    {
        // Check the masks
        // If there is enough stamina
        // If there is enough health
        return (mWorldStateMask & worldStateDestination.mWorldStateMask) == worldStateDestination.mWorldStateMask &&
               stamina >= worldStateDestination.stamina && playerHealth >= worldStateDestination.playerHealth
               && monsterHealth <= worldStateDestination.monsterHealth;
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
        newWorldState.mWorldStateMask |= effects.mWorldStateMask;
        newWorldState.mWorldStateMask &= ~negativeEffects.mWorldStateMask;
        newWorldState.stamina = stamina + effects.stamina + negativeEffects.stamina;
        newWorldState.playerHealth = playerHealth + effects.playerHealth + negativeEffects.playerHealth;
        newWorldState.monsterHealth = monsterHealth + effects.monsterHealth + negativeEffects.monsterHealth;
        return newWorldState;
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

public enum WeaponType
{
    NONE,
    LONGSWORD,
    HAMMER,
    LANCE,
    SWORD,
}

public class Weapon
{
    public WeaponType weaponType;
    public int damage;
    public int damageSlash;
    public int damageHit;
    public bool canBlock;

    public Weapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.LONGSWORD:
                damage = 15;
                damageSlash = 20;
                damageHit = 10;
                canBlock = false;
                break;
            case WeaponType.HAMMER:
                damage = 15;
                damageSlash = 10;
                damageHit = 20;
                canBlock = false;
                break;
            case WeaponType.LANCE:
                damage = 15;
                damageSlash = 10;
                damageHit = 10;
                canBlock = true;
                break;
            case WeaponType.SWORD:
                damage = 15;
                damageSlash = 15;
                damageHit = 15;
                canBlock = false;
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
}