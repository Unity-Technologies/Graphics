#ifndef UNITY_FILTER_UVRECT_INCLUDED
#define UNITY_FILTER_UVRECT_INCLUDED

// The atlas sub-rect this element's filter input occupies (xy = origin, zw = size, atlas 0-1 UV space).
// Set per draw call via MaterialPropertyBlock (UIRRenderTreeCompositor.cs's s_UVRects) -- the same
// property Shaders/Includes/UnityUIEFilter.cginc declares for hand-authored filters, redeclared here
// since ShaderGraph can't reach that engine-only include. Declared pregraph since NormalizeFilterUVNode
// and Filter Input's Local UV Space reference it directly by name, not through a Slot.
float4 unity_uie_UVRect[1];

#endif
