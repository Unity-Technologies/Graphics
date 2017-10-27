using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.AnimatedValues;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public abstract class BaseCharacterGUI : ShaderGUI
    {
        protected MaterialEditor m_MaterialEditor;

        protected Shader hairShader;

        protected AnimBool animSkin;
        protected AnimBool animHair;
        protected AnimBool animEye;
        private void SubstanceAnim(Character.CharacterMaterialID id) { //Works for now
            switch (id)
            {
                case Character.CharacterMaterialID.Skin:
                    animSkin.valueChanged.AddListener(m_MaterialEditor.Repaint);
                    animSkin.target = true;  animHair.target = false; animEye.target = false;
                    break;
                case Character.CharacterMaterialID.Hair:
                    animHair.valueChanged.AddListener(m_MaterialEditor.Repaint);
                    animSkin.target = false; animHair.target = true;  animEye.target = false;
                    break;
                case Character.CharacterMaterialID.Eye:
                    animEye.valueChanged.AddListener(m_MaterialEditor.Repaint);
                    animSkin.target = false; animHair.target = false; animEye.target = true;
                    break;
            }
        }
        public BaseCharacterGUI() {
            animSkin = new AnimBool(false);
            animHair = new AnimBool(false);
            animEye  = new AnimBool(false);

            hairShader = Shader.Find("HDRenderPipeline/ExperimentalHair");
        }

        static public void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        //---------------------------------------------------------
        //
        //---------------------------------------------------------
        protected abstract void CharacterSkinGUI(Material material);
        protected abstract void CharacterHairGUI(Material material);
        protected abstract void CharacterEyeGUI (Material material);

        protected abstract void CharacterSkinKeywordAndPass(Material material);
        protected abstract void CharacterHairKeywordAndPass(Material material);
        protected abstract void CharacterEyeKeywordAndPass (Material material);

        //---------------------------------------------------------
        //GUI Text
        //---------------------------------------------------------
        protected static class Styles
        {
            public static GUIContent characterMaterialIDText = new GUIContent();
            public static GUIContent characterDiffuseText = new GUIContent("Diffuse Map");
            public static GUIContent characterNormalText = new GUIContent("Normal Map");

            //TODO: move
            public static GUIContent hairRoughnessText = new GUIContent("Roughness", "");
            public static GUIContent hairDetailMap = new GUIContent("Detail Map", "");
            public static GUIContent hairPrimarySpecText = new GUIContent("Primary Spec", "");
            public static GUIContent hairSecondarySpecText = new GUIContent("Secondary Spec", "");
            public static GUIContent hairPrimarySpecShiftText = new GUIContent("Primary Spec Shift", "");
            public static GUIContent hairSecondarySpecShiftText = new GUIContent("Secondary Spec Shift", "");
            public static GUIContent hairSpecTintText = new GUIContent("Spec Tint", "");
            public static GUIContent hairScatterText = new GUIContent("Scatter", "");
            public static GUIContent hairAlphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "");
            public static GUIContent hairAlphaCutoffText = new GUIContent("Alpha Cutoff", "");
            public static GUIContent hairAlphaCutoffShadowText = new GUIContent("Alpha Cutoff Shadow", "");
            public static GUIContent hairTransparentDepthWriteEnableText = new GUIContent("Transparent Depth Write", "");
            public static GUIContent hairAlphaCutoffPrepass = new GUIContent("Alpha Cutoff Prepass", "");
            public static GUIContent hairAlphaCutoffOpacityThreshold = new GUIContent("Alpha Cutoff Opacity Threshold", "");

        }

        //----------------------------------------------------------
        //Properties
        //----------------------------------------------------------
        protected MaterialProperty materialID = null;
        protected const string kMaterialID = "_CharacterMaterialID";

        protected MaterialProperty diffuseColor = null;
        protected const string kDiffuseColor = "_DiffuseColor";

        protected MaterialProperty diffuseMap = null;
        protected const string kDiffuseMap = "_DiffuseColorMap";

        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";

        protected MaterialProperty normalMapScale = null;
        protected const string kNormalMapScale = "_NormalScale";

        //TODO: Move
        protected MaterialProperty hairRoughness = null;
        protected const string kHairRoughness = "_Smoothness"; //Note, in the shader code we don't invert.

        protected MaterialProperty hairDetailMap = null;
        protected const string kHairDetailMap = "_DetailMap";

        protected MaterialProperty hairPrimarySpec = null;
        protected const string kHairPrimarySpec = "_PrimarySpecular";

        protected MaterialProperty hairSecondarySpec = null;
        protected const string kHairSecondarySpec = "_SecondarySpecular";

        protected MaterialProperty hairPrimarySpecShift = null;
        protected const string kHairPrimarySpecShift = "_PrimarySpecularShift";

        protected MaterialProperty hairSecondarySpecShift = null;
        protected const string kHairSecondarySpecShift = "_SecondarySpecularShift";

        protected MaterialProperty hairSpecTint = null;
        protected const string kHairSpecTint = "_SpecularTint";

        protected MaterialProperty hairScatter = null;
        protected const string kHairScatter = "_Scatter";

        protected MaterialProperty hairAlphaCutoffEnable = null;
        protected const string kHairAlphaCutoffEnable = "_AlphaCutoffEnable";

        protected MaterialProperty hairAlphaCutoff = null;
        protected const string kHairAlphaCutoff = "_AlphaCutoff";

        protected MaterialProperty hairAlphaCutoffShadow = null;
        protected const string kHairAlphaCutoffShadow = "_AlphaCutoffShadow";

        protected MaterialProperty hairTransparentDepthWriteEnable = null;
        protected const string kHairTransparentDepthWriteEnable = "_TransparentDepthPrepassEnable";

        protected MaterialProperty hairCutoffPrepass = null;
        protected const string kHairCutoffPrepass = "_AlphaCutoffPrepass";

        //----------------------------------------------------------
        //GUI Layout
        //----------------------------------------------------------
        public void ShaderPropertiesGUI(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;

            EditorGUI.BeginChangeCheck();
            {
               EditorGUILayout.LabelField("Substance Type", EditorStyles.largeLabel);

               GUI.color = new Color(0.7f, 0.7f, 0.7f); //NOTE: EditorGUIUtility.isProSkin
               m_MaterialEditor.ShaderProperty(materialID, Styles.characterMaterialIDText);
               GUI.color = Color.white;

               EditorGUI.indentLevel++;
               switch((Character.CharacterMaterialID)materialID.floatValue)
               {
                    case Character.CharacterMaterialID.Skin:
                        CharacterSkinGUI(material);
                        break;
                    case Character.CharacterMaterialID.Hair:
                        CharacterHairGUI(material);
                        break;
                    case Character.CharacterMaterialID.Eye:
                        CharacterEyeGUI(material);
                        break;
                    default:
                        Debug.Assert(false, "Unsupported Character MaterialID!");
                        break;
               }
               EditorGUI.indentLevel--;

               EditorGUILayout.Separator();

               GUILayout.Label("General", EditorStyles.boldLabel);
               m_MaterialEditor.TexturePropertySingleLine(Styles.characterDiffuseText, diffuseMap, diffuseColor);
               m_MaterialEditor.TexturePropertySingleLine(Styles.characterNormalText, normalMap, normalMapScale);
            }

            if (EditorGUI.EndChangeCheck())
            {
                //TODO: Common Keywords

                //TODO: Unique Flag Sets
                int matID = material.GetInt(kMaterialID);
                switch ((Character.CharacterMaterialID)materialID.floatValue)
                {
                    case Character.CharacterMaterialID.Skin:
                        CharacterSkinKeywordAndPass(material);
                        break;
                    case Character.CharacterMaterialID.Hair:
                        CharacterHairKeywordAndPass(material);
                        break;
                    case Character.CharacterMaterialID.Eye:
                        CharacterEyeKeywordAndPass(material);
                        break;
                }
            }
        }

        public void FindProperties(MaterialProperty[] props)
        {
            materialID            = FindProperty(kMaterialID, props, false);
            diffuseColor          = FindProperty(kDiffuseColor, props);
            diffuseMap            = FindProperty(kDiffuseMap, props);
            normalMap             = FindProperty(kNormalMap, props);
            normalMapScale        = FindProperty(kNormalMapScale, props);

            hairRoughness = FindProperty(kHairRoughness, props);
            hairDetailMap = FindProperty(kHairDetailMap, props);
            hairPrimarySpec = FindProperty(kHairPrimarySpec, props);
            hairSecondarySpec = FindProperty(kHairSecondarySpec, props);
            hairPrimarySpecShift = FindProperty(kHairPrimarySpecShift, props);
            hairSecondarySpecShift = FindProperty(kHairSecondarySpecShift, props);
            hairSpecTint = FindProperty(kHairSpecTint, props);
            hairScatter = FindProperty(kHairScatter, props);

            hairAlphaCutoffEnable = FindProperty(kHairAlphaCutoffEnable, props);
            hairAlphaCutoff = FindProperty(kHairAlphaCutoff, props);
            hairAlphaCutoffShadow = FindProperty(kHairAlphaCutoffShadow, props);
            hairCutoffPrepass = FindProperty(kHairCutoffPrepass, props);
            hairTransparentDepthWriteEnable = FindProperty(kHairTransparentDepthWriteEnable, props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;
            m_MaterialEditor.serializedObject.Update();

            FindProperties(props); //TODO: Only Base properties here
            Material material = materialEditor.target as Material;
            SubstanceAnim((Character.CharacterMaterialID)materialID.floatValue);
            ShaderPropertiesGUI(material);

            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
}
