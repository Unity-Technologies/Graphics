namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface ISelector<T>
    {
        bool Select(T element);
    }
}
