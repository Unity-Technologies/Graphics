using System.Text;

namespace UnityEditor.Rendering
{
    internal interface IVariantStrippingInfo<TShader, TShaderVariant>
        where TShader : UnityEngine.Object
    {
        void SetShader(TShader shader);
        void Add(TShaderVariant shaderVariant, int variantsIn, int variantsOut, double stripTimeMs);
        void AppendLog(StringBuilder sb, bool onlySrp);
    }
}
