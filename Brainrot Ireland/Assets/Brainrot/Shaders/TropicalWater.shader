Shader "Brainrot/TropicalWater"
{
 Properties
 {
  _BaseColor("Глубокая вода",Color)=(.018,.28,.46,1)
  _ShallowColor("Мелководье",Color)=(.16,.77,.75,1)
  _WaveSpeed("Скорость волн",Range(0,3))=.6
  _WaveScale("Масштаб волн",Range(.05,2))=.45
  _WaveStrength("Сила ряби",Range(0,.4))=.12
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
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct Attributes {float4 positionOS:POSITION;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half fog:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor,_ShallowColor;
    float _WaveSpeed,_WaveScale,_WaveStrength;
   CBUFFER_END
   Varyings Vert(Attributes v){Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(Varyings i):SV_Target
   {
    float2 p=i.world.xz*max(.05,_WaveScale);float t=_Time.y*_WaveSpeed;
    float a=dot(p,float2(.88,.47))*.65+t*.65;
    float b=dot(p,float2(-.41,.91))*1.31-t*.52;
    float c=dot(p,float2(.94,-.34))*2.45+t*.73;
    float wave=sin(a)*.54+sin(b)*.3+sin(c)*.16;
    float2 slope=float2(.88,.47)*cos(a)*.351+float2(-.41,.91)*cos(b)*.393+float2(.94,-.34)*cos(c)*.392;
    float3 normal=normalize(float3(-slope.x*_WaveStrength*1.6,1,-slope.y*_WaveStrength*1.6));
    float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
    Light sun=GetMainLight();float3 halfVector=SafeNormalize(view+sun.direction);
    float angle=atan2(i.world.z,i.world.x);
    // Same 22 m sandy shore as WorldBuilder.Island, not the former 100 m island.
    float coast=22+sin(angle*5)*.3+cos(angle*9)*.17;
    float shore=max(0,length(i.world.xz)-coast);
    half shallow=1-smoothstep(.5,14,shore);
    half3 color=lerp(_BaseColor.rgb,_ShallowColor.rgb,shallow*.92);
    half fresnel=pow(1-saturate(dot(normal,view)),4);
    color=lerp(color,half3(.49,.77,.88),fresnel*.33);
    color*=.88+.12*saturate(dot(normal,sun.direction));
    color+=wave*_WaveStrength*.13;
    // Broad soft glints and rare smaller sparkles, bounded to avoid white clipping.
    half spec=pow(saturate(dot(normal,halfVector)),60)*.25;
    half glint=pow(saturate(dot(normal,halfVector)),180)*.17;
    color+=sun.color*(spec+glint);
    float advancing=shore*2.3-t*.72+sin(angle*11+t*.22)*.24;
    half foamBand=pow(saturate(sin(advancing)*.5+.5),10);
    half shoreMask=(1-smoothstep(.4,3.3,shore));
    half broken=.6+.4*sin(angle*37+wave*2)*sin(angle*17-t*.2);
    half foam=saturate(foamBand*shoreMask*broken*.62);
    color=lerp(color,half3(.86,.98,.91),foam);
    return half4(MixFog(saturate(color),i.fog),1);
   }
   ENDHLSL
  }
 }
}
