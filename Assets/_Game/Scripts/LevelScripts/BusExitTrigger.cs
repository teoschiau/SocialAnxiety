using UnityEngine;

public class BusExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the box is the Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached the exit!");
            Bus_MissionManager.Instance.RegisterReachedExit();
        }
    }
}