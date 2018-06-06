//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using UnityEditor.ShaderGraph;
//using UnityEngine;
//using UnityEngine.AI;
//using Utf8Json;
//using Utf8Json.Internal;
//
//namespace UnityEditor.Importers
//{
//    public class UpgradeFormatter<T> : IJsonFormatter<T>
//    {
//        struct VersionInfo
//        {
//            public Type type;
//            public MethodInfo upgradeMethod;
//        }
//
//        readonly AutomataDictionary m_KeyMapping;
//        readonly byte[] m_VersionByteKey;
//        readonly byte[] m_VersionsByteKey;
//        readonly byte[] m_DataByteKey;
//        readonly List<VersionInfo> m_Versions;
//
//        public UpgradeFormatter()
//        {
//            m_KeyMapping = new AutomataDictionary
//            {
//                { JsonWriter.GetEncodedPropertyNameWithoutQuotation("version"), 0 },
//                { JsonWriter.GetEncodedPropertyNameWithoutQuotation("versions"), 1 },
//                { JsonWriter.GetEncodedPropertyNameWithoutQuotation("data"), 2 }
//            };
//
//            m_VersionByteKey = JsonWriter.GetEncodedPropertyNameWithBeginObject("version");
//            m_VersionsByteKey = JsonWriter.GetEncodedPropertyNameWithBeginObject("versions");
//            m_DataByteKey = JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("data");
//
////            {
////                var attributes = typeof(T).GetCustomAttributes(typeof(FormerNameAttribute), false);
////            }
//
//            m_Versions = new List<VersionInfo>();
//
//            // Iterate through the upgrade chain.
//            var type = typeof(T);
//            while (true)
//            {
//                var attributes = type.GetCustomAttributes(typeof(JsonVersionedAttribute), false);
//                if (attributes.Length == 0)
//                    break;
//
//                var attribute = (JsonVersionedAttribute)attributes[0];
//                if (attribute.previousVersionType == null)
//                    break;
//
//                var upgradableToType = typeof(IUpgradableTo<>).MakeGenericType(type);
//
//                // Test that the type specific in the attribute can be upgraded to the type that has the attribute
//                if (!upgradableToType.IsAssignableFrom(attribute.previousVersionType))
//                    throw new InvalidOperationException(string.Format("Type {0} cannot use {1} as an argument to {2} because it doesn't implement {3}", type, attribute.previousVersionType, typeof(JsonVersionedAttribute), upgradableToType));
//
//                m_Versions.Add(new VersionInfo
//                {
//                    type = attribute.previousVersionType,
//                    upgradeMethod = upgradableToType.GetMethod("Upgrade")
//                });
//
//                type = attribute.previousVersionType;
//            }
//
//            // Make the index match the version.
//            m_Versions.Reverse();
//        }
//
//        public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
//        {
//            var upgradeResolver = (UpgradeResolver)formatterResolver;
//            var currentFormatter = upgradeResolver.GetCurrentFormatter<T>();
//
//            writer.WriteRaw(m_VersionByteKey);
//            writer.WriteInt32(m_Versions.Count);
//            writer.WriteRaw(m_DataByteKey);
//            currentFormatter.Serialize(ref writer, value, formatterResolver);
//            writer.WriteEndObject();
//        }
//
//        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
//        {
//            if (reader.ReadIsNull())
//                return default(T);
//
//            // If we don't find a version field, we assume the version is 0.
//            var versionFound = false;
//            var version = 0;
//            var dataOffset = -1;
//            var count = 0;
//            reader.ReadIsBeginObjectWithVerify();
//            while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref count))
//            {
//                var stringKey = reader.ReadPropertyNameSegmentRaw();
//                int key;
//                if (!m_KeyMapping.TryGetValueSafe(stringKey, out key))
//                {
//                    // Found unknown key, skip the block.
//                    reader.ReadNextBlock();
//
//                    continue;
//                }
//
//                // yay, we found a field.
//
//                if (key == 0)
//                {
//                    // "version" field
//                    version = reader.ReadInt32();
//                    versionFound = true;
//                }
//                else if (key == 1)
//                {
//                    // "versions" field
//                    reader.ReadIsBeginArrayWithVerify();
//                    var arrayCount = 0;
//                    while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref arrayCount))
//                    {
//                        var value = reader.ReadInt32();
//                        if (arrayCount == 0)
//                            version = value;
//                    }
//
//                    versionFound = true;
//                }
//                else if (key == 2)
//                {
//                    // "data" field
//                    // We handle two cases:
//                    // A: The version field comes before the data field. (Fast path)
//                    //    In this case we simply break the loop immediately after reading `"data":`, as we're then
//                    //    at the point that we want to be when we call the inner formatter. The advantage is that we
//                    //    don't have to read past the data only to move it back and then read it again.
//                    // B: The data field comes before the version field. (Slow path)
//                    //    We really only handle this because we don't want to rely on a specific key ordering. If
//                    //    this is the case, we record the offset after `"data":` and let the loop run till it has
//                    //    read the entire object. We then roll back the reader to that point later on when we need
//                    //    to run the inner formatter on the contents of "data". This is the slow path, which is only
//                    //    here to make sure we can read weirdly ordered JSON.
//                    if (versionFound)
//                        break;
//
//                    dataOffset = reader.GetCurrentOffsetUnsafe();
//                    reader.ReadNextBlock();
//                }
//                else
//                {
//                    // Ignore unknown fields
//                    reader.ReadNextBlock();
//                }
//            }
//
//            // If case B occured, we know that we have read the entire object, and so we record the offset immediately
//            // after the object ends, such that we can advance to this offset later on.
//            var endOffset = -1;
//            if (dataOffset != -1)
//            {
//                endOffset = reader.GetCurrentOffsetUnsafe();
//
//                // Advancing with a negative offset rolls back the reader. ¯\_(ツ)_/¯
//                reader.AdvanceOffset(dataOffset - endOffset);
//            }
//
//            // At this point the reader is guaranteed to be right before the "data" contents.
//            var exceptionThrown = false;
//            try
//            {
//                var upgradeResolver = (UpgradeResolver)formatterResolver;
//
//                // If the serialized version is the latest version, just use the formatter for that one.
//                // Avoid slow reflection as we know the type statically and this is the fast path.
//                if (version == m_Versions.Count)
//                {
//                    var currentFormatter = upgradeResolver.GetCurrentFormatter<T>();
//                    var inst = currentFormatter.Deserialize(ref reader, formatterResolver);
//
//                    return inst;
//                }
//
//                // Can't deserialize newer versions than the current one.
//                if (version > m_Versions.Count)
//                    throw new InvalidOperationException("The value was serialized using a newer version than the current one.");
//
//                // Negative versions make no sense, silly.
//                if (version < 0)
//                    throw new InvalidOperationException("Invalid version.");
//
//                // Deserialize the version of the type we're reading.
//                var initialVersionInfo = m_Versions[version];
//                var formatter = upgradeResolver.GetCurrentFormatterDynamic(initialVersionInfo.type);
//                var deserializeParameters = new object[] { reader, upgradeResolver };
//                var deserializeMethod = formatter.GetType().GetInterfaces().First(t => t == typeof(IJsonFormatter<>).MakeGenericType(initialVersionInfo.type)).GetMethod("Deserialize");
//                var instance = deserializeMethod.Invoke(formatter, deserializeParameters);
//
//                // When passing a ref parameter via reflection, the value in the parameter array is updated after the call.
//                // Thus we need to set reader to that value to mimic passing `ref reader`.
//                reader = (JsonReader)deserializeParameters[0];
//
//                // Upgrade to the latest version one version at a time.
//                for (var i = version; i < m_Versions.Count; i++)
//                {
//                    var versionInfo = m_Versions[i];
//                    instance = versionInfo.upgradeMethod.Invoke(instance, new object[] { });
//                }
//
//                return (T)instance;
//            }
//            catch (Exception)
//            {
//                exceptionThrown = true;
//                throw;
//            }
//            finally
//            {
//                // We don't bother resetting the state if an exception was thrown, as we're likely to just cause another
//                // exception and thus obscure the actual exception.
//                if (!exceptionThrown)
//                {
//                    // Since we're done reading JSON now, we'd like to move the reader to the end of the object.
//                    if (dataOffset == -1)
//                    {
//                        // Case A (aka fast path) we just read the rest of the object. There should be any more keys.
//                        if (!reader.ReadIsEndObjectWithSkipValueSeparator(ref count))
//                            throw new InvalidOperationException("Expected object to end.");
//                    }
//                    else
//                    {
//                        // Case B (aka slow path) we already know where the end is, so we advance the reader to that point.
//                        reader.AdvanceOffset(endOffset - reader.GetCurrentOffsetUnsafe());
//                    }
//                }
//            }
//        }
//    }
//}
