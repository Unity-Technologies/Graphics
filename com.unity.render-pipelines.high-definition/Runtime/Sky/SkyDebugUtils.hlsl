#ifndef __SKYDEBUGUTILS_H__
#define __SKYDEBUGUTILS_H__

#ifdef DEBUG_DISPLAY
float4 ModifySkyColorDebug(float4 color)
{
	if (_DebugLightingMode != 0)
	{
		switch (_DebugLightingMode)
		{
		case DEBUGLIGHTINGMODE_DIRECT_DIFFUSE:
			color.rgb = 0;
			break;

		case DEBUGLIGHTINGMODE_DIRECT_SPECULAR:
			color.rgb = 0;
			break;

		case DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE:
			color.rgb = 0;
			break;

		case DEBUGLIGHTINGMODE_REFLECTION:
			color.rgb = 0;
			break;

		case DEBUGLIGHTINGMODE_REFRACTION:
			break;

		case DEBUGLIGHTINGMODE_TRANSMITTANCE:
			color.rgb = 0;
			break;

		case DEBUGLIGHTINGMODE_EMISSIVE:
			break;
		}
	}

	return color;
}
#endif

#endif // __SKYDEBUGUTILS_H__
