Shader "Brainrot/TropicalSky"
{
 Properties
 {
  _Zenith("Зенит",Color)=(.21,.63,.91,1)
  _Horizon("Горизонт",Color)=(.69,.88,.97,1)
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
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
   Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
   half4 Frag(Varyings i):SV_Target
   {
    float3 d=normalize(i.direction);float height=saturate(d.y);
    half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(height,.52));
    float alignment=saturate(dot(d,normalize(_SunDirection.xyz)));
    // A broad peach halo with a soft cream sun; no hard HDR disk or blown horizon.
    color+=half3(1,.78,.50)*pow(alignment,18)*.14;
    color=lerp(color,half3(1,.96,.80),smoothstep(.9985,.9996,alignment)*.95);
    float2 p=d.xz/max(.19,d.y)*1.7+float2(_Time.y*.0015,0);
    float cloud=Noise(p)*.70+Noise(p*2.12+7.3)*.23+Noise(p*4.1-3.7)*.07;
    float upper=Noise(p+float2(0,.08))*.70+Noise(p*2.12+7.3+float2(0,.08))*.23;
    half mask=smoothstep(.57,.69,cloud)*smoothstep(.07,.19,height)*(1-smoothstep(.61,.91,height));
    half3 cloudColor=lerp(half3(.79,.90,1),half3(1,.985,.94),saturate(.5+(cloud-upper)*4));
    color=lerp(color,cloudColor,mask*.91);
    // Fog-coloured lower hemisphere keeps a full camera orbit clean below the water horizon.
    color=lerp(_Horizon.rgb,color,smoothstep(-.16,.04,d.y));
    return half4(saturate(color),1);
   }
   ENDHLSL
  }
 }
}
