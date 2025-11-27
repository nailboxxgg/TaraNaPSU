using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

[RequireComponent(typeof(RawImage))]
public class ZXingScannerController : MonoBehaviour
{
    public RawImage cameraPreview;
    public float scanInterval = 0.15f;
    private WebCamTexture camTexture;
    private IBarcodeReader reader;
    private Coroutine scanRoutine;

    public event Action<string> OnQRFound;

    private void Awake()
    {
        reader = new BarcodeReader { AutoRotate = true };
    }

    public void StartScanner()
    {
        if (camTexture != null) return;
        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("No camera found!");
            return;
        }

        camTexture = new WebCamTexture(devices[0].name);
        cameraPreview.texture = camTexture;

        // Adjust camera preview orientation for portrait mode on mobile devices
        RectTransform previewRect = cameraPreview.GetComponent<RectTransform>();

        // On mobile devices, force the camera to portrait mode regardless of device orientation
        #if UNITY_ANDROID || UNITY_IOS
            // Force portrait orientation for mobile camera
            previewRect.rotation = Quaternion.Euler(0, 0, -90);
        #else
            // For editor and other platforms, use the automatic rotation
            previewRect.rotation = Quaternion.Euler(0, 0, 0);
        #endif

        // Apply vertical mirroring if needed
        if (camTexture.videoVerticallyMirrored)
        {
            cameraPreview.uvRect = new Rect(1, 0, -1, 1);
        }

        camTexture.Play();
        scanRoutine = StartCoroutine(ScanLoop());
    }

    public void StopScanner()
    {
        if (scanRoutine != null) StopCoroutine(scanRoutine);
        if (camTexture != null)
        {
            camTexture.Stop();
            camTexture = null;
        }
    }

    private IEnumerator ScanLoop()
    {
        yield return new WaitForSeconds(0.5f);
        while (camTexture != null && camTexture.isPlaying)
        {
            try
            {
                var pixels = camTexture.GetPixels32();
                var result = reader.Decode(pixels, camTexture.width, camTexture.height);
                if (result != null)
                {
                    Debug.Log("QR Found: " + result.Text);
                    OnQRFound?.Invoke(result.Text);
                }
            }
            catch { }
            yield return new WaitForSeconds(scanInterval);
        }
    }
}
