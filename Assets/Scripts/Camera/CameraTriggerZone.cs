using UnityEngine;

public class CameraTriggerZone : MonoBehaviour
{
    public int cameraIndex;
    private CameraPriorityManager manager;

    private void Start()
    {
        manager = FindObjectOfType<CameraPriorityManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            manager.SetCameraPriority(cameraIndex);
        }
    }
}
