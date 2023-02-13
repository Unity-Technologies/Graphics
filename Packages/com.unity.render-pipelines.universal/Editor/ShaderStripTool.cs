using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Struct used to determine whether keyword variants can be stripped from builds
    /// </summary>
    /// <typeparam name="T">The Shader Features used for verifying against the keywords</typeparam>
    internal struct ShaderStripTool<T> where T : Enum
    {
        T m_Features;
        Shader m_Shader;
        ShaderKeywordSet m_KeywordSet;
        ShaderSnippetData m_passData;
        ShaderCompilerPlatform m_ShaderCompilerPlatform;

        public ShaderStripTool(T features, Shader shader, ShaderSnippetData passData, in ShaderKeywordSet keywordSet, ShaderCompilerPlatform shaderCompilerPlatform)
        {
            m_Features = features;
            m_Shader = shader;
            m_passData = passData;
            m_KeywordSet = keywordSet;
            m_ShaderCompilerPlatform = shaderCompilerPlatform;
        }

        public bool StripMultiCompileKeepOffVariant(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2, in LocalKeyword kw3, T feature3)
        {
            if (StripMultiCompileKeepOffVariant(kw, feature))
                return true;
            if (StripMultiCompileKeepOffVariant(kw2, feature2))
                return true;
            if (StripMultiCompileKeepOffVariant(kw3, feature3))
                return true;
            return false;
        }

        public bool StripMultiCompile(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2, in LocalKeyword kw3, T feature3)
        {
            if (StripMultiCompileKeepOffVariant(kw, feature, kw2, feature2, kw3, feature3))
                return true;

            if (ShaderBuildPreprocessor.s_StripUnusedVariants)
            {
                bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2) && ContainsKeyword(kw3);
                bool keywordsDisabled = !m_KeywordSet.IsEnabled(kw) && !m_KeywordSet.IsEnabled(kw2) && !m_KeywordSet.IsEnabled(kw3);
                bool hasAnyFeatureEnabled = m_Features.HasFlag(feature) || m_Features.HasFlag(feature2) || m_Features.HasFlag(feature3);
                if (containsKeywords && keywordsDisabled && hasAnyFeatureEnabled)
                    return true;
            }

            return false;
        }

        public bool StripMultiCompileKeepOffVariant(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2)
        {
            if (StripMultiCompileKeepOffVariant(kw, feature))
                return true;
            if (StripMultiCompileKeepOffVariant(kw2, feature2))
                return true;
            return false;
        }

        public bool StripMultiCompile(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2)
        {
            if (StripMultiCompileKeepOffVariant(kw, feature, kw2, feature2))
                return true;

            if (ShaderBuildPreprocessor.s_StripUnusedVariants)
            {
                bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2);
                bool keywordsDisabled = !m_KeywordSet.IsEnabled(kw) && !m_KeywordSet.IsEnabled(kw2);
                bool hasAnyFeatureEnabled = m_Features.HasFlag(feature) || m_Features.HasFlag(feature2);
                if (containsKeywords && keywordsDisabled && hasAnyFeatureEnabled)
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StripMultiCompileKeepOffVariant(in LocalKeyword kw, T feature)
        {
            return !m_Features.HasFlag(feature) && m_KeywordSet.IsEnabled(kw);
        }

        public bool StripMultiCompile(in LocalKeyword kw, T feature)
        {
            if (!m_Features.HasFlag(feature))
            {
                if (m_KeywordSet.IsEnabled(kw))
                    return true;
            }
            else if (ShaderBuildPreprocessor.s_StripUnusedVariants)
            {
                if (!m_KeywordSet.IsEnabled(kw) && ContainsKeyword(kw))
                    return true;
            }
            return false;
        }

        private bool ContainsKeyword(in LocalKeyword kw)
        {
            return ShaderUtil.PassHasKeyword(m_Shader, m_passData.pass, kw, m_passData.shaderType, m_ShaderCompilerPlatform);
        }

    }
}
