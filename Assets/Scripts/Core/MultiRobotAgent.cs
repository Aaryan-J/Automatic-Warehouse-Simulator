using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiRobotAgent : MonoBehaviour
{
    public Transform robotPrefab;
    public bool debugMode = true;
    private Grid_script grid;
    private Pathfinding pathfinding;
    public TaskManager taskManager;

    public int obstacleCount = 5;

    private List<RobotBehaviour> robots = new List<RobotBehaviour>();

    public List<RobotBehaviour> Robots => robots;

    void Start()
    {
        // Create grid and pathfinding
        grid = new Grid_script(25, 25, 1f);
        pathfinding = new Pathfinding(grid);

        taskManager = FindObjectOfType<TaskManager>();
        taskManager.Initialize(grid);
        StartCoroutine(GenerateTasksafterDelay());

        //blocked tiles
        for (int i = 0; i < obstacleCount; i++)
        {
            Node n;
            int attempts = 0;
            do
            {
                n = grid.GetNode(Random.Range(0, grid.width), Random.Range(0, grid.height));
                attempts++;
                if (attempts > 100) break;
            } while (!n.isWalkable || taskManager.predefinedPickups.Contains(n) || taskManager.predefinedDeliveries.Contains(n));

            grid.SetWalkable(n.x, n.z, false);
            grid.UpdateNodeVisual(n);
        }
    }

    IEnumerator GenerateTasksafterDelay()
    {
        yield return new WaitForEndOfFrame();
        taskManager.GenerateRandomTasks();
    }

    void Update()
    {
        HandleRobotSpawn();
    }

    void HandleRobotSpawn()
    {
        if (!Input.GetKeyDown(KeyCode.N)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        Node spawnNode = grid.GetNodeFromVisual(hit.collider.gameObject);
        if (spawnNode != null && spawnNode.isWalkable)
        {
            SpawnRobot(spawnNode.worldPosition);
        }
    }

    void SpawnRobot(Vector3 pos)
    {
        Transform robotObj = Instantiate(robotPrefab, pos + Vector3.up * 0.1f, Quaternion.identity);
        RobotBehaviour rb = robotObj.GetComponent<RobotBehaviour>();
        
        rb.Initialize(grid, pathfinding, debugMode, taskManager);

        Node startNode = grid.GetNodeFromWorldPosition(pos);
        if (startNode != null) startNode.isOccupied = true;

        robots.Add(rb);
    }

    void OnDrawGizmos()
    {
        if (!debugMode || robots == null) return;

        foreach (RobotBehaviour r in robots)
        {
            r.DrawDebugPath();
        }
    }
}