using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

public class World_Mine : MonoBehaviour
{
    public List<NodePlanning_Mine> openSet;
    public HashSet<NodePlanning_Mine> closedSet;
    public List<NodePlanning_Mine> plan; 
    
    // A WorldState_Mine class with the parameters exposed in the inspector
    [BoxGroup("Starting World State")]
    public WorldState_Mask mWorldStateMask = WorldState_Mask.WS_NONE;
    [BoxGroup("Starting World State")]
    public int mWorldStateHealth;
    [BoxGroup("Starting World State")]
    public int mWorldStateStamina;
    [BoxGroup("Starting World State")] 
    public int mWorldStateMonsterHealth;
    
    [BoxGroup("Ending World State")]
    public WorldState_Mask mWorldStateMaskTarget;
    [BoxGroup("Ending World State")]
    public int mWorldStateMinumumHealth;
    [BoxGroup("Ending World State")]
    public int mWorldStateMinumumStamina;

    [HorizontalGroup("Split", 0.5f)]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
    public void InitialState1()
    {
        // Initial state is all disabled and NONE is enabled
        mWorldStateMask = WorldState_Mask.WS_NONE;
        mWorldStateHealth = 100;
        mWorldStateStamina = 100;
    }

    [VerticalGroup("Split/right")]
    [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void InitialState2()
    {
        // Initial state = MONSTER_SLEEPING
        mWorldStateMask = WorldState_Mask.WS_MONSTER_SLEEPING;
        mWorldStateHealth = 100;
        mWorldStateStamina = 100;
        // STATUS
    }

    public List<ActionPlanning_Mine> mActionList;
    /***************************************************************************/

    /***************************************************************************/

    void Awake()
    {
        mActionList = new List<ActionPlanning_Mine>();

        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_IDLE,
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                0.0f, "None")
        );
/*
         #region ACTION_TYPE_MOVE
         mActionList.Add(
             new ActionPlanning_Mine(
                 ActionPlanning_Mine.ActionType.ACTION_TYPE_MOVE_TO_TARGET_SLOW,
             )
         );
         
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_MOVE_TO_TARGET_FAST,
            )
        );

        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_MOVE_TO_TARGET_SNEAK,
            )
        );
        #endregion

        #region ACTION_TYPE_ATTACK
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPES
                )
            );
        #endregion
        */
    }

    /***************************************************************************/

    public List<NodePlanning_Mine> GetNeighbours(NodePlanning_Mine node)
    {
        List<NodePlanning_Mine> neighbours = new List<NodePlanning_Mine>();
        foreach (ActionPlanning_Mine action in mActionList)
        {
            // If preconditions are met we can apply effects and the new state is valid
            if (node.mWorldState.checkPreconditions(action.mPreconditions, action.mNegativePreconditions))
            {
                // Apply action, effects and negative effects
                NodePlanning_Mine newNodePlanning = new NodePlanning_Mine(
                    action,
                    node.mWorldState.applyEffects(action.mEffects, action.mNegativeEffects),
                    action.mActionType,
                    node.mWorldState.stamina,
                    node.mWorldState.playerHealth,
                    node.mWorldState.monsterCurrentHealth,
                    node.mWorldState.weapon,
                    node.mWorldState.monsterHealth);
                
                neighbours.Add(newNodePlanning);
            }
        }

        return neighbours;
    }

    /***************************************************************************/

    public static int PopulationCount(int n)
    {
        return System.Convert.ToString(n, 2).ToCharArray().Count(c => c == '1');
    }

    /***************************************************************************/
}