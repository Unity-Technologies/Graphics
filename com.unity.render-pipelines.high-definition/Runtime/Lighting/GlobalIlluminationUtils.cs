using UnityEngine.Experimental.GlobalIllumination;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    class GlobalIlluminationUtils
    {
        // Return true if the light must be added to the baking
        public static bool LightDataGIExtract(Light light, ref LightDataGI lightDataGI)
        {
            var add = light.GetComponent<HDAdditionalLightData>();
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalLightData;
            }

            Cookie cookie;
            LightmapperUtils.Extract(light, out cookie);
            lightDataGI.cookieID    = cookie.instanceID;
            lightDataGI.cookieScale = cookie.scale;

            // TODO: Currently color temperature is not handled at runtime, need to expose useColorTemperature publicly
            Color cct = new Color(1.0f, 1.0f, 1.0f);
#if UNITY_EDITOR
            if (add.useColorTemperature)
                cct = Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);
#endif

            // TODO: Only take into account the light dimmer when we have real time GI.
            lightDataGI.instanceID = light.GetInstanceID();
            LinearColor directColor, indirectColor;
            directColor = add.affectDiffuse ? LinearColor.Convert(light.color, light.intensity) : LinearColor.Black();
            directColor.red *= cct.r;
            directColor.green *= cct.g;
            directColor.blue *= cct.b;
            indirectColor = add.affectDiffuse ? LightmapperUtils.ExtractIndirect(light) : LinearColor.Black();
            indirectColor.red *= cct.r;
            indirectColor.green *= cct.g;
            indirectColor.blue *= cct.b;
#if UNITY_EDITOR
            LightMode lightMode = LightmapperUtils.Extract(light.lightmapBakeType);
#else
            LightMode lightMode = LightmapperUtils.Extract(light.bakingOutput.lightmapBakeType);
#endif

            lightDataGI.color = directColor;
            lightDataGI.indirectColor = indirectColor;

            // Note that the HDRI is correctly integrated in the GlobalIllumination system, we don't need to do anything regarding it.

            // The difference is that `l.lightmapBakeType` is the intent, e.g.you want a mixed light with shadowmask. But then the overlap test might detect more than 4 overlapping volumes and force a light to fallback to baked.
            // In that case `l.bakingOutput.lightmapBakeType` would be baked, instead of mixed, whereas `l.lightmapBakeType` would still be mixed. But this difference is only relevant in editor builds
#if UNITY_EDITOR
            lightDataGI.mode = LightmapperUtils.Extract(light.lightmapBakeType);
#else
            lightDataGI.mode = LightmapperUtils.Extract(light.bakingOutput.lightmapBakeType);
