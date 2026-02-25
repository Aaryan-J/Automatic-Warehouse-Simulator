using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public List<WarehouseTask> activeTasks = new List<WarehouseTask>();
    public List<Node> predefinedPickups = new List<Node>();
    public List<Node> predefinedDeliveries = new List<Node>();

    public float taskSpawnInterval = 3f;
    public List<Node> allNodes = new List<Node>();

    public void Initialize(Grid_script grid)
    {
        allNodes = grid.GetAllNodes();
    }
    private void Awake()
    {
        allNodes.Clear();
    }

    private void Start()
    {
        StartCoroutine(AutoPredefinedTaskGenerator());
    }

    public void AddPredefinedTasks()
    {
        if (predefinedPickups.Count != predefinedDeliveries.Count)
        {
            Debug.LogWarning("Predefined pickup/delivery lists have different lengths");
            return;
        }

        for (int i = 0; i < predefinedPickups.Count; i++)
        {
            AddTask(predefinedPickups[i], predefinedDeliveries[i]);
            Debug.Log($"Predefined task added: Pickup ({predefinedPickups[i].x},{predefinedPickups[i].z}) -> Delivery ({predefinedDeliveries[i].x},{predefinedDeliveries[i].z})");
        }
    }

    public WarehouseTask RequestTask(RobotBehaviour robot)
    {
        float bestDist = float.MaxValue;
        WarehouseTask bestTask = null;

        foreach (var task in activeTasks)
        {
            if (task.state != TaskState.Waiting) continue;

            float dist = Vector3.Distance(robot.transform.position, task.pickupNode.worldPosition);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestTask = task;
            }
        }

        if (bestTask != null)
        {
            Debug.Log($"Assigning closest task to {robot.name}");
            bestTask.state = TaskState.Assigned;
            bestTask.assignedRobot = robot;
            return bestTask;
        }

        Debug.Log("No available tasks.");
        return null;
    }

    public void CompleteTask(WarehouseTask task)
    {
        Debug.Log($"Task completed by {task.assignedRobot.name}");

        task.state = TaskState.Completed;
        activeTasks.Remove(task);
    }

    public void AddTask(Node pickup, Node delivery)
    {
        WarehouseTask task = new WarehouseTask(pickup, delivery);
        activeTasks.Add(task);
    }
    private IEnumerator AutoPredefinedTaskGenerator()
    {
        while (true)
        {
            yield return new WaitForSeconds(taskSpawnInterval);

            // pick a random pickup and random delivery from the predefined lists
            if (predefinedPickups.Count == 0 || predefinedDeliveries.Count == 0) continue;

            Node pickup = predefinedPickups[Random.Range(0, predefinedPickups.Count)];
            Node delivery = predefinedDeliveries[Random.Range(0, predefinedDeliveries.Count)];

            // avoid pickup == delivery
            if (pickup == delivery) continue;

            AddTask(pickup, delivery);
            Debug.Log($"New task added: Pickup ({pickup.x},{pickup.z}) -> Delivery ({delivery.x},{delivery.z})");
        }
    }
}
