using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    public float Speed = 10.0f;
    public GameObject Astar;
    private List<NodePathfinding> mPath;

    private int targetIndex;
    /***************************************************************************/

    private void Update()
    {
        // Mouse click
        if (!Input.GetMouseButtonDown(0)) return;
        // Raycast
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Hit?
        if (!Physics.Raycast(ray, out hit, 1000.0f)) return;
        // Find path
        mPath = Astar.GetComponent<Pathfinding>().FindPath(transform.position, hit.point, -1);

        // If a path was found follow it
        if (mPath == null) return;
        targetIndex = 0;
        StopCoroutine("FollowPath");
        StartCoroutine("FollowPath");
    }

    /***************************************************************************/

    IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = mPath[0].mWorldPosition;
        while (true)
        {
            if (transform.position == currentWaypoint)
            {
                targetIndex++;
                if (targetIndex >= mPath.Count)
                {
                    yield break;
                }

                currentWaypoint = mPath[targetIndex].mWorldPosition;
            }

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, Speed * Time.deltaTime);
            yield return null;
        }
    }

    /***************************************************************************/

    public int GetTargetIndex()
    {
        return targetIndex;
    }

    /***************************************************************************/
}