#ifndef UNIVERSAL_UNITY_TYPES_HLSL
#define UNIVERSAL_UNITY_TYPES_HLSL

// Match  UnityEngine.TextureWrapMode
#define URP_TEXTURE_WRAP_MODE_REPEAT       0
#define URP_TEXTURE_WRAP_MODE_CLAMP        1
#define URP_TEXTURE_WRAP_MODE_MIRROR       2
#define URP_TEXTURE_WRAP_MODE_MIRROR_ONCE  3
// Additional NULL case for shaders
#define URP_TEXTURE_WRAP_MODE_NONE        -1

// Match  UnityEngine.LightType
#define URP_LIGHT_TYPE_SPOT        0
#define URP_LIGHT_TYPE_DIRECTIONAL 1
#define URP_LIGHT_TYPE_POINT       2
// Area and Rectangle are aliases
#define URP_LIGHT_TYPE_AREA        3
#define URP_LIGHT_TYPE_RECTANGLE   3
#define URP_LIGHT_TYPE_DISC        4

#endif //UNIVERSAL_UNITY_TYPES_HLSL
