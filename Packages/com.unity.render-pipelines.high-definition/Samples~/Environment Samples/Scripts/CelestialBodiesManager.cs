using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteAlways]
public class CelestialBodiesManager : MonoBehaviour
{
    [System.Serializable]
    public class CelestialBodyData
    {
        public Light light;
        public HDAdditionalLightData hdLightData;
        public Transform transform;
        public float evaluatedIntensity;
        public float fadeFactor;
        public float shadowFadeFactor;
        public bool shadowEnabled;

        public float volumetricMultiplier = 1f;

        public CelestialBodyData(Light _light)
        {
            if (_light != null)
            {
                light = _light;
                transform = light.transform;
                hdLightData = light.GetComponent<HDAdditionalLightData>();
                evaluatedIntensity = light.intensity;
            }
            else
            {
                light = null;
                transform = null;
                hdLightData = null;
                evaluatedIntensity = 1f;
            }
            fadeFactor = 1f;
            shadowFadeFactor = 1f;
            shadowEnabled = false;
        }

        public float Evaluate(float fadeStart, float fadeEnd)
        {
            var angle = transform.eulerAngles.x;
            fadeFactor = GetHorizonMultiplier(angle, fadeStart, fadeEnd);

            shadowFadeFactor = fadeFactor;

            evaluatedIntensity = light.intensity * fadeFactor;

            return evaluatedIntensity;
        }

        

        public void ApplyFade(bool _shadowsEnabled)
        {
            if (light == null)
                return;

            hdLightData.EnableShadows(_shadowsEnabled);

            hdLightData.lightDimmer = fadeFactor;
            hdLightData.shadowDimmer = shadowFadeFactor;
            hdLightData.volumetricDimmer = fadeFactor * volumetricMultiplier;
            hdLightData.volumetricShadowDimmer = shadowFadeFactor;
        }

        public void ApplyFade()
        {
            ApplyFade(shadowEnabled);
        }
    }

    [SerializeField]
    private List<Light> celestialBodies = new List<Light>();

    private List<CelestialBodyData> bodiesData;

    [SerializeField]
    private Vector2 startEndDecreaseAngle = new Vector3(-15f, -20f);

    // Update is called once per frame
    void Update()
    {
        EvaluateBodies();

        SortBodies();

        ApplyFade();
    }

    private void EvaluateBodies()
    {
        foreach (var body in bodiesData)
        {
            body.Evaluate(startEndDecreaseAngle.x, startEndDecreaseAngle.y);
        }
    }

    private void SortBodies()
    {
        bodiesData.Sort((CelestialBodyData a, CelestialBodyData b) => a.evaluatedIntensity > b.evaluatedIntensity ? -1 : 1);
    }

    void ApplyFade()
    {
        // remap the two first bodies to a non overlapping sawtooth pattern
        if (bodiesData.Count > 1)
        {
            bodiesData[0].fadeFactor = bodiesData[0].fadeFactor * 2f - 1f;

            bodiesData[1].fadeFactor = Mathf.Clamp01(-bodiesData[0].fadeFactor);
            bodiesData[0].fadeFactor = Mathf.Clamp01(bodiesData[0].fadeFactor);

            bodiesData[0].shadowEnabled = bodiesData[0].fadeFactor > 0;
            bodiesData[1].shadowEnabled = bodiesData[1].fadeFactor > 0;
        }

        for (int i = 0; i<bodiesData.Count; i++)
        {
            // Disable light on bodies after two first ones
            if (i > 1)
            {
                bodiesData[i].fadeFactor = 0;
                bodiesData[i].shadowEnabled = false;
            }

            bodiesData[i].shadowFadeFactor = bodiesData[i].fadeFactor;

            // Only the first body has shadow enabled
            bodiesData[i].ApplyFade();
        }
    }

    private void OnValidate()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        bodiesData = new List<CelestialBodyData>();
        foreach (var light in celestialBodies)
            bodiesData.Add(new CelestialBodyData(light));
    }

    // Returns a float between 0 and 1
    public static float GetHorizonMultiplier(float angle, float fadeStart, float fadeEnd)
    {
        angle %= 360f;

        // after this the angle should be in the range [0;360] for all cases
        if (angle < 0f)
            angle += 360f;

        // Range [-180, 180]
        if (angle > 180f)
            angle -= 360;

        var sign = Mathf.Sign(angle);
        var abs = Mathf.Abs(angle);
        // Mirror over 90° to make it symmetrical
        if (abs > 90f)
            abs = 90f - abs;

        // Angle is now symmetric, ranging from -90° bellow the ground, 0° at the horizon an 90° at top
        angle = abs * sign;

        float factor = Mathf.Clamp01(Mathf.InverseLerp(fadeEnd, fadeStart, angle));

        return factor;
    }
}