#endif

            lightDataGI.shadow = (byte)(light.shadows != LightShadows.None ? 1 : 0);

            HDLightType lightType = add.ComputeLightType(light);
            if (lightType != HDLightType.Area)
            {
                // For HDRP we need to divide the analytic light color by PI (HDRP do explicit PI division for Lambert, but built in Unity and the GI don't for punctual lights)
                // We apply it on both direct and indirect are they are separated, seems that direct is no used if we used mixed mode with indirect or shadowmask bake.
                lightDataGI.color.intensity         /= Mathf.PI;
                lightDataGI.indirectColor.intensity /= Mathf.PI;
                directColor.intensity               /= Mathf.PI;
                indirectColor.intensity             /= Mathf.PI;
            }

            switch (lightType)
            {
                case HDLightType.Directional:
                    lightDataGI.orientation = light.transform.rotation;
                    lightDataGI.position = light.transform.position;
                    lightDataGI.range = 0.0f;
                    lightDataGI.coneAngle = add.shapeWidth;
                    lightDataGI.innerConeAngle = add.shapeHeight;
#if UNITY_EDITOR
                    lightDataGI.shape0 = light.shadows != LightShadows.None ? (Mathf.Deg2Rad * light.shadowAngle) : 0.0f;
#else
                    lightDataGI.shape0 = 0.0f;
#endif
                    lightDataGI.shape1 = 0.0f;
                    lightDataGI.type = UnityEngine.Experimental.GlobalIllumination.LightType.Directional;
                    lightDataGI.falloff = FalloffType.Undefined;
                    lightDataGI.coneAngle = add.shapeWidth;
                    lightDataGI.innerConeAngle = add.shapeHeight;
                    break;

                case HDLightType.Spot:
                    switch (add.spotLightShape)
                    {
                        case SpotLightShape.Cone:
                        {
                            SpotLight spot;
                            spot.instanceID = light.GetInstanceID();
                            spot.shadow = light.shadows != LightShadows.None;
                            spot.mode = lightMode;
#if UNITY_EDITOR
                            spot.sphereRadius = light.shadows != LightShadows.None ? light.shadowRadius : 0.0f;
#else
                            spot.sphereRadius   = 0.0f;
#endif
                            spot.position = light.transform.position;
                            spot.orientation = light.transform.rotation;
                            spot.color = directColor;
                            spot.indirectColor = indirectColor;
                            spot.range = light.range;
                            spot.coneAngle = light.spotAngle * Mathf.Deg2Rad;
                            spot.innerConeAngle = light.spotAngle * Mathf.Deg2Rad * add.innerSpotPercent01;
                            spot.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                            spot.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                            lightDataGI.Init(ref spot, ref cookie);
                            lightDataGI.shape1 = (float)AngularFalloffType.AnalyticAndInnerAngle;
                            if (light.cookie != null)
                                lightDataGI.cookieID = light.cookie.GetInstanceID();
                            else if (add.IESSpot != null)
                                lightDataGI.cookieID = add.IESSpot.GetInstanceID();
                            else
                                lightDataGI.cookieID = 0;
                        }
                        break;

                        case SpotLightShape.Pyramid:
                        {
                            SpotLightPyramidShape pyramid;
                            pyramid.instanceID = light.GetInstanceID();
                            pyramid.shadow = light.shadows != LightShadows.None;
                            pyramid.mode = lightMode;
                            pyramid.position = light.transform.position;
                            pyramid.orientation = light.transform.rotation;
                            pyramid.color = directColor;
                            pyramid.indirectColor = indirectColor;
                            pyramid.range = light.range;
                            pyramid.angle = light.spotAngle * Mathf.Deg2Rad;
                            pyramid.aspectRatio = add.aspectRatio;
                            pyramid.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                            lightDataGI.Init(ref pyramid, ref cookie);
                            if (light.cookie != null)
                                lightDataGI.cookieID = light.cookie.GetInstanceID();
                            else if (add.IESSpot != null)
                                lightDataGI.cookieID = add.IESSpot.GetInstanceID();
                            else
                                lightDataGI.cookieID = 0;
                        }
                        break;

                        case SpotLightShape.Box:
                        {
                            SpotLightBoxShape box;
                            box.instanceID = light.GetInstanceID();
                            box.shadow = light.shadows != LightShadows.None;
                            box.mode = lightMode;
                            box.position = light.transform.position;
                            box.orientation = light.transform.rotation;
                            box.color = directColor;
                            box.indirectColor = indirectColor;
                            box.range = light.range;
                            box.width = add.shapeWidth;
                            box.height = add.shapeHeight;
                            lightDataGI.Init(ref box, ref cookie);
                            if (light.cookie != null)
                                lightDataGI.cookieID = light.cookie.GetInstanceID();
                            else if (add.IESSpot != null)
                                lightDataGI.cookieID = add.IESSpot.GetInstanceID();
                            else
                                lightDataGI.cookieID = 0;
                        }
                        break;

                        default:
                            Debug.Assert(false, "Encountered an unknown SpotLightShape.");
                            break;
                    }
                    break;

                case HDLightType.Point:
                    lightDataGI.orientation = light.transform.rotation;
                    lightDataGI.position = light.transform.position;
                    lightDataGI.range = light.range;
                    lightDataGI.coneAngle = 0.0f;
                    lightDataGI.innerConeAngle = 0.0f;

#if UNITY_EDITOR
                    lightDataGI.shape0 = light.shadows != LightShadows.None ? light.shadowRadius : 0.0f;
#else
                    lightDataGI.shape0 = 0.0f;
#endif
                    lightDataGI.shape1 = 0.0f;
                    lightDataGI.type = UnityEngine.Experimental.GlobalIllumination.LightType.Point;
                    lightDataGI.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                    break;

                case HDLightType.Area:
                    switch (add.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            lightDataGI.orientation = light.transform.rotation;
                            lightDataGI.position = light.transform.position;
                            lightDataGI.range = light.range;
                            lightDataGI.coneAngle = 0.0f;
                            lightDataGI.innerConeAngle = 0.0f;
#if UNITY_EDITOR
                            lightDataGI.shape0 = light.areaSize.x;
                            lightDataGI.shape1 = light.areaSize.y;
#else
                            lightDataGI.shape0 = 0.0f;
                            lightDataGI.shape1 = 0.0f;
#endif
                            // TEMP: for now, if we bake a rectangle type this will disable the light for runtime, need to speak with GI team about it!
                            lightDataGI.type = UnityEngine.Experimental.GlobalIllumination.LightType.Rectangle;
                            lightDataGI.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                            if (add.areaLightCookie != null)
                                lightDataGI.cookieID = add.areaLightCookie.GetInstanceID();
                            else if (add.IESSpot != null)
                                lightDataGI.cookieID = add.IESSpot.GetInstanceID();
                            else
                                lightDataGI.cookieID = 0;
                            break;

                        case AreaLightShape.Tube:
                            lightDataGI.InitNoBake(lightDataGI.instanceID);
                            break;

                        case AreaLightShape.Disc:
                            lightDataGI.orientation = light.transform.rotation;
                            lightDataGI.position = light.transform.position;
                            lightDataGI.range = light.range;
                            lightDataGI.coneAngle = 0.0f;
                            lightDataGI.innerConeAngle = 0.0f;
#if UNITY_EDITOR
                            lightDataGI.shape0 = light.areaSize.x;
                            lightDataGI.shape1 = light.areaSize.y;
#else
                            lightDataGI.shape0 = 0.0f;
                            lightDataGI.shape1 = 0.0f;
#endif
                            // TEMP: for now, if we bake a rectangle type this will disable the light for runtime, need to speak with GI team about it!
                            lightDataGI.type = UnityEngine.Experimental.GlobalIllumination.LightType.Disc;
                            lightDataGI.falloff = add.applyRangeAttenuation ? FalloffType.InverseSquared : FalloffType.InverseSquaredNoRangeAttenuation;
                            lightDataGI.cookieID = add.areaLightCookie ? add.areaLightCookie.GetInstanceID() : 0;
                            break;

                        default:
                            Debug.Assert(false, "Encountered an unknown AreaLightShape.");
                            break;
                    }
                    break;

                default:
                    Debug.Assert(false, "Encountered an unknown LightType.");
                    break;
            }

            return true;
        }

        static public Lightmapping.RequestLightsDelegate hdLightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
        {
            // Get all lights in the scene
            LightDataGI lightDataGI = new LightDataGI();
            for (int i = 0; i < requests.Length; i++)
            {
                Light light = requests[i];

                // For editor we need to discard realtime light as otherwise we get double contribution
                // At runtime for Enlighten we must keep realtime light but we can discard other light as they aren't used.

                // The difference is that `l.lightmapBakeType` is the intent, e.g.you want a mixed light with shadowmask. But then the overlap test might detect more than 4 overlapping volumes and force a light to fallback to baked.
                // In that case `l.bakingOutput.lightmapBakeType` would be baked, instead of mixed, whereas `l.lightmapBakeType` would still be mixed. But this difference is only relevant in editor builds
#if UNITY_EDITOR
                LightDataGIExtract(light, ref lightDataGI);
#else
                if (LightmapperUtils.Extract(light.bakingOutput.lightmapBakeType) == LightMode.Realtime)
                    LightDataGIExtract(light, ref lightDataGI);
                else
                    lightDataGI.InitNoBake(light.GetInstanceID());
#endif

                lightsOutput[i] = lightDataGI;
            }
        };
    }
}
