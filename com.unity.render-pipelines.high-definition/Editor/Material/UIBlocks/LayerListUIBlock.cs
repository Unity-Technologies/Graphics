using UnityEngine;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents a material layer list. The Layered Lit shader uses this UI block.
    /// </summary>
    public class LayerListUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Layering List");
            public static readonly GUIContent layerNameHeader = EditorGUIUtility.TrTextContent("Layer name");
            public static readonly GUIContent layerMaterialHeader = EditorGUIUtility.TrTextContent("Layer Material");
            public static readonly GUIContent uvHeader = EditorGUIUtility.TrTextContent("UV", "Also reset UV.");
            public static readonly GUIContent resetButtonIcon = EditorGUIUtility.TrTextContent("Reset", "Copy again Material parameters to layer. If UV is disabled, this will not copy UV."); //EditorGUIUtility.IconContent("RotateTool", "Copy Material parameters to layer. If UV is disabled, this will not copy UV.");
        }

        MaterialProperty layerCount = null;

        bool[]          m_WithUV = new bool[kMaxLayerCount];
        Material[]      m_MaterialLayers = new Material[kMaxLayerCount];
        AssetImporter   m_MaterialImporter;

        int numLayer => (int)layerCount.floatValue;

        /// <summary>
        /// Constructs a LayerListUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the state of the foldout</param>
        public LayerListUIBlock(ExpandableBit expandableBit)
            : base(expandableBit, Styles.header)
        {
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            layerCount = FindProperty(kLayerCount);

            // TODO: does not work with multi-selection
            Material material = materials[0];

            m_MaterialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            // Material importer can be null when the selected material doesn't exists as asset (Material saved inside the scene)
            if (m_MaterialImporter != null)
                LayeredLitGUI.InitializeMaterialLayers(m_MaterialImporter, ref m_MaterialLayers, ref m_WithUV);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
        {
            bool    layersChanged = false;
            var     oldLabelWidth = EditorGUIUtility.labelWidth;

            // TODO: does not work with multi-selection
            Material material = materials[0];

            float indentOffset = EditorGUI.indentLevel * 15f;
            float UVWidth = 30;
            float resetButtonWidth = 43;
            float padding = 4f;
            float endOffset = 2f;
            float labelWidth = 100f;
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUIUtility.labelWidth = labelWidth;

            Rect headerLineRect = GUILayoutUtility.GetRect(1, height);
            Rect headerLabelRect = new Rect(headerLineRect.x, headerLineRect.y, EditorGUIUtility.labelWidth - indentOffset + 15f, height);
            Rect headerUVRect = new Rect(headerLineRect.x + headerLineRect.width - 37f - resetButtonWidth - endOffset, headerLineRect.y, UVWidth + 5, height);
            Rect headerMaterialDropRect = new Rect(headerLineRect.x + headerLabelRect.width - 20f, headerLineRect.y, headerLineRect.width - headerLabelRect.width - headerUVRect.width, height);

            EditorGUI.LabelField(headerLabelRect, Styles.layerNameHeader, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.LabelField(headerMaterialDropRect, Styles.layerMaterialHeader, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.LabelField(headerUVRect, Styles.uvHeader, EditorStyles.centeredGreyMiniLabel);

            for (int layerIndex = 0; layerIndex < numLayer; ++layerIndex)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);

                    Rect materialRect = new Rect(lineRect.x + padding, lineRect.y, lineRect.width - UVWidth - padding - 3 - resetButtonWidth + endOffset, lineRect.height);
                    Rect uvRect = new Rect(lineRect.x + lineRect.width - resetButtonWidth - padding - UVWidth - endOffset, lineRect.y, UVWidth, lineRect.height);
                    Rect resetRect = new Rect(lineRect.x + lineRect.width - resetButtonWidth - endOffset, lineRect.y, resetButtonWidth, lineRect.height);

                    materialRect.yMin += EditorGUIUtility.standardVerticalSpacing;
                    uvRect.yMin += EditorGUIUtility.standardVerticalSpacing;
                    resetRect.yMin += EditorGUIUtility.standardVerticalSpacing;

                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUIUtility.IconSizeScope(LayersUIBlock.Styles.layerIconSize))
                        m_MaterialLayers[layerIndex] = EditorGUI.ObjectField(materialRect, LayersUIBlock.Styles.layers[layerIndex], m_MaterialLayers[layerIndex], typeof(Material), allowSceneObjects: true) as Material;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { material, m_MaterialImporter }, "Change layer material");
                        LayeredLitGUI.SynchronizeLayerProperties(material, layerIndex, m_MaterialLayers[layerIndex], m_WithUV[layerIndex]);
                        layersChanged = true;

                        // Update external reference.
                        foreach (var target in materialEditor.targets)
                        {
                            MaterialExternalReferences matExternalRefs = MaterialExternalReferences.GetMaterialExternalReferences(target as Material);
                            matExternalRefs.SetMaterialReference(layerIndex, m_MaterialLayers[layerIndex]);
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    m_WithUV[layerIndex] = EditorGUI.Toggle(uvRect, m_WithUV[layerIndex]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { material, m_MaterialImporter }, "Change layer material");
                        layersChanged = true;
                    }

                    if (GUI.Button(resetRect, Styles.resetButtonIcon))
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { material, m_MaterialImporter }, "Reset layer material");
                        LayeredLitGUI.SynchronizeLayerProperties(material, layerIndex, m_MaterialLayers[layerIndex], m_WithUV[layerIndex]);
                        layersChanged = true;
                    }
                }

                if (m_MaterialLayers[layerIndex] != null && m_MaterialLayers[layerIndex].shader != null)
                {
                    var shaderName = m_MaterialLayers[layerIndex].shader.name;
                    if (shaderName != "HDRP/Lit" && shaderName != "HDRP/LitTessellation")
                        EditorGUILayout.HelpBox("Selected material is not an HDRP Lit Material. Some properties may not be correctly imported.", MessageType.Info);
                }
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;

            if (layersChanged)
            {
                foreach (var mat in materials)
                {
                    LayeredLitGUI.SetupLayeredLitKeywordsAndPass(mat);
                }

                // SaveAssetsProcessor the referenced material in the users data
                if (m_MaterialImporter != null)
                    LayeredLitGUI.SaveMaterialLayers(material, m_MaterialLayers, m_WithUV);

                // We should always do this call at the end
                materialEditor.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
