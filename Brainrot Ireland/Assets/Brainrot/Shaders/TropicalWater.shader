Shader "Brainrot/TropicalWater"
{
 Properties
 {
  _BaseColor("Глубокая вода",Color)=(.07,.39,.59,1)
  _ShallowColor("Мелководье",Color)=(.28,.79,.78,1)
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
   #pragma target 3.0
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
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
   Varyings Vert(Attributes v){Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(Varyings i):SV_Target
   {
    float2 p=i.world.xz*max(.05,_WaveScale);float t=_Time.y*_WaveSpeed;
    // Domain warping breaks the former parallel sine bands into long organic water cells.
    float2 warped=p+float2(sin(p.y*.36+t*.12),sin(p.x*.29-t*.16))*1.15;
    float a=dot(warped,float2(.83,.55))*.82+t*.47;
    float b=dot(warped,float2(-.68,.73))*1.18-t*.39;
    float broad=Noise(p*.26+float2(t*.035,-t*.028));
    float wave=sin(a)*.55+sin(b)*.30+(broad-.5)*.3;
    float2 slope=float2(.83,.55)*cos(a)*.45+float2(-.68,.73)*cos(b)*.35;
    float3 normal=normalize(float3(-slope.x*_WaveStrength*1.45,1,-slope.y*_WaveStrength*1.45));
    float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
    Light sun=GetMainLight();float3 halfVector=SafeNormalize(view+sun.direction);
    float angle=atan2(i.world.z,i.world.x);
    // Same coastline as the playable island; no depth/opaque texture dependency or extra render passes.
    float coast=22+sin(angle*5)*.3+cos(angle*9)*.17;
    float shore=max(0,length(i.world.xz)-coast);
    half shallow=1-smoothstep(.2,16,shore);
    half3 deep=lerp(_BaseColor.rgb,half3(.20,.49,.66),.18);
    half3 color=lerp(deep,_ShallowColor.rgb,shallow*.90);
    half fresnel=pow(1-saturate(dot(normal,view)),4);
    color=lerp(color,half3(.70,.85,.91),fresnel*.46);
    color*=.95+.05*saturate(dot(normal,sun.direction));
    color+=wave*_WaveStrength*.14;
    // Soft broad sunlight rather than a clipped white spotlight; distance fades small details.
    float detail=saturate(view.y*4);
    half spec=pow(saturate(dot(normal,halfVector)),80)*.19;
    color+=sun.color*spec;
    float cells=abs(sin(warped.x*2.9+sin(warped.y*2.1)+t*.13)*sin(warped.y*2.6+sin(warped.x*1.7)-t*.18));
    half caustic=1-smoothstep(.025,.10,cells);
    color+=half3(.22,.45,.36)*caustic*shallow*detail*.12;
    float sweep=shore*1.65-t*.48+sin(angle*13+t*.1)*.19;
    half roll=pow(saturate(sin(sweep)*.5+.5),13);
    half broken=.73+.27*sin(angle*43+wave)*sin(angle*19-t*.2);
    half foam=saturate(roll*(1-smoothstep(.3,3.8,shore))*broken*.60);
    // A persistent narrow lace line ties the ocean to the wet sand.
    foam=max(foam,(1-smoothstep(.04,.43,shore))*broken*.72);
    color=lerp(color,half3(.91,.98,.93),foam);
    float spark=pow(saturate(sin(warped.x*6.1+t)*sin(warped.y*7.3-t*.7)),28);
    color+=half3(.92,.97,1)*spark*spec*detail*.35;
    return half4(MixFog(saturate(color),i.fog),1);
   }
   ENDHLSL
  }
 }
}
