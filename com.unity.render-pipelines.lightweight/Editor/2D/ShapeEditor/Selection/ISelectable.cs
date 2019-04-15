namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface ISelectable<T>
    {
        bool Select(ISelector<T> selector);
    }
}
