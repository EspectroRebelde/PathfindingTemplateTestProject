﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

public class Planning_Mine : MonoBehaviour
{
    private NodePlanning_Mine CurrentStartNode;
    private NodePlanning_Mine CurrentTargetNode;

    private World_Mine mWorld;
    /***************************************************************************/

    private void Start()
    {
        mWorld = GetComponent<World_Mine>();
        UnityEngine.Debug.Log("Planning...");
        WorldState_Mine startWorldState = new WorldState_Mine(mWorld.mWorldStateMask, mWorld.mWorldStateHealth, mWorld.mWorldStateStamina);
        WorldState_Mine targetWorldState = new WorldState_Mine(mWorld.mWorldStateMask, mWorld.mWorldStateHealth, mWorld.mWorldStateStamina);
        FindPlan(startWorldState, targetWorldState);
    }

    /***************************************************************************/

    public List<NodePlanning_Mine> FindPlan(WorldState_Mine startWorldState, WorldState_Mine targetWorldState)
    {
        CurrentStartNode = new NodePlanning_Mine(null, startWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, startWorldState.stamina,
            startWorldState.playerHealth, startWorldState.monsterHealth, default, startWorldState.monsterHealth);
        CurrentTargetNode = new NodePlanning_Mine(null, targetWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, targetWorldState.stamina,
            targetWorldState.playerHealth, 0, null, startWorldState.monsterHealth);
        
        List<NodePlanning_Mine> openSet = new List<NodePlanning_Mine>();
        HashSet<NodePlanning_Mine> closedSet = new HashSet<NodePlanning_Mine>();
        openSet.Add(CurrentStartNode);
        mWorld.openSet = openSet;
        NodePlanning_Mine node = CurrentStartNode;
        while (openSet.Count > 0 && ((node.mWorldState.mWorldStateMask & CurrentTargetNode.mWorldState.mWorldStateMask) !=
                                     CurrentTargetNode.mWorldState.mWorldStateMask))
        {
            // Select best node from open list
            node = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < node.fCost || (openSet[i].fCost == node.fCost && openSet[i].hCost < node.hCost))
                {
                    node = openSet[i];
                }
            }

            // Manage open/closed list
            openSet.Remove(node);
            closedSet.Add(node);
            mWorld.openSet = openSet;
            mWorld.closedSet = closedSet;

            // Check destination
            if (((node.mWorldState.mWorldStateMask & CurrentTargetNode.mWorldState.mWorldStateMask) != CurrentTargetNode.mWorldState.mWorldStateMask))
            {
                // Open neighbours
                foreach (NodePlanning_Mine neighbour in mWorld.GetNeighbours(node))
                {
                    if ( /*!neighbour.mWalkable ||*/ closedSet.Any(n => n.mWorldState == neighbour.mWorldState))
                    {
                        continue;
                    }

                    float newCostToNeighbour = node.gCost + GetDistance(node, neighbour);
                    if (newCostToNeighbour < neighbour.gCost || openSet.All(n => n.mWorldState != neighbour.mWorldState))
                    {
                        neighbour.gCost = newCostToNeighbour;
                        neighbour.hCost = Heuristic(neighbour, CurrentTargetNode);
                        neighbour.mParent = node;
                        if (openSet.All(n => n.mWorldState != neighbour.mWorldState))
                        {
                            openSet.Add(neighbour);
                            mWorld.openSet = openSet;
                        }
                        else
                        {
                            // Find neighbour and replace
                            openSet[openSet.FindIndex(x => x.mWorldState == neighbour.mWorldState)] = neighbour;
                        }
                    }
                }
            }
            else
            {
                // Path found!

                // End node must be copied
                CurrentTargetNode.mParent = node.mParent;
                CurrentTargetNode.MActionPlanning = node.MActionPlanning;
                CurrentTargetNode.gCost = node.gCost;
                CurrentTargetNode.hCost = node.hCost;
                RetracePlan(CurrentStartNode, CurrentTargetNode);
                UnityEngine.Debug.Log("Statistics:");
                UnityEngine.Debug.LogFormat("Total nodes:  {0}", openSet.Count + closedSet.Count);
                UnityEngine.Debug.LogFormat("Open nodes:   {0}", openSet.Count);
                UnityEngine.Debug.LogFormat("Closed nodes: {0}", closedSet.Count);
            }
        }

        // Log plan
        UnityEngine.Debug.Log("PLAN FOUND!");
        foreach (var t in mWorld.plan)
        {
            UnityEngine.Debug.LogFormat("{0} Accumulated cost: {1}", t.MActionPlanning.mName, t.gCost);
        }

        return mWorld.plan;
    }

    /***************************************************************************/

    private void RetracePlan(NodePlanning_Mine startNode, NodePlanning_Mine endNode)
    {
        List<NodePlanning_Mine> plan = new List<NodePlanning_Mine>();
        NodePlanning_Mine currentNode = endNode;
        while (currentNode != startNode)
        {
            plan.Add(currentNode);
            currentNode = currentNode.mParent;
        }

        plan.Reverse();
        mWorld.plan = plan;
    }

    /***************************************************************************/

    private float GetDistance(NodePlanning_Mine nodeA, NodePlanning_Mine nodeB)
    {
        // Distance function
        return nodeB.MActionPlanning.mCost;
    }

    /***************************************************************************/

    private int Heuristic(NodePlanning_Mine nodeA, NodePlanning_Mine goalNode)
    {
        // Heuristic function
        // The function weight should be determinated by:
        // 70% - Monster health (the less health the closer to destination)
        // 15% - Player health (the more health the better)
        // 15% - Stamina (the more stamina the better)
        // NodeB is the target node (the monster health we want to reach, the player health we want to have and the stamina we want to have)
        // NodeA is the current node (the monster health we have, the player health we have and the stamina we have)
        // The function should return a value between 0 and 100
        // 0 means that the node is exactly the same as the target node
        // 100 means that the node is the opposite of the target node
        
        // Monster health
        int monsterHealthHeuristic = (goalNode.mWorldState.monsterHealth - nodeA.mWorldState.monsterHealth) * 100 / goalNode.mWorldState.monsterHealth;
        // Player health
        int playerHealthHeuristic = (nodeA.mWorldState.playerHealth - goalNode.mWorldState.playerHealth) * 100 / nodeA.mWorldState.playerHealth;
        // Stamina
        int staminaHeuristic = (nodeA.mWorldState.stamina - goalNode.mWorldState.stamina) * 100 / nodeA.mWorldState.stamina;
        // Weighted average
        return (monsterHealthHeuristic * 70 + playerHealthHeuristic * 15 + staminaHeuristic * 15) / 100;
    }

    /***************************************************************************/
}