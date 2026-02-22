using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Grid_script
{
    private Node[,] gridArray;
    public Dictionary<GameObject, Node> visualToNode = new Dictionary<GameObject, Node>();
    public int width, height;
    public float cellSize = 1f;

    private Transform gridParent;

    public Grid_script(int width, int height, float cellSize)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        gridArray = new Node[width, height];

        gridParent = new GameObject("Grid").transform;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                gridArray[i, j] = new Node(i, j, cellSize, width, height);
                SpawnGrid(gridArray[i, j]);
            }
        }
    }

    public Node GetNode(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            return gridArray[x, z];
        }
        else
        {
            return null;
        }
    }

    public void ResetNodes()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Node node = gridArray[i, j];
                node.gCost = 0;
                node.hCost = 0;
                node.parent = null;
            }
        }
    }

    public List<Node> GetNeighbours(Node node, bool ignoreOccupied = false)
    {
        List<Node> neighbours = new List<Node>();

        int[,] directions =
        {
            {0, 1},
            {1, 0},
            {0, -1},
            {-1, 0}
        };

        for (int i = 0; i < 4; i++)
        {
            int checkX = node.x + directions[i, 0];
            int checkZ = node.z + directions[i, 1];

            Node neighbour = GetNode(checkX, checkZ);

            if (neighbour != null && neighbour.isWalkable && (ignoreOccupied || !neighbour.isOccupied))
            {
                neighbours.Add(neighbour);
            }
        }
        return neighbours;
    }

    public Node GetNodeFromWorldPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x + (width * cellSize / 2f)) / cellSize);
        int z = Mathf.FloorToInt((worldPos.z + (height * cellSize / 2f)) / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        z = Mathf.Clamp(z, 0, height - 1);

        return GetNode(x, z);
    }

    public void SetWalkable(int x, int z, bool walkable)
    {
        Node node = GetNode(x, z);
        if (node != null)
        {
            node.isWalkable = walkable;
            UpdateNodeVisual(node);
        }
    }

    public void UpdateNodeVisual(Node node)
    {
        if (node.visual != null)
        {
            if (!node.isWalkable)
            {
                node.visual.GetComponent<Renderer>().material.color = Color.red;
            }
            else if (node.isOccupied)
            {
                node.visual.GetComponent<Renderer>().material.color = Color.yellow;
            }
            else
            {
                if ((node.x + node.z) % 2 == 0)
                {
                    node.visual.GetComponent<Renderer>().material.color = new Color(0.85f, 0.85f, 0.85f);
                }
                else
                {
                    node.visual.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 0.8f);
                }
            }
        }
    }

    private void SpawnGrid(Node node)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = $"x: {node.x} z: {node.z}";

        g.transform.position = node.worldPosition;
        g.transform.localScale = new Vector3(cellSize, 0.05f, cellSize);

        g.transform.parent = gridParent;

        Renderer renderer = g.GetComponent<Renderer>();
        if ((node.x + node.z) % 2 == 0)
        {
            renderer.material.color = new Color(0.85f, 0.85f, 0.85f);
        }
        else
        {
            renderer.material.color = new Color(0.8f, 0.8f, 0.8f);
        }
        node.visual = g;

        //Add to dictionary
        visualToNode[g] = node;
    }

    public Node GetNodeFromVisual(GameObject visual)
    {
        visualToNode.TryGetValue(visual, out Node node);
        return node;
    }
}
