using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

public class Planning_Mine : MonoBehaviour
{
    public int seed = 42;
    [Tooltip("20 is the base for 50/50, choose carefully")]
    public int heuristicWeight = 200;
    private NodePlanning_Mine CurrentStartNode;
    private NodePlanning_Mine CurrentTargetNode;

    private World_Mine mWorld;
    /***************************************************************************/

    private void Start()
    {
        // Define a Random seed for the whole game
        Random.InitState(seed);
        
        mWorld = GetComponent<World_Mine>();

        PlanningSetUp(mWorld.mWorldStateMask, mWorld.mWorldStateHealth, mWorld.mWorldStateStamina, mWorld.mWorldStateMonsterHealth,
            mWorld.mWorldStateMaskTarget, mWorld.mWorldStateMinumumHealth, mWorld.mWorldStateMinumumStamina, 0, mWorld.mWorldStateMonsterHealth);
    }

    public List<NodePlanning_Mine> PlanningSetUp(WorldState_Mask initialMask, int initialHealth, int initialStamina, int initialMonsterHealth, WorldState_Mask targetMask, int targetHealth, int targetStamina, int targetMonsterHealth, int targetMH)
    {
        UnityEngine.Debug.Log("Planning...");
        WorldState_Mine startWorldState = new WorldState_Mine(initialMask, initialHealth, initialStamina, initialMonsterHealth);
        WorldState_Mine targetWorldState = new WorldState_Mine(targetMask, targetHealth, targetStamina, targetMonsterHealth, default, default, targetMH);
        return FindInitialPlan(startWorldState, targetWorldState);
    }

    public List<NodePlanning_Mine> PlanningSetUp(WorldState_Mask initialMask, int initialHealth, int initialStamina, int initialMonsterHealth,
        WorldState_Mine targetNode)
    {
        return PlanningSetUp(initialMask, initialHealth, initialStamina, initialMonsterHealth, 
            targetNode.mWorldStateMask, targetNode.playerHealth, targetNode.stamina, targetNode.monsterCurrentHealth, targetNode.monsterHealth);
    }

    /***************************************************************************/

    public List<NodePlanning_Mine> FindPlan(NodePlanning_Mine startNode, NodePlanning_Mine targetNode)
    {
        CurrentStartNode = startNode;
        CurrentTargetNode = targetNode;
        
        List<NodePlanning_Mine> openSet = new List<NodePlanning_Mine>();
        HashSet<NodePlanning_Mine> closedSet = new HashSet<NodePlanning_Mine>();
        openSet.Add(CurrentStartNode);
        mWorld.openSet = openSet;
        NodePlanning_Mine node = CurrentStartNode;
        
        // While there are nodes to check and the target has not been reached
        while (openSet.Count > 0 && !WorldState_Mine.FinalStateCheck(node.mWorldState, CurrentTargetNode.mWorldState))
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
            if (!WorldState_Mine.FinalStateCheck(node.mWorldState, CurrentTargetNode.mWorldState))
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
                        if (!openSet.Any(n => n.mWorldState == neighbour.mWorldState))
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
        // Log the chosen weapon
        UnityEngine.Debug.LogFormat("Weapon: {0}", mWorld.mWorldStateMask.HasFlag(WorldState_Mask.WS_WEAPON_TYPE_SWORD) ? "Sword" : mWorld.mWorldStateMask.HasFlag(WorldState_Mask.WS_WEAPON_TYPE_LANCE) ? "Lance" : mWorld.mWorldStateMask.HasFlag(WorldState_Mask.WS_WEAPON_TYPE_HAMMER) ? "Hammer" : "Longsword");
        if (mWorld.plan != null)
        {
            UnityEngine.Debug.LogFormat("Plan length: {0}", mWorld.plan.Count);
            foreach (NodePlanning_Mine nodePlanning in mWorld.plan)
            {
                UnityEngine.Debug.LogFormat("Action: {0}", nodePlanning.MActionPlanning);
            }
        }
        else
        {
            UnityEngine.Debug.Log("Plan is null!");
        }

        return mWorld.plan;
    }

