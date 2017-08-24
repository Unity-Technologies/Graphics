namespace UnityEngine.MaterialGraph
{
    public enum CommonMatrixType
    {
        ModelView,
        View,
        Projection,
        ViewProjection,
        TransposeModelView,
        InverseTransposeModelView,
        ObjectToWorld,
        WorldToObject
    };

    public enum SimpleMatrixType
    {
        World,
        Local,
        Tangent,
        View
    };
}
