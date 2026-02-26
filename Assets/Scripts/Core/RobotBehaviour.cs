using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotBehaviour : MonoBehaviour
{
    public GameObject labelPrefab;
    private Text labelText;
    private GameObject labelObj;

    private void LateUpdate()
    {
        if (labelObj != null)
        {
            // Hover above the robot
            labelObj.transform.position = transform.position + Vector3.up * 2f;

            // Face the camera
            labelObj.transform.rotation = Quaternion.LookRotation(labelObj.transform.position - Camera.main.transform.position);

            // Update text
            string taskInfo = currentTask != null
                ? $"{currentTask.pickupNode.x},{currentTask.pickupNode.z} -> {currentTask.deliveryNode.x},{currentTask.deliveryNode.z}"
                : "No task";

            if (labelText != null)
                labelText.text = $"{name}\n{currentState}\n{taskInfo}";
        }
    }

    public enum RobotState
    {
        Idle,
        MovingToPickup,
        PickingUp,
        MovingToDelivery,
        Delivering
    }

    public RobotBehaviour.RobotState CurrentState => currentState;
    public WarehouseTask CurrentTask => currentTask;

    private RobotState currentState = RobotState.Idle;
    private float stateTimer = 0f;

    private Grid_script grid;
    private Pathfinding pathfinding;
    private bool debugMode;

    private TaskManager taskManager;
    private WarehouseTask currentTask;

    private List<Node> currentPath = null;
    private int pathIndex = 0;
    private bool pathNeedsRecalc = false;

    private Node previousNode = null;

    public float actionPauseTime = 5f;
    private float recalcTimer = 0f;
    private float recalcDelay = 0.5f;
    public Vector3 Position => transform.position;

    public float moveSpeed = 2f;

    private bool HasPriorityOver(RobotBehaviour other)
    {
        return this.GetInstanceID() < other.GetInstanceID();
    }

    public void Initialize(Grid_script g, Pathfinding p, bool dbg, TaskManager tm)
    {
        grid = g;
        pathfinding = p;
        debugMode = dbg;
        taskManager = tm;
        currentState = RobotState.Idle;

        // Inside RobotBehaviour.Initialize() or after instantiating the robot
        if (labelPrefab != null)
        {
            labelObj = Instantiate(labelPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            labelObj.transform.SetParent(null); // keep it independent in hierarchy

            labelText = labelObj.GetComponentInChildren<Text>(); // grab the Text component inside
            if (labelText == null)
                Debug.LogError("Text component not found in labelPrefab!");
        }
        else
        {
            Debug.LogWarning("No labelPrefab assigned to robot.");
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case RobotState.Idle:
                HandleIdle();
                break;

            case RobotState.MovingToPickup:
                HandleMoving(currentTask?.pickupNode, RobotState.PickingUp);
                break;

            case RobotState.PickingUp:
                HandleActionPause(RobotState.MovingToDelivery, currentTask?.deliveryNode);
                break;

            case RobotState.MovingToDelivery:
                HandleMoving(currentTask?.deliveryNode, RobotState.Delivering);
                break;

            case RobotState.Delivering:
                HandleActionPause(RobotState.Idle, null);
                break;
        }

        recalcTimer -= Time.deltaTime;
    }

    #region FSM Handlers
    void HandleIdle()
    {
        if (currentTask == null && taskManager != null)
        {
            currentTask = taskManager.RequestTask(this);
            if (currentTask != null)
            {
                Debug.Log($"{name} received task. Moving to pickup.");
                currentState = RobotState.MovingToPickup;
                SetTarget(currentTask.pickupNode.worldPosition);
                currentTask.state = TaskState.Assigned;
                currentTask.assignedRobot = this;
            }
        }
    }

    void HandleMoving(Node targetNode, RobotState nextState)
    {
        if (targetNode == null) return;

        if (currentPath == null || pathIndex >= currentPath.Count || pathNeedsRecalc)
        {
            RecalculatePath(targetNode);
            recalcTimer = recalcDelay;
        }

        MoveAlongPath();

        if (ReachedNode(targetNode))
        {
            if (previousNode != null) previousNode.isOccupied = false;

            previousNode = targetNode;
            previousNode.isOccupied = true;
            grid.UpdateNodeVisual(previousNode);

            if (nextState == RobotState.PickingUp || nextState == RobotState.Delivering)
                stateTimer = actionPauseTime;

            currentState = nextState;

            // Mark task state
            if (nextState == RobotState.PickingUp)
                currentTask.state = TaskState.InProgress;
        }
    }

    void HandleActionPause(RobotState nextState, Node nextTarget)
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            if (nextState == RobotState.MovingToDelivery && nextTarget != null)
                SetTarget(nextTarget.worldPosition);
            else if (nextState == RobotState.Idle)
            {
                if (currentTask != null)
                {
                    currentTask.state = TaskState.Completed;
                    taskManager.CompleteTask(currentTask);
                    currentTask = null;
                    currentState = RobotState.Idle;
                }

                currentPath = null;
                pathIndex = 0;
            }

            currentState = nextState;
        }
    }
    #endregion

    #region Path Helpers
    private void RecalculatePath(Node targetNode)
    {
        if (targetNode == null) return;

        // Always ignore occupied for pathfinding, because we're handling collisions separately
        currentPath = pathfinding.FindPath(transform.position, targetNode.worldPosition, ignoreOccupied: true);
        pathIndex = 0;
        pathNeedsRecalc = false;

        if (currentPath == null || currentPath.Count == 0)
            Debug.Log($"{name}: No path found to target.");
    }

    private void MoveAlongPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count) return;

        Node targetNode = currentPath[pathIndex];

        RobotBehaviour other = targetNode.assignedRobot;

        // Head-on swap detection
        bool isHeadOn = false;
        if (other != null && other != this &&
            other.currentPath != null &&
            other.pathIndex < other.currentPath.Count)
        {
            Node otherNextNode = other.currentPath[other.pathIndex];
            if (otherNextNode == previousNode)
                isHeadOn = true;
        }

        // Step 1: Handle blocked or head-on nodes
        if (!targetNode.isWalkable || (other != null && other != this))
        {
            // If head-on, yield based on priority
            if (isHeadOn)
            {
                if (!HasPriorityOver(other))
                    return; // lower-priority robot waits
            }
            else
            {
                // Temporarily treat occupied node as blocked
                recalcTimer -= Time.deltaTime;
                if (recalcTimer <= 0f)
                {
                    pathNeedsRecalc = true;
                    recalcTimer = recalcDelay;
                }
                return;
            }
        }

        // Step 2: Reserve the node
        if (targetNode.assignedRobot == null)
            targetNode.assignedRobot = this;

        // Step 3: Move toward the target node
        Vector3 moveTarget = targetNode.worldPosition + Vector3.up * 0.1f;
        transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed * Time.deltaTime);

        // Step 4: When arrived
        if (Vector3.Distance(transform.position, moveTarget) < 0.1f)
        {
            // Free previous node
            if (previousNode != null && previousNode != targetNode)
            {
                previousNode.assignedRobot = null;
                previousNode.isOccupied = false;
                grid.UpdateNodeVisual(previousNode);
            }

            // Occupy current node
            targetNode.isOccupied = true;
            targetNode.assignedRobot = this;
            grid.UpdateNodeVisual(targetNode);

            previousNode = targetNode;
            pathIndex++;
        }
    }

    private bool ReachedNode(Node node)
    {
        if (node == null) return false;
        return Vector3.Distance(transform.position, node.worldPosition + Vector3.up * 0.1f) < 0.1f;
    }

    public void SetTarget(Vector3 newTarget)
    {
        pathNeedsRecalc = true;
    }
    #endregion

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
