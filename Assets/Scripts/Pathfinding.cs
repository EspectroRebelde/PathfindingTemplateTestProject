using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

public class Pathfinding : MonoBehaviour
{
    [ReadOnly] public float DIAG_COST = 1.41421356237f;
    public Transform mSeeker;
    [PropertySpace(SpaceBefore = 0, SpaceAfter = 15)]
    public Transform mTarget;
    private NodePathfinding CurrentStartNode;
    private NodePathfinding CurrentTargetNode;
    private Grid Grid;
    private int Iterations;
    private float LastStepTime;
    private const float TimeBetweenSteps = 0.01f;
    private const float EPSILON = 0.0001f;

    [BoxGroup("Waypoint Overestimation")]
    [ValidateInput("IsGreaterThanZero", "Percentage must be greater than zero")]
    [Range(0.0f, 0.5f)]
    [InfoBox("Calculated Waypoint Cost <= Path Cost * (PercentageAdmissible+1)", InfoMessageType.Info)]
    [InlineButton("SetEpsilon", "Min")]
    public float PercentageAdmissible = 0.05f;
    
    private void SetEpsilon()
    {
        PercentageAdmissible = EPSILON;
    }

    private bool IsGreaterThanZero(float value)
    {
        return value > 0;
    }

    [PropertySpace(SpaceBefore = 15, SpaceAfter = 15)]
    public bool EightConnectivity = true;

    [FoldoutGroup("Heuristics and Weights")] [Title("Weight Toggles")]
    public bool SpecificWeights = true;

    [FoldoutGroup("Heuristics and Weights")] [Title("Heuristic Function", "$HeuristicDefinition")] [ValueDropdown("HeuristicFunctions")]
    public string HeuristicFunction = "Euclidean";

    [FoldoutGroup("Heuristics and Weights")]
    public bool ForceChebyshev;

    [FoldoutGroup("Heuristics and Weights")] [Title("Weights")] [ShowIf("SpecificWeights")] [LabelText("Base Weight")] [Range(0.0f, 1.0f)]
    public float WeightG = 1.0f;

    [FoldoutGroup("Heuristics and Weights")] [ShowIf("SpecificWeights")] [LabelText("Heuristic Weight")] [Range(0.0f, 1.0f)]
    public float WeightH = 1.0f;

    [Title("Relational Weight (G-H)")]
    [FoldoutGroup("Heuristics and Weights")]
    [HideIf("SpecificWeights")]
    [LabelText("Relational Weight")]
    [Range(0.0f, 1.0f)]
    public float WeightR = 1.0f;

    private List<string> HeuristicFunctions => new() { "Manhattan", "Octile", "Chebyshev", "Euclidean" };

    private string HeuristicDefinition
    {
        get
        {
            return HeuristicFunction switch
            {
                "Manhattan" => "Manhattan: d(x,y) = Σ|i - yi|",
                "Octile" => "Octile: d(x,y) = max(|xi-yi|) + 1/2 min(|xi-yi|)",
                "Chebyshev" => "Chebyshev: d(x,y) = max(|xi-yi|)",
                "Euclidean" => "Euclidean: d(x,y) = sqrt(Σ(xi-yi)^2)",
                _ => "No Heuristic Function Selected"
            };
        }
    }

    /***************************************************************************/

    private void Awake()
    {
        Grid = GetComponent<Grid>();
        Iterations = 0;
        LastStepTime = 0.0f;
    }

    /***************************************************************************/

    // private void Update()
    // {
    //     // Positions changed?
    //     if (PathInvalid())
    //     {
    //         // Remove old path
    //         if (Grid.path != null)
    //         {
    //             Grid.path.Clear();
    //         }
    //
    //         // Start calculating path again
    //         Iterations = 0;
    //         if (TimeBetweenSteps == 0.0f)
    //         {
    //             Iterations = -1;
    //         }
    //
    //         FindPath(mSeeker.position, mTarget.position, Iterations);
    //     }
    //     else
    //     {
    //         // Path found?
    //         if (Iterations < 0) return;
    //         // One or more iterations?
    //         if (TimeBetweenSteps == 0.0f)
    //         {
    //             // One iteration, look until path is found
    //             Iterations = -1;
    //             FindPath(mSeeker.position, mTarget.position, Iterations);
    //         }
    //
    //         if (!(Time.time > LastStepTime + TimeBetweenSteps)) return;
    //         // Iterate increasing depth every time step
    //         LastStepTime = Time.time;
    //         Iterations++;
    //         FindPath(mSeeker.position, mTarget.position, Iterations);
    //     }
    // }
    /***************************************************************************/

    private bool PathInvalid()
    {
        return CurrentStartNode != Grid.NodeFromWorldPoint(mSeeker.position) || CurrentTargetNode != Grid.NodeFromWorldPoint(mTarget.position);
    }

