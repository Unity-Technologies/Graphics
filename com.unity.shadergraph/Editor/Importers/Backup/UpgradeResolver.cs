//using System;
//using Utf8Json;
//
//namespace UnityEditor.Importers
//{
//    public class UpgradeResolver : IJsonFormatterResolver
//    {
//        IJsonFormatterResolver m_CurrentResolver;
//
//        public UpgradeResolver(IJsonFormatterResolver currentResolver)
//        {
//            m_CurrentResolver = currentResolver;
//        }
//
//        public IJsonFormatter<T> GetFormatter<T>()
//        {
//            return FormatterCache<T>.formatter ?? m_CurrentResolver.GetFormatter<T>();
//        }
//
//        public IJsonFormatter<T> GetCurrentFormatter<T>()
//        {
//            return m_CurrentResolver.GetFormatter<T>();
//        }
//
//        public object GetCurrentFormatterDynamic(Type type)
//        {
//            return m_CurrentResolver.GetFormatterDynamic(type);
//        }
//
//        static class FormatterCache<T>
//        {
//            public static readonly UpgradeFormatter<T> formatter;
//
//            static FormatterCache()
//            {
//                if (typeof(T).IsDefined(typeof(JsonVersionedAttribute), false))
//                    formatter = new UpgradeFormatter<T>();
//            }
//        }
//    }
//}
