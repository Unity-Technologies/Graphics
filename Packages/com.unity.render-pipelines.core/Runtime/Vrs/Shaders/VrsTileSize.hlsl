#ifndef VRS_TILE_SIZE_INCLUDED
#define VRS_TILE_SIZE_INCLUDED

#if defined(VRS_TILE_SIZE_8)
    #define VRS_TILE_SIZE 8
#elif defined(VRS_TILE_SIZE_16)
    #define VRS_TILE_SIZE 16
#elif defined(VRS_TILE_SIZE_32)
    #define VRS_TILE_SIZE 32
#else
    #error Unsupported tile size
#endif

#endif // VRS_TILE_SIZE_INCLUDED
