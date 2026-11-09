using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AsyncSceneLoader : MonoBehaviour
{
    [Header("Scene Target")]
    [Tooltip("Exact name of the lab practical scene to load.")]
    [SerializeField] private string targetSceneName = "LabPracticalScene";

    [Header("UI Elements")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("Animation Settings")]
    [Tooltip("Minimum artificial time in seconds so the bar doesn't flash by too quickly.")]
    [SerializeField] private float minimumLoadTime = 1.5f;
    [SerializeField] private float fillSmoothSpeed = 3f;

    [Header("Lab Safety / Educational Tips")]
    [SerializeField]
    private string[] labTips = new string[]
    {
        "Always clean the platinum loop in concentrated HCl before every test.",
        "A pale blue flame provides the best background for flame color detection.",
        "Sodium produces a characteristic intense golden-yellow flame.",
        "Copper salts color the Bunsen flame an emerald blue-green."
    };

    private void Start()
    {
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        if (progressText != null)
        {
            progressText.text = "0%";
        }

        if (tipText != null && labTips.Length > 0)
        {
            tipText.text = labTips[Random.Range(0, labTips.Length)];
        }

        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        // Give the UI one frame to render cleanly before hitching the thread
        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
        // Prevent immediate auto-activation so we can smoothly animate the bar to 100%
        operation.allowSceneActivation = false;

        float targetFill = 0f;
        float currentFill = 0f;
        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            // Unity async progress caps at 0.9f until allowSceneActivation is set to true
            float rawProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Calculate progress scaled by our minimum visual duration
            float timeProgress = Mathf.Clamp01(timer / minimumLoadTime);
            targetFill = Mathf.Min(rawProgress, timeProgress);

            // Smoothly lerp the slider towards targetFill
            currentFill = Mathf.MoveTowards(currentFill, targetFill, fillSmoothSpeed * Time.deltaTime);

            if (progressBar != null)
            {
                progressBar.value = currentFill;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(currentFill * 100f)}%";
            }

            // Once both the async operation is loaded (0.9) and smooth bar reaches ~1.0
            if (operation.progress >= 0.9f && currentFill >= 0.99f && timer >= minimumLoadTime)
            {
                if (progressBar != null) progressBar.value = 1f;
                if (progressText != null) progressText.text = "100%";

                // Small pause so user sees 100% before screen switch
                yield return new WaitForSeconds(0.2f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}