using UnityEngine;

public class KeepScreenAlive : MonoBehaviour 
{
    private void Start() 
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}

