Shader "Brainrot/TropicalSky"
{
 Properties
 {
  _Zenith("Зенит",Color)=(.12,.44,.76,1)
  _Horizon("Горизонт",Color)=(.64,.86,.92,1)
  _SunDirection("Направление солнца",Vector)=(.4,.7,.3,0)
 }
 SubShader
 {
  Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma target 2.0
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
    half4 _Zenith,_Horizon;float4 _SunDirection;
   CBUFFER_END
   Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
   half4 Frag(Varyings i):SV_Target
   {
    float3 d=normalize(i.direction);float height=saturate(d.y);
    half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(height,.45));
    float alignment=saturate(dot(d,normalize(_SunDirection.xyz)));
    color+=half3(1,.80,.48)*pow(alignment,14)*.13;
    color=lerp(color,half3(1,.97,.82),smoothstep(.99955,.99988,alignment));
    float2 p=d.xz/max(.2,d.y)*2.2+float2(_Time.y*.002,0);
    float cloud=sin(p.x+sin(p.y*.7))*.45+sin(p.y*1.4-p.x*.31)*.35+sin(p.x*2.3+p.y*.8)*.20;
    half mask=smoothstep(.48,.83,cloud)*smoothstep(.05,.20,height)*(1-smoothstep(.65,.95,height));
    color=lerp(color,half3(.95,.98,1),mask*.66);
    return half4(saturate(color),1);
   }
   ENDHLSL
  }
 }
}
