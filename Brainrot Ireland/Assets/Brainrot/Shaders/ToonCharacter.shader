Shader "Brainrot/ToonCharacter"
{
 Properties
 {
  [MainTexture] _BaseMap("Текстура",2D)="white"{}
  [MainColor] _BaseColor("Цвет",Color)=(1,1,1,1)
  _Brightness("Яркость",Range(.5,2))=1.05
  _Saturation("Насыщенность",Range(0,2))=1.16
  _TextureSoftening("Мягкость текстуры",Range(0,1))=.08
  _RimStrength("Мягкий контровой свет",Range(0,.3))=.07
  _GlossStrength("Широкий блик",Range(0,.5))=.17
  _Cull("Стороны",Float)=2
  _Cutoff("Порог",Range(0,1))=.5
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  // Every pass shares this exact material layout. Lit UsePass would bring a different CBUFFER,
  // making the custom character material incompatible with the SRP Batcher.
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;half _Brightness,_Saturation,_TextureSoftening,_RimStrength,_GlossStrength,_Cull,_Cutoff;
  CBUFFER_END
  ENDHLSL
  Pass
  {
   Name "ToonForward"
   Tags {"LightMode"="UniversalForward"}
   Cull [_Cull]
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;float4 shadowCoord:TEXCOORD4;half3 vertexLight:TEXCOORD5;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
   V Vert(A v){V o=(V)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);VertexPositionInputs position=GetVertexPositionInputs(v.positionOS.xyz);o.world=position.positionWS;o.positionCS=position.positionCS;o.shadowCoord=GetShadowCoord(position);o.normal=TransformObjectToWorldNormal(v.normalOS);o.vertexLight=VertexLighting(o.world,o.normal);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half3 texColor=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
    half luma=dot(texColor,half3(.2126,.7152,.0722));texColor=max(0,lerp(luma.xxx,texColor,_Saturation));
    // Preserve dark pupils, mouth and painted wood grain. Mixing towards white erased
    // those features, while an early saturate flattened bright texture colours to one value.
    texColor=max(0,(texColor-.18)*1.08+.18);
    texColor+=texColor*(1-saturate(texColor))*saturate(_TextureSoftening)*.45;
    half3 albedo=texColor*_BaseColor.rgb*_Brightness;
    half3 n=normalize(i.normal);
    // Cascade selection must happen per fragment; interpolating shadow coordinates across
    // a cascade boundary produces a visible seam on large sharks and Orcaladon.
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
     float4 shadowCoord=i.shadowCoord;
    #else
     float4 shadowCoord=TransformWorldToShadowCoord(i.world);
    #endif
    Light sun=GetMainLight(shadowCoord);
    half ndotl=dot(n,sun.direction);
    // Two wide, feathered bands keep the toy volume readable without hard plastic stripes.
    half terminator=smoothstep(-.24,.22,ndotl);
    half litFace=smoothstep(.12,.82,ndotl);
    half band=terminator*.44+litFace*.56;
    half sunLuma=max(.001,dot(sun.color,half3(.2126,.7152,.0722)));
    half3 sunTint=lerp(half3(1,1,1),sun.color/sunLuma,.65);
    half energy=min(1.25,sunLuma);
    half shadow=saturate(sun.shadowAttenuation);
    // Warm bounce under the feet; blue sky on upward-facing surfaces. Keep enough contrast
    // in the occluded side so a grey shark is solid and sculpted rather than milky/translucent.
    half skyAmount=smoothstep(-.65,.85,n.y);
    half3 skyFill=lerp(half3(.43,.36,.27),half3(.34,.52,.70),skyAmount);
    half3 light=skyFill*.78+sunTint*(band*.64*lerp(.30,1,shadow)*energy);
    light+=clamp(SampleSH(n),half3(0,0,0),half3(.8,.8,.8))*.045;
    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
     light+=i.vertexLight*.20;
    #endif
    #if defined(_ADDITIONAL_LIGHTS)
     uint count=GetAdditionalLightsCount();
     for(uint lightIndex=0u;lightIndex<count;lightIndex++){
      Light fill=GetAdditionalLight(lightIndex,i.world);
      light+=fill.color*(smoothstep(-.10,.80,dot(n,fill.direction))*.20)*fill.distanceAttenuation;
     }
    #endif
    float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
    half facing=saturate(dot(n,view));
    // Reflected sky hugs only the silhouette. Modulation by the paint preserves black pupils,
    // mouth and Orcaladon markings instead of adding a blue emissive outline to every surface.
    half rim=pow(1-facing,4)*_RimStrength*smoothstep(-.45,.5,n.y);
    half3 rimColor=lerp(albedo,half3(.38,.76,.92),.40)*smoothstep(.015,.24,dot(albedo,half3(.2126,.7152,.0722)));
    half specDot=saturate(dot(n,SafeNormalize(view+sun.direction)));
    half broad=pow(specDot,18);
    // Filter the tight lobe using screen derivatives to suppress distant sparkling.
    half detail=pow(saturate(specDot-max(fwidth(specDot),.001)*.7),64)*.22;
    half gloss=(broad*.78+detail)*saturate(ndotl)*shadow*_GlossStrength*energy;
    half3 color=albedo*light+rim*rimColor+gloss*lerp(albedo,half3(1,.94,.79),.36)*sunTint;
    return half4(MixFog(color,i.fog),1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster"
   Tags {"LightMode"="ShadowCaster"}
   ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex ShadowVertex
   #pragma fragment ShadowFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   float3 _LightDirection;
   float3 _LightPosition;
   struct ShadowAttributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct ShadowVaryings {float4 positionCS:SV_POSITION;};
   ShadowVaryings ShadowVertex(ShadowAttributes input)
   {
    UNITY_SETUP_INSTANCE_ID(input);
    float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS=TransformObjectToWorldNormal(input.normalOS);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
     float3 directionWS=normalize(_LightPosition-positionWS);
    #else
     float3 directionWS=_LightDirection;
    #endif
    ShadowVaryings output;
    output.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,directionWS)));
    return output;
   }
   half4 ShadowFragment(ShadowVaryings input):SV_Target{return 0;}
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly"
   Tags {"LightMode"="DepthOnly"}
   ZWrite On ColorMask R Cull [_Cull]
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex DepthVertex
   #pragma fragment DepthFragment
   #pragma multi_compile_instancing
   struct DepthAttributes {float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct DepthVaryings {float4 positionCS:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
   DepthVaryings DepthVertex(DepthAttributes input)
   {
    DepthVaryings output=(DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS=TransformObjectToHClip(input.positionOS.xyz);return output;
   }
   half DepthFragment(DepthVaryings input):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);return input.positionCS.z;}
   ENDHLSL
  }
 }
}
