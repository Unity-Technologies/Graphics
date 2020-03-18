using System;
using System.Linq.Expressions;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering
{
    public sealed class PropertyFetcher<T> : IDisposable
    {
        public readonly SerializedObject obj;

        public PropertyFetcher(SerializedObject obj)
        {
            Assert.IsNotNull(obj);
            this.obj = obj;
        }

        public SerializedProperty Find(string str)
        {
            return obj.FindProperty(str);
        }
        
        /// <summary>
        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.
        /// </summary>
        public SerializedProperty Find<TValue>(Expression<Func<T, TValue>> expr)
        {
            string path = CoreEditorUtils.FindProperty(expr);
            return obj.FindProperty(path);
        }

        public void Dispose()
        {
            // Nothing to do here, still needed so we can rely on the using/IDisposable pattern
        }
    }

    public sealed class RelativePropertyFetcher<T> : IDisposable
    {
        public readonly SerializedProperty obj;

        public RelativePropertyFetcher(SerializedProperty obj)
        {
            Assert.IsNotNull(obj);
            this.obj = obj;
        }

        public SerializedProperty Find(string str)
        {
            return obj.FindPropertyRelative(str);
        }

        /// <summary>
        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.
        /// </summary>
        public SerializedProperty Find<TValue>(Expression<Func<T, TValue>> expr)
        {
            string path = CoreEditorUtils.FindProperty(expr);
            return obj.FindPropertyRelative(path);
        }

        public void Dispose()
        {
            // Nothing to do here, still needed so we can rely on the using/IDisposable pattern
        }
    }

    public static class PropertyFetcherExtensions
    {
        /// <summary>
        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.
        /// </summary>
        public static SerializedProperty Find<TSource, TValue>(this SerializedObject obj, Expression<Func<TSource, TValue>> expr)
        {
            var path = CoreEditorUtils.FindProperty(expr);
            return obj.FindProperty(path);
        }

        /// <summary>
        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.
        /// </summary>
        public static SerializedProperty Find<TSource, TValue>(this SerializedProperty obj, Expression<Func<TSource, TValue>> expr)
        {
            var path = CoreEditorUtils.FindProperty(expr);
            return obj.FindPropertyRelative(path);
        }
    }
}