    /// <summary>
    /// This will set 4 initial plans, one for each possible initial state depending on weapons
    /// Then, the best plan will be selected
    /// </summary>
    /// <returns></returns>
    public List<NodePlanning_Mine> FindInitialPlan(WorldState_Mine initial, WorldState_Mine final)
    {
        Weapon weapon = null;
        
        // Check if the current initial state defines a weapon
        if ((initial.mWorldStateMask & WorldState_Mask.WS_WEAPON_EQUIPPED) != 0)
        {
            // Set the weapon on the final state as the same as the initial state
            if ((initial.mWorldStateMask & WorldState_Mask.WS_WEAPON_TYPE_SWORD) != 0)
            {
                weapon = new Weapon(WeaponType.SWORD);
            }
            else if ((initial.mWorldStateMask & WorldState_Mask.WS_WEAPON_TYPE_LANCE) != 0)
            {
                weapon = new Weapon(WeaponType.LANCE);
            }
            else if ((initial.mWorldStateMask & WorldState_Mask.WS_WEAPON_TYPE_HAMMER) != 0)
            {
                weapon = new Weapon(WeaponType.HAMMER);
            }
            else
            {
                weapon = new Weapon(WeaponType.LONGSWORD);
            }
        }
        // If the final state defines one, set it on the initial state
        else if ((final.mWorldStateMask & WorldState_Mask.WS_WEAPON_EQUIPPED) != 0)
        {
            if ((final.mWorldStateMask & WorldState_Mask.WS_WEAPON_TYPE_SWORD) != 0)
            {
                initial.mWorldStateMask |= WorldState_Mask.WS_WEAPON_TYPE_SWORD;
                weapon = new Weapon(WeaponType.SWORD);
            }
            else if ((final.mWorldStateMask & WorldState_Mask.WS_WEAPON_TYPE_LANCE) != 0)
            {
                initial.mWorldStateMask |= WorldState_Mask.WS_WEAPON_TYPE_LANCE;
                weapon = new Weapon(WeaponType.LANCE);
            }
            else if ((final.mWorldStateMask & WorldState_Mask.WS_WEAPON_TYPE_HAMMER) != 0)
            {
                initial.mWorldStateMask |= WorldState_Mask.WS_WEAPON_TYPE_HAMMER;
                weapon = new Weapon(WeaponType.HAMMER);
            }
            else
            {
                initial.mWorldStateMask |= WorldState_Mask.WS_WEAPON_TYPE_LONGSWORD;
                weapon = new Weapon(WeaponType.LONGSWORD);
            }
            
            // Activate the initial weapon equpped
            initial.mWorldStateMask |= WorldState_Mask.WS_WEAPON_EQUIPPED;
        }
        
        // Create the nodeplannings for initial and final states
        // CurrentStartNode = new NodePlanning_Mine(new ActionPlanning_Mine(), startWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, startWorldState.stamina,
        // startWorldState.playerHealth, startWorldState.monsterHealth, default, startWorldState.monsterHealth);
        // CurrentTargetNode = new NodePlanning_Mine(new ActionPlanning_Mine(), targetWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, targetWorldState.stamina,
        //     targetWorldState.playerHealth, 0, null, startWorldState.monsterHealth);

        NodePlanning_Mine initialNode = new NodePlanning_Mine(new ActionPlanning_Mine(), initial, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, initial.stamina,
            initial.playerHealth, initial.monsterHealth, weapon, initial.monsterHealth);
        
        NodePlanning_Mine finalNode = new NodePlanning_Mine(new ActionPlanning_Mine(), final, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, final.stamina,
            final.playerHealth, 0, weapon, initial.monsterHealth);
        
        // If we have a weapon, we Find plan with both nodeplanings
        List<NodePlanning_Mine> plan = null;
        if (weapon != null)
        {
            // Find plan with both nodeplanings
            plan =
            FindPlan(initialNode, finalNode);
        }
        else
        {
            plan =
            FindFullPlan(initialNode, finalNode);
            
            //Log the plan then return it
            UnityEngine.Debug.Log("--------------------");
            UnityEngine.Debug.Log("PLAN FOUND!");
            foreach (var t in plan)
            {
                UnityEngine.Debug.LogFormat("{0} Accumulated cost: {1}", t.MActionPlanning.mName, t.gCost);
            }
        }

        return plan;
        
    }

