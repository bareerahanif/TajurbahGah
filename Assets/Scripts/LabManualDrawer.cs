using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LabManualDrawer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public RectTransform drawerPanel;
    [SerializeField] public CanvasGroup dimBackgroundGroup;
    [SerializeField] public Button openButton;
    [SerializeField] public Button closeButton;
    [SerializeField] public Button backdropCloseButton;

    [Header("Animation Settings")]
    [SerializeField] private float slideDuration = 0.25f;

    public GameObject playerMovement;

    private Vector2 hiddenPos;
    private Vector2 visiblePos;
    private Coroutine currentRoutine;
    private bool isOpen = false;

    private void Awake()
    {
        float panelWidth = drawerPanel.rect.width;
        hiddenPos = new Vector2(panelWidth, 0f);
        visiblePos = Vector2.zero;

        // Start hidden
        drawerPanel.anchoredPosition = hiddenPos;
        if (dimBackgroundGroup != null)
        {
            dimBackgroundGroup.alpha = 0f;
            dimBackgroundGroup.blocksRaycasts = false;
        }

        // Setup listeners
        if (openButton != null) openButton.onClick.AddListener(OpenDrawer);
        if (closeButton != null) closeButton.onClick.AddListener(CloseDrawer);
        if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(CloseDrawer);
    }

    public void OpenDrawer()
    {
        if (isOpen) return;
        isOpen = true;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimateDrawer(drawerPanel.anchoredPosition, visiblePos, 0f, 1f));

        playerMovement.GetComponent<LabFirstPersonMovement>().enabled = false;
    }

    public void CloseDrawer()
    {
        if (!isOpen) return;
        isOpen = false;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimateDrawer(drawerPanel.anchoredPosition, hiddenPos, 1f, 0f));

        playerMovement.GetComponent<LabFirstPersonMovement>().enabled = true;
    }

    private IEnumerator AnimateDrawer(Vector2 fromPos, Vector2 toPos, float fromAlpha, float toAlpha)
    {
        float elapsed = 0f;
        if (dimBackgroundGroup != null) dimBackgroundGroup.blocksRaycasts = isOpen;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled so it works even if game is paused
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);

            drawerPanel.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);

            if (dimBackgroundGroup != null)
            {
                dimBackgroundGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            }

            yield return null;
        }

        drawerPanel.anchoredPosition = toPos;
        if (dimBackgroundGroup != null) dimBackgroundGroup.alpha = toAlpha;
    }
}