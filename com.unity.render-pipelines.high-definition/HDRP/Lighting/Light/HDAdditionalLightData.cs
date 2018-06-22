#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.Rendering.HDPipeline;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This enum extent the original LightType enum with new light type from HD
    public enum LightTypeExtent
    {
        Punctual, // Fallback on LightShape type
        Rectangle,
        Line,
        // Sphere,
        // Disc,
    };

    public enum SpotLightShape { Cone, Pyramid, Box };
    
    public enum LightUnit
    {
        Lumen,
        Candela,
        Lux,
        Luminance,
    }

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class HDAdditionalLightData : MonoBehaviour
    {
        [HideInInspector]
        public float version = 1.0f;

        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        public float directionalIntensity   = Mathf.PI; // In Lux
        public float punctualIntensity      = 600.0f;   // Light default to 600 lumen, i.e ~48 candela
        public float areaIntensity          = 200.0f;   // Light default to 200 lumen to better match point light
        
        public float intensity
        {
            get { return editorLightIntensity; }
            set { SetLightIntensity(value); }
        }

        // Only for Spotlight, should be hide for other light
        public bool enableSpotReflector = false;

        [Range(0.0f, 100.0f)]
        public float m_InnerSpotPercent = 0.0f; // To display this field in the UI this need to be public

        public float GetInnerSpotPercent01()
        {
            return Mathf.Clamp(m_InnerSpotPercent, 0.0f, 100.0f) / 100.0f;
        }

        [Range(0.0f, 1.0f)]
        public float lightDimmer = 1.0f;

        [Range(0.0f, 1.0f)]
        public float volumetricDimmer = 1.0f;

        // Used internally to convert any light unit input into light intensity
        public LightUnit lightUnit;

        // Not used for directional lights.
        public float fadeDistance = 10000.0f;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

        // This property work only with shadow mask and allow to say we don't render any lightMapped object in the shadow map
        public bool nonLightmappedOnly = false;

        public LightTypeExtent lightTypeExtent = LightTypeExtent.Punctual;

        // Only for Spotlight, should be hide for other light
        public SpotLightShape spotLightShape = SpotLightShape.Cone;

        // Only for Rectangle/Line/box projector lights
        public float shapeWidth = 0.5f;

        // Only for Rectangle/box projector lights
        public float shapeHeight = 0.5f;

        // Only for pyramid projector
        public float aspectRatio = 1.0f;

        // Only for Sphere/Disc
        public float shapeRadius = 0.0f;

        // Only for Spot/Point - use to cheaply fake specular spherical area light
        [Range(0.0f, 1.0f)]
        public float maxSmoothness = 1.0f;

        // If true, we apply the smooth attenuation factor on the range attenuation to get 0 value, else the attenuation is just inverse square and never reach 0
        public bool applyRangeAttenuation = true;

        // This is specific for the LightEditor GUI and not use at runtime
        public bool useOldInspector = false;
        public bool featuresFoldout = true;
        public bool showAdditionalSettings = false;
        public float editorLightIntensity;

        // Runtime datas used to compute light intensity
        Light       _light;
        Light       m_Light
        {
            get
            {
                if (_light == null)
                    _light = GetComponent<Light>();
                return _light;
            }
        }
        
        void SetLightIntensity(float intensity)
        {
            switch (lightTypeExtent)
            {
                case LightTypeExtent.Punctual:
                    SetLightIntensityPunctual(intensity);
                    break;
                case LightTypeExtent.Line:
                    if (lightUnit == LightUnit.Lumen)
                        m_Light.intensity = LightUtils.CalculateLineLightIntensity(intensity, shapeWidth);
                    else
                        m_Light.intensity = intensity;
                    break;
                case LightTypeExtent.Rectangle:
                    if (lightUnit == LightUnit.Lumen)
                        m_Light.intensity = LightUtils.ConvertRectLightIntensity(intensity, shapeWidth, shapeHeight);
                    else
                        m_Light.intensity = intensity;
                    break;
            }
        }

        void SetLightIntensityPunctual(float intensity)
        {
            switch (m_Light.type)
            {
                case LightType.Directional:
                    m_Light.intensity = intensity; // Alwas in lux
                    break;
                case LightType.Point:
                    if (lightUnit == LightUnit.Candela)
                        m_Light.intensity = intensity;
                    else
                        m_Light.intensity = LightUtils.ConvertPointLightIntensity(intensity);
                    break;
                case LightType.Spot:
                    if (lightUnit == LightUnit.Candela)
                        m_Light.intensity = intensity;
                    else if (enableSpotReflector)
                    {
                        if (spotLightShape == SpotLightShape.Cone)
                        {
                            m_Light.intensity = LightUtils.ConvertSpotLightIntensity(intensity, m_Light.spotAngle * Mathf.Deg2Rad, true);
                        }
                        else if (spotLightShape == SpotLightShape.Pyramid)
                        {
                            float angleA, angleB;
                            LightUtils.CalculateAnglesForPyramid(aspectRatio, m_Light.spotAngle,
                                out angleA, out angleB);

                            m_Light.intensity = LightUtils.ConvertFrustrumLightIntensity(intensity, angleA, angleB);
                        }
                        else // Box shape, fallback to punctual light.
                        {
                            m_Light.intensity = LightUtils.ConvertPointLightIntensity(intensity);
                        }
                    }
                    else // Reflector disabled, fallback to punctual light.
                    {
                        m_Light.intensity = LightUtils.ConvertPointLightIntensity(intensity);
                    }
                    break;
            }
        }

#if UNITY_EDITOR

        private void DrawGizmos(bool selected)
        {
            var light = gameObject.GetComponent<Light>();
            var gizmoColor = light.color;
            gizmoColor.a = selected ? 1.0f : 0.3f; // Fade for the gizmo
            Gizmos.color = Handles.color = gizmoColor;

            if (lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch (light.type)
                {
                    case LightType.Directional:
                        HDLightEditorUtilities.DrawDirectionalLightGizmo(light);
                        break;
                    case LightType.Point:
                        HDLightEditorUtilities.DrawPointlightGizmo(light, selected);
                        break;
                    case LightType.Spot:
                        if (spotLightShape == SpotLightShape.Cone)
                            HDLightEditorUtilities.DrawSpotlightGizmo(light, selected);
                        else if (spotLightShape == SpotLightShape.Pyramid)
                            HDLightEditorUtilities.DrawFrustumlightGizmo(light);
                        else if (spotLightShape == SpotLightShape.Box)
                            HDLightEditorUtilities.DrawFrustumlightGizmo(light);
                        break;
                }
            }
            else
            {
                switch (lightTypeExtent)
                {
                    case LightTypeExtent.Rectangle:
                        HDLightEditorUtilities.DrawArealightGizmo(light);
                        break;
                    case LightTypeExtent.Line:
                        HDLightEditorUtilities.DrawArealightGizmo(light);
                        break;
                }
            }

            if (selected)
            {
                DrawVerticalRay();
            }
        }

        // Trace a ray down to better locate the light location
        private void DrawVerticalRay()
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Handles.color = Color.green;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawLine(transform.position, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);

                Handles.color = Color.red;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawLine(transform.position, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
            }
        }

        private void OnDrawGizmos()
        {
            // DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        public void RefreshLightIntensity()
        {
            // The editor can only access editorLightIntensity (because of SerializedProperties) so we update the intensity to get the real value
            intensity = editorLightIntensity;
        }

#endif

        // Caution: this function must match the one in HDLightEditor.UpdateLightIntensity - any change need to be replicated
        public void ConvertPhysicalLightIntensityToLightIntensity()
        {
            var light = m_Light;

            if (lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch (light.type)
                {
                    case LightType.Directional:
                        light.intensity = Mathf.Max(0, directionalIntensity);
                        break;

                    case LightType.Point:
                        light.intensity = LightUtils.ConvertPointLightIntensity(Mathf.Max(0, punctualIntensity));
                        break;

                    case LightType.Spot:

                        if (enableSpotReflector)
                        {
                            if (spotLightShape == SpotLightShape.Cone)
                            {
                                light.intensity = LightUtils.ConvertSpotLightIntensity(Mathf.Max(0, punctualIntensity), light.spotAngle * Mathf.Deg2Rad, true);
                            }
                            else if (spotLightShape == SpotLightShape.Pyramid)
                            {
                                float angleA, angleB;
                                LightUtils.CalculateAnglesForPyramid(aspectRatio, light.spotAngle,
                                    out angleA, out angleB);

                                light.intensity = LightUtils.ConvertFrustrumLightIntensity(Mathf.Max(0, punctualIntensity), angleA, angleB);
                            }
                            else // Box shape, fallback to punctual light.
                            {
                                light.intensity = LightUtils.ConvertPointLightIntensity(Mathf.Max(0, punctualIntensity));
                            }
                        }
                        else
                        {
                            // Spot should used conversion which take into account the angle, and thus the intensity vary with angle.
                            // This is not easy to manipulate for lighter, so we simply consider any spot light as just occluded point light. So reuse the same code.
                            light.intensity = LightUtils.ConvertPointLightIntensity(Mathf.Max(0, punctualIntensity));
                            // TODO: What to do with box shape ?
                            // var spotLightShape = (SpotLightShape)m_AdditionalspotLightShape.enumValueIndex;
                        }
                        break;
                }
            }
            else if (lightTypeExtent == LightTypeExtent.Rectangle)
            {
                light.intensity = LightUtils.ConvertRectLightIntensity(Mathf.Max(0, areaIntensity), shapeWidth, shapeHeight);
            }
            else if (lightTypeExtent == LightTypeExtent.Line)
            {
                light.intensity = LightUtils.CalculateLineLightIntensity(Mathf.Max(0, areaIntensity), shapeWidth);
            }
        }

        // As we have our own default value, we need to initialize the light intensity correctly
        public static void InitDefaultHDAdditionalLightData(HDAdditionalLightData lightData)
        {
            // Special treatment for Unity built-in area light. Change it to our rectangle light
            var light = lightData.gameObject.GetComponent<Light>();

            // Sanity check: lightData.lightTypeExtent is init to LightTypeExtent.Punctual (in case for unknow reasons we recreate additional data on an existing line)
            if (light.type == LightType.Area && lightData.lightTypeExtent == LightTypeExtent.Punctual)
            {
                lightData.lightTypeExtent = LightTypeExtent.Rectangle;
                light.type = LightType.Point; // Same as in HDLightEditor
#if UNITY_EDITOR
                light.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            }

            // We don't use the global settings of shadow mask by default
            light.lightShadowCasterMode = LightShadowCasterMode.Everything;

            // At first init we need to initialize correctly the default value
            lightData.ConvertPhysicalLightIntensityToLightIntensity();

            // TODO: Initialize the light intensity in function of the light type
        }
    }
}
