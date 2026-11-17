Shader "Brainrot/TropicalSky"
{
 Properties
 {
  _Zenith("Зенит",Color)=(.035,.43,.94,1)
  _Horizon("Горизонт",Color)=(.43,.81,.99,1)
  _SunDirection("Направление солнца",Vector)=(.4,.7,.3,0)
 }
 SubShader
 {
  Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma target 3.0
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
    float3 sun=normalize(_SunDirection.xyz);
    half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(height,.46));
    float alignment=saturate(dot(d,sun));
    // Broad warm glow and a tiny luminous disk. The sky stays blue outside the sun,
    // so restrained bloom adds light instead of washing the whole screen white.
    color+=half3(1,.76,.47)*pow(alignment,24)*.12;
    color=lerp(color,half3(1.65,1.49,1.15),smoothstep(.9986,.9997,alignment));
    // Low-frequency billows, gently advected across the sky. All evaluation remains
    // in one sky pass: no transparent cloud cards, scene objects or particle overdraw.
    float2 p=d.xz/max(.16,d.y)*1.35+float2(_Time.y*.0014,_Time.y*.0004);
    float2 warp=float2(Noise(p*.7+13),Noise(p*.7-8))-.5;
    p+=warp*.85;
    float cloud=Noise(p)*.64+Noise(p*2.03+7.3)*.27+Noise(p*4.1-3.7)*.09;
    float2 towardSun=sun.xz/max(.1,length(sun.xz))*.17;
    float litDensity=Noise(p+towardSun)*.64+Noise((p+towardSun)*2.03+7.3)*.27;
    float bank=smoothstep(.51,.63,cloud);
    float altitude=smoothstep(.035,.14,height)*(1-smoothstep(.80,.99,height));
    float mask=bank*altitude;
    // Slightly blue underside, cream tops and a restrained sun-facing silver rim.
    // Directional density avoids a uniform flat-white noise layer.
    float light=saturate(.53+(cloud-litDensity)*3.0);
    half3 cloudColor=lerp(half3(.60,.76,.91),half3(1.02,1.015,.97),light);
    float rim=4*bank*(1-bank)*pow(alignment,6)*.14;
    cloudColor+=half3(1,.91,.72)*rim;
    color=lerp(color,cloudColor,mask);
    // Distant banks sink into a narrow cyan atmospheric veil at the waterline.
    float horizonVeil=(1-smoothstep(.015,.085,height))*.22;
    color=lerp(color,_Horizon.rgb,horizonVeil);
    color=lerp(_Horizon.rgb,color,smoothstep(-.16,.04,d.y));
    return half4(max(color,0),1);
   }
   ENDHLSL
  }
 }
}
