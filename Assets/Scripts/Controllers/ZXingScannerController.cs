using UnityEngine;
using UnityEngine.UI;
using ZXing;
using System.Collections;
using TMPro;

public class ZXingScannerController : MonoBehaviour
{
    [Header("UI References")]
    public RawImage cameraFeedDisplay;
    public AspectRatioFitter aspectFitter;
    public TMP_Text statusText; // New: To show directions to user

    private WebCamTexture webCamTexture;
    private bool isScanning = false;
    private IBarcodeReader barcodeReader;

    void Start()
    {
        barcodeReader = new BarcodeReader();
    }

    public void StartScanner()
    {
        if (isScanning) return;

        // Initialize camera
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("No camera detected");
            return;
        }

        webCamTexture = new WebCamTexture(devices[0].name, 1080, 1920);
        cameraFeedDisplay.texture = webCamTexture;
        webCamTexture.Play();

        isScanning = true;
        if (statusText != null) statusText.text = "Align the camera to the QR Code";
        StartCoroutine(ScanRoutine());
    }

    public void StopScanner()
    {
        isScanning = false;
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }

    private IEnumerator ScanRoutine()
    {
        while (isScanning)
        {
            try
            {
                var result = barcodeReader.Decode(webCamTexture.GetPixels32(), webCamTexture.width, webCamTexture.height);
                if (result != null)
                {
                    OnCodeScanned(result.Text);
                }
            }
            catch
            {
                // Decoding failed, continue
            }
            yield return new WaitForSeconds(0.5f); // Scan every half second
        }
    }

    private void OnCodeScanned(string result)
    {
        Debug.Log("QR Scanned: " + result);
        
        if (statusText != null) statusText.text = "Scanned!";

        if (AppFlowController2D.Instance != null)
        {
            AppFlowController2D.Instance.OnQRCodeScanned(result);
        }
        StopScanner();
        
        // Notify UI to close scanner
        if (QRUIController.Instance != null)
        {
            QRUIController.Instance.CloseScanner();
        }
    }

    void Update()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            float ratio = (float)webCamTexture.width / (float)webCamTexture.height;
            aspectFitter.aspectRatio = ratio;

            int orient = -webCamTexture.videoRotationAngle;
            cameraFeedDisplay.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
        }
    }

    void OnDisable()
    {
        StopScanner();
    }
}
