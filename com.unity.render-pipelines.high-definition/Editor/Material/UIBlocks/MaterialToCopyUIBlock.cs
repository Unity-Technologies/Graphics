using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;
using UnityEditor;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class MaterialToCopyUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Material To Copy";
            public static readonly GUIContent materialToCopyHeader = EditorGUIUtility.TrTextContent("Material to copy");
            public static readonly GUIContent layerNameHeader = EditorGUIUtility.TrTextContent("Layer name");
            public static readonly GUIContent uvHeader = EditorGUIUtility.TrTextContent("UV", "Also copy UV.");
            public static readonly GUIContent copyButtonIcon = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow", "Copy Material parameters to layer. If UV is disabled, this will not copy UV.");

            // TODO: material constant
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

        public MaterialToCopyUIBlock(Expandable expandableBit)
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
                    DrawMaterialCopyGUI();
                }
            }
        }

        void DrawMaterialCopyGUI()
        {
            bool    layersChanged = false;
            var     width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 90;

            // TODO: does not work with multi-selection
            Material material = materials[0];

            Color originalContentColor = GUI.contentColor;

            float indentOffset = EditorGUI.indentLevel * 15f;
            float colorWidth = 14;
            float UVWidth = 30;
            float copyButtonWidth = EditorGUIUtility.singleLineHeight;
            float endOffset = 5f;

            Rect headerLineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            Rect headerLabelRect = new Rect(headerLineRect.x, headerLineRect.y, EditorGUIUtility.labelWidth - indentOffset, headerLineRect.height);
            Rect headerUVRect = new Rect(headerLineRect.x + headerLineRect.width - 48 - endOffset, headerLineRect.y, UVWidth + 5, headerLineRect.height);
            Rect headerMaterialDropRect = new Rect(headerLineRect.x + headerLabelRect.width, headerLineRect.y, headerLineRect.width - headerLabelRect.width - headerUVRect.width, headerLineRect.height);

            EditorGUI.LabelField(headerLabelRect, Styles.layerNameHeader, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.LabelField(headerMaterialDropRect, Styles.materialToCopyHeader, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.LabelField(headerUVRect, Styles.uvHeader, EditorStyles.centeredGreyMiniLabel);

            for (int layerIndex = 0; layerIndex < numLayer; ++layerIndex)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                    Rect colorRect = new Rect(lineRect.x, lineRect.y, colorWidth, lineRect.height);
                    Rect materialRect = new Rect(lineRect.x + colorRect.width, lineRect.y, lineRect.width - UVWidth - colorWidth - copyButtonWidth + endOffset, lineRect.height);
                    Rect uvRect = new Rect(lineRect.x + lineRect.width - copyButtonWidth - UVWidth - endOffset, lineRect.y, UVWidth, lineRect.height);
                    Rect copyRect = new Rect(lineRect.x + lineRect.width - copyButtonWidth - endOffset, lineRect.y, copyButtonWidth, lineRect.height);

                    m_MaterialLayers[layerIndex] = EditorGUI.ObjectField(materialRect, Styles.layerLabels[layerIndex], m_MaterialLayers[layerIndex], typeof(Material), true) as Material;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(m_MaterialImporter, "Change layer material");
                        LayeredLitGUI.SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, true);
                        layersChanged = true;
                    }

                    
                    colorRect.width = 30f;
                    GUI.contentColor = kLayerColors[layerIndex];
                    EditorGUI.LabelField(colorRect, "■");
                    GUI.contentColor = originalContentColor;
                    
                    m_WithUV[layerIndex] = EditorGUI.Toggle(uvRect, m_WithUV[layerIndex]);
                    
                    if (GUI.Button(copyRect, GUIContent.none))
                    {
                        LayeredLitGUI.SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, !m_WithUV[layerIndex]);
                        layersChanged = true;
                    }

                    //fake the icon with two Console icon
                    //Rect copyRect = GUILayoutUtility.GetLastRect();
                    copyRect.x -= 16;
                    copyRect.width = 40;
                    EditorGUI.LabelField(copyRect, Styles.copyButtonIcon);
                    copyRect.x -= 3;
                    copyRect.y += 3;
                    EditorGUI.LabelField(copyRect, Styles.copyButtonIcon);
                }
            }

            EditorGUIUtility.labelWidth = width;

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