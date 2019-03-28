namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface ISelectable<T>
    {
        void Select(ISelector<T> selector);
    }
}
