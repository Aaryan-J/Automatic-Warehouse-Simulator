using UnityEngine;

public class Node
{
    public int x, z;
    public bool isWalkable;
    public Vector3 worldPosition;
    public GameObject visual;
    public bool isOccupied;

    public bool isPickup;
    public bool isDelivery;

    public RobotBehaviour assignedRobot = null;

    // A* variables
    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;

    public Node parent;

    public Node(int x, int z, float cellSize, int width, int height)
    {
        this.x = x;
        this.z = z;
        this.isWalkable = true;

        worldPosition = new Vector3(x * cellSize - (width * cellSize /2f), 0, z * cellSize - (height * cellSize/2f));
    }
}
