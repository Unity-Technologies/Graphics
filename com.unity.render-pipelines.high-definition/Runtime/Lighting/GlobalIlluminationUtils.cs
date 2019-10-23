using UnityEngine.Experimental.GlobalIllumination;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    class GlobalIlluminationUtils
    {
        // Return true if the light must be added to the baking
        public static bool LightDataGIExtract(Light l, ref LightDataGI ld)
        {
            var add = l.GetComponent<HDAdditionalLightData>();
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalLightData;
            }

            // TODO: Currently color temperature is not handled at runtime, need to expose useColorTemperature publicly
            Color cct = new Color(1.0f, 1.0f, 1.0f);
#if UNITY_EDITOR
            if (add.useColorTemperature)
                cct = Mathf.CorrelatedColorTemperatureToRGB(l.colorTemperature);
#endif

            // TODO: Only take into account the light dimmer when we have real time GI.
            ld.instanceID = l.GetInstanceID();
            LinearColor directColor, indirectColor;
            directColor = add.affectDiffuse ? LinearColor.Convert(l.color, l.intensity) : LinearColor.Black();
            directColor.red *= cct.r;
            directColor.green *= cct.g;
            directColor.blue *= cct.b;
            indirectColor = add.affectDiffuse ? LightmapperUtils.ExtractIndirect(l) : LinearColor.Black();
            indirectColor.red *= cct.r;
            indirectColor.green *= cct.g;
            indirectColor.blue *= cct.b;
#if UNITY_EDITOR
            LightMode lightMode = LightmapperUtils.Extract(l.lightmapBakeType);
#else
            LightMode lightMode = LightmapperUtils.Extract(l.bakingOutput.lightmapBakeType);
#endif

            ld.color = directColor;
            ld.indirectColor = indirectColor;

            // Note that the HDRI is correctly integrated in the GlobalIllumination system, we don't need to do anything regarding it.

            // The difference is that `l.lightmapBakeType` is the intent, e.g.you want a mixed light with shadowmask. But then the overlap test might detect more than 4 overlapping volumes and force a light to fallback to baked.
            // In that case `l.bakingOutput.lightmapBakeType` would be baked, instead of mixed, whereas `l.lightmapBakeType` would still be mixed. But this difference is only relevant in editor builds
#if UNITY_EDITOR
            ld.mode = LightmapperUtils.Extract(l.lightmapBakeType);
#else
            ld.mode = LightmapperUtils.Extract(l.bakingOutput.lightmapBakeType);
#endif

            ld.shadow = (byte)(l.shadows != LightShadows.None ? 1 : 0);

            if (add.lightTypeExtent == LightTypeExtent.Punctual)
            {
                // For HDRP we need to divide the analytic light color by PI (HDRP do explicit PI division for Lambert, but built in Unity and the GI don't for punctual lights)
                // We apply it on both direct and indirect are they are separated, seems that direct is no used if we used mixed mode with indirect or shadowmask bake.
                ld.color.intensity          /= Mathf.PI;
                ld.indirectColor.intensity  /= Mathf.PI;
                directColor.intensity       /= Mathf.PI;
                indirectColor.intensity     /= Mathf.PI;

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
                        switch (add.spotLightShape)
                        {
                            case SpotLightShape.Cone:
                                {
                                    SpotLight spot;
                                    spot.instanceID = l.GetInstanceID();
                                    spot.shadow = l.shadows != LightShadows.None;
                                    spot.mode = lightMode;
#if UNITY_EDITOR
                                    spot.sphereRadius = l.shadows != LightShadows.None ? l.shadowRadius : 0.0f;
#else
                                    spot.sphereRadius   = 0.0f;
#endif
                                    spot.position = l.transform.position;
                                    spot.orientation = l.transform.rotation;
                                    spot.color = directColor;
                                    spot.indirectColor = indirectColor;
                                    spot.range = l.range;
                                    spot.coneAngle = l.spotAngle * Mathf.Deg2Rad;
                                    spot.innerConeAngle = l.spotAngle * Mathf.Deg2Rad * add.innerSpotPercent01;
                                    spot.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                                    spot.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                                    ld.Init(ref spot);
                                    ld.shape1 = (float)AngularFalloffType.AnalyticAndInnerAngle;
                                }
                                break;
                            case SpotLightShape.Pyramid:
                                {
                                    SpotLightPyramidShape pyramid;
                                    pyramid.instanceID = l.GetInstanceID();
                                    pyramid.shadow = l.shadows != LightShadows.None;
                                    pyramid.mode = lightMode;
                                    pyramid.position = l.transform.position;
                                    pyramid.orientation = l.transform.rotation;
                                    pyramid.color = directColor;
                                    pyramid.indirectColor = indirectColor;
                                    pyramid.range = l.range;
                                    pyramid.angle = l.spotAngle * Mathf.Deg2Rad;
                                    pyramid.aspectRatio = add.aspectRatio;
                                    pyramid.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                                    ld.Init(ref pyramid);
                                }
                                break;
                            case SpotLightShape.Box:
                                {
                                    SpotLightBoxShape box;
                                    box.instanceID = l.GetInstanceID();
                                    box.shadow = l.shadows != LightShadows.None;
                                    box.mode = lightMode;
                                    box.position = l.transform.position;
                                    box.orientation = l.transform.rotation;
                                    box.color = directColor;
                                    box.indirectColor = indirectColor;
                                    box.range = l.range;
                                    box.width = add.shapeWidth;
                                    box.height = add.shapeHeight;
                                    ld.Init(ref box);
                                }
                                break;
                            default:
                                Debug.Assert(false, "Encountered an unknown SpotLightShape.");
                                break;
                        }
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
                    case LightType.Rectangle:
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
                ld.type = UnityEngine.Experimental.GlobalIllumination.LightType.Rectangle;
                ld.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
            }
            else if (add.lightTypeExtent == LightTypeExtent.Tube)
            {
                ld.InitNoBake(ld.instanceID);
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

                // For editor we need to discard realtime light as otherwise we get double contribution
                // At runtime for Enlighten we must keep realtime light but we can discard other light as they aren't used.

                // The difference is that `l.lightmapBakeType` is the intent, e.g.you want a mixed light with shadowmask. But then the overlap test might detect more than 4 overlapping volumes and force a light to fallback to baked.
                // In that case `l.bakingOutput.lightmapBakeType` would be baked, instead of mixed, whereas `l.lightmapBakeType` would still be mixed. But this difference is only relevant in editor builds
#if UNITY_EDITOR
                LightDataGIExtract(l, ref ld);
#else
                if (LightmapperUtils.Extract(l.bakingOutput.lightmapBakeType) == LightMode.Realtime)
                    LightDataGIExtract(l, ref ld);
                else
                    ld.InitNoBake(l.GetInstanceID());
#endif

                lightsOutput[i] = ld;
            }
        };
    }
}
