using UnityEngine.Rendering;
using Utilities;

namespace UnityEditor.Rendering.Utilities
{
    public static class EditorMaterialQualityUtilities
    {
        public static MaterialQuality GetMaterialQuality(this ShaderKeywordSet keywordSet)
        {
            var result = (MaterialQuality)0;
            for (var i = 0; i < MaterialQualityUtilities.Keywords.Length; ++i)
            {
                if (keywordSet.IsEnabled(MaterialQualityUtilities.Keywords[i]))
                    result |= (MaterialQuality) (1 << i);
            }

            return result;
        }
    }
}
