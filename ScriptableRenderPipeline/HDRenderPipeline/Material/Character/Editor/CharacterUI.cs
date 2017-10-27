using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.AnimatedValues;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class CharacterGUI : BaseCharacterGUI
    {
        //-----------------------------------------------------------------------------
        //GUI
        //-----------------------------------------------------------------------------
        protected override void CharacterSkinGUI(Material material)
        {
            if (EditorGUILayout.BeginFadeGroup(animSkin.faded))
            {
                //Skin GUI
                //------------------------------------------------
                EditorGUILayout.LabelField("TODO: Skin Parameters", EditorStyles.miniBoldLabel);
            }
            EditorGUILayout.EndFadeGroup();
        }

        protected override void CharacterHairGUI(Material material)
        {

            if (EditorGUILayout.BeginFadeGroup(animHair.faded))
            {
                //Hair GUI
                //--------------------------------------------------
                m_MaterialEditor.ShaderProperty(hairRoughness, Styles.hairRoughnessText);

                m_MaterialEditor.TexturePropertySingleLine(Styles.hairDetailMap, hairDetailMap);
                m_MaterialEditor.TextureScaleOffsetProperty(hairDetailMap);

                m_MaterialEditor.ShaderProperty(hairPrimarySpec, Styles.hairPrimarySpecText);

                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(hairPrimarySpecShift, Styles.hairPrimarySpecShiftText);
                EditorGUI.indentLevel--;

                m_MaterialEditor.ShaderProperty(hairSpecTint, Styles.hairSpecTintText);

                m_MaterialEditor.ShaderProperty(hairSecondarySpec, Styles.hairSecondarySpecText);

                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(hairSecondarySpecShift, Styles.hairSecondarySpecShiftText);
                EditorGUI.indentLevel--;

                m_MaterialEditor.ShaderProperty(hairScatter, Styles.hairScatterText);

                EditorGUILayout.Space();

                m_MaterialEditor.ShaderProperty(hairAlphaCutoffEnable, Styles.hairAlphaCutoffEnableText);
                if(hairAlphaCutoffEnable.floatValue == 1.0f)
                {
                    EditorGUI.indentLevel++;

                    m_MaterialEditor.ShaderProperty(hairAlphaCutoff, Styles.hairAlphaCutoffText);
                    if(hairAlphaCutoffShadow != null)
                    {
                        m_MaterialEditor.ShaderProperty(hairAlphaCutoffShadow, Styles.hairAlphaCutoffShadowText);
                        m_MaterialEditor.ShaderProperty(hairTransparentDepthWriteEnable, Styles.hairTransparentDepthWriteEnableText);

                        EditorGUI.indentLevel++;
                        if(hairTransparentDepthWriteEnable != null && hairTransparentDepthWriteEnable.floatValue == 1.0f)
                        {
                            m_MaterialEditor.ShaderProperty(hairCutoffPrepass, Styles.hairAlphaCutoffPrepass);
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        protected override void CharacterEyeGUI(Material material)
        {
            if (EditorGUILayout.BeginFadeGroup(animEye.faded))
            {
                //Eye GUI
                //----------------------------------------------------
                EditorGUILayout.LabelField("TODO: Eye Parameters", EditorStyles.miniBoldLabel);
            }
            EditorGUILayout.EndFadeGroup();
        }

        //-----------------------------------------------------------------------------
        //Keyword + Pass
        //-----------------------------------------------------------------------------
        protected override void CharacterSkinKeywordAndPass(Material material)
        {
            material.SetShaderPassEnabled("ShadowCaster",           false);
            material.SetShaderPassEnabled("Forward",                false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr,  false);
        }

        protected override void CharacterHairKeywordAndPass(Material material)
        {
            //Shader/Pass
            //------------------------
            m_MaterialEditor.SetShader(hairShader,        true);               //NOTE: Do this beforehand
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.SetShaderPassEnabled("Forward",      true);

            if (material.HasProperty(kHairTransparentDepthWriteEnable))
            {
                bool depthWriteEnable = material.GetFloat(kHairTransparentDepthWriteEnable) > 0.0f;
                if (depthWriteEnable)
                {
                    material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr, true);
                }
                else
                {
                    material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr, false);
                }
            }

            //Keywords
            //------------------------
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);  //Blend: Lerp
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off); //Double Side Enabled
            SetKeyword(material, "_DOUBLESIDED_ON", true);
            SetKeyword(material, "_ALPHATEST_ON", material.GetFloat(kHairAlphaCutoffEnable) > 0.0f);
            SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kHairDetailMap));

        }

        protected override void CharacterEyeKeywordAndPass(Material material)
        {
            material.SetShaderPassEnabled("ShadowCaster",           false);
            material.SetShaderPassEnabled("Forward",                false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr,  false);
        }
    }
}
