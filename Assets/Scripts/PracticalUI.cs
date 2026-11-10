using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Builds the whole practical interface in code when the game starts, so you don't have to lay it out by hand:
///  - top bar: practical title + Procedure, Restart, Pause, End buttons
///  - bottom: a coloured tip strip and an instruction card (step number, title, text, progress bar, Next/Back)
///  - pop-ups: Pause menu, full Procedure (scrollable), confirm dialogs, quiz, results
///
/// Put it on an empty GameObject. CuSO4PracticalSteps drives it. You only set the title, colours and (optionally) a font.
/// </summary>
public class PracticalUI : MonoBehaviour
{
    public enum TipKind { Tip, Observe, Science, Hint }

    [Header("Text")]
    public string practicalTitle = "Test for Water  ·  Anhydrous Copper(II) Sulfate";

    [Header("Start screen")]
    public string introLabel = "CHEMISTRY PRACTICAL";
    public string introTitle = "Test for Water Using Anhydrous Copper(II) Sulfate";
    [TextArea] public string introText = "Add a few drops of water to white anhydrous copper(II) sulfate and observe what happens.";
    [Tooltip("Optional. Leave empty to use the TextMeshPro default font.")]
    public TMP_FontAsset font;

    public enum Theme { MidnightBlue, CleanLab, ChemistryTeal, BlossomPink, Custom }
    static readonly string[] ThemeNames = { "Midnight Blue", "Clean Lab", "Chemistry Teal", "Blossom Pink" };

    [Header("Colours")]
    [Tooltip("Starting colour theme. Students can change it any time with the Theme button. Custom uses the colours below exactly.")]
    public Theme theme = Theme.MidnightBlue;
    [Tooltip("Remember the student's chosen theme next time the app opens")]
    public bool rememberTheme = true;
    [Tooltip("Glow colour on the object to tap next")]
    public Color highlightColor = new Color32(255, 209, 77, 255);
    [Tooltip("Glow colour on an object that has been identified")]
    public Color foundColor = new Color32(127, 216, 208, 255);
    public Color panelColor = new Color32(20, 26, 38, 238);
    public Color panelLight = new Color32(34, 42, 58, 245);
    public Color accent = new Color32(46, 125, 255, 255);
    public Color danger = new Color32(214, 72, 72, 255);
    public Color success = new Color32(46, 170, 96, 255);
    public Color textMain = Color.white;
    public Color textMuted = new Color32(196, 206, 220, 255);
    public Color tipColor = new Color32(255, 200, 87, 255);
    public Color observeColor = new Color32(46, 196, 182, 255);
    public Color scienceColor = new Color32(160, 128, 255, 255);
    public Color hintColor = new Color32(255, 138, 101, 255);

    // Raised for the step controller
    public event Action NextPressed, BackPressed, RestartConfirmed, EndConfirmed, BeginPressed, ThemeChanged;
    /// <summary>+1 = zoom in, -1 = zoom out, 0 = fit (reset view)</summary>
    public event Action<int> ZoomPressed;
    [Tooltip("Show the + / - / Fit zoom buttons on the right edge of the screen")]
    public bool showZoomButtons = false;
    RectTransform _zoomCol;

    public bool IsBlocking => _overlay != null && _overlay.gameObject.activeSelf && _blocking;
    public bool IsPaused { get; private set; }
    [NonSerialized] public string procedureText = "";

    // ---- built objects ----
    Sprite _round;
    RectTransform _safe, _hud;
    CanvasGroup _hudGroup;
    TextMeshProUGUI _stepLabel, _stepTitle, _stepBody, _tipChipText, _tipText;
    Image _tipChip;
    readonly List<Image> _progress = new List<Image>();
    Button _nextBtn, _backBtn;
    TextMeshProUGUI _nextLabel;

    RectTransform _overlay;
    CanvasGroup _overlayGroup;
    bool _blocking;
    RectTransform _pauseDlg, _confirmDlg, _procDlg, _quizDlg, _resultDlg, _introDlg;
    RectTransform _angleDlg, _infoDlg;
    TextMeshProUGUI _angleTitle, _anglePrompt, _angleValue, _infoTitle, _infoBody, _infoBtnLabel, _quizHeader;
    int _angle;
    Action<int> _angleSubmit;
    Action _infoClose;
    bool _returnToIntro;
    TextMeshProUGUI _confirmTitle, _confirmMsg, _confirmYesLabel, _procText;
    Button _confirmYes;
    Image _confirmYesImg;
    Action _confirmAction;
    bool _returnToPause;
    ScrollRect _procScroll;

    TextMeshProUGUI _quizCounter, _quizQuestion, _quizFeedback;
    RectTransform _quizOptions;
    readonly List<Button> _optionBtns = new List<Button>();
    Button _quizContinue;
    TextMeshProUGUI _resultScore, _resultMsg;
    Action _resultRestart, _resultFinish;

    Rect _lastSafe;
    RectTransform _label, _labelPointer;
    TextMeshProUGUI _labelTitle, _labelBody;
    Image _labelImg, _labelPointerImg, _labelStripe;
    Transform _labelTarget;
    float _labelAge;
    Coroutine _pop;
    GameObject _canvasGo;
    RectTransform _themeDlg;
    enum Origin { Hud, Pause, Intro }
    Origin _themeOrigin;

    // Remembered state, so the UI can be rebuilt in a new theme without losing anything
    bool _hasStep;
    int _sStep, _sTotal;
    string _sTitle = "", _sBody = "";
    TipKind _sTipKind;
    string _sTip = "";
    bool _sBack, _sNext;
    string _sNextLabel = "Next";
    string _sLabelTitle, _sLabelBody;

