using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A real plane-mirror reflection for URP. Renders the scene from the mirrored viewpoint into a texture,
/// which the "Tajurbah/PlanarMirror" shader shows on the mirror surface.
/// Added automatically by ReflectionPractical onto a quad on the mirror's silvered surface.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PlanarMirrorReflection : MonoBehaviour
{
    public Camera source;
    [Tooltip("Reflection resolution compared with the screen (0.5 = half). Lower is faster on phones.")]
    [Range(0.25f, 1f)] public float textureScale = 0.6f;
    [Tooltip("Tick if the reflection appears upside down on your device")]
    public bool flipY;
    public float clipPlaneOffset = 0.0005f;

    Camera _refl;
    RenderTexture _rt;
    Renderer _renderer;
    Material _mat;
    static bool s_rendering;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mat = _renderer.material;
    }

    void OnDestroy()
    {
        if (_rt) { _rt.Release(); Destroy(_rt); }
        if (_refl) Destroy(_refl.gameObject);
    }

    void LateUpdate()
    {
        if (s_rendering || !source || !_renderer.enabled || !gameObject.activeInHierarchy) return;
        RenderReflection();
    }

    void RenderReflection()
    {
        EnsureResources();

        Vector3 pos = transform.position;
        Vector3 normal = -transform.forward;          // the quad faces its local -Z

        // Don't render when the camera is behind the mirror
        if (Vector3.Dot(source.transform.position - pos, normal) <= 0f) return;

        float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
        var plane = new Vector4(normal.x, normal.y, normal.z, d);
        Matrix4x4 reflection = ReflectionMatrix(plane);

        _refl.CopyFrom(source);
        _refl.enabled = false;
        _refl.targetTexture = _rt;
        _refl.worldToCameraMatrix = source.worldToCameraMatrix * reflection;
        _refl.transform.position = reflection.MultiplyPoint(source.transform.position);
        Vector4 clip = CameraSpacePlane(_refl, pos, normal);
        _refl.projectionMatrix = source.CalculateObliqueMatrix(clip);

        var data = _refl.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.renderPostProcessing = false;
            data.renderShadows = true;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
        }

        s_rendering = true;
        _renderer.enabled = false;
        GL.invertCulling = true;
        try
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = _rt };
            if (RenderPipeline.SupportsRenderRequest(_refl, request))
                RenderPipeline.SubmitRenderRequest(_refl, request);
        }
        finally
        {
            GL.invertCulling = false;
            _renderer.enabled = true;
            s_rendering = false;
        }

        _mat.SetTexture("_ReflectionTex", _rt);
        _mat.SetFloat("_FlipY", flipY ? 1f : 0f);
    }

    void EnsureResources()
    {
        int w = Mathf.Max(64, Mathf.RoundToInt(Screen.width * textureScale));
        int h = Mathf.Max(64, Mathf.RoundToInt(Screen.height * textureScale));
        if (!_rt || _rt.width != w || _rt.height != h)
        {
            if (_rt) { _rt.Release(); Destroy(_rt); }
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.Default) { name = "MirrorReflection", antiAliasing = 2 };
            _rt.Create();
        }
        if (!_refl)
        {
            var go = new GameObject("MirrorReflectionCamera") { hideFlags = HideFlags.HideAndDontSave };
            _refl = go.AddComponent<Camera>();
            _refl.enabled = false;
            go.AddComponent<UniversalAdditionalCameraData>();
        }
    }

    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    static Matrix4x4 ReflectionMatrix(Vector4 p)
    {
        Matrix4x4 r = Matrix4x4.identity;
        r.m00 = 1f - 2f * p.x * p.x; r.m01 = -2f * p.x * p.y; r.m02 = -2f * p.x * p.z; r.m03 = -2f * p.w * p.x;
        r.m10 = -2f * p.y * p.x; r.m11 = 1f - 2f * p.y * p.y; r.m12 = -2f * p.y * p.z; r.m13 = -2f * p.w * p.y;
        r.m20 = -2f * p.z * p.x; r.m21 = -2f * p.z * p.y; r.m22 = 1f - 2f * p.z * p.z; r.m23 = -2f * p.w * p.z;
        r.m30 = 0f; r.m31 = 0f; r.m32 = 0f; r.m33 = 1f;
        return r;
    }
}
