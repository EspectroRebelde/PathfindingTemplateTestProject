using UnityEngine;
using System.Collections;

public class Node
{
    public bool mWalkable;
    public Vector3 mWorldPosition;
    public int mGridX;
    public int mGridY;
    public float mCostMultiplier;
    public float gCost;
    public float hCost;

    public Node mParent;
    /***************************************************************************/

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY, float costMultiplier)
    {
        mWalkable = walkable;
        mWorldPosition = worldPosition;
        mGridX = gridX;
        mGridY = gridY;
        mCostMultiplier = costMultiplier;
        gCost = 1;
    }

    /***************************************************************************/
    public float fCost => gCost + hCost;
    /***************************************************************************/
}