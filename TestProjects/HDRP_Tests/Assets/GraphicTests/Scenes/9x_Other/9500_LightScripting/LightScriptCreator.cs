using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition;

public class LightScriptCreator : MonoBehaviour
{
    public int          gridWidth = 13;
    public int          gridHeight = 7;

    [Space, Header("Resources")]
    public Texture2D    cookie2D;
    public Cubemap      cookieCube;

    public Material     transparentShadowCastingMaterial;

    IEnumerable< Vector2Int > GetGridPositions()
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
            
            var hdLight = go.AddHDLight(HDLightTypeAndShape.Point);

            // Set global parameters
            hdLight.SetIntensity(50);
            hdLight.SetRange(1.01f);
            hdLight.SetSpotAngle(60);

            switch (position.y)
            {
                case 0: // Spot Box
                    hdLight.SetLightTypeAndShape(HDLightTypeAndShape.BoxSpot);
                    break;
                case 1: // Spot Pyramid
                    hdLight.SetLightTypeAndShape(HDLightTypeAndShape.PyramidSpot);
                    break;
                case 2: // Spot Cone
                    hdLight.SetLightTypeAndShape(HDLightTypeAndShape.ConeSpot);
                    break;
                case 3: // Point
                    hdLight.SetLightTypeAndShape(HDLightTypeAndShape.Point);
                    break;
                case 4: // Directional
                    hdLight.SetLightTypeAndShape(HDLightTypeAndShape.Directional);
                    hdLight.SetIntensity(0.01f);
                    break;
                case 5: // Rectangle
                    hdLight.SetLightTypeAndShape(HDLightTypeAndShape.RectangleArea);
                    hdLight.intensity /= 4;
                    break;
                case 6: // Tube
                    hdLight.SetLightTypeAndShape(HDLightTypeAndShape.TubeArea);
                    hdLight.intensity /= 2;
                    break;
                default:
                    break;
            }

            var supportedLightUnits = hdLight.GetSupportedLightUnits();
            var type = hdLight.GetLightTypeAndShape();

            switch (position.x)
            {
                case 0: // Color
                    hdLight.SetColor(Random.ColorHSV(0, 1, .5f, 1, 1, 1));
                    break;
                case 1: // Intensity in by unit
                    hdLight.SetIntensity(hdLight.intensity * Random.Range(.5f, 1f), supportedLightUnits[0]);
                    break;
                case 2: // Cookie
                    hdLight.SetCookie(type == HDLightTypeAndShape.Point ? (Texture)cookieCube : cookie2D);
                    break;
                case 3: // Range
                    hdLight.range *= Random.Range(0.5f, 0.8f); // Note spot box is not visible with this range
                    break;
                case 4: // Light Unit
                    if (type != HDLightTypeAndShape.Directional)
                        hdLight.SetLightUnit(supportedLightUnits.Length > 1 ? supportedLightUnits[1] : supportedLightUnits[0]);
                    break;
                case 5: // Color temperature
                    hdLight.EnableColorTemperature(true);
                    hdLight.SetColor(hdLight.color, Random.Range(1000, 20000));
                    break;
                case 6: // Spot: Outer Angle / Inner Angle | Area Light: Set size | Box Spot: size
                    if (type == HDLightTypeAndShape.BoxSpot)
                        hdLight.SetBoxSpotSize(new Vector2(0.1f, 0.6f));
                    else if (type == HDLightTypeAndShape.PyramidSpot)
                        hdLight.aspectRatio = 2;
                    else if (type == HDLightTypeAndShape.ConeSpot)
                        hdLight.SetSpotAngle(30, Random.Range(20, 90));
                    else if (type.IsArea())
                        hdLight.SetAreaLightSize(new Vector2(0.1f, 1.5f));
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
                    occluder.GetComponent< MeshRenderer >().sharedMaterial = transparentShadowCastingMaterial;
                    occluder.transform.SetParent(hdLight.transform, false);
                    occluder.transform.localPosition = new Vector3(0, 0, 0.5f);
                    occluder.transform.localScale = Vector3.one * 0.4f;
                    break;
                case 9: // Contact Shadows
                    hdLight.useContactShadow.useOverride = true;
                    hdLight.useContactShadow.@override = true;
                    break;
                case 10: // Light Layer
                    hdLight.lightlayersMask = LightLayerEnum.LightLayer1;
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
