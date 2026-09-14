#ifndef UNIVERSAL_LIT_FEATURES_INCLUDED
#define UNIVERSAL_LIT_FEATURES_INCLUDED
// Feature-state queries (*Available()) for every shader_feature the Lit and ComplexLit shaders use.
// One include per feature axis; see the individual headers in Shaders/Utils/.

#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Emission.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/AlphaBlend.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/MetallicSpecGloss.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SpecularHighlights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/EnvironmentReflections.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/OcclusionMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/ClearCoat.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SpecGloss.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/ReceiveShadows.hlsl"

#endif // UNIVERSAL_LIT_FEATURES_INCLUDED
