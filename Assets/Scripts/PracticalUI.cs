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
    [Tooltip("Optional. Leave empty to use the TextMeshPro default font.")]
    public TMP_FontAsset font;

    [Header("Colours")]
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
    public event Action NextPressed, BackPressed, RestartConfirmed, EndConfirmed;

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
    RectTransform _pauseDlg, _confirmDlg, _procDlg, _quizDlg, _resultDlg;
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
    Coroutine _pop;

    // =====================================================================
    void Awake()
    {
        _round = MakeRoundedSprite(64, 22);
        BuildCanvas();
        BuildTopBar();
        BuildBottom();
        BuildOverlay();
        Show(false);
    }

    void Update()
    {
        if (Screen.safeArea != _lastSafe) ApplySafeArea();
    }

    // =========================== public API ==============================

    public void Show(bool show)
    {
        _hud.gameObject.SetActive(show);
        if (!show) HideOverlays();
    }

    public void SetStep(int step, int total, string title, string body)
    {
        _stepLabel.text = $"STEP {step} OF {total}";
        _stepTitle.text = title;
        _stepBody.text = body;
        while (_progress.Count < total) AddProgressSegment();
        for (int i = 0; i < _progress.Count; i++)
        {
            _progress[i].gameObject.SetActive(i < total);
            _progress[i].color = i < step ? accent : new Color(1f, 1f, 1f, 0.15f);
        }
    }

    public void SetTip(TipKind kind, string text)
    {
        Color c = kind == TipKind.Tip ? tipColor : kind == TipKind.Observe ? observeColor : kind == TipKind.Science ? scienceColor : hintColor;
        _tipChip.color = c;
        _tipChipText.text = kind == TipKind.Tip ? "TIP" : kind == TipKind.Observe ? "OBSERVE" : kind == TipKind.Science ? "SCIENCE" : "HINT";
        _tipText.text = text;
    }

    public void SetNav(bool showBack, bool showNext, string nextLabel = "Next")
    {
        _backBtn.gameObject.SetActive(showBack);
        _nextBtn.gameObject.SetActive(showNext);
        _nextLabel.text = nextLabel;
    }

    public void ShowQuiz(int index, int count, string question, string[] options, Action<int> onAnswer)
    {
        OpenOverlay(_quizDlg, false);
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

        MakeButton("ProcedureBtn", row, "Procedure", panelColor, 200, 72, () => OpenProcedure(false));
        MakeButton("RestartBtn", row, "Restart", panelColor, 170, 72, () => AskRestart(false));
        MakeButton("PauseBtn", row, "Pause", panelColor, 150, 72, OpenPause);
        MakeButton("EndBtn", row, "End", danger, 130, 72, () => AskEnd(false));
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

        // ---- Pause ----
        _pauseDlg = NewDialog("PauseDialog", 720);
        NewText("Title", _pauseDlg, "Paused", 52, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        NewText("Sub", _pauseDlg, "The practical is paused. Take your time.", 30, textMuted, FontStyles.Normal, TextAlignmentOptions.Center);
        MakeButton("Resume", _pauseDlg, "Resume", accent, 0, 90, HideOverlays);
        MakeButton("Procedure", _pauseDlg, "Read Procedure", panelLight, 0, 84, () => OpenProcedure(true));
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
        foreach (var d in new[] { _pauseDlg, _confirmDlg, _procDlg, _quizDlg, _resultDlg })
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

    void OpenProcedure(bool fromPause)
    {
        _returnToPause = fromPause;
        _procText.text = procedureText;
        OpenOverlay(_procDlg, true);
        Canvas.ForceUpdateCanvases();
        _procScroll.verticalNormalizedPosition = 1f;
    }

    void CloseProcedure()
    {
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

        var t = NewText("Label", img.transform, label, 32, textMain, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 20, 20, 8, 8);
        t.textWrappingMode = TextWrappingModes.Normal;
        return b;
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
