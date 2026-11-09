
using System.Collections;
using TMPro;
using UnityEngine;

public class CuSO4InteractionManager : MonoBehaviour
{
    [Header("Experiment Objects")]
    [SerializeField] private GameObject waterDropper;
    [SerializeField] private GameObject waterSample;
    [SerializeField] private Renderer copperSulfateRenderer;

    [Header("Sample Materials")]
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Material blueMaterial;

    [Header("Droplet Animation")]
    [SerializeField] private GameObject dropletPrefab;
    [SerializeField] private Transform dropletSpawnPoint;
    [SerializeField] private Transform dropletTargetPoint;
    [SerializeField] private int numberOfDrops = 3;
    [SerializeField] private float timeBetweenDrops = 0.3f;
    [SerializeField] private float dropletTravelTime = 0.45f;

    [Header("On-screen Feedback")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text tipText;

    [Header("Assessment")]
    [SerializeField] private CuSO4Quiz quiz;

    private int stage = 0;
    private bool experimentComplete = false;
    private bool animationRunning = false;

    private void Start()
    {
        if (copperSulfateRenderer != null && whiteMaterial != null)
            copperSulfateRenderer.material = whiteMaterial;

        ShowMessage(
            "Start by selecting the glass dropper.",
            "TIP: Anhydrous copper(II) sulfate is white."
        );
    }

    public void SelectDropper()
    {
        if (experimentComplete || animationRunning) return;

        if (stage == 0)
        {
            stage = 1;
            ShowMessage(
                "Dropper selected. Now select the water sample.",
                "TIP: Use the dropper to collect a small amount of water."
            );
        }
        else
        {
            ShowMessage(
                "Follow the instruction shown on the screen.",
                "TIP: Complete the actions in the correct order."
            );
        }
    }

    public void SelectWater()
    {
        if (experimentComplete || animationRunning) return;

        if (stage == 1)
        {
            stage = 2;
            ShowMessage(
                "Water collected! Now select the white copper sulfate in the china dish.",
                "TIP: Add a few drops to make the colour change easy to observe."
            );
        }
        else
        {
            ShowMessage(
                "Select the dropper first, then collect water.",
                "TIP: The order of actions matters."
            );
        }
    }

    public void AddWaterToSample()
    {
        if (experimentComplete || animationRunning) return;

        if (stage != 2)
        {
            ShowMessage(
                "First select the dropper, then collect water.",
                "TIP: The sample is still white because water has not been added."
            );
            return;
        }

        if (dropletPrefab == null ||
            dropletSpawnPoint == null ||
            dropletTargetPoint == null)
        {
            ShowMessage(
                "The droplet animation is not configured yet.",
                "TIP: Check the Droplet Animation fields on ExperimentSetup."
            );

            Debug.LogWarning(
                "Assign the droplet prefab, spawn point, and target point."
            );
            return;
        }

        StartCoroutine(PlayWaterDropAnimation());
    }

    private IEnumerator PlayWaterDropAnimation()
    {
        animationRunning = true;

        ShowMessage(
            "Adding water to the copper sulfate sample...",
            "OBSERVE: Watch the droplets reach the white solid."
        );

        int drops = Mathf.Max(1, numberOfDrops);

        for (int i = 0; i < drops; i++)
        {
            GameObject droplet = Instantiate(
                dropletPrefab,
                dropletSpawnPoint.position,
                Quaternion.identity
            );

            Vector3 startPosition = dropletSpawnPoint.position;
            Vector3 endPosition = dropletTargetPoint.position;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, dropletTravelTime);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                droplet.transform.position = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

                yield return null;
            }

            Destroy(droplet);

            if (i < drops - 1)
                yield return new WaitForSeconds(timeBetweenDrops);
        }

        if (copperSulfateRenderer != null && blueMaterial != null)
            copperSulfateRenderer.material = blueMaterial;

        stage = 3;
        experimentComplete = true;
        animationRunning = false;

        ShowMessage(
            "Observation: The white solid turns blue. Conclusion: Water is present in the tested sample.",
            "SCIENCE: Anhydrous copper(II) sulfate takes up water and forms blue hydrated copper sulfate."
        );

        if (quiz != null)
        {
            quiz.ShowQuiz();
        }
    }

    private void ShowMessage(string description, string tip)
    {
        if (descriptionText != null)
            descriptionText.text = description;

        if (tipText != null)
            tipText.text = tip;
    }
}
