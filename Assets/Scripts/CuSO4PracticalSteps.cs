using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs the anhydrous CuSO4 practical from start to finish:
///   1 Identify apparatus -> 2 Pick up dropper -> 3 Collect water -> 4 Add water and observe -> 5 Record result -> quiz -> results
/// It detects taps on the apparatus itself, drives the new PracticalUI, the glow, the dropper and the colour change,
/// and handles Restart, Pause, End and the Procedure pop-up.
///
/// Put it on an empty GameObject (with PracticalUI on the same object) and fill the slots.
/// </summary>
[RequireComponent(typeof(PracticalUI))]
public class CuSO4PracticalSteps : MonoBehaviour
{
    [System.Serializable]
    public class QuizQuestion
    {
        [TextArea] public string question;
        public string[] options;
        public int correctIndex;
        [TextArea] public string explanation;
    }

    [Header("Apparatus")]
    public DropperController dropper;
    public CopperSulfateSample sample;
    [Tooltip("The beaker of water the student taps (WaterSample)")]
    public Transform waterBeaker;
    [Tooltip("Optional: the china dish, so tapping the dish also counts as tapping the powder")]
    public Transform chinaDish;

    [Header("Glow")]
    public InteractableHighlight dropperHighlight;
    public InteractableHighlight waterHighlight;
    public InteractableHighlight sampleHighlight;

    [Header("When to start")]
    [Tooltip("Your PracticalCameraPoint. The practical starts by itself when the camera arrives there.")]
    public Transform practicalCameraPoint;
    [Tooltip("Tick to start as soon as the scene loads instead (for testing)")]
    public bool startImmediately = false;

    [Header("Old UI to hide")]
    [Tooltip("Your old instruction panel, tip bar and quiz panel. They are switched off when the practical starts.")]
    public GameObject[] oldUiToHide;

    [Header("Ending")]
    [Tooltip("Scene to open when the student ends or finishes (e.g. your main menu). Leave empty to reload this scene.")]
    public string exitSceneName = "";

    [Header("Timing")]
    [Tooltip("Seconds to let the student look at the blue colour before moving on")]
    public float lookTime = 2f;

    [Header("Procedure text (shown by the Procedure button)")]
    [TextArea(12, 40)]
    public string procedureText =
        "<size=36><b><color=#FFFFFF>Aim</color></b></size>\n" +
        "To test for the presence of water using anhydrous copper(II) sulfate.\n\n" +
        "<size=36><b><color=#FFFFFF>Apparatus</color></b></size>\n" +
        "China dish, dropper, beaker, spatula\n\n" +
        "<size=36><b><color=#FFFFFF>Chemicals</color></b></size>\n" +
        "Anhydrous copper(II) sulfate (white powder), water\n\n" +
        "<size=36><b><color=#FFFFFF>Procedure</color></b></size>\n" +
        "1.  Take a small amount of white anhydrous copper(II) sulfate in a clean, dry china dish.\n" +
        "2.  Fill the dropper with water from the beaker.\n" +
        "3.  Add 2 to 3 drops of water onto the copper(II) sulfate.\n" +
        "4.  Observe the change in colour.\n\n" +
        "<size=36><b><color=#FFFFFF>Observation</color></b></size>\n" +
        "The white anhydrous copper(II) sulfate turns blue.\n\n" +
        "<size=36><b><color=#FFFFFF>Conclusion</color></b></size>\n" +
        "The liquid added contains water.\n\n" +
        "<size=36><b><color=#FFFFFF>Chemical equation</color></b></size>\n" +
        "CuSO<sub>4</sub> (white)  +  5H<sub>2</sub>O  →  CuSO<sub>4</sub>·5H<sub>2</sub>O (blue)\n\n" +
        "<size=36><b><color=#FFFFFF>Precautions</color></b></size>\n" +
        "•  Use a clean and dry china dish and spatula.\n" +
        "•  Keep the copper(II) sulfate bottle closed. It absorbs moisture from the air.\n" +
        "•  Add water drop by drop.\n" +
        "•  Do not touch or taste chemicals. Wash your hands after the practical.";

    [Header("Quiz")]
    public QuizQuestion[] quiz =
    {
        new QuizQuestion
        {
            question = "What colour change did you observe?",
            options = new[] { "White to green", "White to blue", "Blue to white" },
            correctIndex = 1,
            explanation = "Anhydrous copper(II) sulfate is white and turns blue when it absorbs water."
        },
        new QuizQuestion
        {
            question = "What does this colour change show?",
            options = new[] { "The liquid contains water", "The liquid contains oxygen", "The liquid is an acid" },
            correctIndex = 0,
            explanation = "Anhydrous copper(II) sulfate turns blue with water, so it is used as a test for water."
        },
        new QuizQuestion
        {
            question = "What is the formula of blue hydrated copper(II) sulfate?",
            options = new[] { "CuSO<sub>4</sub>", "CuO", "CuSO<sub>4</sub>·5H<sub>2</sub>O" },
            correctIndex = 2,
            explanation = "Each unit of CuSO<sub>4</sub> takes up five molecules of water of crystallisation."
        },
    };

