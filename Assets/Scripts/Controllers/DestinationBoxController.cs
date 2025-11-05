using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DestinationBoxController : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI currentFloorText;
    public TextMeshProUGUI destinationTitleText;
    public TextMeshProUGUI destinationNameText;

    [Header("Buttons")]
    public Button stopButton;

    [Header("Progress")]
    public RectTransform progressBarFill; // the UI image Rect we resize (if used)
    public float progressAnimSpeed = 2f;

    [Header("Polish")]
    public CanvasGroup canvasGroup; // for fade in/out

    private void Awake()
    {
        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopPressed);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    // Call to open + populate
    public void Show(string destinationName, string floorInfo)
    {
        destinationNameText.text = destinationName;
        currentFloorText.text = floorInfo;
        statusText.text = "Status: Navigating...";
        destinationTitleText.text = "Navigating To:";

        // fade in
        if (canvasGroup != null)
            StartCoroutine(FadeCanvasGroup(0f, 1f, 0.35f));
    }

    public void Hide()
    {
        if (canvasGroup != null)
            StartCoroutine(FadeCanvasGroup(1f, 0f, 0.25f));
        else
            gameObject.SetActive(false);
    }

    IEnumerator FadeCanvasGroup(float from, float to, float dur)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        canvasGroup.gameObject.SetActive(true);
        while (t < dur)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        canvasGroup.alpha = to;
        if (to == 0f) canvasGroup.gameObject.SetActive(false);
    }

    // Optional: animate progress (0..1)
    public void SetProgress(float normalized)
    {
        if (progressBarFill == null) return;
        normalized = Mathf.Clamp01(normalized);
        // assuming anchors left aligned; change width
        float parentWidth = (progressBarFill.parent as RectTransform).rect.width;
        float targetWidth = parentWidth * normalized;
        StopCoroutine("AnimateProgress");
        StartCoroutine(AnimateProgress(targetWidth));
    }

    IEnumerator AnimateProgress(float targetWidth)
    {
        float startWidth = progressBarFill.rect.width;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * progressAnimSpeed;
            float newW = Mathf.Lerp(startWidth, targetWidth, t);
            progressBarFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newW);
            yield return null;
        }
    }

    private void OnStopPressed()
    {
        statusText.text = "Status: Stopped";
        // Inform NavigationController or AppFlowController
        AppFlowController.Instance.StopNavigation();
    }
}
