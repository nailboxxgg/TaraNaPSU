using UnityEngine;

public class QRUIController : MonoBehaviour
{
    public static QRUIController Instance { get; private set; }

    public GameObject qrPanel;
    public ZXingScannerController scanner;

    void Awake()
    {
        Instance = this;
    }

    public void OpenScanner()
    {
        qrPanel.SetActive(true);
        if (scanner != null)
        {
            scanner.StartScanner();
        }
    }

    public void CloseScanner()
    {
        if (scanner != null)
        {
            scanner.StopScanner();
        }
        qrPanel.SetActive(false);
    }
}
