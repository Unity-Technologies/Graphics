#ifndef _OCCLUSION_CULLING_DEFINE_H
#define _OCCLUSION_CULLING_DEFINE_H

// If using this the shader should add
// #pragma multi_compile _ USE_ARRAY

static int g_slice_index = 0;
#ifdef USE_ARRAY
#define TEXTURE2D_A                                             TEXTURE2D_ARRAY
#define RW_TEXTURE2D_A                                          RW_TEXTURE2D_ARRAY
#define SET_SLICE_INDEX(N)                                      g_slice_index = N
#define ARRAY_COORD(C)                                          int3((C), g_slice_index)
#define GATHER_TEXTURE2D_A(textureName, samplerName, coord2)    GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, g_slice_index)
#define LOAD_TEXTURE2D_A(textureName, coord2)                   LOAD_TEXTURE2D_ARRAY(textureName, coord2, g_slice_index)
#else
#define TEXTURE2D_A                                             TEXTURE2D
#define RW_TEXTURE2D_A                                          RW_TEXTURE2D
#define SET_SLICE_INDEX(N)
#define ARRAY_COORD(C)                                          C
#define GATHER_TEXTURE2D_A                                      GATHER_TEXTURE2D
#define LOAD_TEXTURE2D_A                                        LOAD_TEXTURE2D
#endif

#endif