    // =====================================================================
    const string ThemeKey = "TajurbahGah_Theme";

    void Awake()
    {
        if (rememberTheme && PlayerPrefs.HasKey(ThemeKey))
        {
            int saved = PlayerPrefs.GetInt(ThemeKey);
            if (saved >= 0 && saved < ThemeNames.Length) theme = (Theme)saved;
        }
        BuildAll();
        Show(false);
    }

    void BuildAll()
    {
        ApplyTheme();
        if (!_round) _round = MakeRoundedSprite(64, 22);
        _progress.Clear();
        _optionBtns.Clear();
        BuildCanvas();
        BuildTopBar();
        BuildBottom();
        BuildLabel();
        BuildOverlay();
    }

    /// <summary>Switch theme while the practical is running. Everything on screen is kept.</summary>
    public void SetTheme(Theme newTheme)
    {
        bool hudOn = _hud && _hud.gameObject.activeSelf;
        Transform labelTarget = _labelTarget;

        if (_pop != null) { StopCoroutine(_pop); _pop = null; }
        HideOverlays();
        if (_canvasGo) Destroy(_canvasGo);

        theme = newTheme;
        if (rememberTheme && (int)newTheme < ThemeNames.Length) { PlayerPrefs.SetInt(ThemeKey, (int)newTheme); PlayerPrefs.Save(); }
        BuildAll();

        _hud.gameObject.SetActive(hudOn);
        if (_hasStep) SetStep(_sStep, _sTotal, _sTitle, _sBody);
        SetTip(_sTipKind, _sTip);
        SetNav(_sBack, _sNext, _sNextLabel);
        if (labelTarget) ShowLabel(labelTarget, _sLabelTitle, _sLabelBody, foundColor);

        ThemeChanged?.Invoke();
        OpenThemePicker(_themeOrigin);      // stay in the picker so different themes can be tried
    }

    void OpenThemePicker(Origin from)
    {
        _themeOrigin = from;
        OpenOverlay(_themeDlg, from != Origin.Intro);
    }

    void CloseThemePicker()
    {
        if (_themeOrigin == Origin.Pause) OpenOverlay(_pauseDlg, true);
        else if (_themeOrigin == Origin.Intro) { HideOverlays(); OpenOverlay(_introDlg, false); }
        else HideOverlays();
    }

    void Update()
    {
        if (Screen.safeArea != _lastSafe) ApplySafeArea();
        UpdateLabel();
    }

    // =========================== public API ==============================

    public void Show(bool show)
    {
        _hud.gameObject.SetActive(show);
        if (!show) HideOverlays();
    }

    /// <summary>Shows a name card floating above an object in the scene.</summary>
    public void ShowLabel(Transform target, string title, string body, Color stripe)
    {
        _labelTarget = target;
        _sLabelTitle = title;
        _sLabelBody = body;
        _labelTitle.text = title;
        _labelBody.text = body;
        _labelStripe.color = stripe;
        _labelPointerImg.color = panelColor;
        _label.gameObject.SetActive(true);
        _labelAge = 0f;
        UpdateLabel();
    }

    public void HideLabel()
    {
        _labelTarget = null;
        if (_label) _label.gameObject.SetActive(false);
    }

    /// <summary>Shows the start screen with the Begin Practical button.</summary>
    public void ShowIntro()
    {
        OpenOverlay(_introDlg, false);
    }

    public void SetStep(int step, int total, string title, string body)
    {
        _hasStep = true; _sStep = step; _sTotal = total; _sTitle = title; _sBody = body;
        _stepLabel.text = $"STEP {step} OF {total}";
        _stepTitle.text = title;
        _stepBody.text = body;
        while (_progress.Count < total) AddProgressSegment();
        for (int i = 0; i < _progress.Count; i++)
        {
            _progress[i].gameObject.SetActive(i < total);
            _progress[i].color = i < step ? accent : new Color(textMain.r, textMain.g, textMain.b, 0.15f);
        }
    }

    public void SetTip(TipKind kind, string text)
    {
        _sTipKind = kind; _sTip = text;
        Color c = kind == TipKind.Tip ? tipColor : kind == TipKind.Observe ? observeColor : kind == TipKind.Science ? scienceColor : hintColor;
        _tipChip.color = c;
        _tipChipText.text = kind == TipKind.Tip ? "TIP" : kind == TipKind.Observe ? "OBSERVE" : kind == TipKind.Science ? "SCIENCE" : "HINT";
        _tipText.text = text;
    }

    public void SetNav(bool showBack, bool showNext, string nextLabel = "Next")
    {
        _sBack = showBack; _sNext = showNext; _sNextLabel = nextLabel;
        _backBtn.gameObject.SetActive(showBack);
        _nextBtn.gameObject.SetActive(showNext);
        _nextLabel.text = nextLabel;
    }

    /// <summary>A single multiple-choice question during the practical (uses the quiz card with a custom header).</summary>
    public void ShowChoice(string header, string question, string[] options, Action<int> onAnswer)
    {
        ShowQuiz(0, 1, question, options, onAnswer);
        _quizHeader.text = header;
        _quizCounter.text = "";
        _quizCounter.gameObject.SetActive(false);
    }

