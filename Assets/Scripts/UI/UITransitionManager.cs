using UnityEngine;
using System.Collections;

public class UITransitionManager : MonoBehaviour
{
    public static UITransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void FadeSwitch(GameObject current, GameObject next, float duration = 0.5f)
    {
        StartCoroutine(FadeRoutine(current, next, duration));
    }

    private IEnumerator FadeRoutine(GameObject current, GameObject next, float duration)
    {
        if (current == null || next == null)
        {
            Debug.LogWarning("UITransitionManager: one of the panels is null.");
            if (current != null) current.SetActive(false);
            if (next != null) next.SetActive(true);
            yield break;
        }

        
        CanvasGroup currentCg = current.GetComponent<CanvasGroup>();
        if (currentCg == null) currentCg = current.AddComponent<CanvasGroup>();

        CanvasGroup nextCg = next.GetComponent<CanvasGroup>();
        if (nextCg == null) nextCg = next.AddComponent<CanvasGroup>();

        
        next.SetActive(true);
        nextCg.alpha = 0f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            currentCg.alpha = Mathf.Lerp(1f, 0f, f);
            nextCg.alpha = Mathf.Lerp(0f, 1f, f);
            yield return null;
        }

        currentCg.alpha = 0f;
        nextCg.alpha = 1f;
        current.SetActive(false);
    }
}

