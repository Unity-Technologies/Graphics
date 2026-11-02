#ifndef UNIVERSAL_TERRAIN_LIT_FEATURES_INCLUDED
#define UNIVERSAL_TERRAIN_LIT_FEATURES_INCLUDED
// Feature-state queries (*Available()) for every shader_feature the terrain shaders use.
// One include per feature axis; see the individual headers in Shaders/Utils/.

#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/NormalMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/AlphaBlend.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/ClearCoat.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/ReceiveShadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SpecularHighlights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/EnvironmentReflections.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/MetallicSpecGloss.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SurfaceType.hlsl"

#endif // UNIVERSAL_TERRAIN_LIT_FEATURES_INCLUDED
