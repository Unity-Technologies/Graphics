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

        bool[] m_WithUV = new bool[kMaxLayerCount];
        Material[] m_MaterialLayers = new Material[kMaxLayerCount];
        AssetImporter m_MaterialImporter;

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
            bool layersChanged = false;
            int oldindentLevel = EditorGUI.indentLevel;

            // TODO: does not work with multi-selection
            Material material = materials[0];

            float indentOffset = EditorGUI.indentLevel * 15f;
            const int UVWidth = 14;
            const int resetButtonWidth = 43;
            const int endOffset = 2;
            const int horizontalSpacing = 4;
            const int headerHeight = 15;

            EditorGUI.indentLevel = 0;

            Rect headerLineRect = GUILayoutUtility.GetRect(1, headerHeight);
            Rect labelRect = new Rect(headerLineRect.x + indentOffset, headerLineRect.y, EditorGUIUtility.labelWidth - indentOffset, headerHeight);
            Rect uvRect = new Rect(headerLineRect.x + headerLineRect.width - horizontalSpacing - UVWidth - resetButtonWidth - endOffset, headerLineRect.y, UVWidth, headerHeight);
            Rect materialDropRect = new Rect(labelRect.xMax + horizontalSpacing, headerLineRect.y, uvRect.xMin - labelRect.xMax - 2 * horizontalSpacing, headerHeight);

            //Minilabel is slighly shifted from 2 px.
            const int shift = 2;
            const int textOverflow = 2;
            Rect headerLabelRect = new Rect(labelRect) { xMin = labelRect.xMin - shift, xMax = labelRect.xMax + shift };
            Rect headerUVRect = new Rect(uvRect) { xMin = uvRect.xMin - shift, xMax = uvRect.xMax + shift + textOverflow }; //dealing with text overflow (centering "UV" on sligthly larger area)
            Rect headerMaterialDropRect = new Rect(materialDropRect) { xMin = materialDropRect.xMin - shift, xMax = materialDropRect.xMax + shift };

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.LabelField(headerLabelRect, Styles.layerNameHeader, EditorStyles.miniLabel);
                EditorGUI.LabelField(headerMaterialDropRect, Styles.layerMaterialHeader, EditorStyles.miniLabel);
                EditorGUI.LabelField(headerUVRect, Styles.uvHeader, EditorStyles.miniLabel);
            }

            for (int layerIndex = 0; layerIndex < numLayer; ++layerIndex)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                    lineRect.xMin += indentOffset;
                    lineRect.yMax -= EditorGUIUtility.standardVerticalSpacing;

                    Rect lineLabelRect = new Rect(labelRect.x, lineRect.y, labelRect.width, lineRect.height);
                    Rect lineMaterialRect = new Rect(materialDropRect.x, lineRect.y, materialDropRect.width, lineRect.height);
                    Rect lineUvRect = new Rect(uvRect.x, lineRect.y, uvRect.width, lineRect.height);
                    Rect lineResetRect = new Rect(uvRect.xMax + horizontalSpacing, lineRect.y, resetButtonWidth, lineRect.height);

                    using (new EditorGUIUtility.IconSizeScope(LayersUIBlock.Styles.layerIconSize))
                        EditorGUI.LabelField(lineLabelRect, LayersUIBlock.Styles.layers[layerIndex]);

                    EditorGUI.BeginChangeCheck();
                    m_MaterialLayers[layerIndex] = EditorGUI.ObjectField(lineMaterialRect, GUIContent.none, m_MaterialLayers[layerIndex], typeof(Material), allowSceneObjects: true) as Material;
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
                    m_WithUV[layerIndex] = EditorGUI.Toggle(lineUvRect, m_WithUV[layerIndex]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { material, m_MaterialImporter }, "Change layer material");
                        layersChanged = true;
                    }

                    if (GUI.Button(lineResetRect, Styles.resetButtonIcon))
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

            EditorGUI.indentLevel = oldindentLevel;

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
