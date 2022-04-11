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

        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.

        /// <summary>
        /// Find a property based on an expression.
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

        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.

        /// <summary>
        /// Find a property based on an expression.
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
        ///
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="obj"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static SerializedProperty Find<TSource, TValue>(this SerializedObject obj, Expression<Func<TSource, TValue>> expr)
        {
            var path = CoreEditorUtils.FindProperty(expr);
            return obj.FindProperty(path);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="obj"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static SerializedProperty Find<TSource, TValue>(this SerializedProperty obj, Expression<Func<TSource, TValue>> expr)
        {
            var path = CoreEditorUtils.FindProperty(expr);
            return obj.FindPropertyRelative(path);
        }
    }
}
