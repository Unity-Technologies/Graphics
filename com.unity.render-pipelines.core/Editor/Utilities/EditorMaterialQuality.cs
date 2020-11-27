using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Utilities
{
    /// <summary>
    /// Editor MaterialQuality utility class.
    /// </summary>
    public static class EditorMaterialQualityUtilities
    {
        /// <summary>
        /// Get the material quality levels enabled in a keyword set.
        /// </summary>
        /// <param name="keywordSet">Input keywords.</param>
        /// <returns>All available MaterialQuality levels in the keyword set.</returns>
        public static MaterialQuality GetMaterialQuality(this ShaderKeywordSet keywordSet)
        {
            var result = (MaterialQuality)0;
            for (var i = 0; i < MaterialQualityUtilities.Keywords.Length; ++i)
            {
                if (keywordSet.IsEnabled(MaterialQualityUtilities.Keywords[i]))
                    result |= (MaterialQuality)(1 << i);
            }

            return result;
        }
    }
}
