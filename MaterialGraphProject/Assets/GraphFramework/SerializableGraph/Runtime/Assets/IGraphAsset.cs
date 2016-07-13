namespace UnityEngine.Graphing
{
    public interface IGraphAsset
    {
        IGraph graph { get; }
        bool shouldRepaint { get; }
        ScriptableObject GetScriptableObject();
        void OnEnable();
    }
}
