using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using UnityEditor;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class LayerListUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Layer List";
            public static readonly GUIContent layerNameHeader = EditorGUIUtility.TrTextContent("Layer name");
            public static readonly GUIContent layerMaterialHeader = EditorGUIUtility.TrTextContent("Layer Material");
            public static readonly GUIContent uvHeader = EditorGUIUtility.TrTextContent("UV", "Also reset UV.");
            public static readonly GUIContent resetButtonIcon = EditorGUIUtility.TrTextContent("Reset", "Copy again Material parameters to layer. If UV is disabled, this will not copy UV."); //EditorGUIUtility.IconContent("RotateTool", "Copy Material parameters to layer. If UV is disabled, this will not copy UV.");

            public static readonly GUIContent[] layerLabels =
            {
                new GUIContent("Main layer"),
                new GUIContent("Layer 1"),
                new GUIContent("Layer 2"),
                new GUIContent("Layer 3"),
            };
        }

        MaterialProperty layerCount = null;

        Expandable      m_ExpandableBit;
        bool[]          m_WithUV;
        Material[]      m_MaterialLayers = new Material[kMaxLayerCount];
        AssetImporter   m_MaterialImporter;

        int numLayer
        {
            get { return (int)layerCount.floatValue; }
            set
            {
                layerCount.floatValue = (float)value;
                UpdateEditorExpended(value);
            }
        }

        void UpdateEditorExpended(int layerNumber)
        {
            if (layerNumber == 4)
            {
                materialEditor.SetExpandedAreas((uint)Expandable.ShowLayer3, true);
            }
            if (layerNumber >= 3)
            {
                materialEditor.SetExpandedAreas((uint)Expandable.ShowLayer2, true);
            }
            materialEditor.SetExpandedAreas((uint)Expandable.ShowLayer1, true);
        }

        public LayerListUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
            m_WithUV = new bool[]{ true, true, true, true };
        }

        public override void LoadMaterialProperties()
        {
            layerCount = FindProperty(kLayerCount);

            // TODO: does not work with multi-selection
            Material material = materials[0];

            m_MaterialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            // Material importer can be null when the selected material doesn't exists as asset (Material saved inside the scene)
            if (m_MaterialImporter != null)
                InitializeMaterialLayers(m_MaterialImporter, ref m_MaterialLayers);
        }

        // We use the user data to save a string that represent the referenced lit material
        // so we can keep reference during serialization
        static void InitializeMaterialLayers(AssetImporter materialImporter, ref Material[] layers)
        {
            if (materialImporter.userData != string.Empty)
            {
                SerializeableGUIDs layersGUID = JsonUtility.FromJson<SerializeableGUIDs>(materialImporter.userData);
                if (layersGUID.GUIDArray.Length > 0)
                {
                    layers = new Material[layersGUID.GUIDArray.Length];
                    for (int i = 0; i < layersGUID.GUIDArray.Length; ++i)
                    {
                        layers[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(layersGUID.GUIDArray[i]), typeof(Material)) as Material;
                    }
                }
            }
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawLayerListGUI();
                }
            }
        }

        void DrawLayerListGUI()
        {
            bool    layersChanged = false;
            var     oldLabelWidth = EditorGUIUtility.labelWidth;

            // TODO: does not work with multi-selection
            Material material = materials[0];

            float indentOffset = EditorGUI.indentLevel * 15f;
            float colorWidth = 5;
            float UVWidth = 30;
            float resetButtonWidth = 41;
            float padding = 4f;
            float endOffset = 2f;
            float labelWidth = 75f;

            EditorGUIUtility.labelWidth = labelWidth;


            Rect headerLineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            Rect headerLabelRect = new Rect(headerLineRect.x, headerLineRect.y, EditorGUIUtility.labelWidth - indentOffset + 15f, headerLineRect.height);
            Rect headerUVRect = new Rect(headerLineRect.x + headerLineRect.width - 37f - resetButtonWidth - endOffset, headerLineRect.y, UVWidth + 5, headerLineRect.height);
            Rect headerMaterialDropRect = new Rect(headerLineRect.x + headerLabelRect.width - 20f, headerLineRect.y, headerLineRect.width - headerLabelRect.width - headerUVRect.width, headerLineRect.height);

            EditorGUI.LabelField(headerLabelRect, Styles.layerNameHeader, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.LabelField(headerMaterialDropRect, Styles.layerMaterialHeader, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.LabelField(headerUVRect, Styles.uvHeader, EditorStyles.centeredGreyMiniLabel);

            for (int layerIndex = 0; layerIndex < numLayer; ++layerIndex)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                    Rect colorRect = new Rect(lineRect.x + 17f, lineRect.y + 7f, colorWidth, colorWidth);
                    Rect materialRect = new Rect(lineRect.x + padding + colorRect.width, lineRect.y, lineRect.width - UVWidth - padding - 3 - resetButtonWidth + endOffset, lineRect.height);
                    Rect uvRect = new Rect(lineRect.x + lineRect.width - resetButtonWidth - padding - UVWidth - endOffset, lineRect.y, UVWidth, lineRect.height);
                    Rect resetRect = new Rect(lineRect.x + lineRect.width - resetButtonWidth - endOffset, lineRect.y, resetButtonWidth, lineRect.height);

                    m_MaterialLayers[layerIndex] = EditorGUI.ObjectField(materialRect, Styles.layerLabels[layerIndex], m_MaterialLayers[layerIndex], typeof(Material), allowSceneObjects: true) as Material;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { material, m_MaterialImporter }, "Change layer material");
                        LayeredLitGUI.SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, true);
                        layersChanged = true;

                        // Update external reference.
                        foreach (var target in materialEditor.targets)
                        {
                            MaterialExternalReferences matExternalRefs = MaterialExternalReferences.GetMaterialExternalReferences(target as Material);
                            if (matExternalRefs != null)
                            {
                                matExternalRefs.SetMaterialReference(layerIndex, m_MaterialLayers[layerIndex]);
                            }
                        }
                    }

                    EditorGUI.DrawRect(colorRect, kLayerColors[layerIndex]);

                    m_WithUV[layerIndex] = EditorGUI.Toggle(uvRect, m_WithUV[layerIndex]);

                    if (GUI.Button(resetRect, GUIContent.none))
                    {
                        Undo.RecordObjects(new UnityEngine.Object[] { material, m_MaterialImporter }, "Reset layer material");
                        LayeredLitGUI.SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, !m_WithUV[layerIndex]);
                        layersChanged = true;
                    }

                    //draw text above to not cut the last letter
                    resetRect.x -= 12;
                    resetRect.width = 50;
                    EditorGUI.LabelField(resetRect, Styles.resetButtonIcon);
                }
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;

            if (layersChanged)
            {
                foreach (var mat in materials)
                {
                    LayeredLitGUI.SetupMaterialKeywordsAndPass(mat);
                }

                // SaveAssetsProcessor the referenced material in the users data
                if (m_MaterialImporter != null)
                    LayeredLitGUI.SaveMaterialLayers(material, m_MaterialLayers);

                // We should always do this call at the end
                materialEditor.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
