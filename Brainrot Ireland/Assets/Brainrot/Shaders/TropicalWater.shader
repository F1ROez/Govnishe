Shader "Brainrot/TropicalWater"
{
 Properties
 {
  _BaseColor("Глубокая вода",Color)=(.015,.49,.73,1)
  _ShallowColor("Мелководье",Color)=(.04,.83,.78,1)
  _WaveSpeed("Скорость волн",Range(0,3))=.6
  _WaveScale("Масштаб волн",Range(.05,2))=.45
  _WaveStrength("Сила ряби",Range(0,.4))=.12
  _FoamColor("Мягкая морская пена",Color)=(.94,.995,.97,1)
  _CausticStrength("Свет на мелководье",Range(0,.5))=.23
  _Shoreline("Радиус берега и ширина лагуны",Vector)=(22.58,.30,.17,16)
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
    half4 _BaseColor,_ShallowColor,_FoamColor;
    float4 _Shoreline;
    float _WaveSpeed,_WaveScale,_WaveStrength,_CausticStrength;
   CBUFFER_END
   // Arithmetic hash avoids a transcendental per noise corner. Use full precision here:
   // half precision loses the small world-locked water cells on a wide ocean plane.
   float Hash(float2 p){float3 h=frac(float3(p.x,p.y,p.x)*.1031);h+=dot(h,h.yzx+33.33);return frac((h.x+h.y)*h.z);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
   // Value and analytic derivatives share four hash samples. Avoids three height evaluations
   // per pixel and removes the coherent parallel slopes that looked like horizon stripes.
   float3 NoiseSlope(float2 p){
    float2 cell=floor(p),f=frac(p),u=f*f*(3-2*f),du=6*f*(1-f);
    float a=Hash(cell),b=Hash(cell+float2(1,0)),c=Hash(cell+float2(0,1)),d=Hash(cell+1);
    float cross=a-b-c+d;
    return float3(a+(b-a)*u.x+(c-a)*u.y+cross*u.x*u.y,
                  du.x*(b-a+cross*u.y),du.y*(c-a+cross*u.x));
   }
   Varyings Vert(Attributes v){Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(Varyings i):SV_Target
   {
    float2 p=i.world.xz*max(.05,_WaveScale);float t=_Time.y*_WaveSpeed;
    // Domain warping breaks the former parallel sine bands into long organic water cells.
    float2 warpPhase=float2(p.y*.36+t*.12,p.x*.29-t*.16);
    float2 warped=p+sin(warpPhase)*1.15;
    float3 large=NoiseSlope(warped*.76+float2(t*.15,-t*.11));
    float2 crossDirection=float2(warped.x*.83+warped.y*.55,-warped.x*.55+warped.y*.83);
    float3 small=NoiseSlope(crossDirection*1.73+float2(-t*.22,t*.17)+19.3);
    float broad=Noise(p*.26+float2(t*.035,-t*.028));
    float wave=(large.x*.64+small.x*.36-.5)*2;
    float2 smallSlope=float2(small.y*.83-small.z*.55,small.y*.55+small.z*.83)*1.73;
    float2 warpedSlope=large.yz*(.76*.64)+smallSlope*.36;
    // Chain rule keeps light moving with the warped height instead of sliding across it.
    float2 slope=float2(warpedSlope.x+warpedSlope.y*cos(warpPhase.y)*(.29*1.15),
                        warpedSlope.y+warpedSlope.x*cos(warpPhase.x)*(.36*1.15));
    float2 swellPhase=float2(dot(p,float2(.37,.15))-t*.45,dot(p,float2(-.19,.31))+t*.33);
    float2 swell=cos(swellPhase);
    slope+=float2(swell.x*.37-swell.y*.19,swell.x*.15+swell.y*.31)*.18;
    // Calm long swell plus smaller crossed ripples: no single sine direction spans the sea.
    float3 normal=normalize(float3(-slope.x*_WaveStrength*2.4,1,-slope.y*_WaveStrength*2.4));
    float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
    Light sun=GetMainLight();float3 halfVector=SafeNormalize(view+sun.direction);
    float angle=atan2(i.world.z,i.world.x);
    // Same coastline as the playable island; no depth/opaque texture dependency or extra render passes.
    float coast=_Shoreline.x+sin(angle*5)*_Shoreline.y+cos(angle*9)*_Shoreline.z;
    float shore=max(0,length(i.world.xz)-coast);
    half shallow=1-smoothstep(.2,max(.21,_Shoreline.w),shore);
    // Colour controls remain authoritative; the editor supplies the bright default palette.
    half3 deep=_BaseColor.rgb;
    half3 lagoon=_ShallowColor.rgb;
    half3 color=lerp(deep,lagoon,shallow*.96);
    half fresnel=pow(1-saturate(dot(normal,view)),4);
    color=lerp(color,half3(.48,.81,.98),fresnel*.24);
    // Sunlit sand under the first metre of lagoon makes the wet beach join feel translucent.
    half sandScatter=(1-smoothstep(.05,2.1,shore))*.14;
    color=lerp(color,half3(.55,.86,.69),sandScatter);
    color*=.97+.03*saturate(dot(normal,sun.direction));
    color+=wave*_WaveStrength*.28;
    color*=.91+broad*.16;
    // Soft broad sunlight rather than a clipped white spotlight; distance fades small details.
    // Fade sub-pixel caustics and sparkles before they flicker at the horizon or on a small Android viewport.
    float footprint=max(length(ddx(warped)),length(ddy(warped)));
    float detail=saturate(view.y*4)*(1-smoothstep(.18,.9,footprint));
    float sunFacing=saturate(dot(normal,halfVector));
    half spec=pow(saturate(sunFacing-fwidth(sunFacing)*.65),110)*.22;
    half sheen=pow(sunFacing,20)*.07;
    color+=sun.color*(spec+sheen);
    float2 causticFlow=warped+float2(large.x,small.x)*.42;
    float cells=abs(sin(causticFlow.x*2.9+sin(causticFlow.y*2.1)+t*.13)*sin(causticFlow.y*2.6+sin(causticFlow.x*1.7)-t*.18));
    float cellsAA=max(.004,fwidth(cells));
    half caustic=1-smoothstep(.025-cellsAA*.35,.10+cellsAA,cells);
    color+=half3(.22,.45,.36)*caustic*shallow*detail*_CausticStrength;
    // Advancing and dissolving surf. The irregular front is confined to the shore, not
    // repeated concentric stripes across the ocean. A second band is only a faint trailing wash.
    float along=angle*19;
    float surfPhase=t*.65+sin(angle*4)*.23;
    float front=1.35+sin(surfPhase)*.72+sin(along+t*.13)*.22+sin(angle*34-t*.21)*.12;
    float foamDistance=abs(shore-front);
    float surfAA=max(.025,fwidth(shore));
    float crestBand=1-smoothstep(.08,.28+surfAA,foamDistance);
    float trailing=(1-smoothstep(.15,.48+surfAA,abs(shore-front-1.0)))*.26;
    float lace=Noise(warped*3.2+float2(-t*.09,t*.12));
    half broken=.50+.50*smoothstep(.16,.76,large.x*.6+small.x*.4);
    half holes=smoothstep(.20,.64,lace);
    float retreat=.68+.32*sin(surfPhase+1.2);
    half foam=saturate((crestBand+trailing)*broken*lerp(.24,1,holes)*retreat);
    // Tiny rings follow the surf, never a full-screen tiled foam layer. Analytic AA
    // dissolves sub-pixel bubbles smoothly rather than making the shore sparkle.
    float2 bubblePoint=warped*3.2+float2(-t*.09,t*.12);
    float2 bubbleCell=floor(bubblePoint),bubbleLocal=frac(bubblePoint)-.5;
    float2 bubbleOffset=float2(Hash(bubbleCell+7.1),Hash(bubbleCell+23.7))*.24-.12;
    float bubbleRadius=.18+Hash(bubbleCell+41.3)*.13;
    float bubbleEdge=abs(length(bubbleLocal-bubbleOffset)-bubbleRadius);
    float bubbleAA=max(.008,fwidth(bubbleEdge));
    half bubbles=1-smoothstep(.018,.035+bubbleAA,bubbleEdge);
    foam=saturate(foam+bubbles*(crestBand+trailing)*holes*detail*.22);
    // The very thin permanent contact line plus irregular bubbles prevents a hard beach cutoff.
    foam=max(foam,(1-smoothstep(.025,.30,shore))*lerp(.45,.88,holes));
    // Small broken whitecaps beyond the beach make the ocean feel lively without stripe bands.
    float crest=smoothstep(.69,.87,large.x*.56+small.x*.44);
    float whitecap=crest*smoothstep(.45,.76,small.x)*smoothstep(3,12,shore)*detail*.60;
    foam=max(foam,whitecap);
    // Tiny warm translucent-looking gaps keep the surf like bubbly lace, not opaque white ribbons.
    color=lerp(color,_FoamColor.rgb,foam);
    float spark=pow(saturate(sin(warped.x*6.1+t)*sin(warped.y*7.3-t*.7)),28);
    color+=half3(.92,.97,1)*spark*spec*detail*.30;
    return half4(MixFog(saturate(color),i.fog),1);
   }
   ENDHLSL
  }
 }
}
