using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;
using IShaderScriptableStrippingData = UnityEditor.Rendering.Universal.ShaderScriptableStripper.IShaderScriptableStrippingData;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Struct used to determine whether keyword variants can be stripped from builds
    /// </summary>
    /// <typeparam name="T">The Shader Features used for verifying against the keywords</typeparam>
    internal struct ShaderStripTool<T> where T : Enum
    {
        T m_Features;
        private IShaderScriptableStrippingData m_StrippingData;

        public ShaderStripTool(T features, ref IShaderScriptableStrippingData strippingData)
        {
            m_Features = features;
            m_StrippingData = strippingData;
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

            // To strip out the OFF variant, it needs to check if
            // * Strip unused variants has been enabled
            // * ALL THREE keywords are present in that pass
            // * ALL THREE keywords are disabled in the keyword set
            // * One one of the keywords is enabled in the feature set gathered in ShaderBuildPreprocessor
            if (m_StrippingData.stripUnusedVariants)
            {
                bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2) && ContainsKeyword(kw3);
                bool keywordsDisabled = !m_StrippingData.IsKeywordEnabled(kw) && !m_StrippingData.IsKeywordEnabled(kw2) && !m_StrippingData.IsKeywordEnabled(kw3);
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

            // To strip out the OFF variant, it needs to check if
            // * Strip unused variants has been enabled
            // * BOTH keywords are present in that pass
            // * BOTH keywords are disabled in the keyword set
            // * One one of the keywords is enabled in the feature set gathered in ShaderBuildPreprocessor
            if (m_StrippingData.stripUnusedVariants)
            {
                bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2);
                bool keywordsDisabled = !m_StrippingData.IsKeywordEnabled(kw) && !m_StrippingData.IsKeywordEnabled(kw2);
                bool hasAnyFeatureEnabled = m_Features.HasFlag(feature) || m_Features.HasFlag(feature2);
                if (containsKeywords && keywordsDisabled && hasAnyFeatureEnabled)
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StripMultiCompileKeepOffVariant(in LocalKeyword kw, T feature)
        {
            return !m_Features.HasFlag(feature) && m_StrippingData.IsKeywordEnabled(kw);
        }

        public bool StripMultiCompile(in LocalKeyword kw, T feature)
        {
            // Same as Strip and Keep OFF variant
            if (!m_Features.HasFlag(feature))
            {
                if (m_StrippingData.IsKeywordEnabled(kw))
                    return true;
            }
            // To strip out the OFF variant, it needs to check if
            // * Strip unused variants has been enabled
            // * The keyword is present in that pass
            // * The keyword is disabled in the keyword set
            // * The keyword is enabled in the feature set gathered in ShaderBuildPreprocessor (Checked in the HasFlag check above)
            else if (m_StrippingData.stripUnusedVariants)
            {
                if (!m_StrippingData.IsKeywordEnabled(kw) && ContainsKeyword(kw))
                    return true;
            }
            return false;
        }

        internal bool ContainsKeyword(in LocalKeyword kw)
        {
            return m_StrippingData.PassHasKeyword(kw);
        }
    }
}
