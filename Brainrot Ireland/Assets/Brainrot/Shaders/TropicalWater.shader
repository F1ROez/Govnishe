Shader "Brainrot/TropicalWater"
{
    Properties
    {
        _BaseColor("Deep turquoise",Color)=(0.015,0.35,0.55,1)
        _ShallowColor("Shallow turquoise",Color)=(0.12,0.75,0.78,1)
        _WaveSpeed("Wave speed",Range(0,3))=0.6
        _WaveScale("Wave scale",Range(0.05,2))=0.45
        _WaveStrength("Wave contrast",Range(0,0.4))=0.12
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShallowColor;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveStrength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.world=TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world);
                o.fog=ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.world.xz*max(.01,_WaveScale);
                float t=_Time.y*_WaveSpeed;
                // Three oblique swells avoid the previous intersecting rectangular grid.
                float a=dot(p,float2(.83,.56))+t*.63;
                float b=dot(p,float2(-.36,.93))*1.47-t*.49;
                float c=dot(p,float2(.98,-.20))*.53+t*.31;
                float waves=sin(a+sin(b)*.20)*.48+sin(b)*.30+sin(c)*.22;
                float distanceFromShore=max(0,length(i.world.xz)-22);
                half shallow=1-smoothstep(0,25,distanceFromShore);
                half3 color=lerp(_BaseColor.rgb,_ShallowColor.rgb,shallow*.78);
                color+=waves*_WaveStrength*.28;
                // Narrow moving highlights only near crests, with a soft, low-contrast finish.
                half crest=smoothstep(.65,.96,waves);
                half sparkle=pow(saturate(sin(a*2.1+sin(b)*.4)*.5+.5),18)*crest;
                float3 view=normalize(GetWorldSpaceViewDir(i.world));
                half fresnel=pow(1-saturate(view.y),3);
                color=lerp(color,half3(.56,.83,.88),fresnel*.22);
                color+=half3(.40,.50,.47)*sparkle*.12;
                half shoreline=(1-smoothstep(.4,3.0,distanceFromShore));
                half foam=shoreline*smoothstep(.70,.98,sin(distanceFromShore*2.3-t*.7+sin(a)*.2));
                color=lerp(color,half3(.75,.94,.89),foam*.25);
                return half4(MixFog(saturate(color),i.fog),1);
            }
            ENDHLSL
        }
    }
}
