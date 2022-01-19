using System;

namespace UnityEditor.Rendering
{
    [Serializable]
    class VariantCounter
    {
        public int totalVariantsIn = 0;
        public int totalVariantsOut = 0;

        internal void RecordVariants(int variantsIn, int variantsOut)
        {
            this.totalVariantsIn += variantsIn;
            this.totalVariantsOut += variantsOut;
        }

        public float percentageStrippedVariants => totalVariantsOut / (float)totalVariantsIn * 100f;

        public string strippedVariantsInfo => $"Total={totalVariantsIn}/{totalVariantsOut}({percentageStrippedVariants:0.00}%)";
    }
}
