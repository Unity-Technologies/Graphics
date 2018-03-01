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

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class HDAdditionalLightData : MonoBehaviour
    {
 #pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        // We can't rely on Unity for our additional data, we need to version it ourself.
        [SerializeField]
        float m_Version = 1.0f;
 #pragma warning restore 414

        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        public float directionalIntensity   = Mathf.PI; // In Lux
        public float punctualIntensity      = 600.0f;   // Light default to 600 lumen, i.e ~48 candela
        public float areaIntensity          = 200.0f;   // Light default to 200 lumen to better match point light

        [Range(0.0f, 100.0f)]
        public float m_InnerSpotPercent = 0.0f; // To display this field in the UI this need to be public

        public float GetInnerSpotPercent01()
        {
            return Mathf.Clamp(m_InnerSpotPercent, 0.0f, 100.0f) / 100.0f;
        }

        [Range(0.0f, 1.0f)]
        public float lightDimmer = 1.0f;

        // Not used for directional lights.
        public float fadeDistance = 10000.0f;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

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
#endif

        // Caution: this function must match the one in HDLightEditor.UpdateLightIntensity - any change need to be replicated
        public void ConvertPhysicalLightIntensityToLightIntensity()
        {
            var light = gameObject.GetComponent<Light>();

            if (lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch (light.type)
                {
                    case LightType.Directional:
                        light.intensity = directionalIntensity;
                        break;

                    case LightType.Point:
                        light.intensity = LightUtils.ConvertPointLightIntensity(punctualIntensity);
                        break;

                    case LightType.Spot:
                        // Spot should used conversion which take into account the angle, and thus the intensity vary with angle.
                        // This is not easy to manipulate for lighter, so we simply consider any spot light as just occluded point light. So reuse the same code.
                        light.intensity = LightUtils.ConvertPointLightIntensity(punctualIntensity);
                        // TODO: What to do with box shape ?
                        // var spotLightShape = (SpotLightShape)m_AdditionalspotLightShape.enumValueIndex;
                        break;

                }
            }
            else if (lightTypeExtent == LightTypeExtent.Rectangle)
            {
                light.intensity = LightUtils.ConvertRectLightIntensity(areaIntensity, shapeWidth, shapeHeight);
            }
            else if (lightTypeExtent == LightTypeExtent.Line)
            {
                light.intensity = LightUtils.CalculateLineLightIntensity(areaIntensity, shapeWidth);
            }
        }

        // As we have our own default value, we need to initialize the light intensity correctly
        public static void InitDefaultHDAdditionalLightData(HDAdditionalLightData lightData)
        {
            // At first init we need to initialize correctly the default value
            lightData.ConvertPhysicalLightIntensityToLightIntensity();
        }

    }
}
