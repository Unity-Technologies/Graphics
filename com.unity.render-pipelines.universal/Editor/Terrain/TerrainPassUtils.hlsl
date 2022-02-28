
#ifdef UNIVERSAL_TERRAIN_SPLAT01
#define GetSplat0UV(x) x.uvSplat01.xy
#define GetSplat1UV(x) x.uvSplat01.zw
#else
#define GetSplat0UV(x) 0.0
#define GetSplat1UV(x) 0.0
#endif

#ifdef UNIVERSAL_TERRAIN_SPLAT23
#define GetSplat2UV(x) x.uvSplat23.xy
#define GetSplat3UV(x) x.uvSplat23.zw
#else
#define GetSplat2UV(x) 0.0
#define GetSplat3UV(x) 0.0
#endif
