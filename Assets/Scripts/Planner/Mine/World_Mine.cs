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
         
         int staminaCost = 0;
         #region ACTION_TYPE_MOVE
         staminaCost = 3;
         mActionList.Add(
             new ActionPlanning_Mine(
                 ActionPlanning_Mine.ActionType.ACTION_TYPE_MOVE_TO_TARGET_SLOW,
                    new WorldState_Mine(WorldState_Mask.WS_MONSTER_SLEEPING | WorldState_Mask.WS_MONSTER_IN_RANGE, staminaCost),
                 new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                    new WorldState_Mine(WorldState_Mask.WS_NONE),
                 new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                 0.0f, "Move Slow"
                 )
         );

         staminaCost = 10;
         mActionList.Add(
             new ActionPlanning_Mine(
                 ActionPlanning_Mine.ActionType.ACTION_TYPE_MOVE_TO_TARGET_FAST,
                 new WorldState_Mine(WorldState_Mask.WS_MONSTER_STUNNED | WorldState_Mask.WS_MONSTER_IN_FOV,
                     staminaCost),
                 new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING | WorldState_Mask.WS_MONSTER_CHARGING),
                 new WorldState_Mine(WorldState_Mask.WS_NONE),
                 new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                 0.0f, "Move Fast")
         );
        
         staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_MOVE_TO_TARGET_SNEAK,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_SLEEPING | WorldState_Mask.WS_MONSTER_IN_RANGE, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Move Sneak"
                )
        );
        #endregion

        #region ACTION_TYPE_ATTACK
        staminaCost = 10;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_IN_RANGE, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Attack Normal"
                )
            );

        staminaCost = 20;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_CHARGING,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_STUNNED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Attack Charging"
                )
        );

        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SUPER,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_STUNNED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Attack Super"
                )
            );
        
        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_NORMAL_POINT,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_IN_RANGE, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Attack Normal Point"
                )
        );
        
        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WEAK_POINT,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_IN_RANGE | WorldState_Mask.WS_MONSTER_STUNNED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Attack Weak Point"
                )
        );
        
        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_SEVERABLE_PART,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_PART_BROKEN | WorldState_Mask.WS_MONSTER_AGGRESSIVE, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_INJURED),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Attack Severable Part"
                )
        );
        
        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_BREAKABLE_PART,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_STUNNED | WorldState_Mask.WS_MONSTER_AGGRESSIVE, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_NONE, -staminaCost),
                0.0f, "Attack Breakable Part"
                )
        );
        #endregion
        
        #region ACTION_TYPE_DEFEND
        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_BLOCK,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_IN_RANGE | WorldState_Mask.WS_WEAPON_EQUIPPED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED, -staminaCost),
                0.0f, "Block"
            )
        );
        
        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_DODGE,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_IN_RANGE | WorldState_Mask.WS_MONSTER_INJURED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED, -staminaCost),
                0.0f, "Dodge"
            )
        );
        
        staminaCost = 5;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_TAKE_COVER,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_IN_FOV | WorldState_Mask.WS_MONSTER_PART_BROKEN, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED, -staminaCost),
                0.0f, "Take Cover"
            )
        );
        #endregion

        #region ACTION_TYPE_EQUIP_WEAPON
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_LONSWORD,
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_TYPE_LONGSWORD),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                0.0f, "Equip Weapon Longsword"
            )
        );
        
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_HAMMER,
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_TYPE_HAMMER),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                0.0f, "Equip Weapon Hammer"
            )
        );
        
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_LANCE,
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_TYPE_LANCE),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                0.0f, "Equip Weapon Lance"
            )
        );
        
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_EQUIP_WEAPON_SWORD,
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_TYPE_SWORD),
                new WorldState_Mine(WorldState_Mask.WS_NONE),
                0.0f, "Equip Weapon Sword"
            )
        );
        #endregion

        #region ACTION_TYPE_ATTACK_WHILE
        staminaCost = 40;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WHILE_FLEEING,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLEEING | WorldState_Mask.WS_MONSTER_IN_RANGE | WorldState_Mask.WS_WEAPON_EQUIPPED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLEEING),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED, -staminaCost),
                0.0f, "Attack While Fleeing"
            )
        );
        
        staminaCost = 40;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WHILE_INJURED,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_INJURED | WorldState_Mask.WS_MONSTER_IN_RANGE | WorldState_Mask.WS_WEAPON_EQUIPPED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_INJURED),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED, -staminaCost),
                0.0f, "Attack While Injured"
            )
        );
        
        staminaCost = 40;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WHILE_STUNNED,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_STUNNED | WorldState_Mask.WS_MONSTER_IN_RANGE | WorldState_Mask.WS_WEAPON_EQUIPPED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_STUNNED),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED, -staminaCost),
                0.0f, "Attack While Stunned"
            )
        );
        
        staminaCost = 40;
        mActionList.Add(
            new ActionPlanning_Mine(
                ActionPlanning_Mine.ActionType.ACTION_TYPE_ATTACK_WHILE_SLEEPING,
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_SLEEPING | WorldState_Mask.WS_MONSTER_IN_RANGE | WorldState_Mask.WS_WEAPON_EQUIPPED, staminaCost),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_FLYING),
                new WorldState_Mine(WorldState_Mask.WS_MONSTER_SLEEPING),
                new WorldState_Mine(WorldState_Mask.WS_WEAPON_EQUIPPED, -staminaCost),
                0.0f, "Attack While Sleeping"
            )
        );
        #endregion
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