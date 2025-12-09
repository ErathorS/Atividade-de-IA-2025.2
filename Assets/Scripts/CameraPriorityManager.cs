using UnityEngine;
using Unity.Cinemachine;

public class CameraPriorityManager : MonoBehaviour
{
    public CinemachineCamera[] cameras;
    public int highPriority = 20;
    public int lowPriority = 5;

    public void SetCameraPriority(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (i == index)
                cameras[i].Priority = highPriority;
            else
                cameras[i].Priority = lowPriority;
        }
    }
}
