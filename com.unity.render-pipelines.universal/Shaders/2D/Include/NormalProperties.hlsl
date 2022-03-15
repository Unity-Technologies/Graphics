#if USE_NORMAL_MAP

    #if LIGHT_QUALITY_FAST
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   lightDirection  : TEXCOORDA;\
            half2   screenUV   : TEXCOORDB;
    #else
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   positionWS : TEXCOORDA;\
            half2   screenUV   : TEXCOORDB;
    #endif

    #define NORMALS_LIGHTING_VARIABLES \
            TEXTURE2D(_NormalMap); \
            SAMPLER(sampler_NormalMap); \
            half4       _LightPosition;\
            half        _LightZDistance;

#if _RENDER_PASS_ENABLED
    #define NORMALBUFFER 0
    FRAMEBUFFER_INPUT_HALF(NORMALBUFFER);
#else
    TEXTURE2D(_NormalMap);
    SAMPLER(sampler_NormalMap);
#endif
    half4 _LightPosition;
    half  _LightZDistance;


#else
    #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB)
    #define NORMALS_LIGHTING_VARIABLES
#endif
#define SHADOW_COORDS(TEXCOORDA)\
    float2  shadowUV    : TEXCOORDA;
