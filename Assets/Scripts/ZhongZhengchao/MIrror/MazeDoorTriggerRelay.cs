using UnityEngine;

public class MazeDoorTriggerRelay : MonoBehaviour
{
    public MazePushDoor pushDoor;
    public MazeKeyDoor keyDoor;

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayerRelated =
            other.GetComponentInParent<MazeInventory>() != null ||
            other.CompareTag("Player");

        if (!isPlayerRelated)
        {
            return;
        }

        if (keyDoor != null)
        {
            keyDoor.TryOpen(other.transform);
            return;
        }

        if (pushDoor != null)
        {
            pushDoor.PushOpen(other.transform);
        }
    }
}