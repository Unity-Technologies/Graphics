namespace UnityEditor.Experimental
{
    public interface ICanvasDataSource
    {
        CanvasElement[] FetchElements();
        void DeleteElement(CanvasElement e);
    }
}
