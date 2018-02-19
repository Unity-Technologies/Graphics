using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class GlobalIlluminationUtils
    {
        // Return true if the light must be added to the baking
        public static bool LightDataGIExtract(Light l, ref LightDataGI ld)
        {
            var add = l.GetComponent<HDAdditionalLightData>();
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalLightData;
            }

            // TODO: Only take into account the light dimmer when we have real time GI.

            ld.instanceID = l.GetInstanceID();
            ld.color = add.affectDiffuse ? LinearColor.Convert(l.color, l.intensity) : LinearColor.Black();
            ld.indirectColor = add.affectDiffuse ? LightmapperUtils.ExtractIndirect(l) : LinearColor.Black();

            // For HDRP we need to divide the analytic light color by PI (HDRP do explicit PI division for Lambert, but built in Unity and the GI don't)
            // We apply it on both direct and indirect are they are separated, seems that direct is no used if we used mixed mode with indirect or shadowmask bake.
            ld.color.red /= Mathf.PI;
            ld.color.green /= Mathf.PI;
            ld.color.blue /= Mathf.PI;

            ld.indirectColor.red /= Mathf.PI;
            ld.indirectColor.green /= Mathf.PI;
            ld.indirectColor.blue /= Mathf.PI;

            // Note that the HDRI is correctly integrated in the GlobalIllumination system, we don't need to do anything regarding it.

#if UNITY_EDITOR
            ld.mode = LightmapperUtils.Extract(l.lightmapBakeType);
#else
            ld.mode = LightMode.Realtime;
#endif
            ld.shadow = (byte)(l.shadows != LightShadows.None ? 1 : 0);

            if (add.lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch (l.type)
                {
                    case LightType.Directional:
                        ld.orientation.SetLookRotation(l.transform.forward, Vector3.up);
                        ld.position = Vector3.zero;
                        ld.range = 0.0f;
                        ld.coneAngle = 0.0f;
                        ld.innerConeAngle = 0.0f;
#if UNITY_EDITOR
                        ld.shape0 = l.shadows != LightShadows.None ? (Mathf.Deg2Rad * l.shadowAngle) : 0.0f;
#else
                        ld.shape0 = 0.0f;
#endif
                        ld.shape1 = 0.0f;
                        ld.type = UnityEngine.Experimental.GlobalIllumination.LightType.Directional;
                        ld.falloff = FalloffType.Undefined;
                        break;

                    case LightType.Spot:

                        ld.orientation = l.transform.rotation;
                        ld.position = l.transform.position;
                        ld.range = l.range;
                        ld.coneAngle = l.spotAngle * Mathf.Deg2Rad; // coneAngle is the full angle
                        ld.innerConeAngle = l.spotAngle * Mathf.Deg2Rad * add.GetInnerSpotPercent01();
#if UNITY_EDITOR
                        ld.shape0 = l.shadows != LightShadows.None ? l.shadowRadius : 0.0f;
#else
                        ld.shape0 = 0.0f;
#endif
                        ld.shape1 = 0.0f;
                        ld.type = UnityEngine.Experimental.GlobalIllumination.LightType.Spot;
                        ld.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;

                        /*
                        switch (add.spotLightShape)
                        {
                            case SpotLightShape.Cone:
                                break;
                            case SpotLightShape.Pyramid:
                                break;
                            case SpotLightShape.Box:
                                break;
                            default:
                                Debug.Assert(false, "Encountered an unknown SpotLightShape.");
                                break;
                        }
                        */
                        break;

                    case LightType.Point:
                        ld.orientation = Quaternion.identity;
                        ld.position = l.transform.position;
                        ld.range = l.range;
                        ld.coneAngle = 0.0f;
                        ld.innerConeAngle = 0.0f;

#if UNITY_EDITOR
                        ld.shape0 = l.shadows != LightShadows.None ? l.shadowRadius : 0.0f;
#else
                        ld.shape0 = 0.0f;
#endif
                        ld.shape1 = 0.0f;
                        ld.type = UnityEngine.Experimental.GlobalIllumination.LightType.Point;
                        ld.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                        break;

                    // Note: We don't support this type in HDRP, but ini just in case
                    case LightType.Area:
                        ld.orientation = l.transform.rotation;
                        ld.position = l.transform.position;
                        ld.range = l.range;
                        ld.coneAngle = 0.0f;
                        ld.innerConeAngle = 0.0f;
#if UNITY_EDITOR
                        ld.shape0 = l.areaSize.x;
                        ld.shape1 = l.areaSize.y;
#else
                ld.shape0 = 0.0f;
                ld.shape1 = 0.0f;
#endif
                        ld.type = UnityEngine.Experimental.GlobalIllumination.LightType.Rectangle;
                        ld.falloff = FalloffType.Undefined;
                        break;

                    default:
                        Debug.Assert(false, "Encountered an unknown LightType.");
                        break;
                }
            }
            else if (add.lightTypeExtent == LightTypeExtent.Rectangle)
            {
                ld.orientation = l.transform.rotation;
                ld.position = l.transform.position;
                ld.range = l.range;
                ld.coneAngle = 0.0f;
                ld.innerConeAngle = 0.0f;
#if UNITY_EDITOR
                ld.shape0 = l.areaSize.x;
                ld.shape1 = l.areaSize.y;
#else
                ld.shape0 = 0.0f;
                ld.shape1 = 0.0f;
#endif
                // TEMP: for now, if we bake a rectangle type this will disable the light for runtime, need to speak with GI team about it!
                // ld.type = UnityEngine.Experimental.GlobalIllumination.LightType.Rectangle;
                ld.type = UnityEngine.Experimental.GlobalIllumination.LightType.Point;
                ld.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
            }
            else if (add.lightTypeExtent == LightTypeExtent.Line)
            {

            }
            else
            {
                Debug.Assert(false, "Encountered an unknown LightType.");
            }

            return true;
        }

        static public Lightmapping.RequestLightsDelegate hdLightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
        {
            // Get all lights in the scene
            LightDataGI ld = new LightDataGI();
            for (int i = 0; i < requests.Length; i++)
            {
                Light l = requests[i];
                LightDataGIExtract(l, ref ld);
                lightsOutput[i] = ld;
            }
        };
    }
}
