namespace UnityEditor.Experimental.Rendering.Univerasl.Path2D
{
    internal interface ISelector<T>
    {
        bool Select(T element);
    }
}
