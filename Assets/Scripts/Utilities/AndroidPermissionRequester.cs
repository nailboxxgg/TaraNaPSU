using UnityEngine;
using UnityEngine.Android;

public static class AndroidPermissionRequester
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public static void RequestCameraPermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }
    }
#else
    public static void RequestCameraPermission() { }
#endif
}