    private List<NodePlanning_Mine> FindFullPlan(NodePlanning_Mine startNode, NodePlanning_Mine targetNode)
    {
        // Generates 4 FindPlans, one for each weapon
        // Then, selects the best one
        
        // Set the weapon and set the equipped and weapon flag on the initial state
        startNode.mWorldState.mWorldStateMask |= WorldState_Mask.WS_WEAPON_EQUIPPED;
        // Create the 4 initial nodes
        NodePlanning_Mine longswordNode = new NodePlanning_Mine(new ActionPlanning_Mine(), startNode.mWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, startNode.mWorldState.stamina,
            startNode.mWorldState.playerHealth, startNode.mWorldState.monsterHealth, new Weapon(WeaponType.LONGSWORD), startNode.mWorldState.monsterHealth);
        
        startNode.mWorldState.mWorldStateMask |= WorldState_Mask.WS_WEAPON_TYPE_SWORD;
        NodePlanning_Mine swordNode = new NodePlanning_Mine(new ActionPlanning_Mine(), startNode.mWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, startNode.mWorldState.stamina,
            startNode.mWorldState.playerHealth, startNode.mWorldState.monsterHealth, new Weapon(WeaponType.SWORD), startNode.mWorldState.monsterHealth);
        
        // Unequip the sword
        startNode.mWorldState.mWorldStateMask &= ~WorldState_Mask.WS_WEAPON_TYPE_SWORD;
        startNode.mWorldState.mWorldStateMask |= WorldState_Mask.WS_WEAPON_TYPE_LANCE;
        NodePlanning_Mine lanceNode = new NodePlanning_Mine(new ActionPlanning_Mine(), startNode.mWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, startNode.mWorldState.stamina,
            startNode.mWorldState.playerHealth, startNode.mWorldState.monsterHealth, new Weapon(WeaponType.LANCE), startNode.mWorldState.monsterHealth);
        
        // Unequip the lance
        startNode.mWorldState.mWorldStateMask &= ~WorldState_Mask.WS_WEAPON_TYPE_LANCE;
        startNode.mWorldState.mWorldStateMask |= WorldState_Mask.WS_WEAPON_TYPE_HAMMER;
        NodePlanning_Mine hammerNode = new NodePlanning_Mine(new ActionPlanning_Mine(), startNode.mWorldState, ActionPlanning_Mine.ActionType.ACTION_TYPE_NONE, startNode.mWorldState.stamina,
            startNode.mWorldState.playerHealth, startNode.mWorldState.monsterHealth, new Weapon(WeaponType.HAMMER), startNode.mWorldState.monsterHealth);
        
        // Find plan for each weapon
        List<NodePlanning_Mine> longswordPlan = FindPlan(longswordNode, targetNode);
        List<NodePlanning_Mine> swordPlan = FindPlan(swordNode, targetNode);
        List<NodePlanning_Mine> lancePlan = FindPlan(lanceNode, targetNode);
        List<NodePlanning_Mine> hammerPlan = FindPlan(hammerNode, targetNode);
        
        // Select the best plan
        List<NodePlanning_Mine> bestPlan = longswordPlan;
        if (swordPlan.Count < bestPlan.Count)
        {
            bestPlan = swordPlan;
        }
        if (lancePlan.Count < bestPlan.Count)
        {
            bestPlan = lancePlan;
        }
        if (hammerPlan.Count < bestPlan.Count)
        {
            bestPlan = hammerPlan;
        }
        
        return bestPlan;
        
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
        // The closer the monster health is to goalNode.health the smaller the heuristic
        // The range is monsterHealth - goalNode.health
        // Needs to be scaled to 20 (maximum health) and 0 (minimum health)
        return nodeA.mWorldState.monsterCurrentHealth * heuristicWeight / nodeA.mWorldState.monsterHealth;
        
        /*
         // The function weight should be determinated by:
        // 70% - Monster health (the less health the closer to destination)
        // 15% - Player health (the more health the better)
        // 15% - Stamina (the more stamina the better)
        // NodeB is the target node (the monster health we want to reach, the player health we want to have and the stamina we want to have)
        // NodeA is the current node (the monster health we have, the player health we have and the stamina we have)
        // The function should return a value between 0 and 100
        */
        
        // TODO:
        // Monster health
        int monsterHealthHeuristic =
            ((goalNode.mWorldState.monsterHealth - nodeA.mWorldState.monsterHealth) * 100 / goalNode.mWorldState.monsterHealth) * heuristicWeight;
        // Player health
        int playerHealthHeuristic = (nodeA.mWorldState.playerHealth - goalNode.mWorldState.playerHealth) * 100 / nodeA.mWorldState.playerHealth;
        // Stamina
        int staminaHeuristic = (nodeA.mWorldState.stamina - goalNode.mWorldState.stamina) * 100 / nodeA.mWorldState.stamina;
        // Weighted average
        // 0 means that the node is exactly the same as the target node
        // 100 means that the node is the opposite of the target node
        return (int)(monsterHealthHeuristic * 0.7f + playerHealthHeuristic * 0.15f + staminaHeuristic * 0.15f);
         
        
    }

    /***************************************************************************/
}