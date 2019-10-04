namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// EXPERIMENTAL: Interface similar to <see cref="System.Func{T, TResult}"/> but consuming <c>in</c> arguments.
    ///
    /// Implement this interface on a struct to have inlined callbacks by the compiler.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="R">The type of the return.</typeparam>
    public interface IInFunc<T1, R>
    {
        /// <summary>Execute the function.</summary>
        R Execute(in T1 t1);
    }
}