    /***************************************************************************/

    public List<NodePathfinding> FindPath(Vector3 startPos, Vector3 targetPos, int iterations)
    {
        CurrentStartNode = Grid.GetClosestWalkableNode(Grid.NodeFromWorldPoint(startPos));
        // If the target is not walkable, find the closest walkable node
        CurrentTargetNode = Grid.GetClosestWalkableNode(Grid.NodeFromWorldPoint(targetPos));
        if (CurrentTargetNode == null || CurrentStartNode == null)
        {
            Debug.Log("No walkable node found");
            return null;
        }

        List<NodePathfinding> openSet = new List<NodePathfinding>();
        HashSet<NodePathfinding> closedSet = new HashSet<NodePathfinding>();
        openSet.Add(CurrentStartNode);
        Grid.openSet = openSet;
        int currentIteration = 0;
        NodePathfinding node = CurrentStartNode;
        while (openSet.Count > 0 && node != CurrentTargetNode && (iterations == -1 || currentIteration < iterations))
        {
            // Select best node from open list
            node = openSet[0];

            // Remove from open list
            openSet.Remove(node);
            Grid.openSet = openSet;

            // Add to closed list
            closedSet.Add(node);
            Grid.closedSet = closedSet;
            if (node == CurrentTargetNode)
            {
                // Path found - reconstruct path
                RetracePath(CurrentStartNode, CurrentTargetNode);
                return Grid.path;
            }

            // Check neighbors
            foreach (NodePathfinding neighbor in Grid.GetNeighbours(node, EightConnectivity))
            {
                // Skip if not walkable or already in closed list
                if (!neighbor.mWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                float newCost = node.gCost + GetDistance(node, neighbor) * neighbor.mCostMultiplier;
                if (!openSet.Contains(neighbor) || newCost < neighbor.gCost)
                {
                    if (SpecificWeights)
                    {
                        neighbor.gCost = WeightG * newCost;
                        neighbor.hCost = WeightH * Heuristic(neighbor, CurrentTargetNode);
                    }
                    else
                    {
                        neighbor.gCost = WeightR * newCost;
                        neighbor.hCost = (1.0f - WeightR) * Heuristic(neighbor, CurrentTargetNode);
                    }

                    neighbor.mParent = node;
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                        Grid.openSet = openSet;
                    }
                }
            }

            currentIteration++;

            // Sort open list
            openSet.Sort((x, y) => (x.fCost.CompareTo(y.fCost)));
            Grid.openSet = openSet;
        }

        // No path found
        return null;
    }

    /***************************************************************************/

    private void RetracePath(NodePathfinding startNode, NodePathfinding endNode)
    {
        List<NodePathfinding> path = new List<NodePathfinding>();

        // Each node has a parent, so we can retrace the path from the end to the start
        NodePathfinding currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.mParent;
        }

        path.Add(startNode);

