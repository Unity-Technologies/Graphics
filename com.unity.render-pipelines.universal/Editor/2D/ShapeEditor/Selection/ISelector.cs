namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal interface ISelector<T>
    {
        bool Select(T element);
    }
}
