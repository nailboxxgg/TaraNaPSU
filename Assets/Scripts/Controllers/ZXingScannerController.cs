using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;

public class ZXingScannerController : MonoBehaviour
{
    [Header("AR Configuration")]
    [SerializeField]
    private ARCameraManager arCameraManager;

    [Header("Scanner Settings")]
    public float scanInterval = 0.5f;

    private IBarcodeReader reader;
    private bool isScanning = false;
    private float nextScanTime = 0;

    public event Action<string> OnQRFound;

    private void Awake()
    {
        reader = new BarcodeReader 
        { 
            AutoRotate = true, 
            Options = new ZXing.Common.DecodingOptions { TryHarder = true } 
        };
    }

    private void Update()
    {
        if (!isScanning) return;
        if (Time.time < nextScanTime) return;

        if (arCameraManager == null)
            arCameraManager = FindObjectOfType<ARCameraManager>();

        if (arCameraManager != null)
        {
            ScanFrame();
            nextScanTime = Time.time + scanInterval;
        }
    }

    public void StartScanner()
    {
        isScanning = true;
        Debug.Log("AR QR Scanner started.");
    }

    public void StopScanner()
    {
        isScanning = false;
        Debug.Log("AR QR Scanner stopped.");
    }

    private void ScanFrame()
    {
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.R8,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        try
        {
            image.Convert(conversionParams, buffer);
            
            byte[] pixelData = buffer.ToArray();

            var result = reader.Decode(pixelData, image.width, image.height, RGBLuminanceSource.BitmapFormat.Gray8);

            if (result != null)
            {
                Debug.Log($"QR Found: {result.Text}");
                OnQRFound?.Invoke(result.Text);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"QR Scan Error: {ex.Message}");
        }
        finally
        {
            buffer.Dispose();
            image.Dispose();
        }
    }
}