        // Reverse path
        path.Reverse();
        path = EightConnectivity ? SmoothPath8C(path, PercentageAdmissible) : SmoothPath4C(path, PercentageAdmissible);
        Grid.path = path;
    }

    /***************************************************************************/

    private float GetDistance(NodePathfinding nodeA, NodePathfinding nodeB)
    {
        // Distance function
        //nodeA.mGridX   nodeB.mGridX
        //nodeA.mGridY   nodeB.mGridY
        // It calculates the distance between two nodes in the grid, cant be the same as the heuristic function
        if (EightConnectivity)
        {
            // Chebyshev distance (D2 = 1.0f) - Octile distance (D2 = sqrt(2))
            float D2 = DIAG_COST;
            if (ForceChebyshev)
            {
                D2 = 1.0f;
            }

            float dx = Mathf.Abs(nodeA.mGridX - nodeB.mGridX);
            float dy = Mathf.Abs(nodeA.mGridY - nodeB.mGridY);
            return D2 * Mathf.Min(dx, dy) + (Mathf.Abs(dx - dy));
        }
        else
        {
            // Manhattan distance (D = 1.0f) - Diagonal distance (D = sqrt(2))
            float D = 1.0f;
            float dx = Mathf.Abs(nodeA.mGridX - nodeB.mGridX);
            float dy = Mathf.Abs(nodeA.mGridY - nodeB.mGridY);
            return D * (dx + dy);
        }
    }

    /***************************************************************************/

    private float Heuristic(NodePathfinding nodeA, NodePathfinding nodeB)
    {
        // Heuristic function
        //nodeA.mGridX   nodeB.mGridX
        //nodeA.mGridY   nodeB.mGridY

        // REFERENCES:
        // http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#heuristics-for-grid-maps

        // For 4-directions, use Manhattan distance (L1)
        // For 8-directions, use Chebyshev distance (L-Infinity)
        // For any direction, you can use Euclidean distance, but an alternative map representation may be better (e.g. using Waypoints)
        // Why do we prefer these instead of euclidean distance?
        // Its cheaper to calculate, 
        float D = 1.0f;
        int dstX, dstY;
        switch (HeuristicFunction)
        {
            case "Manhattan":
                // Manhattan distance
                dstX = Mathf.Abs(nodeA.mGridX - nodeB.mGridX);
                dstY = Mathf.Abs(nodeA.mGridY - nodeB.mGridY);
                return D * (dstX + dstY);
            case "Chebyshev":
                float dx = Mathf.Abs(nodeA.mGridX - nodeB.mGridX);
                float dy = Mathf.Abs(nodeA.mGridY - nodeB.mGridY);
                return D * Mathf.Min(dx, dy) + (Mathf.Abs(dx - dy));
            case "Octile":
                D = DIAG_COST;
                // Octile distance
                dstX = Mathf.Abs(nodeA.mGridX - nodeB.mGridX);
                dstY = Mathf.Abs(nodeA.mGridY - nodeB.mGridY);
                return D * (dstX + dstY) + (D - 2 * D) * Mathf.Min(dstX, dstY);
            case "Euclidean":
                // Euclidean distance
                dstX = Mathf.Abs(nodeA.mGridX - nodeB.mGridX);
                dstY = Mathf.Abs(nodeA.mGridY - nodeB.mGridY);
                return Mathf.Sqrt(dstX * dstX + dstY * dstY);
        }

        return 0.0f;
    }

    /***************************************************************************/
    /***************************************************************************/

    /// <summary>
    /// Smooths the path removing unnecessary nodes (finds the waypoints of the path)
    /// It checks if the path is walkable between two nodes (using Bresenham's line algorithm)
    /// Starting from the first node, it checks if the path is walkable between the first and the last node
    /// If not, then, it checks if the path is walkable between the first and the last-1 node
    /// And so on, until it finds a walkable path
    /// Then repeat the process with that walkable node and the last node
    /// Until the first and the last node are the same
    /// </summary>
    /// <param name="path"></param>
    private List<NodePathfinding> SmoothPath8C(List<NodePathfinding> path, float maxOverestimationPercentage = 0.1f)
    {
        List<NodePathfinding> newPath = new List<NodePathfinding> { path[0] };

        // Make sure maxOverestimationPercentage is higher than 0
        maxOverestimationPercentage = Mathf.Max(maxOverestimationPercentage, EPSILON);

        // While the new start node is not the same as the last node
        while (newPath[^1] != path[^1])
        {
            // For each node in the path starting from the last node
            for (int i = path.Count - 1; i > 0; i--)
            {
                // G Cost of latest node minus G Cost of current node
                float GCostOfPath = path[i].gCost - newPath[^1].gCost;
                // If the path is walkable between the new start node and the current node
                if (!BresenhamWalkable8C(newPath[^1].mGridX, newPath[^1].mGridY, path[i].mGridX, path[i].mGridY, GCostOfPath,
                        maxOverestimationPercentage)) continue;
                // Add the current node to the new path
                newPath.Add(path[i]);
                break;
            }
        }

        // Sort the new path
        newPath.Sort((a, b) => a.gCost.CompareTo(b.gCost));
        return newPath;
    }

    private List<NodePathfinding> SmoothPath4C(List<NodePathfinding> path, float maxOverestimationPercentage = 0.1f)
    {
        List<NodePathfinding> newPath = new List<NodePathfinding> { path[0] };

        // Make sure maxOverestimationPercentage is higher than 0
        maxOverestimationPercentage = Mathf.Max(maxOverestimationPercentage, EPSILON);

        // While the new start node is not the same as the last node
        while (newPath[^1] != path[^1])
        {
            // For each node in the path
            for (int i = path.Count - 1; i > 0; i--)
            {
                // G Cost of latest node minus G Cost of current node
                float GCostOfPath = path[i].gCost - newPath[^1].gCost;
                // If the path is walkable between the new start node and the current node
                if (!BresenhamWalkable4C(newPath[^1].mGridX, newPath[^1].mGridY, path[i].mGridX, path[i].mGridY, GCostOfPath,
                        maxOverestimationPercentage)) continue;
                newPath.Add(path[i]);
                break;
            }
        }

        newPath.Add(path[^1]);
        return newPath;
    }

    /***************************************************************************/

    /// <summary>
    /// Breseham's line algorithm for 8-connectivity
    /// </summary>
    /// <param name="x" type="int"> x coordinate of the first point </param>
    /// <param name="y" type="int"> y coordinate of the first point </param>
    /// <param name="x2" type="int"> x coordinate of the second point </param>
    /// <param name="y2" type="int"> y coordinate of the second point </param>
    /// <param name="costToMatch" type="float"> The cost of the path from (x,y) to (x2,y2) </param>
    /// <param name="maxOverestimationPercentage" type="float"> The maximum percentage of overestimation allowed </param>
    /// <returns> True if the path is walkable, false otherwise.
    ///           If the cost of the path is overestimated, then it returns false
    ///           Overestimation is calculated as the difference between the costToMatch*maxOverestimationPercentage against the
    ///           traversed cost of the path which is calculated by adding the base cost of each node traversed (1.0f for orthogonal and 1.41421356237f for diagonal)
    /// </returns>
    public bool BresenhamWalkable8C(int x, int y, int x2, int y2, float costToMatch, float maxOverestimationPercentage)
    {
        maxOverestimationPercentage++;
        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        float traversedCost = 0.0f;
        dx1 = w switch
        {
            // If the line is horizontal or vertical, then it is walkable
            < 0 => -1,
            > 0 => 1,
            _ => dx1
        };
        dy1 = h switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => dy1
        };
        dx2 = w switch
        {
            // If the line is diagonal, then it is walkable
            < 0 => -1,
            > 0 => 1,
            _ => dx2
        };
        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);
        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            dy2 = h switch
            {
                < 0 => -1,
                > 0 => 1,
                _ => dy2
            };
            dx2 = 0;
        }

        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            // If the node is not walkable, then the path is not walkable
            if (!Grid.GetNode(x, y).mWalkable) return false;
            if (traversedCost > costToMatch * maxOverestimationPercentage) return false;
            numerator += shortest;
            // If the numerator is greater than the longest, then add the x and y coordinates of the node
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;

                // We add the cost to the traversed cost
                // If the line is diagonal, then the cost of the node is 1.41421356237f
                // We have to make sure that the node is still on the grid (not out of bounds, 30x30 grid)
                if (x >= 0 && x < Grid.gridWorldSize.x && y >= 0 && y < Grid.gridWorldSize.y)
                    traversedCost += DIAG_COST * Grid.GetNode(x, y).mCostMultiplier;
                else traversedCost += DIAG_COST;
            }

            // If the numerator is less than the longest, then add the x and y coordinates of the node
            else
            {
                x += dx2;
                y += dy2;

                // We add the cost to the traversed cost
                // If the line is orthogonal, then the cost of the node is 1.0f
                // We have to make sure that the node is still on the grid (not out of bounds, 30x30 grid)
                if (x >= 0 && x < Grid.gridWorldSize.x && y >= 0 && y < Grid.gridWorldSize.y)
                    traversedCost += 1.0f * Grid.GetNode(x, y).mCostMultiplier;
                else traversedCost += 1.0f;
            }
        }

        return true;
    }

    /// <summary>
    /// Breseham's line algorithm for 4-connectivity
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <returns></returns>
    /// https://stackoverflow.com/questions/13542925/line-rasterization-4-connected-bresenham
    public bool BresenhamWalkable4C(int x1, int y1, int x2, int y2, float costToMatch, float maxOverestimationPercentage)
    {
        maxOverestimationPercentage++;
        int dx = Mathf.Abs(x2 - x1), dy = Mathf.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        float estimation = 0.0f;
        while (true)
        {
            // If the current node is not walkable, then return false
            if (!Grid.GetNode(x1, y1).mWalkable) return false;

            // If the current node is the last node, then return true
            if (x1 == x2 && y1 == y2 && estimation <= costToMatch * maxOverestimationPercentage) break;
            // If the current node is not the last node, but the estimation is greater than the cost of the path, then return false
            if (estimation > costToMatch * maxOverestimationPercentage) return false;
            int e2 = err << 1;
            // If the error is greater than -dy, then move in the x direction
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;

                // We add the cost to the estimation
                // We have to make sure that the node is still on the grid (not out of bounds, 30x30 grid)
                if (x1 >= 0 && x1 < Grid.gridWorldSize.x && y1 >= 0 && y1 < Grid.gridWorldSize.y)
                    estimation += 1.0f * Grid.GetNode(x1, y1).mCostMultiplier;
                else estimation += 1.0f;
            }
            // If the error is less than dx, then move in the y direction
            else if (e2 < dx)
            {
                // else if instead of if
                err += dx;
                y1 += sy;

                // We add the gCost of the node to the estimation
                // We have to make sure that the node is still on the grid (not out of bounds, 30x30 grid)
                if (x1 >= 0 && x1 < Grid.gridWorldSize.x && y1 >= 0 && y1 < Grid.gridWorldSize.y)
                    estimation += 1.0f * Grid.GetNode(x1, y1).mCostMultiplier;
                else estimation += 1.0f;
            }
        }

        return true;
    }

    // OnValdation, make sure PercentageAdmissibleOnWaypoints is higher than 0
    private void OnValidate()
    {
        if (PercentageAdmissible <= 0)
        {
            PercentageAdmissible = 0.001f;
        }
    }
}