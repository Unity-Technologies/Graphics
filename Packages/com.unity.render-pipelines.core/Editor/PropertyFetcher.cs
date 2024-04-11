using System;
using System.Linq.Expressions;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Serialized property fetcher.
    /// </summary>
    /// <typeparam name="T">Serialized object type.</typeparam>
    public sealed class PropertyFetcher<T> : IDisposable
    {
        /// <summary>
        /// Serialized object associated with the fetcher.
        /// </summary>
        public readonly SerializedObject obj;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="obj">Serialized object containing properties to fetch.</param>
        public PropertyFetcher(SerializedObject obj)
        {
            Assert.IsNotNull(obj);
            this.obj = obj;
        }

        /// <summary>
        /// Find a property by name.
        /// </summary>
        /// <param name="str">Property name.</param>
        /// <returns>Required property if it exists, null otherwise.</returns>
        public SerializedProperty Find(string str)
        {
            return obj.FindProperty(str);
        }

        /// <summary>
        /// Find a property based on an expression.
        ///
        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.
        /// </summary>
        /// <typeparam name="TValue">Type of the serialized object.</typeparam>
        /// <param name="expr">Expression for the property.</param>
        /// <returns>Required property if it exists, null otherwise.</returns>
        public SerializedProperty Find<TValue>(Expression<Func<T, TValue>> expr)
        {
            string path = CoreEditorUtils.FindProperty(expr);
            return obj.FindProperty(path);
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        public void Dispose()
        {
            // Nothing to do here, still needed so we can rely on the using/IDisposable pattern
        }
    }

    /// <summary>
    /// Relative property fetcher.
    /// </summary>
    /// <typeparam name="T">SerializedObject type.</typeparam>
    public sealed class RelativePropertyFetcher<T> : IDisposable
    {
        /// <summary>
        /// Serialized object associated with the fetcher.
        /// </summary>
        public readonly SerializedProperty obj;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="obj">Serialized object containing properties to fetch.</param>
        public RelativePropertyFetcher(SerializedProperty obj)
        {
            Assert.IsNotNull(obj);
            this.obj = obj;
        }

        /// <summary>
        /// Find a property by name.
        /// </summary>
        /// <param name="str">Property name.</param>
        /// <returns>Required property if it exists, null otherwise.</returns>
        public SerializedProperty Find(string str)
        {
            return obj.FindPropertyRelative(str);
        }

        /// <summary>
        /// Find a property based on an expression.
        ///
        /// Use with extreme caution as this method does not directly retrieve the property but instead searches for a field with a similar name.
        ///  Inheritance and property overrides are not supported, and renaming a variable may break the linkage without warning.
        /// </summary>
        /// <typeparam name="TValue">Type of the serialized object.</typeparam>
        /// <param name="expr">Expression for the property.</param>
        /// <returns>Required property if it exists, null otherwise.</returns>
        public SerializedProperty Find<TValue>(Expression<Func<T, TValue>> expr)
        {
            string path = CoreEditorUtils.FindProperty(expr);
            return obj.FindPropertyRelative(path);
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        public void Dispose()
        {
            // Nothing to do here, still needed so we can rely on the using/IDisposable pattern
        }
    }

    /// <summary>
    /// Property fetcher extension class.
    /// </summary>
    public static class PropertyFetcherExtensions
    {
        /// <summary>
        /// Retrieves a <see cref="SerializedProperty"/> by using a lambda expression to reference its containing class and field.
        /// </summary>
        /// <typeparam name="TSource">The class type containing the field.</typeparam>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="obj">The <see cref="SerializedObject"/> being searched.</param>
        /// <param name="expr">A lambda expression pointing to the field within the source class.</param>
        /// <returns>The corresponding <see cref="SerializedProperty"/>, or null if not found.</returns>
        public static SerializedProperty Find<TSource, TValue>(this SerializedObject obj, Expression<Func<TSource, TValue>> expr)
        {
            var path = CoreEditorUtils.FindProperty(expr);
            return obj.FindProperty(path);
        }

        /// <summary>
        /// Retrieves a relative <see cref="SerializedProperty"/> based on a lambda expression pointing to a specific field within the source object.
        /// </summary>
        /// <typeparam name="TSource">The class type containing the field.</typeparam>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="obj">The instance of <see cref="SerializedProperty"/> to begin the search from.</param>
        /// <param name="expr">>A lambda expression pointing to the field within the source class.</param>
        /// <returns>The relative <see cref="SerializedProperty"/> if found; otherwise, null.</returns>
        public static SerializedProperty Find<TSource, TValue>(this SerializedProperty obj, Expression<Func<TSource, TValue>> expr)
        {
            var path = CoreEditorUtils.FindProperty(expr);
            return obj.FindPropertyRelative(path);
        }
    }
}
