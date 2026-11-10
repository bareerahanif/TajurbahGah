using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// "Verify the laws of reflection using a plane mirror" (pin method), done the way it is in the Punjab board lab.
/// The student: identifies the apparatus, fixes the paper, draws MM', marks O, draws the normal, draws the incident ray
/// with the protractor, places the mirror, fixes P1 and P2, looks into the mirror and lines up P3 and P4 with the images,
/// removes the mirror, draws the reflected ray, measures angle r and records it. Repeated for 3 angles, then
/// results (with the student's real errors explained) and a viva.
///
/// Setup: put this on an empty GameObject (PracticalUI is added automatically), drag ReflectionApparatus into "Apparatus".
/// Camera points IntroCameraPoint / ApproachCameraPoint / PracticalCameraPoint are found by name.
/// </summary>
[RequireComponent(typeof(PracticalUI))]
public class ReflectionPractical : MonoBehaviour
{
    [Serializable]
    public class QuizQuestion
    {
        [TextArea] public string question;
        public string[] options;
        public int correctIndex;
        [TextArea] public string explanation;
    }

    [Header("Apparatus")]
    [Tooltip("The ReflectionApparatus object made by ReflectionApparatusBuilder")]
    public Transform apparatus;

    [Header("Readings")]
    [Tooltip("Angles of incidence to use, one reading each")]
    public int[] anglesOfIncidence = { 30, 45, 60 };

    [Header("Paper layout (metres, measured from the centre of the paper)")]
    [Tooltip("How far behind the centre of the paper the mirror line MM' is drawn")]
    public float mirrorLineOffset = 0.045f;
    public float rayLength = 0.13f;

    [Header("Camera")]
    [Tooltip("Work out the drawing view automatically: almost straight down, with the whole board above the instruction card")]
    public bool autoTopView = true;
    [Tooltip("How steeply the drawing view looks down (90 = straight down)")]
    [Range(60f, 90f)] public float topViewTilt = 82f;
    [Tooltip("Extra space around the board in the drawing view (1 = tight fit)")]
    [Range(0.9f, 1.5f)] public float topViewPadding = 1.05f;
    public float cameraMoveTime = 1.6f;
    [Tooltip("How far the student can zoom in (3 = three times closer)")]
    [Range(1.5f, 6f)] public float maxZoom = 4f;
    [Tooltip("Height of the eye above the paper when looking into the mirror (metres)")]
    public float eyeHeight = 0.03f;
    public float eyeDistance = 0.30f;
    [Tooltip("How much the eye-level view zooms in on the mirror so the images are easy to see")]
    [Range(1f, 4f)] public float eyeViewZoom = 2.2f;

    [Header("Ending")]
    [Tooltip("Scene to open when the student ends or finishes, e.g. PracticalSelectScene. Empty = reload this scene.")]
    public string exitSceneName = "PracticalSelectScene";

    [Header("Procedure text (Procedure button)")]
    [TextArea(12, 40)]
    public string procedureText =
        "<size=36><b><color=#FFFFFF>Aim</color></b></size>\n" +
        "To verify the laws of reflection of light using a plane mirror.\n\n" +
        "<size=36><b><color=#FFFFFF>Apparatus</color></b></size>\n" +
        "Drawing board, white sheet of paper, drawing pins, plane mirror strip with stand, four common pins, protractor, ruler, sharp pencil.\n\n" +
        "<size=36><b><color=#FFFFFF>Laws of reflection</color></b></size>\n" +
        "1.  The angle of incidence is equal to the angle of reflection (angle <i>i</i> = angle <i>r</i>).\n" +
        "2.  The incident ray, the reflected ray and the normal at the point of incidence all lie in the same plane.\n\n" +
        "<size=36><b><color=#FFFFFF>Procedure</color></b></size>\n" +
        "1.  Fix the white paper on the drawing board with drawing pins.\n" +
        "2.  Draw a straight line MM′ in the middle of the paper. Mark a point O near the middle of MM′.\n" +
        "3.  Draw the normal ON at O, at 90° to MM′, using the protractor.\n" +
        "4.  Draw the incident ray AO making an angle angle <i>i</i> (e.g. 30°) with the normal.\n" +
        "5.  Place the mirror upright with its silvered surface exactly on MM′.\n" +
        "6.  Fix two pins P<sub>1</sub> and P<sub>2</sub> upright on AO, at least 5 cm apart.\n" +
        "7.  Look into the mirror from the other side of the normal. Fix P<sub>3</sub> and P<sub>4</sub> so that they and the images of P<sub>1</sub> and P<sub>2</sub> appear in one straight line.\n" +
        "8.  Remove the mirror and pins. Join P<sub>4</sub>P<sub>3</sub> and produce it to meet MM′. This is the reflected ray OB.\n" +
        "9.  Measure angle <i>r</i> between OB and the normal.\n" +
        "10. Repeat for angle <i>i</i> = 45° and 60°. Record the readings in the table.\n\n" +
        "<size=36><b><color=#FFFFFF>Precautions</color></b></size>\n" +
        "•  The pins should be fixed upright and at least 5 cm apart.\n" +
        "•  The mirror should be upright, with its silvered surface exactly on MM′.\n" +
        "•  Look at the feet (bottoms) of the pins and their images to avoid parallax error.\n" +
        "•  Use a sharp pencil and draw thin lines.\n" +
        "•  The lines should be longer than the radius of the protractor.";

    [Header("Viva questions")]
    public QuizQuestion[] viva =
    {
        new QuizQuestion { question = "What is the angle of incidence?",
            options = new[] { "The angle between the incident ray and the mirror", "The angle between the incident ray and the normal", "The angle between the incident and reflected rays" },
            correctIndex = 1, explanation = "Both angle <i>i</i> and angle <i>r</i> are always measured from the normal, not from the mirror." },
        new QuizQuestion { question = "If the angle of incidence is 0°, what is the angle of reflection?",
            options = new[] { "90°", "0°, the ray goes back along the normal", "180°" },
            correctIndex = 1, explanation = "A ray falling along the normal is reflected straight back along itself." },
        new QuizQuestion { question = "Why should the pins be at least 5 cm apart?",
            options = new[] { "So they don't touch the mirror", "So the direction of the ray can be drawn more accurately", "To make the image bigger" },
            correctIndex = 1, explanation = "A small sideways error in a pin changes the angle much less when the pins are far apart." },
        new QuizQuestion { question = "Why do we look at the feet of the pins and their images?",
            options = new[] { "To avoid parallax error", "Because the heads are coloured", "To see the image more clearly" },
            correctIndex = 0, explanation = "Pins may not be perfectly upright; lining up their bottoms avoids parallax error." },
        new QuizQuestion { question = "In a glass mirror, where does reflection actually take place?",
            options = new[] { "At the front glass surface", "At the silvered back surface", "Inside the glass" },
            correctIndex = 1, explanation = "That's why the silvered surface, not the front glass, is placed on MM′." },
        new QuizQuestion { question = "The image formed by a plane mirror is:",
            options = new[] { "Real and inverted", "Virtual, upright and laterally inverted", "Real and magnified" },
            correctIndex = 1, explanation = "A plane mirror forms a virtual, upright, same-size, laterally inverted image." },
    };

    // ============================================================ state

    enum Stage
    {
        None, Identify, FixPaper, DrawMirrorLine, MarkO, MarkNormal, DrawNormal, MarkIncident, DrawIncident,
        MirrorChoice, PlaceMirror, PlaceP1P2, PlaceP3P4, EyeView, RemoveItems, DrawReflected, MeasureR, Busy, Done
    }

    class Reading
    {
        public int i;
        public int rEntered;
        public float rTrue;
        public float p12, p34;
        public bool frontGlass;
        public float pointShift;
    }

    PracticalUI _ui;
    PaperDrawing _draw;
    Camera _cam;
    Stage _stage;
    bool _started;
    static bool s_skipIntro;

    // apparatus parts
    Transform _paper, _board, _mirror, _protractor, _ruler, _pencil, _pinTray;
    Transform[] _pins = new Transform[4];
    readonly List<Transform> _drawingPins = new List<Transform>();
    readonly Dictionary<Transform, (Vector3 pos, Quaternion rot)> _home = new Dictionary<Transform, (Vector3, Quaternion)>();
    Transform _introPoint, _approachPoint, _practicalPoint;

    // paper space
    Vector3 _c, _u, _w;            // centre (on the paper surface), right, back
    float _zM;                     // mirror line (paper space z)
    Vector2 _O;                    // point of incidence (paper space)
    float _protR;