    const int TotalSteps = 5;

    PracticalUI _ui;
    int _step;
    bool _started, _busy, _inQuiz;
    int _quizIndex, _score;
    float _cameraStillTime;
    PracticalUI.TipKind _tipKind;
    string _tipText;
    Coroutine _hintRoutine;

    // ================================================================
    void Awake()
    {
        _ui = GetComponent<PracticalUI>();
    }

    void Start()
    {
        _ui.procedureText = procedureText;
        _ui.NextPressed += OnNext;
        _ui.BackPressed += OnBack;
        _ui.RestartConfirmed += Restart;
        _ui.EndConfirmed += EndPractical;
        if (sample) sample.OnFullyHydrated += OnHydrated;

        if (startImmediately) Begin();
    }

    void OnDestroy()
    {
        if (sample) sample.OnFullyHydrated -= OnHydrated;
        Time.timeScale = 1f;
    }

    /// <summary>Starts the practical. Called automatically, or hook it to a button if you prefer.</summary>
    public void Begin()
    {
        if (_started) return;
        _started = true;
        if (oldUiToHide != null) foreach (var go in oldUiToHide) if (go) go.SetActive(false);
        _ui.Show(true);
        ShowStep(1);
    }

    void Update()
    {
        if (!_started)
        {
            WaitForCamera();
            return;
        }
        if (_ui.IsBlocking || _inQuiz) return;
        if (TryGetTap(out Vector2 screenPos)) HandleTap(screenPos);
    }

    void WaitForCamera()
    {
        if (!practicalCameraPoint || !Camera.main) return;
        Transform cam = Camera.main.transform;
        bool arrived = Vector3.Distance(cam.position, practicalCameraPoint.position) < 0.05f &&
                       Quaternion.Angle(cam.rotation, practicalCameraPoint.rotation) < 3f;
        _cameraStillTime = arrived ? _cameraStillTime + Time.deltaTime : 0f;
        if (_cameraStillTime > 0.3f) Begin();
    }

    // ============================ taps ==============================

    static bool TryGetTap(out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        var p = UnityEngine.InputSystem.Pointer.current;
        if (p != null && p.press.wasPressedThisFrame) { pos = p.position.ReadValue(); return true; }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
#endif
        pos = default;
        return false;
    }

