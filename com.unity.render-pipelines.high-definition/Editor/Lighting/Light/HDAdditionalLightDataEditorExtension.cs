using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Extension class that contains all the Editor Only functions available for the HDAdditionalLightData component
    /// </summary>
    public static class HDAdditionalLightDataEditorExtension
    {
        /// <summary>
        /// Set Lightmap Bake Type.
        /// </summary>
        /// <param name="hdLight"></param>
        /// <param name="lightmapBakeType"></param>
        /// <returns></returns>
        public static LightmapBakeType SetLightmapBakeType(this HDAdditionalLightData hdLight, LightmapBakeType lightmapBakeType) => hdLight.legacyLight.lightmapBakeType = lightmapBakeType;

        /// <summary>
        /// Get the display emissive mesh value
        /// </summary>
        public static bool GetDisplayAreaLightEmissiveMesh(this HDAdditionalLightData hdLight) => hdLight.displayAreaLightEmissiveMesh;

        /// <summary>
        /// Displays or hide an emissive mesh for the area light
        /// </summary>
        /// <param name="hdLight"></param>
        /// <param name="display"></param>
        public static void SetDisplayAreaLightEmissiveMesh(this HDAdditionalLightData hdLight, bool display)
        {
            if (hdLight.displayAreaLightEmissiveMesh == display)
                return;

            if (display)
            {
                // fix the local scale to match the emissive quad size
                hdLight.transform.localScale = new Vector3(hdLight.shapeWidth, hdLight.shapeHeight, HDAdditionalLightData.k_MinAreaWidth);
            }

            hdLight.displayAreaLightEmissiveMesh = display;

            hdLight.UpdateEmissiveMeshComponents();
        }
        
        internal static void UpdateEmissiveMeshComponents(this HDAdditionalLightData hdLight)
        {
            // If the display emissive mesh is disabled, skip to the next selected light
            if (hdLight.emissiveMeshFilter == null || hdLight.emissiveMeshRenderer == null)
                return;

            // We only load the mesh and it's material here, because we can't do that inside HDAdditionalLightData (Editor assembly)
            // Every other properties of the mesh is updated in HDAdditionalLightData to support timeline and editor records
            if (hdLight.type == HDLightType.Area)
            {
                switch (hdLight.areaLightShape)
                {
                    case AreaLightShape.Tube:
                        hdLight.emissiveMeshFilter.mesh = HDEditorUtils.LoadAsset<Mesh>("Runtime/RenderPipelineResources/Mesh/Cylinder.fbx");
                        break;
                    case AreaLightShape.Rectangle:
                    default:
                        hdLight.emissiveMeshFilter.mesh = HDEditorUtils.LoadAsset<Mesh>("Runtime/RenderPipelineResources/Mesh/Quad.FBX");
                        break;
                }
            }
            else // [TODO: check if we need this for non area lights as it was done]
            {
                hdLight.emissiveMeshFilter.mesh = HDEditorUtils.LoadAsset<Mesh>("Runtime/RenderPipelineResources/Mesh/Quad.FBX");
            }
            if (hdLight.emissiveMeshRenderer.sharedMaterial == null)
            {
                hdLight.emissiveMeshRenderer.sharedMaterial = new Material(Shader.Find("HDRP/Unlit"));
            }
            hdLight.emissiveMeshRenderer.sharedMaterial.SetFloat("_IncludeIndirectLighting", 0.0f);
        }
    }
}
