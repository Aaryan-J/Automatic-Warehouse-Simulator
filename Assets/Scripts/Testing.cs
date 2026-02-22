using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiRobotTesting : MonoBehaviour
{
    public Transform robotPrefab;
    public bool debugMode = true;
    private Grid_script grid;
    private Pathfinding pathfinding;

    private List<RobotBehaviour> robots = new List<RobotBehaviour>();

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
        RobotBehaviour rb = robotObj.gameObject.AddComponent<RobotBehaviour>();
        rb.Initialize(grid, pathfinding, debugMode);

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

// Handles individual robot behaviour
public class RobotBehaviour : MonoBehaviour
{
    private Grid_script grid;
    private Pathfinding pathfinding;
    private bool debugMode;

    private List<Node> currentPath = new List<Node>();
    private int pathIndex = 0;
    private Vector3 targetPos;
    private bool pathNeedsRecalc = false;

    private Node previousNode = null;

    public Vector3 Position => transform.position;

    public void Initialize(Grid_script g, Pathfinding p, bool dbg)
    {
        grid = g;
        pathfinding = p;
        debugMode = dbg;
        targetPos = transform.position;
        StartCoroutine(MoveRoutine());
    }

    public void SetTarget(Vector3 newTarget)
    {
        targetPos = newTarget;
        pathNeedsRecalc = true;
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            Node targetNode = grid.GetNodeFromWorldPosition(targetPos);
            if (targetNode == null || !targetNode.isWalkable)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            // Recalculate path if needed
            if (pathNeedsRecalc || currentPath == null || pathIndex >= currentPath.Count
                || !currentPath[pathIndex].isWalkable || (currentPath[pathIndex].isOccupied && previousNode != currentPath[pathIndex]))
            {
                if (previousNode != null)
                {
                    previousNode.isOccupied = true; // robot stays on its current tile
                    grid.UpdateNodeVisual(previousNode);
                }

                grid.ResetNodes();
                currentPath = pathfinding.FindPath(transform.position, targetPos, ignoreOccupied: true);
                pathIndex = 0;
                pathNeedsRecalc = false;

                if (currentPath == null || currentPath.Count == 0)
                {
                    yield return null;
                    continue;
                }
            }

            if (pathIndex < currentPath.Count)
            {
                Node currentNode = currentPath[pathIndex];

                // If next node is occupied by another robot, wait and recalc later
                if (currentNode.isOccupied && previousNode != currentNode)
                {
                    pathNeedsRecalc = true;
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                // Move towards the center of the current node
                Vector3 nextTarget = currentNode.worldPosition + Vector3.up * 0.1f;
                transform.position = Vector3.MoveTowards(transform.position, nextTarget, 2f * Time.deltaTime);

                // Check if we reached the node
                if (Vector3.Distance(transform.position, nextTarget) < 0.05f)
                {
                    // Free previous node
                    if (previousNode != null && previousNode != currentNode)
                    {
                        previousNode.isOccupied = false;
                        grid.UpdateNodeVisual(previousNode);
                    }

                    // Occupy current node
                    currentNode.isOccupied = true;
                    grid.UpdateNodeVisual(currentNode);

                    previousNode = currentNode;
                    pathIndex++;
                }
            }

            yield return null;
        }
    }

    public void DrawDebugPath()
    {
        if (currentPath == null || currentPath.Count < 1) return;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 from = currentPath[i].worldPosition + Vector3.up * 0.2f;
            Vector3 to = currentPath[i + 1].worldPosition + Vector3.up * 0.2f;
            Debug.DrawLine(from, to, Color.green);
        }

        if (currentPath.Count > 0)
        {
            Node nextNode = currentPath[Mathf.Clamp(pathIndex, 0, currentPath.Count - 1)];
            Debug.DrawLine(transform.position, nextNode.worldPosition + Vector3.up * 0.2f, Color.yellow);
        }
    }
}