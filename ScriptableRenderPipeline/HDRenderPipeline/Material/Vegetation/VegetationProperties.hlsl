TEXTURE2D(_VegNoise);
SAMPLER2D(sampler_VegNoise);

TEXTURE2D(_VegWindMask);
SAMPLER2D(sampler_VegWindMask);

CBUFFER_START(_PerMaterial)
float4 _VegPivot;
float  _VegStiffness;
float4 _VegAssistantDirectional;
float4 _VegWindDirection;
float  _VegWindIntensity;
float  _VegWindSpeed;
float  _VegDetailVariation;
float  _VegLeafShakeScale;
float  _VegLeafShakeSpeed;
float  _VegLeafShakePower;
float  _VegPerLeafBendScale;
float  _VegPerLeafBendSpeed;
float  _VegPerLeafBendPower;
CBUFFER_END