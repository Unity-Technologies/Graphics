// NOTE: For performing a project upgrade where you temporarily support both old and new renderpipelines
//       in the same sahder
//
// The basic approach is:
// Upgrade all your shaders to the new naming convention, using a SubShader that also contains the legacy // renderloop code.
//
// 1. Copy HDRenderPipeline Lit.shader into your project
// 2. Add a SubShader and copy old Standard shader passes into it.
// 2. Set LOD on subshader to make Unity pick at runtime to use new renderloop shaders or
// legacy standard shaders based on if SRL is enabled or not.
// In the legacy standard shader section add
// #include "PatchStandardShaderToNewNamingConvention.cginc"

// List of name remaps
#define _MainTex _BaseColorMap
#define _MainTex_ST _BaseColorMap_ST
#define _BumpMap _NormalMap
#define _ParallaxMap _HeightMap
#define _Parallax _HeightScale
#define _Glossiness _Smoothness
