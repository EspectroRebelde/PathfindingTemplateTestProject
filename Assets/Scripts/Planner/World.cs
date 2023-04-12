using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class World : MonoBehaviour
{
  public List<NodePlanning> openSet;
	public HashSet<NodePlanning> closedSet;

  public List<NodePlanning> plan;

  public WorldState mWorldState;

  public List<ActionPlanning> mActionList;

  /***************************************************************************/
  
  [Flags]
  public enum WorldState
  {
    WORLD_STATE_NONE                    =   0,
    WORLD_STATE_ENEMY_DEAD              =   1 << 0,
    WORLD_STATE_GUN_OWNED               =   1 << 1,
    WORLD_STATE_GUN_LOADED              =   1 << 2,
    WORLD_STATE_KNIFE_OWNED             =   1 << 3,
    WORLD_STATE_CLOSE_TO_ENEMY          =   1 << 4,
    WORLD_STATE_CLOSE_TO_GUN            =   1 << 5,
    WORLD_STATE_CLOSE_TO_KNIFE          =   1 << 6,
    WORLD_STATE_LINE_OF_SIGHT_TO_ENEMY  =   1 << 7, 
  }

  /***************************************************************************/

	void Awake()
  {
    mActionList = new List<ActionPlanning>();
    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_STAB,
        WorldState.WORLD_STATE_CLOSE_TO_ENEMY | WorldState.WORLD_STATE_KNIFE_OWNED,
        WorldState.WORLD_STATE_ENEMY_DEAD,
        WorldState.WORLD_STATE_NONE,
        5.0f, "Stab" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_SHOOT,
        WorldState.WORLD_STATE_LINE_OF_SIGHT_TO_ENEMY | WorldState.WORLD_STATE_GUN_LOADED | WorldState.WORLD_STATE_GUN_OWNED,
        WorldState.WORLD_STATE_ENEMY_DEAD,
        WorldState.WORLD_STATE_NONE,
        100.0f, "Shoot" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_LOAD_GUN,
        WorldState.WORLD_STATE_GUN_OWNED,
        WorldState.WORLD_STATE_GUN_LOADED,
        WorldState.WORLD_STATE_NONE,
        1.0f, "Load gun" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_PICK_UP_GUN,
        WorldState.WORLD_STATE_CLOSE_TO_GUN,
        WorldState.WORLD_STATE_GUN_OWNED,
        WorldState.WORLD_STATE_CLOSE_TO_GUN,
        1.0f, "Pick up gun" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_PICK_UP_KNIFE,
        WorldState.WORLD_STATE_CLOSE_TO_KNIFE,
        WorldState.WORLD_STATE_KNIFE_OWNED,
        WorldState.WORLD_STATE_CLOSE_TO_KNIFE,
        1.0f, "Pick up knife" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_GO_TO_ENEMY,
        WorldState.WORLD_STATE_NONE,
        WorldState.WORLD_STATE_CLOSE_TO_ENEMY,
        WorldState.WORLD_STATE_CLOSE_TO_GUN | WorldState.WORLD_STATE_CLOSE_TO_KNIFE,
        1.0f, "Go to enemy" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_GO_TO_GUN,
        WorldState.WORLD_STATE_NONE,
        WorldState.WORLD_STATE_CLOSE_TO_GUN,
        WorldState.WORLD_STATE_CLOSE_TO_ENEMY | WorldState.WORLD_STATE_CLOSE_TO_KNIFE,
        20.0f, "Go to gun" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_GO_TO_KNIFE,
        WorldState.WORLD_STATE_NONE,
        WorldState.WORLD_STATE_CLOSE_TO_KNIFE,
        WorldState.WORLD_STATE_CLOSE_TO_ENEMY | WorldState.WORLD_STATE_CLOSE_TO_GUN,
        20.0f, "Go to knife" )
    );

    mActionList.Add(
      new ActionPlanning( 
        ActionPlanning.ActionType.ACTION_TYPE_GET_LINE_OF_SIGHT_TO_ENEMY,
        WorldState.WORLD_STATE_GUN_LOADED | WorldState.WORLD_STATE_GUN_OWNED,
        WorldState.WORLD_STATE_LINE_OF_SIGHT_TO_ENEMY,
        WorldState.WORLD_STATE_NONE,
        10.0f, "Get line of sight to enemy" )
    );
  }

  /***************************************************************************/

	public List<NodePlanning> GetNeighbours( NodePlanning node )
  {
		List<NodePlanning> neighbours = new List<NodePlanning>();

    foreach ( ActionPlanning action in mActionList ) {
      // If preconditions are met we can apply effects and the new state is valid
      if( ( node.mWorldState & action.mPreconditions ) == action.mPreconditions ){
        // Apply action, effects and negative effects
        NodePlanning newNodePlanning = new NodePlanning((node.mWorldState | action.mEffects) & ~action.mNegativeEffects, action );
        neighbours.Add( newNodePlanning );
      }
    }

		return neighbours;
	}

  /***************************************************************************/

  public static int PopulationCount( int n )
  {
    return System.Convert.ToString(n,2).ToCharArray().Count( c => c=='1' );
  }
	
  /***************************************************************************/

}