
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public static string SelectedSubject = "Chemistry";

    public void StartGame()
    {
        SceneManager.LoadScene("SubjectSelectScene");
    }

    public void SelectPhysics()
    {
        SelectSubject("Physics");
    }

    public void SelectChemistry()
    {
        SelectSubject("Chemistry");
    }

    public void SelectBiology()
    {
        SelectSubject("Biology");
    }

    private void SelectSubject(string subject)
    {
        SelectedSubject = subject;
        SceneManager.LoadScene("PracticalSelectScene");
    }

    public void OpenCuSO4Practical()
    {
        if (SelectedSubject != "Chemistry")
        {
            Debug.LogWarning(
                "This practical belongs to Chemistry."
            );
            return;
        }

        SceneManager.LoadScene("ChemistryCuSO4Scene");
    }

    public void BackToSubjects()
    {
        SceneManager.LoadScene("SubjectSelectScene");
    }

    public void BackToPracticals()
    {
        SceneManager.LoadScene("PracticalSelectScene");
    }

    public void BackToStart()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
