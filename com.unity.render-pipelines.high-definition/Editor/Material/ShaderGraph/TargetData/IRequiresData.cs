namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    interface IRequiresData<T> where T : HDTargetData
    {
        T data { get; set; }
    }
}
