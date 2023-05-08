﻿using System;
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
    [ReadOnly]
    public WorldState_Mask mWorldStateMask;
    [BoxGroup("Starting World State")]
    public int mWorldStateHealth;
    [BoxGroup("Starting World State")]
    public int mWorldStateStamina;
    
    [BoxGroup("Ending World State")]
    //TODO: Validate input for the weapons, it has to be the same weapon on both start and end
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
        // TODO: Add all actions to the list
        // STATUS
        
         mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_IDLE,
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                0.0f, "Idle")
        );
         

    }

    /***************************************************************************/

    public List<NodePlanning_Mine> GetNeighbours(NodePlanning_Mine node)
    {
        List<NodePlanning_Mine> neighbours = new List<NodePlanning_Mine>();
        foreach (ActionPlanning_Mine action in mActionList)
        {
            // If preconditions are met we can apply effects and the new state is valid
            if (node.mWorldState.preconditionsMet(action.mPreconditions))
            {
                // Apply action, effects and negative effects
                NodePlanning_Mine newNodePlanning = new NodePlanning_Mine(node.mWorldState.applyEffects(action.mEffects, action.mNegativeEffects), action);
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