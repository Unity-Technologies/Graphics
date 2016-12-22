namespace UnityEngine.Graphing
{
    public interface IGraphAsset
    {
        IGraph graph { get; }
        bool shouldRepaint { get; }
        GraphDrawingData drawingData { get; }
        ScriptableObject GetScriptableObject();
        void OnEnable();
    }
}
