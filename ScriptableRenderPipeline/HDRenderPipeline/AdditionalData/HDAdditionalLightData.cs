#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum LightShape
    {
        Directional = 0,
        Point = 1,
        Spot = 2,        
        Rectangle = 3,
        Line = 4,
        // Sphere = 5, 
        // Disc = 6,
    }

    // Deprecated / Obsolete - TODO: Remove once project have done the migration to the new LightEditor
    public enum LightArchetype { Punctual, Area, Null }; // Null value have been added to detect that we have updated the light correctly.

    public enum SpotLightShape { Cone, Pyramid, Box };

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class HDAdditionalLightData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Range(0.0f, 100.0f)]
        [FormerlySerializedAs("m_innerSpotPercent")]
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

        // Caution m_lightShape need to be in sync with m_Type of original light component (i.e it need to drive the value). This is necessary for the GI to work correctly and this is handled by the HLightEditor
        public LightShape m_LightShape; // To display this field in the UI this need to be public

        // This setter/getter here can be use by C# code when creating procedural light.
        public void SetLightshape(LightShape lightShape)
        {
            m_LightShape = lightShape;
            Light light = gameObject.GetComponent<Light>();

            switch (lightShape)
            {
                case LightShape.Directional:
                    light.type = LightType.Directional;
                    break;
                case LightShape.Point:
                    light.type = LightType.Point;
                    break;
                case LightShape.Spot:
                    light.type = LightType.Spot;
                    break;
                case LightShape.Rectangle:
                    light.type = LightType.Area;
                    break;
                case LightShape.Line:
                    light.type = LightType.Area;
                    break;
                default:
                    light.type = LightType.Area;
                    break;
            }            
        }

        public LightShape GetLightShape()
        {
            return m_LightShape;
        }

        // Only for Spotlight, should be hide for other light
        public SpotLightShape spotLightShape = SpotLightShape.Cone;

        // Only for Rectangle/Line/projector lights
        [Range(0.0f, 20.0f)]
        public float shapeLength = 0.5f;

        // Only for Rectangle/projector lights
        [Range(0.0f, 20.0f)]
        [FormerlySerializedAs("lightLength")]
        public float shapeWidth = 0.5f;

        // Only for Sphere/Disc
        [FormerlySerializedAs("lightWidth")]
        public float shapeRadius = 0.0f;

        // Only for Spot/Point - use to cheaply fake specular spherical area light
        [Range(0.0f, 1.0f)]
        public float maxSmoothness = 1.0f;

        // If true, we apply the smooth attenuation factor on the range attenuation to get 0 value, else the attenuation is just inverse square and never reach 0
        public bool applyRangeAttenuation = true;

        // This is specific for the LightEditor GUI and not use at runtime
        public bool useOldInspector = false;
        public bool showAdditionalSettings = true;

        // Deprecated / Obsolete - TODO: Remove once project have done the migration to the new LightEditor
        private LightArchetype archetype = LightArchetype.Null;

        // Deprecated / Obsolete - TODO: Remove once project have done the migration to the new LightEditor
        public void OnBeforeSerialize()
        {

        }

        // Deprecated / Obsolete - TODO: Remove once project have done the migration to the new LightEditor

        public void OnAfterDeserialize()
        {
            if (archetype != LightArchetype.Null)
            {
                Light light = gameObject.GetComponent<Light>();
                
                if (archetype == LightArchetype.Punctual)
                {
                    switch (light.type)
                    {
                        case LightType.Spot:
                            m_LightShape = LightShape.Spot;
                            break;
                        case LightType.Directional:
                            m_LightShape = LightShape.Directional;
                            break;
                        case LightType.Point:
                            m_LightShape = LightShape.Point;
                            break; 
                    }
                }
                else if (archetype == LightArchetype.Area)
                {
                    switch (light.type)
                    {
                        case LightType.Spot:
                            m_LightShape = LightShape.Spot;
                            break;
                        case LightType.Directional:
                            m_LightShape = LightShape.Directional;
                            break;
                        case LightType.Point:
                            m_LightShape = LightShape.Point;
                            break; 
                    }
                }

                UnityEditor.EditorUtility.SetDirty(this);
                archetype = LightArchetype.Null;
            }

        }

#if UNITY_EDITOR

        private void DrawGizmos(bool selected)
        {
            var light = gameObject.GetComponent<Light>();
            var gizmoColor = light.color;
            gizmoColor.a = selected ? 1.0f : 0.3f; // Fade for the gizmo
            Gizmos.color = Handles.color = gizmoColor;

            switch (m_LightShape)
            {
                case LightShape.Directional:
                    EditorLightUtilities.DrawDirectionalLightGizmo(light);
                    break;
                case LightShape.Point:
                    EditorLightUtilities.DrawPointlightGizmo(light, selected);
                    break;
                case LightShape.Spot:
                    if(spotLightShape == SpotLightShape.Cone)
                        EditorLightUtilities.DrawSpotlightGizmo(light, selected);
                    if (spotLightShape == SpotLightShape.Pyramid)
                        EditorLightUtilities.DrawFrustumlightGizmo(light);
                    if (spotLightShape == SpotLightShape.Box) // TODO
                        EditorLightUtilities.DrawFrustumlightGizmo(light);
                    break;
                case LightShape.Rectangle:
                    EditorLightUtilities.DrawArealightGizmo(light);
                    break;
                case LightShape.Line:
                    EditorLightUtilities.DrawArealightGizmo(light);
                    break;
            }
        }

        private void OnDrawGizmos()
        {
            DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }
#endif

    }
}
