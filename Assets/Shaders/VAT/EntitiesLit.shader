Shader "Hidden/VAT/EntitiesLit_Array"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _BaseMap("Base Map", 2D) = "white" {}
        _PosArr("PosArr", 2DArray) = "" {}
        _NrmArr("NrmArr", 2DArray) = "" {}
        _Frames("Frames", Float) = 1
        _Verts("Verts",  Float) = 1
        _VertsPerRow("Verts Per Row", Float) = 256
        _SliceHeight("Slice Height", Float) = 256
        _ManualTime("Time", Float) = 0
        _ManualFrame("Manual Frame", Float) = 0
        _FPS("FPS", Float) = 30
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags{ "LightMode"="UniversalForward" }
            Cull Back ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // DOTS 인스턴싱 헤더 (Entities Graphics가 자동 include 못하는 환경 대비)
            //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityDOTSInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_PosArr); SAMPLER(sampler_PosArr);
            TEXTURE2D_ARRAY(_NrmArr); SAMPLER(sampler_NrmArr);
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            // 머티리얼 상수(CBUFFER)에 "엔티티 타임"은 절대 넣지 말 것
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _Frames, _Verts, _VertsPerRow, _SliceHeight;
                float  _ManualTime, _ManualFrame, _FPS;
            CBUFFER_END
            
            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _EntitiesTime)
                    UNITY_DOTS_INSTANCED_PROP(float, _AnimOffset)
                    UNITY_DOTS_INSTANCED_PROP(float, _AnimStartTime)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define ACCESS_PROP(name) UNITY_ACCESS_DOTS_INSTANCED_PROP(float, name)
            #else
                UNITY_INSTANCING_BUFFER_START(PerInstance)
                    UNITY_DEFINE_INSTANCED_PROP(float, _EntitiesTime)                    
                    UNITY_DEFINE_INSTANCED_PROP(float, _AnimOffset)
                    UNITY_DEFINE_INSTANCED_PROP(float, _AnimStartTime)
                UNITY_INSTANCING_BUFFER_END(PerInstance)
                #define ACCESS_PROP(name) UNITY_ACCESS_INSTANCED_PROP(PerInstance, name)
            #endif

            struct Attributes { float3 positionOS:POSITION; float3 normalOS:NORMAL; float2 baseUV:TEXCOORD0; uint vid:SV_VertexID; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 baseUV:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT; UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                const float F = max(_Frames, 1.0);
                const float W = max(_VertsPerRow, 1.0);
                const float H = max(_SliceHeight, 1.0);

                float t0   = ACCESS_PROP(_AnimStartTime);
                float ofs  = ACCESS_PROP(_AnimOffset);
                float et   = ACCESS_PROP(_EntitiesTime);
                
                // useE==1 → 엔티티 시간, 0 → 수동 시간
                float baseTime = et;

                // 수동 프레임 강제 사용 옵션
                //bool  useManualFrame = false;
                float frames = baseTime * _FPS + ofs;

                uint   Fi = max((int)round(_Frames), 1);
                uint   i0 = (uint)floor(frames);
                uint   s0 = i0 % Fi; if (s0 < 0) s0 += Fi;
                uint   s1 = s0 + 1;  if (s1 >= Fi) s1 -= Fi;
                float w  = frac(frames);

                float row = floor(IN.vid / W);
                float col = IN.vid - row * W;
                float2 vatUV = float2((col + 0.5) / W, (row + 0.5) / H);

                float3 p0 = _PosArr.SampleLevel(sampler_PosArr, float3(vatUV, s0), 0).xyz;
                float3 p1 = _PosArr.SampleLevel(sampler_PosArr, float3(vatUV, s1), 0).xyz;
                float3 n0 = _NrmArr.SampleLevel(sampler_NrmArr, float3(vatUV, s0), 0).xyz;
                float3 n1 = _NrmArr.SampleLevel(sampler_NrmArr, float3(vatUV, s1), 0).xyz;

                float3 posOS = lerp(p0, p1, w);
                float3 nrmOS = normalize(lerp(n0, n1, w));

                float3 posWS = TransformObjectToWorld(posOS);
                OUT.positionHCS = TransformWorldToHClip(float4(posWS, 1));
                OUT.normalWS    = TransformObjectToWorldNormal(nrmOS);
                OUT.baseUV      = TRANSFORM_TEX(IN.baseUV, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN):SV_Target
            {
                Light ml = GetMainLight();
                float3 N = normalize(IN.normalWS);
                float  nl = saturate(dot(N, -ml.direction));
                float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.baseUV).rgb * _BaseColor.rgb;
                return half4(albedo * (0.2h + 0.8h * nl), 1);
            }
            ENDHLSL
        }
    }
}
