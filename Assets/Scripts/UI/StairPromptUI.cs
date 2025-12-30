using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class StairPromptUI : MonoBehaviour
{
    public static StairPromptUI Instance { get; private set; }

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI messageText;
    public float fadeDuration = 0.3f;
    public float autoHideDelay = 4f;

    private Coroutine currentRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    
    
    
    public void ShowMessage(string message, float duration = -1f)
    {
        if (messageText == null || canvasGroup == null) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        messageText.text = message;
        currentRoutine = StartCoroutine(ShowRoutine(duration > 0 ? duration : autoHideDelay));
    }

    
    
    
    public void HideMessage()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        StartCoroutine(FadeOutRoutine(0.2f));
    }

    private IEnumerator ShowRoutine(float visibleTime)
    {
        yield return StartCoroutine(FadeInRoutine());
        yield return new WaitForSeconds(visibleTime);
        yield return StartCoroutine(FadeOutRoutine());
        currentRoutine = null;
    }

    private IEnumerator FadeInRoutine()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }

    private IEnumerator FadeOutRoutine(float overrideDuration = -1f)
    {
        float dur = (overrideDuration > 0) ? overrideDuration : fadeDuration;
        float t = 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / dur);
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}