    /// <summary>A pad for entering an angle in whole degrees.</summary>
    public void ShowAngleInput(string title, string prompt, int startValue, Action<int> onSubmit)
    {
        _angleTitle.text = title;
        _anglePrompt.text = prompt;
        _angle = Mathf.Clamp(startValue, 0, 180);
        _angleValue.text = _angle + "\u00b0";
        _angleSubmit = onSubmit;
        OpenOverlay(_angleDlg, false);
    }

    /// <summary>A window with a title, rich text (e.g. a table) and one button.</summary>
    public void ShowInfo(string title, string body, string buttonLabel, Action onClose)
    {
        _infoTitle.text = title;
        _infoBody.text = body;
        _infoBtnLabel.text = buttonLabel;
        _infoClose = onClose;
        OpenOverlay(_infoDlg, false);
    }

    void ChangeAngle(int delta)
    {
        _angle = Mathf.Clamp(_angle + delta, 0, 180);
        _angleValue.text = _angle + "\u00b0";
    }

    public void ShowQuiz(int index, int count, string question, string[] options, Action<int> onAnswer)
    {
        OpenOverlay(_quizDlg, false);
        _quizHeader.text = "CHECK YOUR UNDERSTANDING";
        _quizCounter.gameObject.SetActive(true);
        _quizCounter.text = $"Question {index + 1} of {count}";
        _quizQuestion.text = question;
        _quizFeedback.text = "";
        _quizFeedback.gameObject.SetActive(false);
        _quizContinue.gameObject.SetActive(false);

        while (_optionBtns.Count < options.Length)
            _optionBtns.Add(MakeButton("Option", _quizOptions, "", panelLight, 0, 92, null));

        for (int i = 0; i < _optionBtns.Count; i++)
        {
            var b = _optionBtns[i];
            bool used = i < options.Length;
            b.gameObject.SetActive(used);
            if (!used) continue;
            b.interactable = true;
            ((Image)b.targetGraphic).color = panelLight;
            b.GetComponentInChildren<TextMeshProUGUI>().text = options[i];
            int captured = i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => onAnswer(captured));
        }
    }

    public void ShowQuizFeedback(bool correct, int correctIndex, int chosen, string explanation, Action onContinue)
    {
        for (int i = 0; i < _optionBtns.Count; i++)
        {
            var b = _optionBtns[i];
            if (!b.gameObject.activeSelf) continue;
            b.interactable = false;
            var img = (Image)b.targetGraphic;
            if (i == correctIndex) img.color = success;
            else if (i == chosen) img.color = danger;
            else img.color = new Color(panelLight.r, panelLight.g, panelLight.b, 0.5f);
        }
        _quizFeedback.gameObject.SetActive(true);
        _quizFeedback.color = correct ? success : hintColor;
        _quizFeedback.text = (correct ? "<b>Correct!</b>  " : "<b>Not quite.</b>  ") + explanation;
        _quizContinue.gameObject.SetActive(true);
        _quizContinue.onClick.RemoveAllListeners();
        _quizContinue.onClick.AddListener(() => onContinue());
    }

    public void ShowResults(int score, int total, Action onRestart, Action onFinish)
    {
        _resultRestart = onRestart;
        _resultFinish = onFinish;
        _resultScore.text = $"{score} / {total}";
        _resultMsg.text = score == total ? "Excellent work! You answered every question correctly."
                        : score >= total / 2f ? "Good effort. Read the procedure again to revise the parts you missed."
                        : "Have another look at the procedure, then try the practical again.";
        OpenOverlay(_resultDlg, false);
    }

    public void HideOverlays()
    {
        if (_overlay) _overlay.gameObject.SetActive(false);
        _blocking = false;
        if (IsPaused) { IsPaused = false; Time.timeScale = 1f; }
    }

    // ============================ building ===============================

    void BuildCanvas()
    {
        var canvasGo = new GameObject("PracticalUI Canvas", typeof(RectTransform));
        _canvasGo = canvasGo;
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _safe = NewRect("SafeArea", canvasGo.transform);
        Stretch(_safe);
        ApplySafeArea();

        _hud = NewRect("HUD", _safe);
        Stretch(_hud);
        _hudGroup = _hud.gameObject.AddComponent<CanvasGroup>();
    }

    void BuildTopBar()
    {
        // Title chip (top left)
        var chip = NewPanel("TitleChip", _hud, panelColor);
        var crt = chip.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
        crt.pivot = new Vector2(0, 1);
        crt.anchoredPosition = new Vector2(28, -24);
        var h = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(30, 30, 16, 16);
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        var fit = chip.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        NewText("Title", chip.transform, practicalTitle, 28, textMain, FontStyles.Bold);

        // Buttons (top right)
        var row = NewRect("TopButtons", _hud);
        row.anchorMin = row.anchorMax = new Vector2(1, 1);
        row.pivot = new Vector2(1, 1);
        row.anchoredPosition = new Vector2(-28, -24);
        var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 12;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        var rf = row.gameObject.AddComponent<ContentSizeFitter>();
        rf.horizontalFit = rf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        MakeButton("ThemeBtn", row, "Theme", panelColor, 150, 72, () => OpenThemePicker(Origin.Hud));
        MakeButton("ProcedureBtn", row, "Procedure", panelColor, 240, 72, () => OpenProcedure(false));
        MakeButton("RestartBtn", row, "Restart", panelColor, 170, 72, () => AskRestart(false));
        MakeButton("PauseBtn", row, "Pause", panelColor, 150, 72, OpenPause);
        MakeButton("EndBtn", row, "End", danger, 130, 72, () => AskEnd(false));

        // Zoom buttons (right edge, middle)
        _zoomCol = NewRect("ZoomButtons", _hud);
        _zoomCol.anchorMin = _zoomCol.anchorMax = new Vector2(1, 0.62f);
        _zoomCol.pivot = new Vector2(1, 0.5f);
        _zoomCol.anchoredPosition = new Vector2(-28, 0);
        var zv = _zoomCol.gameObject.AddComponent<VerticalLayoutGroup>();
        zv.spacing = 12;
        zv.childControlWidth = zv.childControlHeight = true;
        zv.childForceExpandWidth = zv.childForceExpandHeight = false;
        var zf = _zoomCol.gameObject.AddComponent<ContentSizeFitter>();
        zf.horizontalFit = zf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        MakeButton("ZoomIn", _zoomCol, "+", panelColor, 96, 96, () => ZoomPressed?.Invoke(1));
        MakeButton("ZoomOut", _zoomCol, "\u2212", panelColor, 96, 96, () => ZoomPressed?.Invoke(-1));
        var fitBtn = MakeButton("ZoomFit", _zoomCol, "Fit", panelColor, 96, 80, () => ZoomPressed?.Invoke(0));
        fitBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 26;
        _zoomCol.gameObject.SetActive(showZoomButtons);
    }

    public void SetZoomButtons(bool show)
    {
        showZoomButtons = show;
        if (_zoomCol) _zoomCol.gameObject.SetActive(show);
    }

    void BuildBottom()
    {
        var col = NewRect("Bottom", _hud);
        col.anchorMin = col.anchorMax = new Vector2(0.5f, 0);
        col.pivot = new Vector2(0.5f, 0);
        col.anchoredPosition = new Vector2(0, 26);
        col.sizeDelta = new Vector2(1560, 0);
        var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 12;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        col.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ---- tip strip ----
        var tip = NewPanel("TipStrip", col, panelColor);
        var th = tip.gameObject.AddComponent<HorizontalLayoutGroup>();
        th.padding = new RectOffset(16, 28, 14, 14);
        th.spacing = 20;
        th.childAlignment = TextAnchor.MiddleLeft;
        th.childControlWidth = th.childControlHeight = true;
        th.childForceExpandWidth = th.childForceExpandHeight = false;

        _tipChip = NewPanel("Chip", tip.transform, tipColor);
        var chipLE = _tipChip.gameObject.AddComponent<LayoutElement>();
        chipLE.preferredWidth = chipLE.minWidth = 170;
        chipLE.preferredHeight = chipLE.minHeight = 50;
        _tipChipText = NewText("ChipText", _tipChip.transform, "TIP", 24, new Color32(20, 26, 38, 255), FontStyles.Bold, TextAlignmentOptions.Center);
        _tipChipText.characterSpacing = 6;
        Stretch(_tipChipText.rectTransform);

        _tipText = NewText("TipText", tip.transform, "", 28, textMain);
        _tipText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        // ---- instruction card ----
        var card = NewPanel("InstructionCard", col, panelColor);
        var ch = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        ch.padding = new RectOffset(40, 32, 30, 30);
        ch.spacing = 36;
        ch.childAlignment = TextAnchor.MiddleLeft;
        ch.childControlWidth = ch.childControlHeight = true;
        ch.childForceExpandWidth = ch.childForceExpandHeight = false;

        var bar = NewPanel("AccentBar", card.transform, accent);
        bar.pixelsPerUnitMultiplier = 6f;
        var barLE = bar.gameObject.AddComponent<LayoutElement>();
        barLE.preferredWidth = barLE.minWidth = 8;
        barLE.minHeight = 150;
        ch.padding = new RectOffset(28, 32, 30, 30);
        ch.spacing = 28;

        var left = NewRect("Text", card.transform);
        var lv = left.gameObject.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 10;
        lv.childControlWidth = lv.childControlHeight = true;
        lv.childForceExpandWidth = true;
        lv.childForceExpandHeight = false;
        var leftLE = left.gameObject.AddComponent<LayoutElement>();
        leftLE.flexibleWidth = 1;
        leftLE.minHeight = 170;

        _stepLabel = NewText("StepLabel", left, "STEP 1 OF 5", 24, accent, FontStyles.Bold);
        _stepLabel.characterSpacing = 6;
        _stepTitle = NewText("StepTitle", left, "", 42, textMain, FontStyles.Bold);
        _stepBody = NewText("StepBody", left, "", 32, textMuted);

        var prog = NewRect("Progress", left);
        var ph = prog.gameObject.AddComponent<HorizontalLayoutGroup>();
        ph.spacing = 10;
        ph.childControlWidth = ph.childControlHeight = true;
        ph.childForceExpandWidth = true;
        ph.childForceExpandHeight = true;
        var progLE = prog.gameObject.AddComponent<LayoutElement>();
        progLE.preferredHeight = progLE.minHeight = 10;
        _progressParent = prog;

        var right = NewRect("Buttons", card.transform);
        var rv = right.gameObject.AddComponent<VerticalLayoutGroup>();
        rv.spacing = 14;
        rv.childAlignment = TextAnchor.MiddleCenter;
        rv.childControlWidth = rv.childControlHeight = true;
        rv.childForceExpandWidth = true;
        rv.childForceExpandHeight = false;
        var rightLE = right.gameObject.AddComponent<LayoutElement>();
        rightLE.preferredWidth = rightLE.minWidth = 240;

        _nextBtn = MakeButton("NextBtn", right, "Next", accent, 240, 88, () => NextPressed?.Invoke());
        _nextLabel = _nextBtn.GetComponentInChildren<TextMeshProUGUI>();
        _backBtn = MakeButton("BackBtn", right, "Back", panelLight, 240, 70, () => BackPressed?.Invoke());
    }

    RectTransform _progressParent;

    void AddProgressSegment()
    {
        var seg = NewPanel("Segment", _progressParent, Color.white);
        seg.pixelsPerUnitMultiplier = 4f;
        _progress.Add(seg);
    }

    void BuildOverlay()
    {
        _overlay = NewRect("Overlay", _safe.parent);     // full screen, above the HUD
        Stretch(_overlay);
        var dim = _overlay.gameObject.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.62f);           // also blocks taps behind it
        _overlayGroup = _overlay.gameObject.AddComponent<CanvasGroup>();

        // ---- Start screen ----
        _introDlg = NewDialog("IntroDialog", 1150);
        var il = NewText("Label", _introDlg, introLabel, 26, accent, FontStyles.Bold, TextAlignmentOptions.Center);
        il.characterSpacing = 8;
        NewText("Title", _introDlg, introTitle, 56, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        NewText("Text", _introDlg, introText, 32, textMuted, FontStyles.Normal, TextAlignmentOptions.Center);
        var irow = NewRect("Row", _introDlg);
        var irh = irow.gameObject.AddComponent<HorizontalLayoutGroup>();
        irh.spacing = 20;
        irh.padding = new RectOffset(0, 0, 16, 0);
        irh.childControlWidth = irh.childControlHeight = true;
        irh.childForceExpandWidth = true;
        irh.childForceExpandHeight = false;
        MakeButton("Procedure", irow, "Read Procedure", panelLight, 0, 96, OpenProcedureFromIntro);
        MakeButton("Begin", irow, "Begin Practical", accent, 0, 96, () => { HideOverlays(); BeginPressed?.Invoke(); });
        MakeButton("Theme", _introDlg, "Change Theme", panelLight, 0, 70, () => OpenThemePicker(Origin.Intro));

        // ---- Pause ----
        _pauseDlg = NewDialog("PauseDialog", 720);
        NewText("Title", _pauseDlg, "Paused", 52, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        NewText("Sub", _pauseDlg, "The practical is paused. Take your time.", 30, textMuted, FontStyles.Normal, TextAlignmentOptions.Center);
        MakeButton("Resume", _pauseDlg, "Resume", accent, 0, 90, HideOverlays);
        MakeButton("Procedure", _pauseDlg, "Read Procedure", panelLight, 0, 84, () => OpenProcedure(true));
        MakeButton("Theme", _pauseDlg, "Change Theme", panelLight, 0, 84, () => OpenThemePicker(Origin.Pause));
        MakeButton("Restart", _pauseDlg, "Restart Practical", panelLight, 0, 84, () => AskRestart(true));
        MakeButton("End", _pauseDlg, "End Practical", danger, 0, 84, () => AskEnd(true));

        // ---- Confirm ----
        _confirmDlg = NewDialog("ConfirmDialog", 860);
        _confirmTitle = NewText("Title", _confirmDlg, "", 44, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        _confirmMsg = NewText("Msg", _confirmDlg, "", 30, textMuted, FontStyles.Normal, TextAlignmentOptions.Center);
        var crow = NewRect("Row", _confirmDlg);
        var crh = crow.gameObject.AddComponent<HorizontalLayoutGroup>();
        crh.spacing = 20;
        crh.childControlWidth = crh.childControlHeight = true;
        crh.childForceExpandWidth = true;
        crh.childForceExpandHeight = false;
        MakeButton("Cancel", crow, "Cancel", panelLight, 0, 86, CancelConfirm);
        _confirmYes = MakeButton("Yes", crow, "Yes", danger, 0, 86, () => { var a = _confirmAction; HideOverlays(); a?.Invoke(); });
        _confirmYesImg = (Image)_confirmYes.targetGraphic;
        _confirmYesLabel = _confirmYes.GetComponentInChildren<TextMeshProUGUI>();

        // ---- Procedure (fixed size, scrollable) ----
        _procDlg = NewRect("ProcedureDialog", _overlay);
        var pimg = _procDlg.gameObject.AddComponent<Image>();
        pimg.sprite = _round; pimg.type = Image.Type.Sliced; pimg.color = panelColor;
        _procDlg.sizeDelta = new Vector2(1380, 900);
        var pv = _procDlg.gameObject.AddComponent<VerticalLayoutGroup>();
        pv.padding = new RectOffset(52, 52, 40, 40);
        pv.spacing = 20;
        pv.childControlWidth = pv.childControlHeight = true;
        pv.childForceExpandWidth = true;
        pv.childForceExpandHeight = false;

        var header = NewRect("Header", _procDlg);
        var hh = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        hh.childAlignment = TextAnchor.MiddleLeft;
        hh.childControlWidth = hh.childControlHeight = true;
        hh.childForceExpandWidth = false;
        hh.childForceExpandHeight = false;
        var ptitle = NewText("Title", header, "Full Procedure", 46, textMain, FontStyles.Bold);
        ptitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        MakeButton("Close", header, "Close", accent, 180, 76, CloseProcedure);

        var scrollGo = NewRect("Scroll", _procDlg);
        scrollGo.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        _procScroll = scrollGo.gameObject.AddComponent<ScrollRect>();
        _procScroll.horizontal = false;
        _procScroll.movementType = ScrollRect.MovementType.Clamped;
        _procScroll.scrollSensitivity = 40;
        var viewport = NewRect("Viewport", scrollGo);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);                 // invisible, but catches drags
        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var cv = content.gameObject.AddComponent<VerticalLayoutGroup>();
        cv.padding = new RectOffset(0, 24, 0, 20);
        cv.childControlWidth = cv.childControlHeight = true;
        cv.childForceExpandWidth = true;
        cv.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _procText = NewText("Text", content, "", 31, textMuted);
        _procText.lineSpacing = 12;
        _procScroll.viewport = viewport;
        _procScroll.content = content;

        // ---- Quiz ----
        _quizDlg = NewDialog("QuizDialog", 1240);
        var qh = NewText("Header", _quizDlg, "CHECK YOUR UNDERSTANDING", 26, accent, FontStyles.Bold, TextAlignmentOptions.Center);
        qh.characterSpacing = 6;
        _quizHeader = qh;
        _quizCounter = NewText("Counter", _quizDlg, "", 28, textMuted, FontStyles.Normal, TextAlignmentOptions.Center);
        _quizQuestion = NewText("Question", _quizDlg, "", 42, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        _quizOptions = NewRect("Options", _quizDlg);
        var ov = _quizOptions.gameObject.AddComponent<VerticalLayoutGroup>();
        ov.spacing = 14;
        ov.childControlWidth = ov.childControlHeight = true;
        ov.childForceExpandWidth = true;
        ov.childForceExpandHeight = false;
        _quizFeedback = NewText("Feedback", _quizDlg, "", 30, success, FontStyles.Normal, TextAlignmentOptions.Center);
        _quizContinue = MakeButton("Continue", _quizDlg, "Continue", accent, 0, 88, null);

        // ---- Angle input ----
        _angleDlg = NewDialog("AngleDialog", 900);
        _angleTitle = NewText("Title", _angleDlg, "", 44, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        _anglePrompt = NewText("Prompt", _angleDlg, "", 30, textMuted, FontStyles.Normal, TextAlignmentOptions.Center);
        _angleValue = NewText("Value", _angleDlg, "0\u00b0", 120, accent, FontStyles.Bold, TextAlignmentOptions.Center);
        var arow = NewRect("Steps", _angleDlg);
        var arh = arow.gameObject.AddComponent<HorizontalLayoutGroup>();
        arh.spacing = 14;
        arh.childControlWidth = arh.childControlHeight = true;
        arh.childForceExpandWidth = true;
        arh.childForceExpandHeight = false;
        MakeButton("M5", arow, "\u2212 5", panelLight, 0, 96, () => ChangeAngle(-5));
        MakeButton("M1", arow, "\u2212 1", panelLight, 0, 96, () => ChangeAngle(-1));
        MakeButton("P1", arow, "+ 1", panelLight, 0, 96, () => ChangeAngle(1));
        MakeButton("P5", arow, "+ 5", panelLight, 0, 96, () => ChangeAngle(5));
        MakeButton("Submit", _angleDlg, "Record this reading", accent, 0, 92, () => { var a = _angleSubmit; HideOverlays(); a?.Invoke(_angle); });

        // ---- Info / table ----
        _infoDlg = NewDialog("InfoDialog", 1300);
        _infoTitle = NewText("Title", _infoDlg, "", 46, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        _infoBody = NewText("Body", _infoDlg, "", 30, textMuted);
        _infoBody.lineSpacing = 10;
        var ib = MakeButton("Close", _infoDlg, "Continue", accent, 0, 90, () => { var a = _infoClose; HideOverlays(); a?.Invoke(); });
        _infoBtnLabel = ib.GetComponentInChildren<TextMeshProUGUI>();

        // ---- Theme picker ----
        _themeDlg = NewDialog("ThemeDialog", 820);
        NewText("Title", _themeDlg, "Choose a Theme", 48, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        for (int i = 0; i < ThemeNames.Length; i++)
        {
            var c = GetThemeColors((Theme)i);
            int idx = i;
            string mark = (int)theme == i ? "\u25CF  " : "";
            var b = MakeButton("Theme" + i, _themeDlg, mark + ThemeNames[i], c.accent, 0, 90, () => SetTheme((Theme)idx));
            // a strip of the theme's panel colour on the left, as a preview
            var sw = NewPanel("Swatch", b.transform, c.panel);
            var srt = sw.rectTransform;
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(0, 1);
            srt.pivot = new Vector2(0, 0.5f);
            srt.offsetMin = new Vector2(12, 12); srt.offsetMax = new Vector2(72, -12);
            sw.raycastTarget = false;
        }
        MakeButton("Done", _themeDlg, "Done", panelLight, 0, 80, CloseThemePicker);

        // ---- Results ----
        _resultDlg = NewDialog("ResultDialog", 900);
        NewText("Title", _resultDlg, "Practical Complete!", 54, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        _resultScore = NewText("Score", _resultDlg, "", 110, accent, FontStyles.Bold, TextAlignmentOptions.Center);
        _resultMsg = NewText("Msg", _resultDlg, "", 30, textMuted, FontStyles.Normal, TextAlignmentOptions.Center);
        var rrow = NewRect("Row", _resultDlg);
        var rrh = rrow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rrh.spacing = 20;
        rrh.childControlWidth = rrh.childControlHeight = true;
        rrh.childForceExpandWidth = true;
        rrh.childForceExpandHeight = false;
        MakeButton("Again", rrow, "Try Again", panelLight, 0, 88, () => { HideOverlays(); _resultRestart?.Invoke(); });
        MakeButton("Finish", rrow, "Finish", accent, 0, 88, () => { HideOverlays(); _resultFinish?.Invoke(); });

        _overlay.gameObject.SetActive(false);
    }

    // ============================ overlay logic ===========================

    void OpenOverlay(RectTransform dialog, bool pauseGame)
    {
        _overlay.gameObject.SetActive(true);
        _overlay.SetAsLastSibling();
        foreach (var d in new[] { _pauseDlg, _confirmDlg, _procDlg, _quizDlg, _resultDlg, _introDlg, _themeDlg, _angleDlg, _infoDlg })
            d.gameObject.SetActive(d == dialog);
        _blocking = true;
        if (pauseGame && !IsPaused) { IsPaused = true; Time.timeScale = 0f; }
        if (_pop != null) StopCoroutine(_pop);
        _pop = StartCoroutine(PopIn(dialog));
    }

    IEnumerator PopIn(RectTransform dialog)
    {
        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.SmoothStep(0f, 1f, t / 0.16f);
            _overlayGroup.alpha = e;
            dialog.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
            yield return null;
        }
        _overlayGroup.alpha = 1f;
        dialog.localScale = Vector3.one;
    }

    void OpenPause() => OpenOverlay(_pauseDlg, true);

    void OpenProcedureFromIntro()
    {
        OpenProcedure(false);
        _returnToIntro = true;
    }

    void OpenProcedure(bool fromPause)
    {
        _returnToPause = fromPause;
        _returnToIntro = false;
        _procText.text = procedureText;
        OpenOverlay(_procDlg, true);
        Canvas.ForceUpdateCanvases();
        _procScroll.verticalNormalizedPosition = 1f;
    }

    void CloseProcedure()
    {
        if (_returnToIntro) { _returnToIntro = false; HideOverlays(); OpenOverlay(_introDlg, false); return; }
        if (_returnToPause) OpenOverlay(_pauseDlg, true);
        else HideOverlays();
    }

    void AskRestart(bool fromPause)
    {
        ShowConfirm(fromPause, "Restart the practical?", "Everything goes back to Step 1.", "Restart", accent,
                    () => RestartConfirmed?.Invoke());
    }

    void AskEnd(bool fromPause)
    {
        ShowConfirm(fromPause, "End the practical?", "Your progress in this practical will be lost.", "End Practical", danger,
                    () => EndConfirmed?.Invoke());
    }

    void ShowConfirm(bool fromPause, string title, string msg, string yes, Color yesColor, Action onYes)
    {
        _returnToPause = fromPause;
        _confirmTitle.text = title;
        _confirmMsg.text = msg;
        _confirmYesLabel.text = yes;
        _confirmYesImg.color = yesColor;
        _confirmAction = onYes;
        OpenOverlay(_confirmDlg, true);
    }

    void CancelConfirm()
    {
        if (_returnToPause) OpenOverlay(_pauseDlg, true);
        else HideOverlays();
    }

    // ============================== helpers ==============================

    RectTransform NewDialog(string name, float width)
    {
        var img = NewPanel(name, _overlay, panelColor);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 0);
        var v = img.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(56, 56, 48, 48);
        v.spacing = 22;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        img.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    Image NewPanel(string name, Transform parent, Color c)
    {
        var rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = _round;
        img.type = Image.Type.Sliced;
        img.color = c;
        return img;
    }

    TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color,
                            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(string name, Transform parent, string label, Color bg, float width, float height, UnityAction onClick)
    {
        var img = NewPanel(name, parent, bg);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        cb.selectedColor = Color.white;
        cb.disabledColor = Color.white;          // keep the colour we set when showing quiz answers
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        if (onClick != null) b.onClick.AddListener(onClick);

        var le = img.gameObject.AddComponent<LayoutElement>();
        if (width > 0) le.preferredWidth = le.minWidth = width;
        le.preferredHeight = le.minHeight = height;

        var t = NewText("Label", img.transform, label, 32, ReadableOn(bg), FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 20, 20, 8, 8);
        t.textWrappingMode = TextWrappingModes.Normal;
        return b;
    }

    /// <summary>White text on dark colours, dark text on light colours.</summary>
    Color ReadableOn(Color bg)
    {
        float lum = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
        return lum > 0.6f ? new Color32(27, 36, 51, 255) : Color.white;
    }

    struct ThemeColors
    {
        public Color panel, light, accent, text, muted, tip, observe, science, hint, danger, success, highlight, found;
    }

    static ThemeColors GetThemeColors(Theme t)
    {
        // Shared defaults (used by the blue, white and teal themes)
        var c = new ThemeColors
        {
            tip = new Color32(255, 196, 77, 255),
            observe = new Color32(46, 196, 182, 255),
            science = new Color32(160, 128, 255, 255),
            hint = new Color32(255, 138, 101, 255),
            danger = new Color32(220, 76, 76, 255),
            success = new Color32(46, 170, 96, 255),
            highlight = new Color32(255, 209, 77, 255),
            found = new Color32(46, 196, 182, 255),
        };
        switch (t)
        {
            case Theme.CleanLab:            // white cards, blue buttons, dark text
                c.panel = new Color32(255, 255, 255, 245);
                c.light = new Color32(232, 238, 246, 255);
                c.accent = new Color32(31, 111, 235, 255);
                c.text = new Color32(27, 36, 51, 255);
                c.muted = new Color32(74, 85, 104, 255);
                break;
            case Theme.ChemistryTeal:       // deep teal panels, teal buttons
                c.panel = new Color32(14, 38, 42, 240);
                c.light = new Color32(28, 63, 68, 250);
                c.accent = new Color32(25, 181, 165, 255);
                c.text = Color.white;
                c.muted = new Color32(190, 222, 218, 255);
                c.found = new Color32(255, 140, 190, 255);
                break;
            case Theme.BlossomPink:         // dark berry panels, hot-pink buttons, pink glow
                c.panel = new Color32(44, 18, 38, 240);
                c.light = new Color32(72, 32, 62, 250);
                c.accent = new Color32(255, 92, 160, 255);
                c.text = Color.white;
                c.muted = new Color32(244, 204, 224, 255);
                c.tip = new Color32(255, 170, 205, 255);
                c.observe = new Color32(127, 216, 208, 255);
                c.science = new Color32(195, 165, 255, 255);
                c.danger = new Color32(222, 70, 104, 255);
                c.highlight = new Color32(255, 120, 180, 255);
                c.found = new Color32(127, 216, 208, 255);
                break;
            default:                        // MidnightBlue: dark navy panels, bright blue buttons
                c.panel = new Color32(18, 24, 36, 240);
                c.light = new Color32(36, 46, 64, 250);
                c.accent = new Color32(61, 139, 255, 255);
                c.text = Color.white;
                c.muted = new Color32(196, 206, 220, 255);
                break;
        }
        return c;
    }

    void ApplyTheme()
    {
        if (theme == Theme.Custom) return;
        var c = GetThemeColors(theme);
        panelColor = c.panel; panelLight = c.light; accent = c.accent;
        textMain = c.text; textMuted = c.muted;
        tipColor = c.tip; observeColor = c.observe; scienceColor = c.science; hintColor = c.hint;
        danger = c.danger; success = c.success;
        highlightColor = c.highlight; foundColor = c.found;
    }

    void BuildLabel()
    {
        _label = NewRect("ObjectLabel", _hud);
        _label.anchorMin = _label.anchorMax = new Vector2(0.5f, 0.5f);
        _label.pivot = new Vector2(0.5f, 0f);

        var card = NewPanel("Card", _label, panelColor);
        _labelImg = card;
        var crt = card.rectTransform;
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0, 14);
        var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(18, 26, 18, 20);
        h.spacing = 16;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        var fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _labelStripe = NewPanel("Stripe", card.transform, accent);
        _labelStripe.pixelsPerUnitMultiplier = 6f;
        var sle = _labelStripe.gameObject.AddComponent<LayoutElement>();
        sle.preferredWidth = sle.minWidth = 7;

        var col = NewRect("Text", card.transform);
        var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var cle = col.gameObject.AddComponent<LayoutElement>();
        cle.preferredWidth = 520;
        _labelTitle = NewText("Title", col, "", 32, textMain, FontStyles.Bold);
        _labelBody = NewText("Body", col, "", 26, textMuted);

        // Little diamond under the card pointing at the object
        var ptr = NewRect("Pointer", _label);
        _labelPointer = ptr;
        _labelPointerImg = ptr.gameObject.AddComponent<Image>();
        _labelPointerImg.color = panelColor;
        ptr.sizeDelta = new Vector2(26, 26);
        ptr.anchorMin = ptr.anchorMax = new Vector2(0.5f, 0f);
        ptr.anchoredPosition = new Vector2(0, 14);
        ptr.localRotation = Quaternion.Euler(0, 0, 45);
        ptr.SetAsFirstSibling();

        _label.gameObject.SetActive(false);
    }

    void UpdateLabel()
    {
        if (!_label || !_label.gameObject.activeSelf) return;
        if (!_labelTarget) { HideLabel(); return; }
        Camera cam = Camera.main ? Camera.main : FindFirstObjectByType<Camera>();
        if (!cam) return;

        // Top of the object on screen
        Bounds b = new Bounds(_labelTarget.position, Vector3.zero);
        bool any = false;
        foreach (var r in _labelTarget.GetComponentsInChildren<Renderer>())
        {
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        Vector3 sp = cam.WorldToScreenPoint(new Vector3(b.center.x, b.max.y, b.center.z));
        if (sp.z < 0f) { _label.gameObject.SetActive(false); return; }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_hud, sp, null, out Vector2 local);
        local.y += 24f;

        // Keep the card on screen
        var card = _labelImg.rectTransform;
        float halfW = card.rect.width * 0.5f + 20f;
        Rect area = _hud.rect;
        float x = Mathf.Clamp(local.x, area.xMin + halfW, area.xMax - halfW);
        float y = Mathf.Min(local.y, area.yMax - card.rect.height - 140f);
        _label.anchoredPosition = new Vector2(x, y);
        _labelPointer.anchoredPosition = new Vector2(local.x - x, 14);

        // Small pop when it first appears
        if (_labelAge < 0.15f)
        {
            _labelAge += Time.unscaledDeltaTime;
            _label.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(_labelAge / 0.15f));
        }
    }

    static void Stretch(RectTransform rt, float l = 0, float r = 0, float top = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, bottom);
        rt.offsetMax = new Vector2(-r, -top);
    }

    void ApplySafeArea()
    {
        _lastSafe = Screen.safeArea;
        if (!_safe || Screen.width <= 0 || Screen.height <= 0) return;
        Vector2 min = _lastSafe.position, max = _lastSafe.position + _lastSafe.size;
        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;
        _safe.anchorMin = min;
        _safe.anchorMax = max;
        _safe.offsetMin = _safe.offsetMax = Vector2.zero;
    }

    static Sprite MakeRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "RoundedRect"
        };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
}