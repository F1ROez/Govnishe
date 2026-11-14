Shader "Brainrot/ToonCharacter"
{
 Properties
 {
  _BaseMap("Текстура",2D)="white"{}
  _BaseColor("Цвет",Color)=(1,1,1,1)
  _Brightness("Яркость",Range(.5,2))=1.18
  _Saturation("Насыщенность",Range(0,2))=1.4
  _TextureSoftening("Мягкость текстуры",Range(0,1))=.16
  _Cull("Стороны",Float)=2
  _Cutoff("Порог",Range(0,1))=.5
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   Cull [_Cull]
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;half4 _BaseColor;half _Brightness,_Saturation,_TextureSoftening,_Cull,_Cutoff;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A v){V o=(V)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half3 texColor=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
    half luma=dot(texColor,half3(.2126,.7152,.0722));texColor=lerp(luma.xxx,texColor,_Saturation);
    half3 albedo=saturate(lerp(texColor,half3(1,1,1),_TextureSoftening)*_BaseColor.rgb*_Brightness);
    half3 n=normalize(i.normal);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half diffuse=dot(n,sun.direction)*.5+.5;half band=smoothstep(.36,.63,diffuse);
    half shadow=lerp(.70,1,sun.shadowAttenuation);half3 light=lerp(half3(.56,.66,.83),half3(1.02,1.01,.96),band)*shadow;
    light+=saturate(SampleSH(n))*.12;
    half rim=pow(1-saturate(dot(n,SafeNormalize(GetWorldSpaceViewDir(i.world)))),3)*.16;
    half3 color=albedo*light+rim*half3(.55,.78,1);
    return half4(MixFog(saturate(color),i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
