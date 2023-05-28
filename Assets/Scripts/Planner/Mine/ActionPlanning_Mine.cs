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
        ACTION_TYPE_NONE                    = 0, // NADA
        ACTION_TYPE_IDLE                    = 0b_1, // QUIETO
        ACTION_TYPE_MOVE_TO_TARGET_FAST     = 0b_10, // MOVERSE RAPIDO
        ACTION_TYPE_MOVE_TO_TARGET_SLOW     = 0b_100, // MOVERSE LENTO
        ACTION_TYPE_MOVE_TO_TARGET_SNEAK    = 0b_1000, // MOVERSE SIGILOSO
        
        ACTION_TYPE_ATTACK_NORMAL           = 0b_10000, // ATAQUE NORMAL
        ACTION_TYPE_ATTACK_CHARGING         = 0b_100000, // ATAQUE CARGADO
        ACTION_TYPE_ATTACK_SUPER            = 0b_1000000, // ATAQUE SUPER
        ACTION_TYPE_ATTACK_NORMAL_POINT     = 0b_10000000, // ATAQUE NORMAL
        ACTION_TYPE_ATTACK_WEAK_POINT       = 0b_1_00000000, // ATAQUE PUNTOS DEBIL
        ACTION_TYPE_ATTACK_SEVERABLE_PART   = 0b_10_00000000, // ATAQUE PARTES SECCIONABLES
        ACTION_TYPE_ATTACK_BREAKABLE_PART   = 0b_100_00000000, // ATAQUE PARTES ROMPIBLES
        ACTION_TYPE_ATTACK                  = ACTION_TYPE_ATTACK_NORMAL | ACTION_TYPE_ATTACK_CHARGING | ACTION_TYPE_ATTACK_SUPER,


        ACTION_TYPE_BLOCK                   = 0b_1000_00000000, // BLOQUEAR
        ACTION_TYPE_DODGE                   = 0b_10000_00000000, // ESQUIVAR
        
        ACTION_TYPE_TAKE_COVER              = 0b_100000_00000000, // TOMAR COBERTURA
        
        ACTION_TYPE_EQUIP_WEAPON_LONSWORD   = 0b_1000000_00000000, // EQUIPAR ESPADA LARGA
        ACTION_TYPE_EQUIP_WEAPON_HAMMER     = 0b_10000000_00000000, // EQUIPAR MARTILLO
        ACTION_TYPE_EQUIP_WEAPON_LANCE      = 0b_1_00000000_00000000, // EQUIPAR LANZA
        ACTION_TYPE_EQUIP_WEAPON_SWORD      = 0b_10_00000000_00000000, // EQUIPAR ESPADA

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