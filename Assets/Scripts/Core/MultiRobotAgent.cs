using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MultiRobotAgent : MonoBehaviour
{
    public Transform robotPrefab;
    public bool debugMode = true;
    private Grid_script grid;
    private Pathfinding pathfinding;
    public TaskManager taskManager;

    private List<RobotBehaviour> robots = new List<RobotBehaviour>();

    public List<RobotBehaviour> Robots => robots;

    void Start()
    {
        // 1. Create grid and pathfinding
        grid = new Grid_script(10, 10, 1f);
        pathfinding = new Pathfinding(grid);

        // 2. Example blocked tiles
        grid.SetWalkable(4, 4, false);
        grid.SetWalkable(5, 3, false);
        grid.SetWalkable(5, 4, false);
        grid.SetWalkable(5, 6, false);
        grid.SetWalkable(0, 4, false);
        grid.SetWalkable(1, 4, false);

        TaskManager taskManager = FindObjectOfType<TaskManager>();
        taskManager.Initialize(grid);
        // setup predefined tasks
        taskManager.predefinedPickups.Add(grid.GetNode(2, 2));
        taskManager.predefinedDeliveries.Add(grid.GetNode(8, 8));
        taskManager.predefinedPickups.Add(grid.GetNode(1, 2));
        taskManager.predefinedDeliveries.Add(grid.GetNode(9, 8));

        taskManager.AddPredefinedTasks();
    }

    void Update()
    {
        HandleObstacleToggle();
        HandleTargetSet();
        HandleRobotSpawn();
    }

    void HandleObstacleToggle()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        Node clickedNode = grid.GetNodeFromVisual(hit.collider.gameObject);
        if (clickedNode != null)
            grid.SetWalkable(clickedNode.x, clickedNode.z, !clickedNode.isWalkable);
    }

    void HandleTargetSet()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        Node clickedNode = grid.GetNodeFromVisual(hit.collider.gameObject);
        if (clickedNode != null && clickedNode.isWalkable)
        {
            RobotBehaviour nearest = GetNearestRobot(clickedNode.worldPosition);
            nearest?.SetTarget(clickedNode.worldPosition);
        }
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

    RobotBehaviour GetNearestRobot(Vector3 pos)
    {
        if (robots.Count == 0) return null;

        RobotBehaviour nearest = robots[0];
        float minDist = Vector3.Distance(pos, nearest.Position);

        for (int i = 1; i < robots.Count; i++)
        {
            float dist = Vector3.Distance(pos, robots[i].Position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = robots[i];
            }
        }

        return nearest;
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