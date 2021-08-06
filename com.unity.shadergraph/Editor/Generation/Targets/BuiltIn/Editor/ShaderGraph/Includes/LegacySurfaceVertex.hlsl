#ifndef UNITY_LEGACY_SURFACE_VERTEX
#define UNITY_LEGACY_SURFACE_VERTEX

struct v2f_surf {
  float4 pos;//UNITY_POSITION(pos);
  float3 worldNormal;// : TEXCOORD1;
  float3 worldPos;// : TEXCOORD2;
  float3 viewDir;
  float4 lmap;// : TEXCOORD3;
  #if UNITY_SHOULD_SAMPLE_SH
  half3 sh;// : TEXCOORD3; // SH
  #endif
  float1 fogCoord; //UNITY_FOG_COORDS(4)
  DECLARE_LIGHT_COORDS(4)//unityShadowCoord4 _LightCoord;
  UNITY_SHADOW_COORDS(5)//unityShadowCoord4 _ShadowCoord;

  //#ifdef DIRLIGHTMAP_COMBINED
  float4 tSpace0 : TEXCOORD6;
  float4 tSpace1 : TEXCOORD7;
  float4 tSpace2 : TEXCOORD8;
  //#endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

#endif // UNITY_LEGACY_SURFACE_VERTEX
