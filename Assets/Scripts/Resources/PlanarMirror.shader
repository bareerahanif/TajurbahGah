// Shows the reflection texture rendered by PlanarMirrorReflection.cs on the mirror's surface.
// IMPORTANT: keep this file inside a folder called "Resources" (e.g. Assets/Resources/PlanarMirror.shader)
// so it is included in the Android build.
Shader "Tajurbah/PlanarMirror"
{
    Properties
    {
        _ReflectionTex ("Reflection", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.93, 0.95, 0.98, 1)
        _FlipY ("Flip Y", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Mirror"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ReflectionTex_ST;
                float4 _Tint;
                float _FlipY;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 screenPos : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
                half4 c = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv);
                return half4(c.rgb * _Tint.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
