using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using UnityEngine.Assertions;

namespace UnityEditor.Experimental.Rendering
{
    public class PropertyFetcher<T> : IDisposable
    {
        public readonly SerializedObject obj;

        public PropertyFetcher(SerializedObject obj)
        {
            Assert.IsNotNull(obj);
            this.obj = obj;
        }

        public SerializedProperty FindProperty(string str)
        {
            return obj.FindProperty(str);
        }

        public SerializedProperty FindProperty<TValue>(Expression<Func<T, TValue>> expr)
        {
            // Get the field path as a string
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            var path = sb.ToString();

            // Fetch the SerializedProperty using Unity's method
            return obj.FindProperty(path);
        }

        public void Dispose()
        {
            // Nothing to do here, still needed so we can rely on the using/IDisposable pattern
        }
    }
}
