using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents the sorting inputs for decal materials.
    /// </summary>
    public class DecalSortingInputsUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Sorting Inputs");
            public static GUIContent meshDecalBiasType = new GUIContent("Mesh Decal Bias Type", "Set the type of bias that is applied to the mesh decal. Depth Bias applies a bias to the final depth value, while View bias applies a world space bias (in meters) alongside the view vector.");
            public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh Decal Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
            public static GUIContent meshDecalViewBiasText = new GUIContent("Mesh Decal View Bias", "Sets a world-space bias alongside the view vector to stop the decal's Mesh from overlapping with other Meshes. The unit is meters.");
            public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of Decal Projectors. HDRP draws decals with lower values first.");
        }

        MaterialProperty decalMeshBiasType = new MaterialProperty();
        const string kDecalMeshBiasType = "_DecalMeshBiasType";

        MaterialProperty decalMeshDepthBias = new MaterialProperty();
        const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        MaterialProperty decalMeshViewBias = new MaterialProperty();
        const string kDecalViewDepthBias = "_DecalMeshViewBias";

        MaterialProperty drawOrder = new MaterialProperty();
        const string kDrawOrder = "_DrawOrder";

        /// <summary>
        /// Constructs the DecalSortingInputsUIBlock.
        /// </summary>
        /// <param name="expandableBit">Bit used for the foldout state.</param>
        public DecalSortingInputsUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            decalMeshBiasType = FindProperty(kDecalMeshBiasType);
            decalMeshViewBias = FindProperty(kDecalViewDepthBias);
            decalMeshDepthBias = FindProperty(kDecalMeshDepthBias);
            drawOrder = FindProperty(kDrawOrder);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            materialEditor.IntSliderShaderProperty(drawOrder, -HDRenderQueue.meshDecalPriorityRange, HDRenderQueue.meshDecalPriorityRange, Styles.drawOrderText);
            materialEditor.ShaderProperty(decalMeshBiasType, Styles.meshDecalBiasType);
            if ((int)decalMeshBiasType.floatValue == (int)DecalMeshDepthBiasType.DepthBias)
            {
                materialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);
            }
            else if ((int)decalMeshBiasType.floatValue == (int)DecalMeshDepthBiasType.ViewBias)
            {
                materialEditor.ShaderProperty(decalMeshViewBias, Styles.meshDecalViewBiasText);
            }
        }
    }
}