    // reading data
    readonly List<Reading> _readings = new List<Reading>();
    int _readingIndex;
    Reading _cur;
    Vector2 _incDir;               // unit direction of the incident ray from O (paper space)
    float[] _pinS = new float[2];  // positions of P1/P2 along the incident ray
    bool[] _pinPlaced = new bool[4];
    Vector2 _P3, _P4, _Oprime;
    readonly List<GameObject> _readingLabels = new List<GameObject>();
    readonly List<GameObject> _images = new List<GameObject>();
    readonly List<PaperDrawing.Line> _imageLines = new List<PaperDrawing.Line>();
    PaperDrawing.Line _normalLine, _incLine;
    readonly List<Renderer> _hiddenMirrorParts = new List<Renderer>();
    Color _glassColor;
    int _wrongTries;
    bool _realMirror;
    Vector2 _img1, _img2, _eye2;
    int _eyeChecks;
    GameObject _imageMarker;
    bool _mirrorRemoved;
    bool[] _pinRemoved = new bool[4];

    // identification
    readonly Dictionary<string, Transform> _idItems = new Dictionary<string, Transform>();
    readonly Dictionary<string, (string title, string info)> _idText = new Dictionary<string, (string, string)>();
    readonly HashSet<string> _found = new HashSet<string>();
    string _selected;
    readonly Dictionary<string, InteractableHighlight> _hl = new Dictionary<string, InteractableHighlight>();
    int _pinsPressed;
    readonly HashSet<Transform> _pressed = new HashSet<Transform>();

    // dragging
    enum DragKind { None, Pin, Mirror, Line }
    DragKind _drag;
    int _dragPin;
    PaperDrawing.Line _dragLine;
    Vector2 _lineStart, _lineDir;
    float _lineTarget;
    Action<float> _lineDone;
    GameObject _startMarker;

    int _quizIndex, _score;

    // zoom / pan
    float _baseFov;
    float _zoom = 1f;
    bool _camMoving;
    bool _multiTouch;           // two fingers down: zooming, so ignore one-finger actions
    float _lastPinch;
    Vector2 _lastMid;
    Vector2 _lastRight;

    const int TotalSteps = 10;
    float Px => 70f * Screen.height / 1080f;   // tap tolerance in pixels

    // ============================================================ setup

    void Awake()
    {
        _ui = GetComponent<PracticalUI>();
        _ui.introLabel = "PHYSICS PRACTICAL";
        _ui.introTitle = "Verify the Laws of Reflection Using a Plane Mirror";
        _ui.introText = "Use pins, a plane mirror and a protractor to show that the angle of incidence equals the angle of reflection.";
        _ui.practicalTitle = "Laws of Reflection  ·  Plane Mirror";
    }

