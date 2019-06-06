namespace UnityEditor.Experimental.Rendering.Univerasl.Path2D
{
    internal interface ISelectable<T>
    {
        bool Select(ISelector<T> selector);
    }
}
