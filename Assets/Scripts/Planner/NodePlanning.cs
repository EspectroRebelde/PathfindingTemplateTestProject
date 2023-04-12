using UnityEngine;
using System.Collections;

public class NodePlanning
{
	public World.WorldState   mWorldState;
  
  public ActionPlanning             MActionPlanning;
                            
	public float              gCost;
	public float              hCost;
                            
	public NodePlanning       mParent;
	
  /***************************************************************************/

	public NodePlanning( World.WorldState worldState, ActionPlanning actionPlanning )
  {
    mWorldState     = worldState;
    MActionPlanning         = actionPlanning;

    gCost           = 0.0f;
    hCost           = 0.0f;
    mParent         = null;
  }
                                                      
  /***************************************************************************/

	public float fCost {
		get {
			return gCost + hCost;
		}
	}

  /***************************************************************************/

  public bool Equals( NodePlanning other )
  {
    return mWorldState == other.mWorldState;
  }

  /***************************************************************************/

}
