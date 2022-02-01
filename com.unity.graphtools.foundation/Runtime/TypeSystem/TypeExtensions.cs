using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for <see cref="Type"/>.
    /// </summary>
    public static class TypeExtensions
    {
        static readonly Dictionary<Type, string> k_TypeToFriendlyName = new Dictionary<Type, string>
        {
            { typeof(string),  "String" },
            { typeof(object),  "System.Object" },
            { typeof(bool),    "Boolean" },
            { typeof(byte),    "Byte" },
            { typeof(char),    "Char" },
            { typeof(decimal), "Decimal" },
            { typeof(double),  "Double" },
            { typeof(short),   "Short" },
            { typeof(int),     "Integer" },
            { typeof(long),    "Long" },
            { typeof(sbyte),   "SByte" },
            { typeof(float),   "Float" },
            { typeof(ushort),  "Unsigned Short" },
            { typeof(uint),    "Unsigned Integer" },
            { typeof(ulong),   "Unsigned Long" },
            { typeof(void),    "Void" },
            { typeof(Color),   "Color"},
            { typeof(Object), "UnityEngine.Object"},
            { typeof(Vector2), "Vector 2"},
            { typeof(Vector3), "Vector 3"},
            { typeof(Vector4), "Vector 4"}
        };

        /// <summary>
        /// Returns a human readable name for a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type for which to get the name.</param>
        /// <param name="expandGeneric">Set to true if generic parameters should be included.</param>
        /// <returns>The human readable name for the <see cref="Type"/></returns>
        public static string FriendlyName(this Type type, bool expandGeneric = true)
        {
            if (k_TypeToFriendlyName.TryGetValue(type, out var friendlyName))
            {
                return friendlyName;
            }

            friendlyName = type.Name;

            if (type.IsGenericType && expandGeneric)
            {
                int backtick = friendlyName.IndexOf('`');
                if (backtick > 0)
                {
                    friendlyName = friendlyName.Remove(backtick);
                }
                friendlyName += " of ";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    string typeParamName = typeParameters[i].FriendlyName();
                    friendlyName += (i == 0 ? typeParamName : " and " + typeParamName);
                }
            }

            if (type.IsArray)
            {
                return type.GetElementType().FriendlyName() + "[]";
            }

            return friendlyName;
        }

        [Obsolete("0.10+ This method will be removed from GTF public API")]
        public static bool IsNumeric(this Type self)
        {
            return IsNumericInternal(self);
        }

        internal static bool IsNumericInternal(this Type self)
        {
            switch (Type.GetTypeCode(self))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        [Obsolete("0.10+ This method will be removed from GTF public API")]
        public static bool HasNumericConversionTo(this Type self, Type other)
        {
            return HasNumericConversionToInternal(self, other);
        }

        internal static bool HasNumericConversionToInternal(this Type self, Type other)
        {
            return self.IsNumericInternal() && other.IsNumericInternal();
        }

        /// <inheritdoc cref="TypeHandleHelpers.GenerateTypeHandle(Type)"/>
        public static TypeHandle GenerateTypeHandle(this Type t)
        {
            return TypeHandleHelpers.GenerateTypeHandle(t);
        }
    }
}
