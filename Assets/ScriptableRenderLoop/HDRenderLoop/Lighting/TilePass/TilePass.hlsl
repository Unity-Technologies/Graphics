#if defined (LIGHTLOOP_TILE_DIRECT) || defined(LIGHTLOOP_TILE_ALL)
#define PROCESS_DIRECTIONAL_LIGHT
#define PROCESS_PUNCTUAL_LIGHT
#define PROCESS_AREA_LIGHT
#endif

#if defined (LIGHTLOOP_TILE_INDIRECT) || defined(LIGHTLOOP_TILE_ALL)
#define PROCESS_ENV_LIGHT
#endif

#include "TilePass.cs.hlsl"

uint _NumTileX;
uint _NumTileY;

Buffer<uint> g_vLightListGlobal;

#define TILE_SIZE 16 // This is fixed
#define DWORD_PER_TILE 16 // See dwordsPerTile in TilePass.cs, we have roomm for 31 lights and a number of light value all store on 16 bit (ushort)

// these uniforms are only needed for when OPAQUES_ONLY is NOT defined
// but there's a problem with our front-end compilation of compute shaders with multiple kernels causing it to error
//#ifdef USE_CLUSTERED_LIGHTLIST
float g_fClustScale;
float g_fClustBase;
float g_fNearPlane;
float g_fFarPlane;
int g_iLog2NumClusters;	// We need to always define these to keep constant buffer layouts compatible

uint g_isLogBaseBufferEnabled;
uint g_isOpaquesOnlyEnabled;
//#endif

#ifdef USE_CLUSTERED_LIGHTLIST
Buffer<uint> g_vLayeredOffsetsBuffer;
Buffer<float> g_logBaseBuffer;
#endif

// TODO: Need to correctly define the shadow framework, WIP
#include "../SinglePass/SinglePass.hlsl"