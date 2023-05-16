using System;
using UnityEngine;
using System.Collections;

public class ActionPlanning_Mine
{
    public ActionType mActionType;
    public WorldState_Mine mPreconditions;
    public WorldState_Mine mNegativePreconditions;
    public WorldState_Mine mEffects;
    public WorldState_Mine mNegativeEffects;
    public float mCost;

    public string mName;
    /***************************************************************************/
    
    // TODO: Modify actions for 1-5-10% of dmg so nehaviour tree can do it
    [Flags]
    // Action state of a hunting game like monster hunter
    public enum ActionType
    {
        ACTION_TYPE_NONE                    = -1,
        ACTION_TYPE_IDLE                    = 1 << 0,
        ACTION_TYPE_MOVE_TO_TARGET_FAST     = 1 << 1,
        ACTION_TYPE_MOVE_TO_TARGET_SLOW     = 1 << 2,
        ACTION_TYPE_MOVE_TO_TARGET_SNEAK    = 1 << 3,
        
        ACTION_TYPE_ATTACK_NORMAL           = 1 << 4,
        ACTION_TYPE_ATTACK_CHARGING         = 1 << 5,
        ACTION_TYPE_ATTACK_SUPER            = 1 << 6,
        ACTION_TYPE_ATTACK_NORMAL_POINT     = 1 << 7,
        ACTION_TYPE_ATTACK_WEAK_POINT       = 1 << 8,
        ACTION_TYPE_ATTACK_SEVERABLE_PART   = 1 << 9,
        ACTION_TYPE_ATTACK_BREAKABLE_PART   = 1 << 10,
        ACTION_TYPE_ATTACK                  = ACTION_TYPE_ATTACK_NORMAL | ACTION_TYPE_ATTACK_CHARGING | ACTION_TYPE_ATTACK_SUPER,


        ACTION_TYPE_BLOCK                   = 1 << 11,
        ACTION_TYPE_DODGE                   = 1 << 12,
        
        ACTION_TYPE_TAKE_COVER              = 1 << 13,
        
        ACTION_TYPE_EQUIP_WEAPON_LONSWORD   = 1 << 14,
        ACTION_TYPE_EQUIP_WEAPON_HAMMER     = 1 << 15,
        ACTION_TYPE_EQUIP_WEAPON_LANCE      = 1 << 16,
        ACTION_TYPE_EQUIP_WEAPON_SWORD      = 1 << 17,

        ACTION_TYPE_ATTACK_WHILE_FLEEING    = (ACTION_TYPE_ATTACK_NORMAL | ACTION_TYPE_ATTACK_SUPER) & ACTION_TYPE_MOVE_TO_TARGET_FAST,

        ACTION_TYPE_ATTACK_WHILE_INJURED    = (ACTION_TYPE_ATTACK_NORMAL | ACTION_TYPE_ATTACK_SUPER) & ACTION_TYPE_MOVE_TO_TARGET_SLOW,
        ACTION_TYPE_ATTACK_WHILE_STUNNED    = (ACTION_TYPE_ATTACK_NORMAL | ACTION_TYPE_ATTACK_SUPER),
        ACTION_TYPE_ATTACK_WHILE_SLEEPING   = (ACTION_TYPE_ATTACK_NORMAL | ACTION_TYPE_ATTACK_SUPER),
        ACTION_TYPES
    }

    /***************************************************************************/

    public ActionPlanning_Mine(ActionType actionType, WorldState_Mine preconditions, WorldState_Mine negativePreconditions,
        WorldState_Mine effects, WorldState_Mine negativeEffects, float cost, string name)
    {
        mActionType = actionType;
        mPreconditions = preconditions;
        mNegativePreconditions = negativePreconditions;
        mEffects = effects;
        mNegativeEffects = negativeEffects;
        mCost = cost;
        mName = name;
    }

    /***************************************************************************/
}