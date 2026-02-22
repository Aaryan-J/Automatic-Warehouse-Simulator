using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    private Grid_script grid;

    public Pathfinding(Grid_script grid)
    {
        this.grid = grid;
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos, bool ignoreOccupied = false)
    {
        Node startNode = grid.GetNodeFromWorldPosition(startPos);
        Node targetNode = grid.GetNodeFromWorldPosition(targetPos);

        if (startNode == null || targetNode == null)
            return null;

        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];

            // Find node with lowest fCost
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost ||
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbour in grid.GetNeighbours(currentNode, ignoreOccupied))
            {
                if (!neighbour.isWalkable || closedList.Contains(neighbour) ||
                    (!ignoreOccupied && neighbour.isOccupied))
                    continue;

                int newCost = currentNode.gCost + 10; // grid-based movement, 4 directions

                if (newCost < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        return null;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.x - b.x);
        int dstZ = Mathf.Abs(a.z - b.z);
        return 10 * (dstX + dstZ); // only 4-direction movement
    }
}