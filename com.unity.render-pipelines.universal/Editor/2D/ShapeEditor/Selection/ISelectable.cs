namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal interface ISelectable<T>
    {
        bool Select(ISelector<T> selector);
    }
}
