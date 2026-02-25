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

    public int randomPickupCount = 2;
    public int randomDeliveryCount = 2;
    public float minDistanceBwNodes = 6f;

    public void Initialize(Grid_script grid)
    {
        allNodes = grid.GetAllNodes();
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

    public void GenerateRandomTasks()
    {
        if (allNodes.Count == 0) return;

        // Clear previous tasks
        predefinedPickups.Clear();
        predefinedDeliveries.Clear();

        // Reset flags on all nodes
        foreach (var node in allNodes)
        {
            node.isPickup = false;
            node.isDelivery = false;
        }

        // Collect all walkable nodes
        List<Node> validNodes = new List<Node>();
        foreach (var node in allNodes)
        {
            if (node.isWalkable)
                validNodes.Add(node);
        }

        // Shuffle the nodes for randomness
        Shuffle(validNodes);

        bool isFarEnough(Node candidate, List<Node> existing)
        {
            foreach (var node in existing)
            {
                if (Vector3.Distance(candidate.worldPosition, node.worldPosition) < minDistanceBwNodes)
                    return false;
            }
            return true;
        }

        // Pick random pickups
        int pickupsAdded = 0;
        int index = 0;
        while (pickupsAdded < randomPickupCount && index < validNodes.Count)
        {
            Node candidate = validNodes[index++];
            if (isFarEnough(candidate, predefinedPickups))
            {
                candidate.isPickup = true;
                predefinedPickups.Add(candidate);
                pickupsAdded++;
            }
        }

        // Pick random deliveries
        int deliveriesAdded = 0;
        index = 0; // reset index to start from beginning for deliveries
        while (deliveriesAdded < randomDeliveryCount && index < validNodes.Count)
        {
            Node candidate = validNodes[index++];
            if (isFarEnough(candidate, predefinedPickups) && isFarEnough(candidate, predefinedDeliveries))
            {
                candidate.isDelivery = true;
                predefinedDeliveries.Add(candidate);
                deliveriesAdded++;
            }
        }

        // Add tasks
        AddPredefinedTasks();

        // Update visuals
        foreach (var node in predefinedPickups)
        {
            if (node.visual != null)
                node.visual.GetComponent<Renderer>().material.color = Color.green;
        }

        foreach (var node in predefinedDeliveries)
        {
            if (node.visual != null)
                node.visual.GetComponent<Renderer>().material.color = Color.blue;
        }
    }

    // Fisher-Yates shuffle
    void Shuffle(List<Node> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            Node temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
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

            pickup.visual.GetComponent<Renderer>().material.color = Color.green;
            delivery.visual.GetComponent<Renderer>().material.color = Color.blue;
            Debug.Log($"New task added: Pickup ({pickup.x},{pickup.z}) -> Delivery ({delivery.x},{delivery.z})");
        }
    }
}
