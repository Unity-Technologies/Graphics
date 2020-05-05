TEXTURECUBE_ARRAY(_SkyTexture);

TEXTURE2D_ARRAY(_SkyTextureIntegrals);
TEXTURE2D_ARRAY(_SkyTextureMarginals);
TEXTURE2D_ARRAY(_SkyTextureConditionalMarginals);

#define _MipFogNear                     _MipFogParameters.x
#define _MipFogFar                      _MipFogParameters.y
#define _MipFogMaxMip                   _MipFogParameters.z

#define _FogColor                       _FogColor

