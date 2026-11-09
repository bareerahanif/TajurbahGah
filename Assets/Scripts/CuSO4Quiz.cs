
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CuSO4Quiz : MonoBehaviour
{
    [Header("Quiz Panel")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Answer Buttons")]
    [SerializeField] private Button answerButton1;
    [SerializeField] private Button answerButton2;
    [SerializeField] private Button answerButton3;
    [SerializeField] private Button nextButton;

    [Header("Answer Button Labels")]
    [SerializeField] private TMP_Text answerLabel1;
    [SerializeField] private TMP_Text answerLabel2;
    [SerializeField] private TMP_Text answerLabel3;

    private int questionIndex = 0;
    private int score = 0;
    private bool answered = false;

    private readonly string[][] questions =
    {
        new string[]
        {
            "What colour change did you observe?",
            "White to green",
            "White to blue",
            "Blue to white"
        },
        new string[]
        {
            "What does the colour change indicate?",
            "Oxygen is present",
            "Water is present",
            "No water is present"
        }
    };

    private readonly int[] correctAnswers = { 1, 1 };

    public void ShowQuiz()
    {
        questionIndex = 0;
        score = 0;
        answered = false;

        if (quizPanel != null)
            quizPanel.SetActive(true);

        ShowQuestion();
    }

    private void Start()
    {
        if (quizPanel != null)
            quizPanel.SetActive(false);

        if (answerButton1 != null)
            answerButton1.onClick.AddListener(() => CheckAnswer(0));

        if (answerButton2 != null)
            answerButton2.onClick.AddListener(() => CheckAnswer(1));

        if (answerButton3 != null)
            answerButton3.onClick.AddListener(() => CheckAnswer(2));

        if (nextButton != null)
            nextButton.onClick.AddListener(NextQuestion);
    }

    private void ShowQuestion()
    {
        answered = false;

        questionText.text =
            "Question " + (questionIndex + 1) + " of " +
            questions.Length + "\n\n" +
            questions[questionIndex][0];

        answerLabel1.text = questions[questionIndex][1];
        answerLabel2.text = questions[questionIndex][2];
        answerLabel3.text = questions[questionIndex][3];

        feedbackText.text = "";
        scoreText.text = "Score: " + score + " / " + questions.Length;

        answerButton1.interactable = true;
        answerButton2.interactable = true;
        answerButton3.interactable = true;

        nextButton.gameObject.SetActive(false);
    }

    private void CheckAnswer(int selectedAnswer)
    {
        if (answered)
            return;

        answered = true;

        if (selectedAnswer == correctAnswers[questionIndex])
        {
            score++;
            feedbackText.text = "Correct! Well done.";
        }
        else
        {
            feedbackText.text =
                questionIndex == 0
                ? "Not quite. The white solid turns blue."
                : "Not quite. This colour change indicates the presence of water.";
        }

        scoreText.text = "Score: " + score + " / " + questions.Length;

        answerButton1.interactable = false;
        answerButton2.interactable = false;
        answerButton3.interactable = false;

        nextButton.gameObject.SetActive(true);

        TMP_Text nextLabel = nextButton.GetComponentInChildren<TMP_Text>();

        if (nextLabel != null)
        {
            nextLabel.text =
                questionIndex == questions.Length - 1
                ? "View Results"
                : "Next Question";
        }
    }

    private void NextQuestion()
    {
        if (!answered)
            return;

        if (questionIndex < questions.Length - 1)
        {
            questionIndex++;
            ShowQuestion();
        }
        else
        {
            ShowResults();
        }
    }

    private void ShowResults()
    {
        questionText.text = "Practical Assessment Complete!";

        feedbackText.text =
            score == questions.Length
            ? "Excellent work! You answered both questions correctly."
            : "Good effort! Review the observation and conclusion, then try again.";

        scoreText.text = "Final Score: " + score + " / " + questions.Length;

        answerButton1.gameObject.SetActive(false);
        answerButton2.gameObject.SetActive(false);
        answerButton3.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
    }
}
