public enum TaskState 
{ 
    Waiting,
    Assigned,
    InProgress,
    Completed
}

public class WarehouseTask 
{
    public Node pickupNode;
    public Node deliveryNode;

    public TaskState state;
    public RobotBehaviour assignedRobot;

    public WarehouseTask(Node pickup, Node delivery)
    {
        pickupNode = pickup;
        deliveryNode = delivery;
        state = TaskState.Waiting;
        assignedRobot = null;
    }
}