    static bool IsOverUI(Vector2 pos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = pos };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        return hits.Count > 0;
    }

    void HandleTap(Vector2 pos)
    {
        if (IsOverUI(pos) || !Camera.main) return;
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(pos), out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore)) return;

        Transform t = hit.transform;
        bool onDropper = dropper && t.IsChildOf(dropper.transform);
        bool onWater = waterBeaker && t.IsChildOf(waterBeaker);
        bool onSample = (sample && t.IsChildOf(sample.transform)) || (chinaDish && t.IsChildOf(chinaDish));

        if (_busy) return;

        if (_step == 2 && onDropper) { StartCoroutine(PickUpRoutine()); return; }
        if (_step == 3 && onWater) { StartCoroutine(CollectRoutine()); return; }
        if (_step == 4 && onSample) { StartCoroutine(AddDropsRoutine()); return; }

        if (onDropper || onWater || onSample) ShowHint();
    }

    void ShowHint()
    {
        string what = _step == 2 ? "dropper" : _step == 3 ? "beaker of water" : _step == 4 ? "copper(II) sulfate" : null;
        string msg = what != null ? $"Tap the glowing {what} to continue." : "Read the step, then tap Next.";
        if (_hintRoutine != null) StopCoroutine(_hintRoutine);
        _hintRoutine = StartCoroutine(HintRoutine(msg));
    }

    IEnumerator HintRoutine(string msg)
    {
        _ui.SetTip(PracticalUI.TipKind.Hint, msg);
        yield return new WaitForSeconds(2.5f);
        _ui.SetTip(_tipKind, _tipText);
        _hintRoutine = null;
    }

    // ============================ steps =============================

    void ShowStep(int step)
    {
        _step = step;
        switch (step)
        {
            case 1:
                Set("Identify the apparatus",
                    "Find the beaker of water, the dropper and the white anhydrous copper(II) sulfate in the china dish.",
                    PracticalUI.TipKind.Tip, "Anhydrous means \"without water\". Anhydrous copper(II) sulfate is a white powder.");
                Glow(null);
                _ui.SetNav(false, true);
                break;
            case 2:
                Set("Pick up the dropper", "Tap the glowing dropper to pick it up.",
                    PracticalUI.TipKind.Tip, "A dropper lets you add a liquid one drop at a time.");
                Glow(dropperHighlight);
                _ui.SetNav(true, false);
                break;
            case 3:
                Set("Collect water", "Tap the glowing beaker to fill the dropper with water.",
                    PracticalUI.TipKind.Tip, "You only need a small amount of water.");
                Glow(waterHighlight);
                _ui.SetNav(false, false);
                break;
            case 4:
                Set("Add water to the copper(II) sulfate", "Tap the glowing powder to add a few drops of water.",
                    PracticalUI.TipKind.Observe, "Watch the colour of the white powder as each drop lands.");
                Glow(sampleHighlight);
                _ui.SetNav(false, false);
                break;
            case 5:
                Set("Record your result",
                    "<b>Observation:</b> the white powder turned blue.\n<b>Conclusion:</b> the liquid contains water.",
                    PracticalUI.TipKind.Science, "CuSO<sub>4</sub> (white) + 5H<sub>2</sub>O → CuSO<sub>4</sub>·5H<sub>2</sub>O (blue)");
                Glow(null);
                _ui.SetNav(false, true, "Take the Quiz");
                break;
        }
    }

    void Set(string title, string body, PracticalUI.TipKind kind, string tip)
    {
        _ui.SetStep(_step, TotalSteps, title, body);
        _tipKind = kind;
        _tipText = tip;
        _ui.SetTip(kind, tip);
    }

    void OnNext()
    {
        if (_busy) return;
        if (_step == 1) ShowStep(2);
        else if (_step == 5) StartQuiz();
    }

    void OnBack()
    {
        if (_busy) return;
        if (_step == 2) ShowStep(1);
    }

    IEnumerator PickUpRoutine()
    {
        _busy = true;
        Glow(null);
        yield return dropper.PickUp();
        _busy = false;
        ShowStep(3);
    }

    IEnumerator CollectRoutine()
    {
        _busy = true;
        Glow(null);
        _ui.SetStep(3, TotalSteps, "Collect water", "Filling the dropper with water...");
        yield return dropper.CollectWater();
        _busy = false;
        ShowStep(4);
    }

    IEnumerator AddDropsRoutine()
    {
        _busy = true;
        Glow(null);
        _ui.SetStep(4, TotalSteps, "Add water to the copper(II) sulfate", "Adding drops of water... watch the powder.");
        yield return dropper.AddDrops();
        // continues in OnHydrated once the colour change has finished
    }

    void OnHydrated()
    {
        StartCoroutine(AfterHydrated());
    }

    IEnumerator AfterHydrated()
    {
        _ui.SetStep(4, TotalSteps, "Add water to the copper(II) sulfate", "The white powder has turned <b>blue</b>.");
        _ui.SetTip(PracticalUI.TipKind.Observe, "Anhydrous copper(II) sulfate changes from white to blue when water is added.");
        yield return new WaitForSeconds(lookTime);
        while (dropper && dropper.IsBusy) yield return null;    // let the dropper finish going home
        _busy = false;
        ShowStep(5);
    }

    // ============================= quiz =============================

    void StartQuiz()
    {
        if (quiz == null || quiz.Length == 0) { ShowResults(); return; }
        _inQuiz = true;
        _quizIndex = 0;
        _score = 0;
        AskQuestion();
    }

    void AskQuestion()
    {
        var q = quiz[_quizIndex];
        _ui.ShowQuiz(_quizIndex, quiz.Length, q.question, q.options, OnAnswer);
    }

    void OnAnswer(int chosen)
    {
        var q = quiz[_quizIndex];
        bool correct = chosen == q.correctIndex;
        if (correct) _score++;
        _ui.ShowQuizFeedback(correct, q.correctIndex, chosen, q.explanation, NextQuestion);
    }

    void NextQuestion()
    {
        _quizIndex++;
        if (_quizIndex < quiz.Length) AskQuestion();
        else ShowResults();
    }

    void ShowResults()
    {
        _ui.ShowResults(_score, quiz != null ? quiz.Length : 0, Restart, EndPractical);
    }

    // ======================= restart / end ==========================

    public void Restart()
    {
        StopAllCoroutines();
        _hintRoutine = null;
        Time.timeScale = 1f;
        _ui.HideOverlays();
        if (dropper) dropper.ResetImmediate();
        if (sample) sample.ResetSample();
        _busy = false;
        _inQuiz = false;
        if (!_started) Begin(); else ShowStep(1);
    }

    public void EndPractical()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(exitSceneName) ? SceneManager.GetActiveScene().name : exitSceneName;
        SceneManager.LoadScene(scene);
    }

    void Glow(InteractableHighlight on)
    {
        if (dropperHighlight) dropperHighlight.SetHighlighted(dropperHighlight == on);
        if (waterHighlight) waterHighlight.SetHighlighted(waterHighlight == on);
        if (sampleHighlight) sampleHighlight.SetHighlighted(sampleHighlight == on);
    }
}
