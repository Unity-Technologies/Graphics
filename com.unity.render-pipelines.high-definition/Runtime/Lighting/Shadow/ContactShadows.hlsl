RW_TEXTURE2D_X(uint, _ContactShadowTextureUAV);

CBUFFER_START(DeferredShadowParameters)
float4  _ContactShadowParamsParameters;
float4  _ContactShadowParamsParameters2;
float4  _ContactShadowParamsParameters3;
int     _SampleCount;
CBUFFER_END

#define _ContactShadowLength                _ContactShadowParamsParameters.x
#define _ContactShadowDistanceScaleFactor   _ContactShadowParamsParameters.y
#define _ContactShadowFadeEnd               _ContactShadowParamsParameters.z
#define _ContactShadowFadeOneOverRange      _ContactShadowParamsParameters.w
#define _RenderTargetHeight                 _ContactShadowParamsParameters2.x
#define _ContactShadowMinDistance           _ContactShadowParamsParameters2.y
#define _ContactShadowFadeInEnd             _ContactShadowParamsParameters2.z
#define _ContactShadowBias                  _ContactShadowParamsParameters2.w
#define _SampleCount                        (int)_ContactShadowParamsParameters3.x
#define _ContactShadowThickness             _ContactShadowParamsParameters3.y
