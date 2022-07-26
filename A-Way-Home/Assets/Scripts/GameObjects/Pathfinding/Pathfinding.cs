using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    public static Vector3[] FindPath(Vector3 startpos, Vector3 targetpos, bool canWalkWater = false)
    {
        NodeGrid nodeGrid = NodeGrid.Instance;
        Vector3[] path = new Vector3[0];
        Node startnode = NodeGrid.NodeWorldPointPos(startpos);
        Node targetnode = NodeGrid.NodeWorldPointPos(targetpos);

        if (!startnode.IsWalkable(canWalkWater) && !targetnode.IsWalkable(canWalkWater))
        {    
            Debug.Log("No Path Found!");
            return path;
        }
        List<Node> OpenSet = new List<Node>();
        HashSet<Node> ClosedSet = new HashSet<Node>();
        OpenSet.Add(startnode);

        while (OpenSet.Count > 0)
        {
            // 
            Node CurrentNode = OpenSet[0];
            for (int i = 1; i < OpenSet.Count; i++)
                if (OpenSet[i].fCost < CurrentNode.fCost || OpenSet[i].fCost == CurrentNode.fCost && OpenSet[i].hCost < CurrentNode.hCost)
                    CurrentNode = OpenSet[i];

            OpenSet.Remove(CurrentNode);
            ClosedSet.Add(CurrentNode);

            if (CurrentNode == targetnode)
            {
                path = RetracePath(startnode, targetnode).ToArray();
                Debug.Log("Path Found! Path Total Nodes: " + path.Length);
                break;
            }
            // 
            foreach (Node Neighbor in Node.GetNeighbors(CurrentNode, nodeGrid.grid, nodeGrid.gridSizeInt))
            {
                if (!Neighbor.IsWalkable(canWalkWater) || ClosedSet.Contains(Neighbor))
                    continue;
                
                int newMovementCostToNeighbor = CurrentNode.gCost + GetDistance(CurrentNode, Neighbor);
                if (newMovementCostToNeighbor < Neighbor.gCost || !OpenSet.Contains(Neighbor))
                {
                    Neighbor.gCost = newMovementCostToNeighbor;
                    Neighbor.hCost = GetDistance(Neighbor, targetnode);
                    Neighbor.parent = CurrentNode;

                    if (!OpenSet.Contains(Neighbor))
                        OpenSet.Add(Neighbor);
                }
            }
        }
        // if (path.Length <= 0)
        //     return;
        //     Debug.Log("path is null");
        return path;
    }

    private static List<Vector3> RetracePath(Node startnode, Node targetnode)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Node CurrentNode = targetnode;
        while(CurrentNode != startnode)
        {
            waypoints.Add(CurrentNode.worldPosition);
            CurrentNode = CurrentNode.parent;
        }
        waypoints.Reverse();
        return waypoints;
    }

    private static int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridPos.x - nodeB.gridPos.x);
        int dstY = Mathf.Abs(nodeA.gridPos.y - nodeB.gridPos.y);

        if (dstX > dstY)
            return dstY + 10 * (dstX - dstY);
        return dstX + 10 * (dstY - dstX);
    }
}
