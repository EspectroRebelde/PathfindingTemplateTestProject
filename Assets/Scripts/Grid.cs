using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Unity.Jobs;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

public class Grid : MonoBehaviour
{
    public LayerMask unwalkableMask;
    public LayerMask costMultiplierMask;
    public Vector2 gridWorldSize;

    [Space(12), HideLabel]
    [Title("Player GameObject", "The player GameObject with a CapsuleCollider", bold: true)]
    [ValidateInput("HasCapsuleColliderDefaultMessage", "Prefab must have a CapsuleCollider component")]
    public GameObject playerGO; 

    private bool HasCapsuleColliderDefaultMessage(GameObject go, ref string errorMessage)
    {
        if (go == null) return true;
        errorMessage = "Prefab must have a CapsuleCollider component";
        return go.GetComponentInChildren<CapsuleCollider>() != null;
    }

    private NodePathfinding[,] grid;
    private float nodeRadius;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    [PropertySpace(SpaceBefore = 15, SpaceAfter = 15)]
    [OnValueChanged("WaterCostUpdated")]
    [Range(0.0f, 6.0f)]
    [InfoBox("Cost is calculated by GValue * WaterCost", InfoMessageType.Info)]
    public float WaterCost = 1.5f;

    public List<NodePathfinding> openSet;
    public HashSet<NodePathfinding> closedSet;
    public List<NodePathfinding> path;

    #region Draw Gizmos
    [Title("Draw Gizmos", "Edit the appearance of the Gizmos for the grid", bold: true)]
    public bool drawGizmos = true;
    
    [ShowIf("drawGizmos")]
    [TabGroup("Draw Gizmos", " Grid", SdfIconType.Grid, TextColor = "white")]
    [LabelText("Grid")]
    [ReadOnly]
    public bool dGrid = true;

    [ShowIf("drawGizmos")] 
    [TabGroup("Draw Gizmos", " Grid", SdfIconType.Grid, TextColor = "white")]
    [ReadOnly]
    public Color gridColor = Color.white;

    [LabelText("Path")] 
    [ShowIf("drawGizmos")]
    [TabGroup("Draw Gizmos", " Path", SdfIconType.ArrowRepeat, TextColor = "white")]
    public bool dPath = true;

    [LabelText("Path -To-")] 
    [ShowIf("@this.drawGizmos && this.dPath")]
    [TabGroup("Draw Gizmos", " Path", SdfIconType.ArrowRepeat, TextColor = "white")]
    public Color pathColor1 = Color.black;

    [LabelText("Path -From-")] 
    [ShowIf("@this.drawGizmos && this.dPath")]
    [TabGroup("Draw Gizmos", " Path", SdfIconType.ArrowRepeat, TextColor = "white")]
    public Color pathColor2 = new Color(0.47f, 0.18f, 0f);

    [LabelText("Open Set")]
    [ShowIf("drawGizmos")]
    [TabGroup("Draw Gizmos", " Open", SdfIconType.DoorOpen, TextColor = "green")]
    public bool dOpenSet = true;

    [ShowIf("@this.drawGizmos && this.dOpenSet")]
    [TabGroup("Draw Gizmos", " Open", SdfIconType.DoorOpen, TextColor = "green")]
    public Color openSetColor = Color.green;

    [LabelText("Closed Set")]
    [ShowIf("drawGizmos")]
    [TabGroup("Draw Gizmos", " Closed", SdfIconType.DoorClosed, TextColor = "yellow")]
    public bool dClosedSet = true;

    [ShowIf("@this.drawGizmos && this.dClosedSet")]
    [TabGroup("Draw Gizmos", " Closed", SdfIconType.DoorClosed, TextColor = "yellow")]
    public Color closedSetColor = Color.yellow;

    [LabelText("Important")]
    [ShowIf("drawGizmos")]
    [TabGroup("Draw Gizmos", " Important", SdfIconType.Flag, TextColor = "purple")]
    public bool dImporantNodes = true;

    [ShowIf("@this.drawGizmos && this.dImporantNodes")]
    [TabGroup("Draw Gizmos", " Important", SdfIconType.Flag, TextColor = "purple")]

