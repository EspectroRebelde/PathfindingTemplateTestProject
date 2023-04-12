using UnityEngine;
using System.Collections;

/// https://www.geeksforgeeks.org/a-search-algorithm/
/// https://www.redblobgames.com/pathfinding/a-star/introduction.html
/// https://www.redblobgames.com/pathfinding/a-star/implementation.html#csharp
/// https://www.researchgate.net/publication/282488307_Pathfinding_Algorithm_Efficiency_Analysis_in_2D_Grid
public class NodePathfinding
{
    public bool mWalkable;
    public Vector3 mWorldPosition;
    public int mGridX;
    public int mGridY;
    public float mCostMultiplier;
    public float gCost;
    public float hCost;

    public NodePathfinding mParent;
    /***************************************************************************/

    public NodePathfinding(bool walkable, Vector3 worldPosition, int gridX, int gridY, float costMultiplier)
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