using UnityEngine;
using System.Collections;

public class NodePlanning_Mine
{
    public WorldState_Mine mWorldState;
    public ActionPlanning_Mine MActionPlanning;
    public float gCost;
    public float hCost;

    public NodePlanning_Mine mParent;
    /***************************************************************************/

    public NodePlanning_Mine(ActionPlanning_Mine actionPlanning,
        WorldState_Mine worldState,
        ActionPlanning_Mine.ActionType actionType,
        int stamina = 0, int playerHealth = 0, int monsterHealth = 0,
        Weapon weaponType = null)
    {
        mWorldState = worldState;
        MActionPlanning = actionPlanning;
        gCost = 0.0f;
        hCost = 0.0f;
        mParent = null;
        
        worldState.mActionType = actionType;
        worldState.stamina = stamina;
        worldState.playerHealth = playerHealth;
        worldState.monsterHealth = monsterHealth;
        worldState.weaponType = weaponType;
    }

    /***************************************************************************/

    public float fCost => gCost + hCost;

    /***************************************************************************/

    public bool Equals(NodePlanning_Mine other)
    {
        return mWorldState == other.mWorldState;
    }

    /***************************************************************************/
}