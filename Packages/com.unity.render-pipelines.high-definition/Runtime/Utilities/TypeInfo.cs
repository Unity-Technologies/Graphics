using System;

namespace UnityEngine.Rendering.HighDefinition
{
    static class TypeInfo
    {
        struct EnumInfoJITCache<TEnum>
            // closest way to constraint to an enum without 'Enum' generic constraint
            where TEnum : struct, IConvertible
        {
            public static readonly TEnum[] values;
            public static readonly string[] names;
            public static readonly int length;

            static EnumInfoJITCache()
            {
                if (!typeof(TEnum).IsEnum)
                    throw new InvalidOperationException(string.Format("{0} must be an enum type.", typeof(TEnum)));

                names = Enum.GetNames(typeof(TEnum));
                length = names.Length;
                values = new TEnum[length];
                var v = Enum.GetValues(typeof(TEnum));
                for (int i = 0; i < values.Length; ++i)
                    values[i] = (TEnum)v.GetValue(i);
            }
        }

        public static TEnum[] GetEnumValues<TEnum>()
            where TEnum : struct, IConvertible
        { return EnumInfoJITCache<TEnum>.values; }

        public static int GetEnumLength<TEnum>()
            where TEnum : struct, IConvertible
        { return EnumInfoJITCache<TEnum>.length; }
        public static string[] GetEnumNames<TEnum>()
            where TEnum : struct, IConvertible
        { return EnumInfoJITCache<TEnum>.names; }

        public static TEnum GetEnumLastValue<TEnum>()
            where TEnum : struct, IConvertible
        { return GetEnumValues<TEnum>()[GetEnumLength<TEnum>() - 1]; }
    }
}
