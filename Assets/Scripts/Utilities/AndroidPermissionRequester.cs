using UnityEngine;

public static class AndroidPermissionRequester
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public static void RequestCameraPermission()
    {
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.CAMERA"))
        {
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.CAMERA");
        }
    }
#else
    public static void RequestCameraPermission() { }
#endif
}

