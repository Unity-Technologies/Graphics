using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VfxExpressionReducer
    {
        public static VFXExpressionContextOption GetReductionOptions(VFXCompilationMode compilationMode)
            => compilationMode == VFXCompilationMode.Runtime
                ? VFXExpressionContextOption.ConstantFolding
                : VFXExpressionContextOption.Reduction;

        public static VFXExpression ReduceForGpu(VFXExpression expression, VFXCompilationMode compilationMode)
        {
            var reductionContext = new VFXExpression.Context(
                GetReductionOptions(compilationMode) | VFXExpressionContextOption.GPUDataTransformation);

            reductionContext.RegisterExpression(expression);
            reductionContext.Compile();
            return reductionContext.GetReduced(expression);
        }
    }
}