    void Start()
    {
        _cam = Camera.main ? Camera.main : FindFirstObjectByType<Camera>();
        if (_cam) { _cam.nearClipPlane = 0.01f; _baseFov = _cam.fieldOfView; }     // we look at pins from very close

        if (!apparatus) { var a = GameObject.Find("ReflectionApparatus"); if (a) apparatus = a.transform; }
        if (!apparatus) { Debug.LogError("[Reflection] Drag ReflectionApparatus into the Apparatus slot."); return; }
        FindParts();
        SetupPaperSpace();
        SetupIdentification();
        CreateMirrorSurface();

        var dgo = new GameObject("PaperDrawing");
        _draw = dgo.AddComponent<PaperDrawing>();
        _draw.Init(_paper, _paper.GetComponent<Renderer>().sharedMaterial.shader);

        // Drawing pins start raised: the student presses them in
        foreach (var dp in _drawingPins) dp.position += Vector3.up * 0.015f;

        _ui.procedureText = procedureText;
        _ui.NextPressed += OnNext;
        _ui.BackPressed += () => { };
        _ui.RestartConfirmed += Restart;
        _ui.EndConfirmed += EndPractical;
        _ui.BeginPressed += OnBeginPressed;
        _ui.ThemeChanged += () => { RefreshGlow(); _ui.SetZoomButtons(true); };
        _ui.ZoomPressed += dir =>
        {
            if (dir == 0) ResetZoom();
            else ZoomAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.6f), dir > 0 ? 1.35f : 1f / 1.35f);
        };
        _ui.SetZoomButtons(true);

        _introPoint = Find("IntroCameraPoint");
        _approachPoint = Find("ApproachCameraPoint");
        _practicalPoint = Find("PracticalCameraPoint");

        if (s_skipIntro)
        {
            s_skipIntro = false;
            if (_cam)
            {
                if (autoTopView) { TopViewPose(out Vector3 tp, out Quaternion tr); _cam.transform.SetPositionAndRotation(tp, tr); }
                else if (_practicalPoint) _cam.transform.SetPositionAndRotation(_practicalPoint.position, _practicalPoint.rotation);
                _viewPos = _cam.transform.position; _viewRot = _cam.transform.rotation;
            }
            Begin();
        }
        else
        {
            if (_cam && _introPoint) _cam.transform.SetPositionAndRotation(_introPoint.position, _introPoint.rotation);
            _ui.ShowIntro();
        }
    }

    void OnDestroy() { Time.timeScale = 1f; }

    static Transform Find(string name) { var g = GameObject.Find(name); return g ? g.transform : null; }

    void FindParts()
    {
        _board = apparatus.Find("DrawingBoard");
        _paper = apparatus.Find("Paper");
        _mirror = apparatus.Find("Mirror");
        _protractor = apparatus.Find("Protractor");
        _ruler = apparatus.Find("Ruler");
        _pencil = apparatus.Find("Pencil");
        _pinTray = apparatus.Find("PinTray");
        for (int k = 0; k < 4; k++) _pins[k] = apparatus.Find("Pin_P" + (k + 1));
        var dps = apparatus.Find("DrawingPins");
        if (dps) foreach (Transform t in dps) _drawingPins.Add(t);

        foreach (var t in new[] { _mirror, _protractor, _ruler, _pencil, _pins[0], _pins[1], _pins[2], _pins[3] })
            if (t) _home[t] = (t.position, t.rotation);

        var pr = _protractor ? _protractor.GetComponent<BoxCollider>() : null;
        _protR = pr ? pr.size.x * 0.5f : 0.05f;
    }

    void SetupPaperSpace()
    {
        var r = _paper.GetComponent<Renderer>();
        _c = new Vector3(r.bounds.center.x, r.bounds.max.y, r.bounds.center.z);
        _u = _paper.right;
        _w = _paper.forward;
        _zM = mirrorLineOffset;
        _O = new Vector2(0, _zM);
    }

    /// <summary>Puts a real reflecting surface on the mirror's silvered side (needs Resources/PlanarMirror.shader).</summary>
    void CreateMirrorSurface()
    {
        if (!_mirror || !_cam) return;
        var shader = Shader.Find("Tajurbah/PlanarMirror");
        if (!shader)
        {
            Debug.LogWarning("[Reflection] PlanarMirror.shader not found. Put it in Assets/Resources. Using simple images instead.");
            return;
        }
        float w = 0.15f, h = 0.05f;
        var fg = _mirror.Find("FrontGlass");
        if (fg) { var mf = fg.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) { w = mf.sharedMesh.bounds.size.x; h = mf.sharedMesh.bounds.size.y; } }

        var go = new GameObject("MirrorSurface");
        go.transform.SetParent(_mirror, false);
        go.transform.localPosition = new Vector3(0, h * 0.5f, 0.0001f);
        go.transform.localRotation = Quaternion.Euler(0, 180, 0);       // the quad faces its local -Z, i.e. towards the student
        var mesh = new Mesh { name = "MirrorQuad" };
        float hw = w * 0.5f, hh = h * 0.5f;
        mesh.vertices = new[] { new Vector3(-hw, -hh, 0), new Vector3(-hw, hh, 0), new Vector3(hw, hh, 0), new Vector3(hw, -hh, 0) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(shader);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var refl = go.AddComponent<PlanarMirrorReflection>();
        refl.source = _cam;
        _realMirror = true;
    }

    Vector3 W(Vector2 p) => _c + _u * p.x + _w * p.y;                // paper space -> world
    Vector2 P(Vector3 w) { Vector3 d = w - _c; return new Vector2(Vector3.Dot(d, _u), Vector3.Dot(d, _w)); }
    Quaternion PaperRot(float yaw = 0f) => Quaternion.LookRotation(_w, Vector3.up) * Quaternion.Euler(0, yaw, 0);

    void SetupIdentification()
    {
        void Add(string key, Transform t, string title, string info)
        {
            if (!t) return;
            _idItems[key] = t;
            _idText[key] = (title, info);
            var h = t.GetComponent<InteractableHighlight>();
            if (!h) h = t.gameObject.AddComponent<InteractableHighlight>();
            _hl[key] = h;
        }
        Add("board", _board, "Drawing board", "A flat board. The paper is fixed on it with drawing pins so it doesn't move.");
        Add("paper", _paper, "White sheet of paper", "All the lines and pin positions are drawn on this sheet.");
        Add("mirror", _mirror, "Plane mirror strip", "A thin glass mirror on a stand. Reflection happens at its <b>silvered back</b> surface, not the front glass.");
        Add("pins", _pinTray, "Common pins", "They mark the path of light. <color=#E03C3C><b>Red</b></color>: P<sub>1</sub>, P<sub>2</sub> on the incident ray. <color=#3C64E0><b>Blue</b></color>: P<sub>3</sub>, P<sub>4</sub> on the reflected ray.");
        Add("protractor", _protractor, "Protractor", "Measures angles. The outer and inner scales both run from 0° to 180°, in opposite directions.");
        Add("ruler", _ruler, "Ruler", "Used to draw straight lines.");
        Add("pencil", _pencil, "Sharp pencil", "A sharp pencil draws thin lines, which gives more accurate angles.");
    }

    // ============================================================ start / camera

    void OnBeginPressed()
    {
        StartCoroutine(BeginRoutine());
    }

    IEnumerator BeginRoutine()
    {
        if (_cam)
        {
            if (_approachPoint) yield return MoveCamera(_approachPoint.position, _approachPoint.rotation, cameraMoveTime * 0.5f);
            yield return ToTopView();
        }
        Begin();
    }

    void Begin()
    {
        if (_started) return;
        _started = true;
        _ui.Show(true);
        GoIdentify();
    }

    IEnumerator MoveCamera(Vector3 pos, Quaternion rot, float time)
    {
        if (!_cam) yield break;
        _camMoving = true;
        Transform c = _cam.transform;
        Vector3 p0 = c.position; Quaternion r0 = c.rotation;
        float f0 = _cam.fieldOfView;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float e = Mathf.SmoothStep(0f, 1f, t / time);
            c.SetPositionAndRotation(Vector3.Lerp(p0, pos, e), Quaternion.Slerp(r0, rot, e));
            _cam.fieldOfView = Mathf.Lerp(f0, _baseFov, e);
            yield return null;
        }
        c.SetPositionAndRotation(pos, rot);
        _cam.fieldOfView = _baseFov;
        _zoom = 1f;
        _viewPos = pos; _viewRot = rot;
        _camMoving = false;
    }

    Vector3 _viewPos;
    Quaternion _viewRot;

    // ------------------------------------------------------------ zoom & pan

    bool CanPan => _stage != Stage.EyeView && _stage != Stage.MeasureR;   // moving the eye would change what lines up

    void ResetZoom()
    {
        if (_camMoving || !_cam) return;
        StartCoroutine(MoveCamera(_viewPos, _viewRot, 0.35f));
    }

    /// <summary>Zoom by 'factor' (>1 = closer), keeping the point under 'screen' in the same place where possible.</summary>
    void ZoomAt(Vector2 screen, float factor)
    {
        if (_camMoving || !_cam) return;
        float newZoom = Mathf.Clamp(_zoom * factor, 1f, maxZoom);
        if (Mathf.Approximately(newZoom, _zoom)) return;
        bool hadA = PlaneWorld(screen, out Vector3 a);
        _zoom = newZoom;
        float halfBase = Mathf.Tan(_baseFov * 0.5f * Mathf.Deg2Rad);
        _cam.fieldOfView = 2f * Mathf.Atan(halfBase / _zoom) * Mathf.Rad2Deg;
        if (hadA && CanPan && PlaneWorld(screen, out Vector3 b)) MoveCam(a - b);
        if (_zoom <= 1.001f && CanPan) _cam.transform.position = _viewPos;
    }

    void PanBy(Vector2 screenDelta)
    {
        if (_camMoving || !_cam || !CanPan || _zoom <= 1.001f) return;
        Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (PlaneWorld(centre, out Vector3 a) && PlaneWorld(centre - screenDelta, out Vector3 b)) MoveCam(b - a);
    }

    void MoveCam(Vector3 worldDelta)
    {
        worldDelta.y = 0f;
        Vector3 p = _cam.transform.position + worldDelta;
        // Don't let the view wander off the board
        Vector3 off = p - _viewPos;
        float limU = 0.2f, limW = 0.16f;
        float du = Mathf.Clamp(Vector3.Dot(off, _u), -limU, limU);
        float dw = Mathf.Clamp(Vector3.Dot(off, _w), -limW, limW);
        _cam.transform.position = _viewPos + _u * du + _w * dw;
    }

    bool PlaneWorld(Vector2 screen, out Vector3 world)
    {
        world = default;
        var ray = _cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, new Vector3(0, _draw ? _draw.SurfaceY : _c.y, 0));
        if (!plane.Raycast(ray, out float d)) return false;
        world = ray.GetPoint(d);
        return true;
    }

    /// <summary>Pinch / two-finger pan on phones, scroll wheel / right-drag in the editor. Returns true while two fingers are down.</summary>
    bool HandleZoomInput()
    {
        if (_camMoving || !_cam) return false;
        int touches = 0;
        Vector2 t0 = default, t1 = default;
        float scroll = 0f;
        bool rightHeld = false, rightDown = false;
        Vector2 mouse = default;

#if ENABLE_INPUT_SYSTEM
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null)
        {
            foreach (var t in ts.touches)
            {
                if (!t.press.isPressed) continue;
                if (touches == 0) t0 = t.position.ReadValue(); else if (touches == 1) t1 = t.position.ReadValue();
                touches++;
            }
        }
        var ms = UnityEngine.InputSystem.Mouse.current;
        if (ms != null)
        {
            scroll = ms.scroll.ReadValue().y;
            rightHeld = ms.rightButton.isPressed;
            rightDown = ms.rightButton.wasPressedThisFrame;
            mouse = ms.position.ReadValue();
            if (Mathf.Abs(scroll) > 10f) scroll /= 120f;      // some platforms report 120 per notch
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        touches = Input.touchCount;
        if (touches >= 2) { t0 = Input.GetTouch(0).position; t1 = Input.GetTouch(1).position; }
        scroll = Input.mouseScrollDelta.y;
        rightHeld = Input.GetMouseButton(1);
        rightDown = Input.GetMouseButtonDown(1);
        mouse = Input.mousePosition;
#endif

        // Two fingers: pinch to zoom, move to pan
        if (touches >= 2)
        {
            float pinch = Vector2.Distance(t0, t1);
            Vector2 mid = (t0 + t1) * 0.5f;
            if (!_multiTouch)
            {
                _multiTouch = true;
                CancelDrag();
            }
            else
            {
                if (_lastPinch > 1f) ZoomAt(mid, pinch / _lastPinch);
                PanBy(mid - _lastMid);
            }
            _lastPinch = pinch;
            _lastMid = mid;
            return true;
        }
        if (_multiTouch)
        {
            if (touches == 0) _multiTouch = false;     // wait until every finger is lifted
            return true;
        }

        // Mouse: scroll to zoom towards the cursor, right-drag to pan
        if (Mathf.Abs(scroll) > 0.01f && !IsOverUI(mouse)) ZoomAt(mouse, Mathf.Pow(1.15f, scroll));
        if (rightDown) _lastRight = mouse;
        if (rightHeld) { PanBy(mouse - _lastRight); _lastRight = mouse; }
        return false;
    }

    void CancelDrag()
    {
        if (_drag == DragKind.Line && _dragLine != null) { _draw.Remove(_dragLine.go); _dragLine = null; }
        _drag = DragKind.None;
    }

    IEnumerator ToTopView()
    {
        if (autoTopView && _cam) { TopViewPose(out Vector3 p, out Quaternion r); yield return MoveCamera(p, r, cameraMoveTime * 0.6f); }
        else if (_practicalPoint) yield return MoveCamera(_practicalPoint.position, _practicalPoint.rotation, cameraMoveTime * 0.6f);
    }

    /// <summary>A view from almost straight above that fits the whole board into the part of the screen the UI doesn't cover.</summary>
    void TopViewPose(out Vector3 pos, out Quaternion rot)
    {
        var br = _board ? _board.GetComponent<Renderer>() : _paper.GetComponent<Renderer>();
        Bounds b = br.bounds;
        Vector3 centre = new Vector3(b.center.x, b.max.y, b.center.z);
        float halfW = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(_u.x), Mathf.Abs(_u.y), Mathf.Abs(_u.z))));
        float halfD = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(_w.x), Mathf.Abs(_w.y), Mathf.Abs(_w.z))));

        float vHalf = _cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * _cam.aspect);
        // Usable screen: below the top bar (~9%) and above the tip strip + instruction card (~33%)
        const float top = 0.09f, bottom = 0.33f;
        float usable = 1f - top - bottom;
        float usableCentre = bottom + usable * 0.5f;              // as a fraction of screen height from the bottom

        float dW = halfW * topViewPadding / Mathf.Tan(hHalf);
        float dD = halfD * topViewPadding / (Mathf.Tan(vHalf) * usable);
        float dist = Mathf.Max(dW, dD);

        // Aim below the board centre so the board appears in the usable area, not the screen centre
        float visibleH = 2f * dist * Mathf.Tan(vHalf);
        Vector3 target = centre - _w * ((usableCentre - 0.5f) * visibleH);

        float tilt = topViewTilt * Mathf.Deg2Rad;
        Vector3 dir = Vector3.up * Mathf.Sin(tilt) - _w * Mathf.Cos(tilt);
        pos = target + dir * dist;
        rot = Quaternion.LookRotation(target - pos, _w);
    }

    IEnumerator MoveObj(Transform t, Vector3 pos, Quaternion rot, float time)
    {
        Vector3 p0 = t.position; Quaternion r0 = t.rotation;
        float k = 0f;
        while (k < time)
        {
            k += Time.deltaTime;
            float e = Mathf.SmoothStep(0f, 1f, k / time);
            t.SetPositionAndRotation(Vector3.Lerp(p0, pos, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.03f, Quaternion.Slerp(r0, rot, e));
            yield return null;
        }
        t.SetPositionAndRotation(pos, rot);
    }

    IEnumerator Home(Transform t, float time = 0.6f)
    {
        if (t == _protractor) FlipProtractorNumbers(false);
        if (t && _home.TryGetValue(t, out var h)) yield return MoveObj(t, h.pos, h.rot, time);
    }

    // ============================================================ UI helpers

    string ReadingTag => _cur != null ? $"  ·  Reading {_readingIndex + 1} of {anglesOfIncidence.Length} (angle <i>i</i> = {_cur.i}°)" : "";

    void Set(int step, string title, string body, PracticalUI.TipKind kind, string tip)
    {
        _ui.SetStep(step, TotalSteps, title, body);
        _ui.SetTip(kind, tip);
    }

    void Hint(string msg) => _ui.SetTip(PracticalUI.TipKind.Hint, msg);

    void GlowOnly(params string[] keys)
    {
        var set = new HashSet<string>(keys);
        foreach (var kv in _hl)
            kv.Value.SetHighlighted(set.Contains(kv.Key), _ui.highlightColor, false);
    }

    void RefreshGlow()
    {
        if (_stage == Stage.Identify) IdentifyGlow();
    }

    // ============================================================ main loop

    void Update()
    {
        if (!_started || _ui.IsBlocking) return;
        if (HandleZoomInput()) return;          // two fingers down: zooming only
        ReadPointer(out bool down, out bool held, out bool up, out Vector2 pos);

        if (down && !IsOverUI(pos)) OnPointerDown(pos);
        if (held && _drag != DragKind.None) OnDrag(pos);
        if (up && _drag != DragKind.None) OnPointerUp(pos);

        if (_startMarker)
            _startMarker.transform.localScale = new Vector3(1f, 0.02f, 1f) * (0.006f + Mathf.Sin(Time.time * 5f) * 0.0015f);
    }

    static void ReadPointer(out bool down, out bool held, out bool up, out Vector2 pos)
    {
        down = held = up = false; pos = default;
#if ENABLE_INPUT_SYSTEM
        var p = UnityEngine.InputSystem.Pointer.current;
        if (p != null)
        {
            pos = p.position.ReadValue();
            down = p.press.wasPressedThisFrame;
            held = p.press.isPressed;
            up = p.press.wasReleasedThisFrame;
            return;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pos = Input.mousePosition;
        down = Input.GetMouseButtonDown(0);
        held = Input.GetMouseButton(0);
        up = Input.GetMouseButtonUp(0);
#endif
    }

    static bool IsOverUI(Vector2 pos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = pos };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        return hits.Count > 0;
    }

    bool PaperHit(Vector2 screen, out Vector2 p)
    {
        p = default;
        if (!_cam) return false;
        var ray = _cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, new Vector3(0, _draw.SurfaceY, 0));
        if (!plane.Raycast(ray, out float d)) return false;
        p = P(ray.GetPoint(d));
        return true;
    }

    float ScreenDist(Vector3 world, Vector2 screen)
    {
        Vector3 s = _cam.WorldToScreenPoint(world);
        if (s.z < 0) return float.MaxValue;
        return Vector2.Distance(new Vector2(s.x, s.y), screen);
    }

    int PickPin(Vector2 screen, int from, int to)
    {
        int best = -1; float bestD = Px * 1.2f;
        for (int k = from; k <= to; k++)
        {
            if (!_pins[k]) continue;
            float d = Mathf.Min(ScreenDist(_pins[k].position + Vector3.up * 0.015f, screen), ScreenDist(_pins[k].position, screen));
            if (d < bestD) { bestD = d; best = k; }
        }
        return best;
    }

    // ============================================================ pointer handling

    void OnPointerDown(Vector2 screen)
    {
        switch (_stage)
        {
            case Stage.Identify: TapIdentify(screen); break;
            case Stage.FixPaper: TapDrawingPin(screen); break;
            case Stage.MarkO: TapMarkO(screen); break;
            case Stage.MarkNormal: TapMarkNormal(screen); break;
            case Stage.MarkIncident: TapMarkIncident(screen); break;
            case Stage.DrawMirrorLine:
            case Stage.DrawNormal:
            case Stage.DrawIncident:
            case Stage.DrawReflected:
                if (ScreenDist(W(_lineStart), screen) < Px * 1.3f) { _drag = DragKind.Line; OnDrag(screen); }
                else Hint("Start dragging from the glowing starting point.");
                break;
            case Stage.PlaceMirror:
                if (ScreenDist(_mirror.position + Vector3.up * 0.02f, screen) < Px * 2f) _drag = DragKind.Mirror;
                break;
            case Stage.PlaceP1P2:
                { int k = PickPin(screen, 0, 1); if (k >= 0) { _drag = DragKind.Pin; _dragPin = k; } }
                break;
            case Stage.PlaceP3P4:
            case Stage.EyeView:
                { int k = PickPin(screen, 2, 3); if (k >= 0) { _drag = DragKind.Pin; _dragPin = k; } }
                break;
            case Stage.RemoveItems: TapRemove(screen); break;
        }
    }

    void OnDrag(Vector2 screen)
    {
        if (!PaperHit(screen, out Vector2 p)) return;
        switch (_drag)
        {
            case DragKind.Pin:
                if (_stage == Stage.EyeView)
                {
                    Vector2 c = P(_pins[_dragPin].position);
                    Vector2 look = (_img2 - _eye2).normalized;
                    Vector2 side = new Vector2(look.y, -look.x);
                    p = c + side * Vector2.Dot(p - c, side);
                }
                _pins[_dragPin].SetPositionAndRotation(W(p), PaperRot());
                break;
            case DragKind.Mirror:
                _mirror.SetPositionAndRotation(W(p), PaperRot(180f));
                break;
            case DragKind.Line:
                float len = Mathf.Clamp(Vector2.Dot(p - _lineStart, _lineDir), 0f, _lineTarget);
                Vector2 end = _lineStart + _lineDir * len;
                if (_dragLine == null) _dragLine = _draw.DrawLine(W(_lineStart), W(end));
                else _draw.SetLine(_dragLine, W(_lineStart), W(end));
                PencilAt(W(end));
                break;
        }
    }

    void OnPointerUp(Vector2 screen)
    {
        var kind = _drag;
        _drag = DragKind.None;
        PaperHit(screen, out Vector2 p);
        switch (kind)
        {
            case DragKind.Line:
                float len = _dragLine != null ? Vector2.Distance(P(_dragLine.a), P(_dragLine.b)) : 0f;
                if (len >= _lineTarget * 0.85f)
                {
                    _draw.SetLine(_dragLine, W(_lineStart), W(_lineStart + _lineDir * _lineTarget));
                    var cb = _lineDone; var line = _dragLine;
                    _dragLineLast = line;
                    _dragLine = null;
                    RemoveStartMarker();
                    StartCoroutine(Home(_pencil, 0.4f));
                    StartCoroutine(Home(_ruler, 0.5f));
                    cb?.Invoke(_lineTarget);
                }
                else
                {
                    if (_dragLine != null) { _draw.Remove(_dragLine.go); _dragLine = null; }
                    Hint("Keep dragging all the way along the ruler to draw the full line.");
                }
                break;
            case DragKind.Mirror: DropMirror(); break;
            case DragKind.Pin:
                if (_stage == Stage.PlaceP1P2) DropP1P2(_dragPin, p);
                else if (_stage == Stage.PlaceP3P4) DropP3P4Top(_dragPin, p);
                else DropP3P4(_dragPin, p);
                break;
        }
    }

    void PencilAt(Vector3 tip)
    {
        if (!_pencil) return;
        // Leans over to the right (like a right-handed student's pencil), so it doesn't hide the paper
        Vector3 tipDir = (Vector3.down * 0.55f - _u * 0.8f - _w * 0.15f).normalized;
        _pencil.rotation = Quaternion.FromToRotation(Vector3.right, tipDir);
        _pencil.position = tip - tipDir * 0.0925f;
    }

    void StartLine(Vector2 start, Vector2 dir, float length, Action<float> done)
    {
        _lineStart = start; _lineDir = dir.normalized; _lineTarget = length; _lineDone = done;
        _dragLine = null;
        // Ruler slides along the line
        Vector3 d3 = (W(start + _lineDir) - W(start)).normalized;
        Quaternion rr = Quaternion.LookRotation(Vector3.Cross(d3, Vector3.up), Vector3.up);
        StartCoroutine(MoveObj(_ruler, W(start) - d3 * 0.005f + Vector3.Cross(d3, Vector3.up) * 0.0015f + Vector3.up * 0.0002f, rr, 0.5f));
        ShowStartMarker(start);
    }

    void ShowStartMarker(Vector2 p)
    {
        RemoveStartMarker();
        _startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _startMarker.name = "StartMarker";
        Destroy(_startMarker.GetComponent<Collider>());
        _startMarker.transform.position = W(p) + Vector3.up * 0.0004f;
        var m = new Material(_paper.GetComponent<Renderer>().sharedMaterial.shader);
        Color c = _ui.highlightColor;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 0.6f);
        _startMarker.GetComponent<Renderer>().sharedMaterial = m;
    }

    void RemoveStartMarker() { if (_startMarker) Destroy(_startMarker); _startMarker = null; }

    // ============================================================ step 1: identify

    void GoIdentify()
    {
        _stage = Stage.Identify;
        IdentifyText();
        IdentifyGlow();
        _ui.SetNav(false, _found.Count >= _idItems.Count, "Next");
    }

    void IdentifyText()
    {
        string list = "";
        foreach (var kv in _idText)
        {
            bool f = _found.Contains(kv.Key);
            list += (f ? $"<color=#2EC4B6>●</color> <b>{kv.Value.title}</b>" : $"<alpha=#88>○ {kv.Value.title}<alpha=#FF>") + "    ";
        }
        string head = _found.Count >= _idItems.Count ? "All apparatus identified. Tap Next." : $"Tap each item to identify it ({_found.Count} of {_idItems.Count}):";
        Set(1, "Identify the apparatus", head + "\n<size=80%>" + list + "</size>", PracticalUI.TipKind.Tip,
            "Every item here is used in the real board practical. Tap each glowing item.");
    }

    void IdentifyGlow()
    {
        foreach (var kv in _hl)
        {
            if (kv.Key == _selected) kv.Value.SetHighlighted(true, _ui.foundColor, false);
            else if (!_found.Contains(kv.Key)) kv.Value.SetHighlighted(true, _ui.highlightColor, false);
            else kv.Value.SetHighlighted(false);
        }
    }

    void TapIdentify(Vector2 screen)
    {
        if (!Physics.Raycast(_cam.ScreenPointToRay(screen), out RaycastHit hit, 10f)) return;
        foreach (var kv in _idItems)
        {
            bool match = hit.transform == kv.Value || hit.transform.IsChildOf(kv.Value);
            if (kv.Key == "pins") foreach (var pin in _pins) if (pin && (hit.transform == pin || hit.transform.IsChildOf(pin))) match = true;
            if (!match) continue;
            _found.Add(kv.Key);
            _selected = kv.Key;
            _ui.ShowLabel(kv.Value, _idText[kv.Key].title, _idText[kv.Key].info, _ui.foundColor);
            IdentifyText();
            IdentifyGlow();
            if (_found.Count >= _idItems.Count) _ui.SetNav(false, true, "Next");
            return;
        }
    }

    // ============================================================ step 2: fix the paper

    void GoFixPaper()
    {
        _ui.HideLabel();
        _selected = null;
        GlowOnly();
        _stage = Stage.FixPaper;
        Set(2, "Fix the paper", "Tap each of the four drawing pins to press it in and fix the paper to the board.",
            PracticalUI.TipKind.Tip, "The paper must not move during the practical, or your angles will be wrong.");
        _ui.SetNav(false, false);
    }

    void TapDrawingPin(Vector2 screen)
    {
        Transform best = null; float bestD = Px * 1.4f;
        foreach (var dp in _drawingPins)
        {
            if (_pressed.Contains(dp)) continue;
            float d = ScreenDist(dp.position, screen);
            if (d < bestD) { bestD = d; best = dp; }
        }
        if (!best) return;
        _pressed.Add(best);
        StartCoroutine(PressPin(best));
    }

    IEnumerator PressPin(Transform dp)
    {
        Vector3 a = dp.position, b = a - Vector3.up * 0.015f;
        float t = 0f;
        while (t < 0.15f) { t += Time.deltaTime; dp.position = Vector3.Lerp(a, b, t / 0.15f); yield return null; }
        dp.position = b;
        if (_pressed.Count >= _drawingPins.Count) GoDrawMirrorLine();
        else _ui.SetTip(PracticalUI.TipKind.Tip, $"{_pressed.Count} of {_drawingPins.Count} pins pressed in.");
    }

    // ============================================================ step 3: MM' and O

    void GoDrawMirrorLine()
    {
        _stage = Stage.DrawMirrorLine;
        Set(3, "Draw the mirror line MM′", "Drag the pencil along the ruler from the glowing point to draw a straight line across the paper.",
            PracticalUI.TipKind.Tip, "The mirror will stand on this line. Draw it thin and straight.");
        _ui.SetNav(false, false);
        StartLine(new Vector2(-0.11f, _zM), Vector2.right, 0.22f, _ =>
        {
            _draw.Label("M", W(new Vector2(-0.118f, _zM + 0.006f)));
            _draw.Label("M′", W(new Vector2(0.118f, _zM + 0.006f)));
            GoMarkO();
        });
    }

    void GoMarkO()
    {
        _stage = Stage.MarkO;
        Set(3, "Mark the point O", "Tap a point near the <b>middle</b> of MM′ to mark it O. The light ray will hit the mirror here.",
            PracticalUI.TipKind.Tip, "Choose a point near the middle so all the rays fit on the paper.");
    }

    void TapMarkO(Vector2 screen)
    {
        if (!PaperHit(screen, out Vector2 p)) return;
        if (Mathf.Abs(p.y - _zM) > 0.012f) { Hint("Tap on the line MM′ itself."); return; }
        if (Mathf.Abs(p.x) > 0.03f) { Hint("Choose a point nearer the middle of MM′, or the rays won't fit on the paper."); return; }
        _O = new Vector2(Mathf.Round(p.x * 1000f) / 1000f, _zM);
        _draw.Dot(W(_O));
        _draw.Label("O", W(_O + new Vector2(0.006f, 0.006f)));
        GoMarkNormal();
    }

    // ============================================================ step 4: normal

    IEnumerator PlaceProtractorAt(Vector2 p)
    {
        // Baseline on MM', centre on the point, scale towards the student
        FlipProtractorNumbers(true);
        yield return MoveObj(_protractor, W(p) + Vector3.up * 0.0003f, PaperRot(180f), 0.6f);
    }

    bool _protFlipped;

    /// <summary>Turns each number round so it reads the right way up when the scale faces the student.</summary>
    void FlipProtractorNumbers(bool flipped)
    {
        if (_protFlipped == flipped || !_protractor) return;
        _protFlipped = flipped;
        var nums = _protractor.Find("Numbers");
        if (!nums) return;
        foreach (Transform t in nums) t.localRotation = t.localRotation * Quaternion.Euler(0, 0, 180);
    }

    /// <summary>Camera close-up from above, so the protractor's numbers can be read.</summary>
    IEnumerator ProtractorCloseUp(Vector2 centre)
    {
        Vector3 target = W(centre + new Vector2(0, -_protR * 0.5f)) - _w * 0.025f;   // a little lower so the card doesn't cover it
        Vector3 pos = target + Vector3.up * 0.15f - _w * 0.03f;
        yield return MoveCamera(pos, Quaternion.LookRotation(target - pos, _w), cameraMoveTime * 0.6f);
    }

    void GoMarkNormal()
    {
        _stage = Stage.Busy;
        Set(4, "Draw the normal ON", "The protractor is placed with its centre on O and its baseline on MM′. Tap the <b>90°</b> mark to mark the normal.",
            PracticalUI.TipKind.Tip, "The normal is the line at 90° to the mirror. All angles are measured from it.");
        StartCoroutine(Routine());
        IEnumerator Routine()
        {
            yield return PlaceProtractorAt(_O);
            yield return ProtractorCloseUp(_O);
            _stage = Stage.MarkNormal;
        }
    }

    float AngleFromNormal(Vector2 v) => Mathf.Atan2(Mathf.Abs(v.x), -v.y) * Mathf.Rad2Deg;

    void TapMarkNormal(Vector2 screen)
    {
        if (!PaperHit(screen, out Vector2 p)) return;
        Vector2 v = p - _O;
        if (v.y >= 0 || v.magnitude < _protR * 0.6f) { Hint("Tap on the protractor's scale, at the 90° mark."); return; }
        float a = AngleFromNormal(v);
        if (a > 2f) { Hint($"That mark is {Mathf.RoundToInt(90 - a)}° on the scale. Tap exactly at 90°."); return; }
        Vector2 mark = _O + new Vector2(0, -(_protR + 0.004f));
        var dot = _draw.Dot(W(mark));
        _stage = Stage.Busy;
        StartCoroutine(Routine());
        IEnumerator Routine()
        {
            yield return Home(_protractor);
            yield return ToTopView();
            _stage = Stage.DrawNormal;
            Set(4, "Draw the normal ON", "Now join O to your mark: drag from O along the ruler past the mark.",
                PracticalUI.TipKind.Tip, "Make the line longer than the protractor's radius (5 cm).");
            StartLine(_O, Vector2.down, 0.12f, _ =>
            {
                _draw.Remove(dot);
                _normalLine = _dragLineLast;
                _draw.Label("N", W(_O + new Vector2(0.006f, -0.122f)));
                StartReading(0);
            });
        }
    }

    PaperDrawing.Line _dragLineLast;

    // ============================================================ readings

    void StartReading(int index)
    {
        _readingIndex = index;
        _cur = new Reading { i = anglesOfIncidence[index] };
        _readings.Add(_cur);
        _pinPlaced = new bool[4];
        foreach (var l in _readingLabels) if (l) _draw.Remove(l);
        _readingLabels.Clear();
        GoMarkIncident();
    }

    // ============================================================ step 5: incident ray

    void GoMarkIncident()
    {
        _stage = Stage.Busy;
        _wrongTries = 0;
        Set(5, "Draw the incident ray AO" + ReadingTag,
            $"Mark a point at <b>{_cur.i}° from the normal</b> on the <b>left</b> side of the normal by tapping the protractor's scale.",
            PracticalUI.TipKind.Tip, "Careful: angle <i>i</i> is measured from the NORMAL, not from the mirror line.");
        _ui.SetNav(false, false);
        StartCoroutine(Routine());
        IEnumerator Routine()
        {
            yield return ToTopView();
            yield return PlaceProtractorAt(_O);
            yield return ProtractorCloseUp(_O);
            _stage = Stage.MarkIncident;
        }
    }

    void TapMarkIncident(Vector2 screen)
    {
        if (!PaperHit(screen, out Vector2 p)) return;
        Vector2 v = p - _O;
        if (v.y >= 0 || v.magnitude < _protR * 0.6f) { Hint("Tap on the protractor's scale, near its edge."); return; }
        if (v.x > 0) { Hint("Draw the incident ray on the LEFT of the normal. The reflected ray will come out on the right."); return; }
        float a = AngleFromNormal(v);
        int i = _cur.i;
        if (Mathf.Abs(a - i) <= 1.5f)
        {
            float rad = i * Mathf.Deg2Rad;
            _incDir = new Vector2(-Mathf.Sin(rad), -Mathf.Cos(rad));
            Vector2 mark = _O + _incDir * (_protR + 0.004f);
            var dot = _draw.Dot(W(mark));
            _stage = Stage.Busy;
            StartCoroutine(Routine());
            IEnumerator Routine()
            {
                yield return Home(_protractor);
                yield return ToTopView();
                _stage = Stage.DrawIncident;
                Set(5, "Draw the incident ray AO" + ReadingTag, "Join O to your mark: drag from O along the ruler, past the mark.",
                    PracticalUI.TipKind.Tip, "This line shows the path of the light coming towards the mirror.");
                StartLine(_O, _incDir, rayLength, _ =>
                {
                    _draw.Remove(dot);
                    _incLine = _dragLineLast;
                    _readingLabels.Add(_draw.Label("A", W(_O + _incDir * (rayLength + 0.006f))));
                    GoMirrorChoice();
                });
            }
            return;
        }
        if (Mathf.Abs(a - (90 - i)) <= 1.5f)
        {
            Hint($"That's {i}° from the MIRROR LINE, a very common mistake! angle <i>i</i> is measured from the normal. On the scale, mark at {90 - i}° (90 − {i}).");
            return;
        }
        _wrongTries++;
        string help = _wrongTries >= 2 ? $" On this protractor that's the <b>{90 - i}</b> on the black outer scale." : "";
        Hint($"That mark is {Mathf.RoundToInt(a)}° from the normal. You need exactly {i}°.{help}");
    }

    // ============================================================ step 6: mirror

    void GoMirrorChoice()
    {
        _stage = Stage.MirrorChoice;
        Set(6, "Place the mirror" + ReadingTag, "Answer the question to place the mirror on MM′.", PracticalUI.TipKind.Tip,
            "The mirror must stand upright, exactly on the line.");
        _ui.ShowChoice("PLACE THE MIRROR", "Which surface of the mirror should lie exactly on the line MM′?",
            new[] { "The front glass surface", "The silvered back surface" }, chosen =>
            {
                _cur.frontGlass = chosen == 0;
                bool ok = chosen == 1;
                _ui.ShowQuizFeedback(ok, 1, chosen,
                    ok ? "Reflection happens at the silvered surface, so that's the surface that goes on the line."
                       : "Reflection happens at the silvered BACK surface. We'll place it your way so you can see what this does to your result.",
                    () => { _ui.HideOverlays(); GoPlaceMirror(); });
            });
    }

    void GoPlaceMirror()
    {
        _stage = Stage.PlaceMirror;
        Set(6, "Place the mirror" + ReadingTag, "Drag the mirror onto the line MM′ at O.",
            PracticalUI.TipKind.Tip, _cur.frontGlass ? "You chose the front glass surface." : "Silvered surface on the line, and keep the mirror upright.");
        GlowOnly("mirror");
    }

    void DropMirror()
    {
        Vector2 p = P(_mirror.position);
        if (Vector2.Distance(p, _O) > 0.04f)
        {
            Hint("Drop the mirror onto the line MM′, near O.");
            StartCoroutine(Home(_mirror, 0.4f));
            return;
        }
        float t = 0.003f;
        var fg = _mirror.Find("FrontGlass");
        if (fg) { var mf = fg.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) t = mf.sharedMesh.bounds.size.z; }
        float zS = _zM + (_cur.frontGlass ? t : 0f);
        _mirror.SetPositionAndRotation(W(new Vector2(_O.x, zS)), PaperRot(180f));
        GlowOnly();
        GoPlaceP1P2();
    }

    float SilveredZ => P(_mirror.position).y;

    // ============================================================ step 7: P1 and P2

    void GoPlaceP1P2()
    {
        _stage = Stage.PlaceP1P2;
        Set(7, "Fix pins P<sub>1</sub> and P<sub>2</sub>" + ReadingTag, "Drag the two <color=#E03C3C><b>red</b></color> pins from the pin cushion onto the <b>glowing line AO</b> (the incident ray). Keep them at least 5 cm apart.",
            PracticalUI.TipKind.Tip, "The incident ray is the line you drew from A to O. Pins far apart give a more accurate direction.");
        _draw.Highlight(_incLine, _ui.highlightColor);
        GlowOnly("pins");
        _ui.SetNav(false, false);
    }

    void DropP1P2(int k, Vector2 p)
    {
        Vector2 v = p - _O;
        float s = Vector2.Dot(v, _incDir);
        float perp = Mathf.Abs(v.x * _incDir.y - v.y * _incDir.x);
        if (perp > 0.015f || s < 0.015f || s > rayLength + 0.01f)
        {
            Hint("Put the pin exactly on the incident ray AO.");
            _pinPlaced[k] = false;
            StartCoroutine(Home(_pins[k], 0.4f));
            return;
        }
        if (s < 0.02f) Hint("That pin is very close to O. Move it further along AO.");
        s = Mathf.Min(s, rayLength);
        _pins[k].SetPositionAndRotation(W(_O + _incDir * s), PaperRot());
        _pinS[k] = s;
        _pinPlaced[k] = true;

        if (_pinPlaced[0] && _pinPlaced[1])
        {
            float gap = Mathf.Abs(_pinS[0] - _pinS[1]);
            _cur.p12 = gap;
            if (gap < 0.05f)
                _ui.SetTip(PracticalUI.TipKind.Hint, $"Your pins are only {gap * 100f:0.0} cm apart. In the lab they should be at least 5 cm apart. Move one, or tap Next to continue anyway.");
            else
                _ui.SetTip(PracticalUI.TipKind.Tip, $"Good: the pins are {gap * 100f:0.0} cm apart.");
            GlowOnly();
            _ui.SetNav(false, true, "Next: fix P<sub>3</sub> and P<sub>4</sub>");
        }
    }

    // ============================================================ step 8a: put P3 and P4 on the paper

    void GoPlaceP3P4()
    {
        _draw.Highlight(_incLine, null);
        // Make sure P1 is the far pin and P2 the near one (swap the objects if needed)
        if (_pinS[0] < _pinS[1])
        {
            var tmp = _pins[0]; _pins[0] = _pins[1]; _pins[1] = tmp;
            float ts = _pinS[0]; _pinS[0] = _pinS[1]; _pinS[1] = ts;
        }
        _stage = Stage.PlaceP3P4;
        _pinPlaced[2] = _pinPlaced[3] = false;
        Set(8, "Fix pins P<sub>3</sub> and P<sub>4</sub>" + ReadingTag,
            "Drag the two <color=#3C64E0><b>blue</b></color> pins onto the paper on the <b>right</b> of the normal, in front of the mirror, roughly where the reflected ray will go. You'll line them up exactly in the next step.",
            PracticalUI.TipKind.Tip, "Put one pin nearer the mirror and one further away, at least 5 cm apart.");
        _ui.SetNav(false, false);
        GlowOnly("pins");
    }

    void DropP3P4Top(int k, Vector2 p)
    {
        float zS = SilveredZ;
        bool onPaper = Mathf.Abs(p.x) < 0.145f && p.y > -0.103f;
        if (!onPaper || p.y > zS - 0.015f || p.x < _O.x + 0.005f)
        {
            Hint("Put the blue pins on the paper, on the RIGHT of the normal and in front of the mirror.");
            _pinPlaced[k] = false;
            StartCoroutine(Home(_pins[k], 0.4f));
            return;
        }
        _pins[k].SetPositionAndRotation(W(p), PaperRot());
        _pinPlaced[k] = true;
        if (_pinPlaced[2] && _pinPlaced[3])
        {
            GlowOnly();
            _ui.SetTip(PracticalUI.TipKind.Tip, "Now look into the mirror from that side to line them up.");
            _ui.SetNav(false, true, "Look into the mirror");
        }
    }

    // ============================================================ step 8: eye view, P3 and P4

    void GoEyeView()
    {
        _stage = Stage.Busy;
        _ui.SetNav(false, false);
        StartCoroutine(Routine());
        IEnumerator Routine()
        {
            float zS = SilveredZ;
            Vector2 img1 = Reflect(_O + _incDir * _pinS[0], zS), img2 = Reflect(_O + _incDir * _pinS[1], zS);
            _img1 = img1; _img2 = img2;
            _eyeChecks = 0;
            Vector2 sight = (img2 - img1).normalized;                 // from deeper image towards the eye
            float tCross = (zS - img2.y) / sight.y;
            Vector2 cross = img2 + sight * tCross;                    // where the line of sight meets the mirror
            Vector2 eye2 = cross + sight * eyeDistance;
            _eye2 = eye2;

            Vector3 eye = W(eye2) + Vector3.up * eyeHeight;
            Vector3 look = W(cross + (-sight) * 0.05f) + Vector3.up * 0.012f;
            Quaternion rot = Quaternion.LookRotation(look - eye, Vector3.up);

            yield return MoveCamera(eye, rot, cameraMoveTime * 0.8f);
            if (!_realMirror) ShowMirrorImages(zS);

            // Zoom in on the mirror so the images are big enough to line up with
            _zoom = eyeViewZoom;
            _cam.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(_baseFov * 0.5f * Mathf.Deg2Rad) / _zoom) * Mathf.Rad2Deg;

            // Point out where the images are (they're inside the mirror, behind the glass)
            if (_imageMarker) Destroy(_imageMarker);
            _imageMarker = new GameObject("ImageMarker");
            _imageMarker.transform.position = W(img2) + Vector3.up * 0.03f;
            _ui.ShowLabel(_imageMarker.transform, "Images of P<sub>1</sub> and P<sub>2</sub>",
                "These are IN the mirror. Line the blue pins up with these, not with the real red pins outside the mirror.", _ui.foundColor);
            _stage = Stage.EyeView;
            Set(8, "Fix pins P<sub>3</sub> and P<sub>4</sub>" + ReadingTag,
                "You're looking into the mirror. Find the <b>images</b> of the red pins in it. Drag the <color=#3C64E0><b>blue</b></color> pins sideways until P<sub>3</sub>, P<sub>4</sub> and both images look like one straight line.",
                PracticalUI.TipKind.Tip, "Look INSIDE the mirror. Line up the FEET (bottoms) of the pins and images to avoid parallax error. Zoom in to check.");
            _ui.SetNav(false, true, "They're in line");
        }
    }

    static float PerpDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 d = (b - a).normalized;
        Vector2 v = p - a;
        return Mathf.Abs(v.x * d.y - v.y * d.x);
    }

    static Vector2 Reflect(Vector2 p, float zS) => new Vector2(p.x, 2f * zS - p.y);

    void ShowMirrorImages(float zS)
    {
        ClearMirrorImages();
        // The mirror becomes a window showing the reflected scene behind it
        _hiddenMirrorParts.Clear();
        foreach (var name in new[] { "SilveredSurface", "BackPaint", "Stand", "StandLip" })
        {
            var t = _mirror.Find(name);
            if (t) { var r = t.GetComponent<Renderer>(); if (r && r.enabled) { r.enabled = false; _hiddenMirrorParts.Add(r); } }
        }
        var fg = _mirror.Find("FrontGlass");
        if (fg)
        {
            var m = fg.GetComponent<Renderer>().material;
            _glassColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.82f, 0.86f, 0.9f, 0.28f));
        }
        // Images of P1, P2 and of the lines in front of the mirror
        for (int k = 0; k < 2; k++)
        {
            var img = Instantiate(_pins[k].gameObject);
            img.name = "Image_P" + (k + 1);
            foreach (var c in img.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var h in img.GetComponentsInChildren<InteractableHighlight>()) Destroy(h);
            img.transform.SetPositionAndRotation(W(Reflect(P(_pins[k].position), zS)), PaperRot());
            _images.Add(img);
        }
        if (_incLine != null) _imageLines.Add(_draw.DrawLine(W(Reflect(P(_incLine.a), zS)), W(Reflect(P(_incLine.b), zS))));
        if (_normalLine != null) _imageLines.Add(_draw.DrawLine(W(Reflect(P(_normalLine.a), zS)), W(Reflect(P(_normalLine.b), zS))));
    }

    void ClearMirrorImages()
    {
        foreach (var g in _images) if (g) Destroy(g);
        _images.Clear();
        foreach (var l in _imageLines) if (l != null) _draw.Remove(l.go);
        _imageLines.Clear();
        foreach (var r in _hiddenMirrorParts) if (r) r.enabled = true;
        _hiddenMirrorParts.Clear();
        var fg = _mirror ? _mirror.Find("FrontGlass") : null;
        if (fg && _glassColor.a > 0f)
        {
            var m = fg.GetComponent<Renderer>().material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", _glassColor);
        }
    }

    void DropP3P4(int k, Vector2 p)
    {
        float zS = SilveredZ;
        bool onPaper = Mathf.Abs(p.x) < 0.145f && p.y > -0.103f;
        if (!onPaper || p.y > zS - 0.015f)
        {
            Hint("Keep the pin on the paper, in front of the mirror.");
            p.x = Mathf.Clamp(p.x, -0.14f, 0.14f);
            p.y = Mathf.Clamp(p.y, -0.10f, zS - 0.016f);
        }
        _pins[k].SetPositionAndRotation(W(p), PaperRot());
    }

    void FinishEyeView()
    {
        _P3 = P(_pins[2].position);
        _P4 = P(_pins[3].position);
        // P4 should be the one further from the mirror
        if (_P3.y < _P4.y) { var tmp = _pins[2]; _pins[2] = _pins[3]; _pins[3] = tmp; var tp = _P3; _P3 = _P4; _P4 = tp; }
        _cur.p34 = Vector2.Distance(_P3, _P4);
        if (_cur.p34 < 0.02f) { Hint("P<sub>3</sub> and P<sub>4</sub> are too close together to draw a line through them. Move them further apart."); return; }

        // How far are P3/P4 from the line of the images (correct) and from the line to the REAL pins (common mistake)?
        float toImages = Mathf.Max(PerpDist(_P3, _img1, _img2), PerpDist(_P4, _img1, _img2));
        Vector2 realMid = _O + _incDir * ((_pinS[0] + _pinS[1]) * 0.5f);
        float toReal = Mathf.Max(PerpDist(_P3, _eye2, realMid), PerpDist(_P4, _eye2, realMid));
        bool forceOn = _eyeChecks >= 2;
        if (toImages > 0.005f && !forceOn)
        {
            _eyeChecks++;
            if (toReal < 0.008f)
                Hint("You've lined the blue pins up with the REAL red pins outside the mirror. Look INSIDE the mirror for their images, and line up with those.");
            else
                Hint("Not quite in line with the images yet. Look into the mirror again: each pin should hide the one behind it.");
            if (_eyeChecks >= 2) _ui.SetNav(false, true, "Continue anyway");
            return;
        }
        _ui.HideLabel();
        if (_imageMarker) Destroy(_imageMarker);

        _stage = Stage.Busy;
        _ui.SetNav(false, false);
        StartCoroutine(Routine());
        IEnumerator Routine()
        {
            ClearMirrorImages();
            yield return ToTopView();
            GoRemoveItems();
        }
    }

    // ============================================================ step 9: reflected ray

    void GoRemoveItems()
    {
        _stage = Stage.RemoveItems;
        _mirrorRemoved = false;
        _pinRemoved = new bool[4];
        Set(9, "Remove the mirror and pins" + ReadingTag, "Tap the <b>mirror</b> and each of the <b>four pins</b> to remove them. Each pin leaves a small hole in the paper.",
            PracticalUI.TipKind.Tip, _cur.p34 < 0.05f ? $"P<sub>3</sub> and P<sub>4</sub> were only {_cur.p34 * 100f:0.0} cm apart, which may add error."
                                                      : "The pin holes mark where the light travelled.");
        _ui.SetNav(false, false);
        GlowOnly("mirror");
    }

    void TapRemove(Vector2 screen)
    {
        string[] names = { "P<sub>1</sub>", "P<sub>2</sub>", "P<sub>3</sub>", "P<sub>4</sub>" };
        int k = PickPin(screen, 0, 3);
        if (k >= 0 && !_pinRemoved[k])
        {
            _pinRemoved[k] = true;
            Vector2 hp = P(_pins[k].position);
            _draw.Dot(W(hp), true);
            Vector2 away = (hp - _O).sqrMagnitude > 1e-6f ? (hp - _O).normalized : Vector2.down;
            Vector2 side2 = new Vector2(-away.y, away.x) * (k % 2 == 0 ? 1f : -1f);
            _readingLabels.Add(_draw.Label(names[k], W(hp + away * 0.006f + side2 * 0.009f), 0.06f));
            StartCoroutine(Home(_pins[k], 0.5f));
        }
        else if (!_mirrorRemoved && ScreenDist(_mirror.position + Vector3.up * 0.02f, screen) < Px * 2f)
        {
            _mirrorRemoved = true;
            GlowOnly();
            StartCoroutine(Home(_mirror, 0.6f));
        }
        else return;

        int left = (_mirrorRemoved ? 0 : 1);
        foreach (bool r in _pinRemoved) if (!r) left++;
        if (left > 0) { _ui.SetTip(PracticalUI.TipKind.Tip, $"{left} more to remove."); return; }

        _stage = Stage.Busy;
        StartCoroutine(Routine());
        IEnumerator Routine()
        {
            yield return new WaitForSeconds(0.6f);
            Vector2 d = (_P3 - _P4).normalized;                  // from P4 towards the mirror
            float t = (_zM - _P4.y) / d.y;
            _Oprime = _P4 + d * t;
            _cur.pointShift = Vector2.Distance(_Oprime, _O);
            _stage = Stage.DrawReflected;
            Set(9, "Draw the reflected ray OB" + ReadingTag, "Join the pin holes P<sub>4</sub> and P<sub>3</sub>: drag from P<sub>4</sub> through P<sub>3</sub> until the line meets MM\u2032.",
                PracticalUI.TipKind.Tip, "This line shows the path of the light after reflection.");
            StartLine(_P4, d, t, _ =>
            {
                _readingLabels.Add(_draw.Label("B", W(_P4 - d * 0.008f)));
                GoMeasure();
            });
        }
    }

    // ============================================================ step 10: measure r

    void GoMeasure()
    {
        _stage = Stage.Busy;
        _wrongTries = 0;
        Vector2 v = _P4 - _Oprime;
        _cur.rTrue = AngleFromNormal(v);
        Set(10, "Measure angle <i>r</i>" + ReadingTag, "Read where the reflected ray OB crosses the protractor's scale, then work out its angle from the normal.",
            PracticalUI.TipKind.Tip, "The normal is at 90° on the protractor. angle <i>r</i> is how far OB is from it.");
        StartCoroutine(Routine());
        IEnumerator Routine()
        {
            yield return PlaceProtractorAt(_Oprime);
            // Close-up from above so the scale can be read
            yield return ProtractorCloseUp(_Oprime);
            _stage = Stage.MeasureR;
            _ui.SetNav(false, true, "Enter angle <i>r</i>");
        }
    }

    void AskAngle()
    {
        _ui.ShowAngleInput("Angle of reflection", "Enter angle <i>r</i>: the angle between the reflected ray OB and the normal ON.", 0, OnAngleEntered);
    }

    void OnAngleEntered(int e)
    {
        float r = _cur.rTrue;
        if (Mathf.Abs(e - r) <= 1.01f)
        {
            _cur.rEntered = e;
            StartCoroutine(Routine());
            return;
        }
        _wrongTries++;
        if (Mathf.Abs(e - (90 - r)) <= 1.01f)
            Hint("That's the angle from the MIRROR LINE. angle <i>r</i> is measured from the NORMAL. Try again.");
        else if (Mathf.Abs(e - (90 + r)) <= 1.01f)
            Hint("That's the scale reading itself. The normal is at 90°, so angle <i>r</i> = reading − 90. Try again.");
        else if (_wrongTries >= 3)
            Hint($"Look closely: OB crosses the outer scale at about {Mathf.RoundToInt(90 + r)}. So angle <i>r</i> = {Mathf.RoundToInt(90 + r)} − 90.");
        else
            Hint("Not quite. Read the scale again carefully where OB crosses it.");
        _ui.SetNav(false, true, "Enter angle <i>r</i>");

        IEnumerator Routine()
        {
            _stage = Stage.Busy;
            _ui.SetNav(false, false);
            yield return Home(_protractor);
            yield return ToTopView();
            _ui.ShowInfo("Observation table", TableText(false), _readingIndex + 1 < anglesOfIncidence.Length ? "Next reading" : "See results", () =>
            {
                if (_readingIndex + 1 < anglesOfIncidence.Length) StartReading(_readingIndex + 1);
                else GoResults();
            });
        }
    }

    // ============================================================ results

    string TableText(bool withNotes)
    {
        string t = "<b><pos=0%>No.<pos=14%>Angle <i>i</i><pos=38%>Angle <i>r</i><pos=62%>Difference (<i>i</i> − <i>r</i>)</b>\n";
        for (int k = 0; k < _readings.Count; k++)
        {
            var r = _readings[k];
            if (r.rEntered == 0 && r.rTrue == 0) continue;
            int diff = r.i - r.rEntered;
            t += $"<pos=0%>{k + 1}<pos=14%>{r.i}°<pos=38%>{r.rEntered}°<pos=62%>{diff}°\n";
        }
        if (!withNotes) return t;

        t += "\n<b>Why your readings may differ slightly:</b>\n";
        bool any = false;
        for (int k = 0; k < _readings.Count; k++)
        {
            var r = _readings[k];
            var notes = new List<string>();
            if (Mathf.Abs(r.rTrue - r.i) >= 0.75f) notes.Add("P<sub>3</sub> and P<sub>4</sub> were not exactly in line with the images");
            if (r.p12 < 0.05f) notes.Add($"P<sub>1</sub> and P<sub>2</sub> were only {r.p12 * 100f:0.0} cm apart");
            if (r.p34 < 0.05f) notes.Add($"P<sub>3</sub> and P<sub>4</sub> were only {r.p34 * 100f:0.0} cm apart");
            if (r.frontGlass) notes.Add($"the front glass was on MM′, so the ray met MM′ {r.pointShift * 1000f:0} mm away from O");
            if (notes.Count == 0) continue;
            any = true;
            t += $"•  Reading {k + 1}: " + string.Join("; ", notes) + ".\n";
        }
        if (!any) t += "•  Your readings were very accurate. Small differences of about 1° are normal, from the thickness of pencil lines and pins.\n";
        return t;
    }

    void GoResults()
    {
        _stage = Stage.Done;
        Set(10, "Result", "<b>Result:</b> In every reading angle <i>i</i> = angle <i>r</i> (within experimental error), and all the rays lie on the same sheet of paper. <b>Both laws of reflection are verified.</b>",
            PracticalUI.TipKind.Science, "Law 1: angle <i>i</i> = angle <i>r</i>.  Law 2: incident ray, reflected ray and normal lie in the same plane.");
        _ui.ShowInfo("Observations and result", TableText(true) + "\n<b>Result:</b> angle <i>i</i> = angle <i>r</i>, so the laws of reflection are verified.", "Viva questions", StartViva);
    }

    void StartViva()
    {
        _quizIndex = 0; _score = 0;
        if (viva == null || viva.Length == 0) { _ui.ShowResults(0, 0, Restart, EndPractical); return; }
        Ask();
        void Ask()
        {
            var q = viva[_quizIndex];
            _ui.ShowQuiz(_quizIndex, viva.Length, q.question, q.options, chosen =>
            {
                bool ok = chosen == q.correctIndex;
                if (ok) _score++;
                _ui.ShowQuizFeedback(ok, q.correctIndex, chosen, q.explanation, () =>
                {
                    _quizIndex++;
                    if (_quizIndex < viva.Length) Ask();
                    else _ui.ShowResults(_score, viva.Length, Restart, EndPractical);
                });
            });
        }
    }

    // ============================================================ Next button

    void OnNext()
    {
        switch (_stage)
        {
            case Stage.Identify: if (_found.Count >= _idItems.Count) GoFixPaper(); break;
            case Stage.PlaceP1P2: if (_pinPlaced[0] && _pinPlaced[1]) GoPlaceP3P4(); break;
            case Stage.PlaceP3P4: if (_pinPlaced[2] && _pinPlaced[3]) GoEyeView(); break;
            case Stage.EyeView: FinishEyeView(); break;
            case Stage.MeasureR: AskAngle(); break;
            case Stage.Done: GoResults(); break;
        }
    }

    // ============================================================ restart / end

    void Restart()
    {
        Time.timeScale = 1f;
        s_skipIntro = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void EndPractical()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(string.IsNullOrEmpty(exitSceneName) ? SceneManager.GetActiveScene().name : exitSceneName);
    }
}