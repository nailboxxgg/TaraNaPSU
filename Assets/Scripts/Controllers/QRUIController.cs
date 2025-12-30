using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRUIController : MonoBehaviour
{
    [Header("QR UI Elements")]
    public ZXingScannerController scanner;
    public Button scanButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    void Start()
    {
        AndroidPermissionRequester.RequestCameraPermission();

        scanButton.onClick.AddListener(OnScanClicked);
        scanner.OnQRFound += HandleQRFound;
    }

    private void OnScanClicked()
    {
        if (scanner == null) return;
        scanner.StartScanner();
        titleText.text = "Scanning...";
        subtitleText.text = "Align the QR code within the frame";
    }

    private void HandleQRFound(string result)
    {
        titleText.text = "Scanned!";
        subtitleText.text = result;
        scanner.StopScanner();

        AppFlowController.Instance.OnQRCodeScanned(result);
    }

    public void ResetScannerUI()
    {
        if (scanner != null)
        {
            scanner.StopScanner();
        }

        titleText.text = "Scan QR Code";
        subtitleText.text = "Position yourself at a QR anchor";
    }
}
