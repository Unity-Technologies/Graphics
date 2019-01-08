namespace UnityEditor.Graphing
{
    interface IGraphObject
    {
        IGraph graph { get; set; }
        void RegisterCompleteObjectUndo(string name);
    }
}