    public Color importantColor = Color.magenta;

    [LabelText("Non Walkable")]
    [ShowIf("drawGizmos")]
    [TabGroup("Draw Gizmos", " Non Walkable", SdfIconType.StopCircle, TextColor = "red")]
    public bool dNonWalkableNodes = true;

    [ShowIf("@this.drawGizmos && this.dNonWalkableNodes")]
    [TabGroup("Draw Gizmos", " Non Walkable", SdfIconType.StopCircle, TextColor = "red")]
    public Color nonWalkableColor = Color.red;

    [LabelText("Cost Multiplier")]
    [ShowIf("drawGizmos")]
    [TabGroup("Draw Gizmos", " Cost Multiplier", SdfIconType.Water, TextColor = "blue")]
    public bool dCostMultiplierNodes = true;

    [ShowIf("@this.drawGizmos && this.dCostMultiplierNodes")]
    [TabGroup("Draw Gizmos", " Cost Multiplier", SdfIconType.Water, TextColor = "blue")]
    public Color costMultiplierColor = Color.blue;

    #endregion

    private void Awake()
    {
        var radius = playerGO.GetComponent<CapsuleCollider>().radius;
        nodeDiameter = radius * 2;
        nodeRadius = radius;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    // On Value Changed for WaterCost, update the grid
    private void WaterCostUpdated()
    {
        // Update the cost of each grid[x,y]
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                float costMultiplier = (Physics.CheckSphere(grid[x, y].mWorldPosition, nodeRadius, costMultiplierMask)) ? WaterCost : 1.0f;
                grid[x, y].mCostMultiplier = costMultiplier;
            }
        }
    }

    /***************************************************************************/

    private void CreateGrid()
    {
        grid = new NodePathfinding[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) +
                                     Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                float costMultiplier = (Physics.CheckSphere(worldPoint, nodeRadius, costMultiplierMask)) ? WaterCost : 1.0f;
                grid[x, y] = new NodePathfinding(walkable, worldPoint, x, y, costMultiplier);
            }
        }
    }

    /***************************************************************************/

    public List<NodePathfinding> GetNeighbours(NodePathfinding node, bool eightConnectivity)
    {
        List<NodePathfinding> neighbours = new List<NodePathfinding>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if ((x == 0 && y == 0))
                {
                    continue;
                }

                if (!eightConnectivity && (Mathf.Abs(x) + Mathf.Abs(y) > 1))
                {
                    continue;
                }

                int checkX = node.mGridX + x;
                int checkY = node.mGridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    /***************************************************************************/

    public NodePathfinding NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    public NodePathfinding GetClosestWalkableNode(NodePathfinding node)
    {
        if (node.mWalkable)
        {
            return node;
        }

        // Check the neighbours and recursively call this function if they are not walkable
        List<NodePathfinding> neighbours = GetNeighbours(node, true);
        foreach (NodePathfinding neighbour in neighbours)
        {
            if (neighbour.mWalkable)
            {
                return neighbour;
            }
        }

        // If none of the neighbours are walkable, check the neighbours of the neighbours through the job system
        // First, randomly shuffle the neighbours
        for (int i = 0; i < neighbours.Count; i++)
        {
            NodePathfinding temp = neighbours[i];
            int randomIndex = Random.Range(i, neighbours.Count);
            neighbours[i] = neighbours[randomIndex];
            neighbours[randomIndex] = temp;
        }

        foreach (NodePathfinding neighbour in neighbours)
        {
            NodePathfinding closestWalkableNode = null;
            JobHandle jobHandle = GetClosestWalkableNodeJob(neighbour, ref closestWalkableNode, new JobHandle());
            jobHandle.Complete();
            if (closestWalkableNode != null)
            {
                return closestWalkableNode;
            }
        }

        // If none of the neighbours of the neighbours are walkable, return null
        return null;
    }

    // Geenrate a unity job for each neighbour to check their neighbours
    // If any of the neighbours are walkable, return that node and stop the job
    // If none of the neighbours are walkable, return null and stop the job
    // If all the jobs are done and none of the neighbours are walkable, return null
    public JobHandle GetClosestWalkableNodeJob(NodePathfinding node, ref NodePathfinding closestWalkableNode, JobHandle dependency)
    {
        if (node.mWalkable)
        {
            closestWalkableNode = node;
            return dependency;
        }

        // Check the neighbours and recursively call this function if they are not walkable
        List<NodePathfinding> neighbours = GetNeighbours(node, true);
        foreach (NodePathfinding neighbour in neighbours)
        {
            if (neighbour.mWalkable)
            {
                closestWalkableNode = neighbour;
                return dependency;
            }
        }

        // If none of the neighbours are walkable, check the neighbours of the neighbours through the job system
        // First, randomly shuffle the neighbours
        for (int i = 0; i < neighbours.Count; i++)
        {
            NodePathfinding temp = neighbours[i];
            int randomIndex = Random.Range(i, neighbours.Count);
            neighbours[i] = neighbours[randomIndex];
            neighbours[randomIndex] = temp;
        }

        foreach (NodePathfinding neighbour in neighbours)
        {
            dependency = GetClosestWalkableNodeJob(neighbour, ref closestWalkableNode, dependency);
            if (closestWalkableNode != null)
            {
                return dependency;
            }
        }

        // If none of the neighbours of the neighbours are walkable, return null
        return dependency;
    }

    /***************************************************************************/

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        
        if (dGrid)
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        }

        if (grid == null) return;
        foreach (NodePathfinding n in grid)
        {
            Gizmos.color = gridColor;
            if (!n.mWalkable && dNonWalkableNodes)
            {
                // Blend the color with the nonWalkableColor
                Gizmos.color = (Gizmos.color + nonWalkableColor) / 2;
            }

            if (dCostMultiplierNodes && n.mCostMultiplier != 1.0f)
            {
                Gizmos.color = (costMultiplierColor + Gizmos.color) / 2;
            }

            if (openSet != null && dOpenSet)
            {
                if (openSet.Contains(n))
                {
                    Gizmos.color = (openSetColor + Gizmos.color) / 2;
                }
            }

            if (closedSet != null && dClosedSet)
            {
                if (closedSet.Contains(n))
                {
                    Gizmos.color = (closedSetColor + Gizmos.color) / 2;
                }
            }

            // If the color is still gridColor
            if (Gizmos.color != gridColor || dPath)
            {
                Gizmos.DrawCube(n.mWorldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }

        #region Path Waypoints

        if (path == null) return;
        if (dPath)
        {
            // Draw the path
            for (int i = playerGO.GetComponent<Unit>().GetTargetIndex(); i < path.Count; i++)
            {
                Gizmos.color = pathColor1;
                Gizmos.DrawCube(path[i].mWorldPosition, Vector3.one * (nodeDiameter - .1f));
                if (i == playerGO.GetComponent<Unit>().GetTargetIndex())
                {
                    Gizmos.DrawLine(playerGO.transform.position, path[i].mWorldPosition);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1].mWorldPosition, path[i].mWorldPosition);
                }
            }

            // Once we pass through a waypoint, draw it in a different color
            for (int i = 0; i < playerGO.GetComponent<Unit>().GetTargetIndex(); i++)
            {
                // Draw in brown
                Gizmos.color = pathColor2;
                Gizmos.DrawCube(path[i].mWorldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }

        #endregion

        #region Path Start-End

        if (dImporantNodes)
        {
            Gizmos.color = importantColor;
            Gizmos.DrawCube(path[0].mWorldPosition, Vector3.one * (nodeDiameter - .1f));
            Gizmos.DrawCube(path[^1].mWorldPosition, Vector3.one * (nodeDiameter - .1f));
        }

        #endregion
    }

    /***************************************************************************/

    public NodePathfinding GetNode(int x, int y)
    {
        return grid[x, y];
    }

    // OnValidate make sure the PlayerGO has a CapsuleCollider
    void OnValidate()
    {
        if (playerGO != null)
        {
            if (playerGO.GetComponent<CapsuleCollider>() == null)
            {
                Debug.LogError("PlayerGO must have a CapsuleCollider");
            }
        }
    }
}