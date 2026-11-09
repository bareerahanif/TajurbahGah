using UnityEngine;
using UnityEngine.SceneManagement;

public class Lab : MonoBehaviour
{
    public void StartExperiment()
    {
        SceneManager.LoadScene("Loading");
    }
}
