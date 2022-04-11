#ifndef UNITY_PP_DEFINES_INCLUDED
#define UNITY_PP_DEFINES_INCLUDED

#if !defined(ENABLE_ALPHA)
    #define CTYPE float3
    #define CTYPE_SWIZZLE xyz
#else
    #define CTYPE float4
    #define CTYPE_SWIZZLE xyzw
#endif //ENABLE_ALPHA

#endif //UNITY_PP_DEFINES_INCLUDED
