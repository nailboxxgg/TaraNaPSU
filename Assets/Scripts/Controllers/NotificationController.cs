using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationController : MonoBehaviour
{
    public static NotificationController Instance;

    [Header("UI References")]
    public GameObject panel;
    public TMP_Text messageText;
    public float displayTime = 2.5f;
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private Coroutine currentRoutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        panel.SetActive(false);
    }

    public void Show(string message)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        panel.SetActive(true);
        messageText.text = message;
        canvasGroup.alpha = 0;

        currentRoutine = StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(displayTime);

        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }

        panel.SetActive(false);
    }
}
