Shader "Brainrot/TropicalWater"
{
    Properties { _BaseColor("Deep turquoise",Color)=(0.015,0.40,0.61,1) _FoamColor("Sunlit ripples",Color)=(0.24,0.86,0.86,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;half4 _FoamColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings o;o.world=TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.world.xz;float t=_Time.y;
                float w=sin(p.x*1.2+t*1.3+sin(p.y*.6+t*.6))*sin(p.y*1.9-t*.9);
                float gleam=pow(saturate(w),12)*.6;
                float broad=sin(p.x*.09+p.y*.12+t*.3)*.08;
                half3 c=lerp(_BaseColor.rgb,_FoamColor.rgb,.22+w*.13+broad+gleam);
                return half4(MixFog(c,ComputeFogFactor(i.positionCS.z)),1);
            }
            ENDHLSL
        }
    }
}
