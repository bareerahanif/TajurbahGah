using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

    [Header("Start screen")]
    [Tooltip("Show the built-in start screen with the Begin Practical button")]
    public bool showStartScreen = true;
    [Tooltip("What Begin Practical should do, e.g. the camera move your old Begin button did (NavigationManager). Leave empty to let this script move the camera.")]
    public UnityEvent onBeginPressed;
    [Tooltip("If On Begin Pressed is empty, smoothly move the camera to the Practical Camera Point")]
    public bool moveCameraIfNoEvent = true;
    public float cameraMoveTime = 3f;

    [Header("Camera points (found by name if left empty)")]
    [Tooltip("Where the camera waits while the start screen is shown (IntroCameraPoint)")]
    public Transform introCameraPoint;
    [Tooltip("Optional point the camera passes through on the way (ApproachCameraPoint)")]
    public Transform approachCameraPoint;

    [Header("When to start")]
    [Tooltip("Your PracticalCameraPoint. The practical starts by itself when the camera arrives there.")]
    public Transform practicalCameraPoint;
    [Tooltip("Tick to start as soon as the scene loads instead (for testing)")]
    public bool startImmediately = false;

    [Header("Old scripts to remove")]
    [Tooltip("Old practical scripts that are removed when the scene starts, so they can't react to taps any more")]
    public string[] oldScriptNames = { "CuSO4Clickable", "CuSO4InteractionManager", "CuSO4PracticalFlow" };

    [Header("Old UI to hide")]
    [Tooltip("Your old instruction panel, tip bar and quiz panel. They are switched off when the practical starts.")]
    public GameObject[] oldUiToHide;

    [Header("Ending")]
    [Tooltip("Scene to open when the student ends or finishes (e.g. your main menu). Leave empty to reload this scene.")]
    public string exitSceneName = "";

    [Header("Step 1: apparatus labels")]
    public string waterName = "Beaker of water";
    [TextArea] public string waterInfo = "Holds the water we will test. A beaker is used to hold and pour liquids.";
    public string dropperName = "Dropper";
    [TextArea] public string dropperInfo = "Sucks up a small amount of liquid and lets it out one drop at a time.";
    public string dishName = "China dish with anhydrous CuSO<sub>4</sub>";
    [TextArea] public string dishInfo = "The white powder is anhydrous copper(II) sulfate. \"Anhydrous\" means it has no water in it.";

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
    bool _started, _busy, _inQuiz, _beginRequested;
    int _quizIndex, _score;
    float _cameraStillTime;
    PracticalUI.TipKind _tipKind;
    string _tipText;
    Coroutine _hintRoutine;
    readonly HashSet<string> _found = new HashSet<string>();
    string _selected;
    bool AllFound => _found.Count >= 3;

    // ================================================================
    void Awake()
    {
        _ui = GetComponent<PracticalUI>();
        RemoveOldScripts();
    }

    /// <summary>Destroys the old practical scripts (only while playing; nothing is changed in your scene file).</summary>
    void RemoveOldScripts()
    {
        if (oldScriptNames == null || oldScriptNames.Length == 0) return;
        var names = new HashSet<string>(oldScriptNames);
        int removed = 0;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb && names.Contains(mb.GetType().Name)) { Destroy(mb); removed++; }
        }
        if (removed > 0) Debug.Log($"[Practical] Removed {removed} old practical script(s) for this run.");
    }

    void Start()
    {
        _ui.procedureText = procedureText;
        _ui.NextPressed += OnNext;
        _ui.BackPressed += OnBack;
        _ui.RestartConfirmed += Restart;
        _ui.EndConfirmed += EndPractical;
        _ui.BeginPressed += OnBeginPressed;
        _ui.ThemeChanged += ReapplyGlow;
        if (sample) sample.OnFullyHydrated += OnHydrated;

        if (!introCameraPoint) introCameraPoint = FindPoint("IntroCameraPoint");
        if (!approachCameraPoint) approachCameraPoint = FindPoint("ApproachCameraPoint");
        if (!practicalCameraPoint) practicalCameraPoint = FindPoint("PracticalCameraPoint");

        if (startImmediately) Begin();
        else if (showStartScreen)
        {
            // Wait at the intro view while the start screen is shown, like the old version did
            Camera sc = SceneCamera();
            if (sc && introCameraPoint) sc.transform.SetPositionAndRotation(introCameraPoint.position, introCameraPoint.rotation);
            _ui.ShowIntro();
        }
    }

    void OnBeginPressed()
    {
        _beginRequested = true;
        Camera cam = SceneCamera();
        Debug.Log($"[Practical] Begin pressed. Camera: {(cam ? cam.name : "NONE")}, " +
                  $"camera point: {(practicalCameraPoint ? practicalCameraPoint.name : "NONE")}, " +
                  $"begin event used: {HasRealListener(onBeginPressed)}");

        if (HasRealListener(onBeginPressed))
        {
            onBeginPressed.Invoke();          // e.g. NavigationManager moves the camera; we start when it arrives
            if (!practicalCameraPoint) Begin();
        }
        else if (moveCameraIfNoEvent && practicalCameraPoint && cam)
        {
            StartCoroutine(MoveCameraThenBegin());
        }
        else Begin();
    }

    /// <summary>True only if the event has an entry with an object AND a function chosen (empty "None" rows don't count).</summary>
    static bool HasRealListener(UnityEvent e)
    {
        if (e == null) return false;
        for (int i = 0; i < e.GetPersistentEventCount(); i++)
            if (e.GetPersistentTarget(i) != null && !string.IsNullOrEmpty(e.GetPersistentMethodName(i))) return true;
        return false;
    }

    static Camera SceneCamera()
    {
        if (Camera.main) return Camera.main;
        return FindFirstObjectByType<Camera>();
    }

    static Transform FindPoint(string name)
    {
        var go = GameObject.Find(name);
        return go ? go.transform : null;
    }

    IEnumerator MoveCameraThenBegin()
    {
        Transform cam = SceneCamera().transform;
        if (approachCameraPoint)
        {
            yield return MoveCamera(cam, approachCameraPoint, cameraMoveTime * 0.5f);
            yield return MoveCamera(cam, practicalCameraPoint, cameraMoveTime * 0.5f);
        }
        else yield return MoveCamera(cam, practicalCameraPoint, cameraMoveTime);
        Begin();
    }

    static IEnumerator MoveCamera(Transform cam, Transform target, float time)
    {
        Vector3 p0 = cam.position;
        Quaternion r0 = cam.rotation;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float e = Mathf.SmoothStep(0f, 1f, t / time);
            cam.SetPositionAndRotation(Vector3.Lerp(p0, target.position, e), Quaternion.Slerp(r0, target.rotation, e));
            yield return null;
        }
        cam.SetPositionAndRotation(target.position, target.rotation);
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
        Camera sc = SceneCamera();
        if (!practicalCameraPoint || !sc) return;
        if (showStartScreen && !_beginRequested) return;   // wait for the Begin Practical button
        Transform cam = sc.transform;
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
        Camera sc = SceneCamera();
        if (IsOverUI(pos) || !sc) return;
        if (!Physics.Raycast(sc.ScreenPointToRay(pos), out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore)) return;

        Transform t = hit.transform;
        bool onDropper = dropper && t.IsChildOf(dropper.transform);
        bool onWater = waterBeaker && t.IsChildOf(waterBeaker);
        bool onSample = (sample && t.IsChildOf(sample.transform)) || (chinaDish && t.IsChildOf(chinaDish));

        if (_busy) return;

        if (_step == 1)
        {
            if (onDropper) Identify("dropper");
            else if (onWater) Identify("water");
            else if (onSample) Identify("dish");
            return;
        }

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
        if (step != 1) { _ui.HideLabel(); _selected = null; }
        switch (step)
        {
            case 1:
                Set("Identify the apparatus", Checklist(),
                    PracticalUI.TipKind.Tip, AllFound ? "You found all the apparatus. Tap Next to begin."
                                                      : "Tap each glowing item to find out what it is.");
                Step1Glow();
                _ui.SetNav(false, AllFound);
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
        _found.Clear();
        _selected = null;
        _ui.HideLabel();
        if (!_started) Begin(); else ShowStep(1);
    }

    public void EndPractical()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(exitSceneName) ? SceneManager.GetActiveScene().name : exitSceneName;
        SceneManager.LoadScene(scene);
    }

    // ---------------- step 1: identify the apparatus ----------------

    void Identify(string key)
    {
        bool isNew = _found.Add(key);
        _selected = key;

        if (key == "water") _ui.ShowLabel(waterBeaker, waterName, waterInfo, _ui.foundColor);
        else if (key == "dropper") _ui.ShowLabel(dropper.transform, dropperName, dropperInfo, _ui.foundColor);
        else _ui.ShowLabel(chinaDish ? chinaDish : sample.transform, dishName, dishInfo, _ui.foundColor);

        _ui.SetStep(1, TotalSteps, "Identify the apparatus", Checklist());
        Step1Glow();

        if (AllFound)
        {
            _tipKind = PracticalUI.TipKind.Tip;
            _tipText = "You found all the apparatus. Tap Next to begin.";
            _ui.SetTip(_tipKind, _tipText);
            _ui.SetNav(false, true);
        }
        else if (isNew)
        {
            _ui.SetTip(PracticalUI.TipKind.Tip, $"{_found.Count} of 3 found. Tap the other glowing items.");
        }
    }

    string Checklist()
    {
        string Item(string key, string label)
        {
            bool f = _found.Contains(key);
            return f ? $"<color=#2EC4B6>\u25CF</color> <b>{label}</b>" : $"<alpha=#88>\u25CB {label}<alpha=#FF>";
        }
        string head = AllFound ? "All apparatus identified." : $"Tap each item to identify it ({_found.Count} of 3):";
        return head + "\n" + Item("water", "Beaker of water") + "     " + Item("dropper", "Dropper") + "     " + Item("dish", "China dish with CuSO<sub>4</sub>");
    }

    void Step1Glow()
    {
        SetStep1Highlight(waterHighlight, "water");
        SetStep1Highlight(dropperHighlight, "dropper");
        SetStep1Highlight(sampleHighlight, "dish");
    }

    void SetStep1Highlight(InteractableHighlight h, string key)
    {
        if (!h) return;
        if (_selected == key) h.SetHighlighted(true, _ui.foundColor, false);          // the one being looked at
        else if (!_found.Contains(key)) h.SetHighlighted(true, _ui.highlightColor, false); // still to find
        else h.SetHighlighted(false);                                             // already found
    }

    InteractableHighlight _glowTarget;

    void Glow(InteractableHighlight on)
    {
        _glowTarget = on;
        Color c = _ui.highlightColor;
        if (dropperHighlight) dropperHighlight.SetHighlighted(dropperHighlight == on, c, true);
        if (waterHighlight) waterHighlight.SetHighlighted(waterHighlight == on, c, true);
        if (sampleHighlight) sampleHighlight.SetHighlighted(sampleHighlight == on, c, true);
    }

    void ReapplyGlow()
    {
        if (!_started) return;
        if (_step == 1) Step1Glow();
        else Glow(_glowTarget);
    }
}