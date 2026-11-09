using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    [Header("Lights to Control")]
    [Tooltip("Assign your lights here in the Inspector, or leave empty to auto-detect child lights.")]
    [SerializeField] private Light[] targetLights;

    [Header("Initial State")]
    [Tooltip("Set to false so lights start switched off.")]
    [SerializeField] private bool areLightsOn = false;

    public GameObject flame;
    [SerializeField] private bool isFlameOn = false;
    private void Awake()
    {
        if (targetLights == null || targetLights.Length == 0)
        {
            targetLights = GetComponentsInChildren<Light>(true);
        }
        flame.SetActive(false);
    }

    private void Start()
    {
        SetLightsState(areLightsOn);
    }
    public void ToggleLights()
    {
        SetLightsState(!areLightsOn);
    }
    public void SetLightsState(bool state)
    {
        areLightsOn = state;

        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
            {
                targetLights[i].enabled = areLightsOn;
            }
        }
    }

    public void FlameToggle()
    {
        flame.SetActive(isFlameOn);
        isFlameOn = !isFlameOn;
    }

    public void StartExperiment()
    {
        SceneManager.LoadScene("LabScene");
    }
    public void Home()
    {
        SceneManager.LoadScene("Menu");
    }

}
