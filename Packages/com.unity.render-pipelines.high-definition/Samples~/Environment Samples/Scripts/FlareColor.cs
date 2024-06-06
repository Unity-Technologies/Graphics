using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class FlareColor : MonoBehaviour
{
    public LensFlareDataSRP lensFlareData = null;
    public string lensFlareIndexes = "0";
    public float defaultFlareIntensity = 1f;
    
    public bool attenuateMoonFlareWithDaylight = false;
    public bool colorSunFlareWithSky = false;
    public bool killFlareWhenBelowHorizon = false;

    LensFlareComponentSRP lensFlareComponent = null;
    int[] indexes;
    Color color;

    void Start()
    {
        Setup();
    }
    void OnValidate()
    {
        Setup();
    }

    void Setup()
    {
        string[] indexesString = lensFlareIndexes.Split(',');
        indexes = new int[indexesString.Length];
        int i = 0;
        foreach (string s in indexesString)
        {
            indexes[i] = Int32.Parse(s);
            i++;
        }

        lensFlareComponent = this.GetComponent<LensFlareComponentSRP>();
    }

    // This sample the ambient probe in the opposite direction the gameobject is pointing;
    Color GetSkyColor()
    {
        var ambientProbe = UnityEngine.RenderSettings.ambientProbe;
        Color[] results = { Color.black };
        Vector3[] directions = { -this.transform.forward };
        directions[0] = Vector3.up;
        ambientProbe.Evaluate(directions, results);
        return results[0];
    }

    // Update is called once per frame
    void Update()
    {
    
        // We basically multiply by the one minus version of the skyColor (giving black at day time)
        if (attenuateMoonFlareWithDaylight)
        {
            Color skyColor = GetSkyColor();
            Color color = Color.white - skyColor;
            color = saturateColor(color);
            foreach(int i in indexes)
                lensFlareData.elements[i].tint = color;
        }

        if (colorSunFlareWithSky)
        {
            Color skyColor = GetSkyColor();
            float sum = skyColor.r + skyColor.g + skyColor.b;
            float reciprocal = 1f / sum;
            Color color = skyColor * reciprocal;
            color.a = 1;
            foreach (int i in indexes)
                lensFlareData.elements[i].tint = color;
        }

        if (killFlareWhenBelowHorizon)
        {
            float flareMultiplier = CelestialBodiesManager.GetHorizonMultiplier(transform.eulerAngles.x, 0, -1);
            lensFlareComponent.intensity = defaultFlareIntensity * flareMultiplier;
        }

    }

    public float getFlareHorizonMultiplier(float fadeStart, float fadeEnd)
    {
        var angle = transform.eulerAngles.x;
        angle %= 360f;

        // after this the angle should be in the range [0;360] for all cases
        if (angle < 0f)
            angle += 360f;

        // Range [-180, 180]
        if (angle > 180f)
            angle -= 360;

        var sign = Mathf.Sign(angle);
        var abs = Mathf.Abs(angle);
        // Mirror over 90� to make it symmetrical
        if (abs > 90f)
            abs = 90f - abs;

        // Angle is now symetric, ranging from -90 bellow the ground, 0� at the horizon an 90 at top
        angle = abs * sign;

        float fadeFactor = Mathf.Clamp01(Mathf.InverseLerp(fadeEnd, fadeStart, angle));

        return fadeFactor;
    }

    // This to avoid having negative colors when doing one minus from SkyColor.
    Color saturateColor(Color c)
    {
        c.r = c.r < 0 ? 0 : c.r;
        c.g = c.g < 0 ? 0 : c.g;
        c.b = c.b < 0 ? 0 : c.b;
        c.a = c.a < 0 ? 0 : c.a;

        return c;
    }

}
