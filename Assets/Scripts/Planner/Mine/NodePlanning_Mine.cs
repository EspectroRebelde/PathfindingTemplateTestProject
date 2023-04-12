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

    public NodePlanning_Mine(WorldState_Mine worldState, ActionPlanning_Mine actionPlanning)
    {
        mWorldState = worldState;
        MActionPlanning = actionPlanning;
        gCost = 0.0f;
        hCost = 0.0f;
        mParent = null;
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