using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class LightScriptCreator : MonoBehaviour
{
    public int gridWidth = 13;
    public int gridHeight = 7;

    [Space, Header("Resources")]
    public Texture2D cookie2D;
    public Cubemap cookieCube;

    public Material transparentShadowCastingMaterial;

    IEnumerable<Vector2Int> GetGridPositions()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                yield return new Vector2Int(x, y);
    }

    void Start()
    {
        Random.InitState(42);

        foreach (var position in GetGridPositions())
        {
            var go = new GameObject("Light " + position);
            go.transform.SetParent(transform, true);

            // Position the light in front of the plane
            go.transform.localPosition = new Vector3(position.x, position.y, -3);

            var hdLight = go.AddHDLight(LightType.Point);
            var light = go.GetComponent<Light>();

            // Set global parameters
            float intensityInLumens = 50.0f;
            light.intensity = LightUnitUtils.LumenToCandela(intensityInLumens, LightUnitUtils.SphereSolidAngle);
            hdLight.SetRange(1.01f);
            light.spotAngle = 60f;
            light.areaSize = new Vector2(0.5f, 0.5f);

            switch (position.y)
            {
                case 0: // Spot Box
                    light.type = LightType.Box;
                    hdLight.applyRangeAttenuation = false;
                    break;
                case 1: // Spot Pyramid
                    light.type = LightType.Pyramid;
                    light.innerSpotAngle = light.spotAngle;
                    break;
                case 2: // Spot Cone
                    light.type = LightType.Spot;
                    light.innerSpotAngle = 0f;
                    break;
                case 3: // Point
                    light.type = LightType.Point;
                    break;
                case 4: // Directional
                    light.type = LightType.Directional;
                    light.intensity = 0.01f;
                    break;
                case 5: // Rectangle
                    light.type = LightType.Rectangle;
                    light.intensity = LightUnitUtils.ConvertIntensity(light, intensityInLumens, LightUnit.Lumen, LightUnit.Nits) * 0.25f;
                    intensityInLumens /= 4;
                    break;
                case 6: // Tube
                    light.type = LightType.Tube;
                    intensityInLumens /= 2;
                    break;
                default:
                    break;
            }

            switch (position.x)
            {
                case 0: // Color
                    hdLight.SetColor(Random.ColorHSV(0, 1, .5f, 1, 1, 1));
                    break;
                case 1: // Intensity in by unit
                    light.intensity = light.intensity * Random.Range(.5f, 1f);
                    break;
                case 2: // Cookie
                    hdLight.SetCookie(light.type == LightType.Point ? (Texture)cookieCube : cookie2D);
                    break;
                case 3: // Range
                    hdLight.range *= Random.Range(0.5f, 0.8f); // Note spot box is not visible with this range
                    break;
                case 5: // Color temperature
                    hdLight.EnableColorTemperature(true);
                    hdLight.SetColor(hdLight.color, Random.Range(1000, 20000));
                    break;
                case 6: // Spot: Outer Angle / Inner Angle | Area Light: Set size | Box Spot: size
                    if (light.type == LightType.Box)
                        light.areaSize = new Vector2(0.1f, 0.6f);
                    else if (light.type == LightType.Pyramid)
                    {
                        const float aspectRatio = 2;
                        light.innerSpotAngle = 360f / Mathf.PI * Mathf.Atan(aspectRatio * Mathf.Tan(light.spotAngle * Mathf.PI / 360f));
                    }
                    else if (light.type == LightType.Spot)
                    {
                        light.spotAngle = 30;
                        light.innerSpotAngle = Random.Range(20, 90) * light.spotAngle / 100f;
                    }
                    else if (light.type.IsArea())
                    {
                        light.areaSize = new Vector2(0.1f, 1.5f);
                        light.intensity = LightUnitUtils.ConvertIntensity(light, intensityInLumens, LightUnit.Lumen, LightUnit.Nits);
                    }
                    break;
                case 7: // Volumetrics
                    hdLight.volumetricDimmer = 0;
                    hdLight.volumetricShadowDimmer = 0;
                    break;
                case 8: // Shadow resolution
                    // Generate an occluder in front of the light
                    hdLight.EnableShadows(true);
                    hdLight.SetShadowResolution(Mathf.NextPowerOfTwo(Random.Range(32, 512)));
                    var occluder = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    occluder.GetComponent<MeshRenderer>().sharedMaterial = transparentShadowCastingMaterial;
                    occluder.transform.SetParent(hdLight.transform, false);
                    occluder.transform.localPosition = new Vector3(0, 0, 0.5f);
                    occluder.transform.localScale = Vector3.one * 0.4f;
                    break;
                case 9: // Contact Shadows
                    hdLight.useContactShadow.useOverride = true;
                    hdLight.useContactShadow.@override = true;
                    break;
                case 10: // Light Layers
                    hdLight.lightlayersMask = UnityEngine.Rendering.HighDefinition.RenderingLayerMask.RenderingLayer2;
                    break;
                case 11: // Affect diffuse
                    hdLight.affectDiffuse = false;
                    break;
                case 12: // Range attenuation
                    hdLight.applyRangeAttenuation = false; // light should not be visible
                    break;
            }
        }
    }
}
