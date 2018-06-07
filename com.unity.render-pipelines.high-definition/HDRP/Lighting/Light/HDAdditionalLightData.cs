#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.Rendering.HDPipeline;
#endif
using UnityEngine.Serialization;
using System.Collections.Generic;

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
        [HideInInspector]
        public float version = 1.0f;

        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        public float directionalIntensity   = Mathf.PI; // In Lux
        public float punctualIntensity      = 600.0f;   // Light default to 600 lumen, i.e ~48 candela
        public float areaIntensity          = 200.0f;   // Light default to 200 lumen to better match point light

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
        }

        //
        //  Light Flags
        //
        [HideInInspector, SerializeField]
        private HDLightFlag[] m_LightFlags; 
        public HDLightFlag[]  LightFlags { get { ValidateLightFlags(); return m_LightFlags; } }

        void InvalidateLightFlags() { m_LightFlags = null; }
        void ValidateLightFlags()
        {
            List<HDLightFlag> flags = new List<HDLightFlag>();
            GetComponentsInChildren(flags);
            if (flags.Count == 0)
            {
                m_LightFlags = new HDLightFlag[0];
            }

            for (int i = flags.Count - 1; i >= 0; --i)
                if (flags[i].transform.parent != transform)
                    flags.RemoveAt(i);

            foreach (var f in flags)
                m_LightFlags = flags.ToArray();

            if (m_LightFlags == null)
                m_LightFlags = new HDLightFlag[0];
        }

        public HDLightFlag AddLightFlag(HDLightFlag copyFrom = null)
        {
            GameObject go = new GameObject("Flag", typeof(HDLightFlag));
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(go, "Add Light Flag");
            Undo.SetTransformParent(go.transform, transform, "Add Light Flag");
            EditorUtility.SetDirty(this);
#else
            go.transform.parent = transform.parent;
#endif
            var flag = go.GetComponent<HDLightFlag>();
            if (copyFrom == null)
            {
                flag.transform.localPosition = Vector3.zero;
                flag.transform.localRotation = Quaternion.identity;
                flag.m_Feather = 1;
            }
            else
            {
                flag.transform.localPosition = copyFrom.transform.localPosition;
                flag.transform.localRotation = copyFrom.transform.localRotation;
                flag.m_Feather = copyFrom.m_Feather;
            }

            return flag;
        }

        void OnTransformChildrenChanged() { InvalidateLightFlags(); }
    }
}
