using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class MultiJsonInternal
    {
        #region Unknown Data Handling
        public class UnknownJsonObject : JsonObject
        {
            public string typeInfo;
            public string jsonData;
            public JsonData<JsonObject> castedObject;

            public UnknownJsonObject(string typeInfo)
            {
                this.typeInfo = typeInfo;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
            }

            public override string Serialize()
            {
                return jsonData;
            }

            public override void OnAfterDeserialize(string json)
            {
                if (castedObject.value != null)
                {
                    Enqueue(castedObject, json.Trim());
                }
            }

            public override void OnAfterMultiDeserialize(string json)
            {
                if (castedObject.value == null)
                {
                    //Never got casted so nothing ever reffed this object
                    //likely that some other unknown json object had a ref
                    //to this thing. Need to include it in the serialization
                    //step of the object still.
                    if (jsonBlobs.TryGetValue(currentRoot.objectId, out var blobs))
                    {
                        blobs[objectId] = jsonData.Trim();
                    }
                    else
                    {
                        var lookup = new Dictionary<string, string>();
                        lookup[objectId] = jsonData.Trim();
                        jsonBlobs.Add(currentRoot.objectId, lookup);
                    }
                }
            }

            public override T CastTo<T>()
            {
                if (castedObject.value != null)
                    return castedObject.value.CastTo<T>();

                Type t = typeof(T);
                if (t == typeof(AbstractMaterialNode) || t.IsSubclassOf(typeof(AbstractMaterialNode)))
                {
                    UnknownNodeType unt = new UnknownNodeType(jsonData);
                    valueMap[objectId] = unt;
                    s_ObjectIdField.SetValue(unt, objectId);
                    castedObject = unt;
                    return unt.CastTo<T>();
                }
                else if (t == typeof(Target) || t.IsSubclassOf(typeof(Target)))
                {
                    UnknownTargetType utt = new UnknownTargetType(typeInfo, jsonData);
                    valueMap[objectId] = utt;
                    s_ObjectIdField.SetValue(utt, objectId);
                    castedObject = utt;
                    return utt.CastTo<T>();
                }
                else if (t == typeof(SubTarget) || t.IsSubclassOf(typeof(SubTarget)))
                {
                    UnknownSubTargetType ustt = new UnknownSubTargetType(typeInfo, jsonData);
                    valueMap[objectId] = ustt;
                    s_ObjectIdField.SetValue(ustt, objectId);
                    castedObject = ustt;
                    return ustt.CastTo<T>();
                }
                else if (t == typeof(ShaderInput) || t.IsSubclassOf(typeof(ShaderInput)))
                {
                    UnknownShaderPropertyType usp = new UnknownShaderPropertyType(typeInfo, jsonData);
                    valueMap[objectId] = usp;
                    s_ObjectIdField.SetValue(usp, objectId);
                    castedObject = usp;
                    return usp.CastTo<T>();
                }
                else if (t == typeof(MaterialSlot) || t.IsSubclassOf(typeof(MaterialSlot)))
                {
                    UnknownMaterialSlotType umst = new UnknownMaterialSlotType(typeInfo, jsonData);
                    valueMap[objectId] = umst;
                    s_ObjectIdField.SetValue(umst, objectId);
                    castedObject = umst;
                    return umst.CastTo<T>();
                }
                else if (t == typeof(AbstractShaderGraphDataExtension) || t.IsSubclassOf(typeof(AbstractShaderGraphDataExtension)))
                {
                    UnknownGraphDataExtension usge = new UnknownGraphDataExtension(typeInfo, jsonData);
                    valueMap[objectId] = usge;
                    s_ObjectIdField.SetValue(usge, objectId);
                    castedObject = usge;
                    return usge.CastTo<T>();
                }
                else
                {
                    Debug.LogError($"Unable to evaluate type {typeInfo} : {jsonData}");
                }
                return null;
            }
        }

        public class UnknownTargetType : Target
        {
            public string jsonData;
            public UnknownTargetType() : base()
            {
                isHidden = true;
            }

            private List<BlockFieldDescriptor> m_activeBlocks = null;

            public UnknownTargetType(string displayName, string jsonData)
            {
                var split = displayName.Split('.');
                var last = split[split.Length - 1];
                this.displayName = last.Replace("Target", "") + " (Unknown)";
                isHidden = false;
                this.jsonData = jsonData;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
                base.Deserailize(typeInfo, jsonData);
            }

            public override string Serialize()
            {
                return jsonData.Trim();
            }

            //When we first call GetActiveBlocks, we assume any unknown blockfielddescriptors are owned by this target
            public override void GetActiveBlocks(ref TargetActiveBlockContext context)
            {
                if (m_activeBlocks == null)
                {
                    m_activeBlocks = new List<BlockFieldDescriptor>();
                    foreach (var cur in context.currentBlocks)
                    {
                        if (cur.isUnknown && !string.IsNullOrEmpty(cur.displayName))
                        {
                            m_activeBlocks.Add(cur);
                        }
                    }
                }

                foreach (var block in m_activeBlocks)
                {
                    context.AddBlock(block);
                }
            }

            public override void GetFields(ref TargetFieldContext context)
            {
            }

            public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
            {
                context.AddHelpBox(MessageType.Warning, "Cannot find the code for this Target, a package may be missing.");
            }

            public override bool IsActive() => false;

            public override void Setup(ref TargetSetupContext context)
            {
            }

            public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline) => false;
        }

        private class UnknownSubTargetType : SubTarget
        {
            public string jsonData;
            public UnknownSubTargetType() : base()
            {
                isHidden = true;
            }

            public UnknownSubTargetType(string displayName, string jsonData) : base()
            {
                isHidden = false;
                this.displayName = displayName;
                this.jsonData = jsonData;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
                base.Deserailize(typeInfo, jsonData);
            }

            public override string Serialize()
            {
                return jsonData.Trim();
            }

            internal override Type targetType => typeof(UnknownTargetType);

            public override void GetActiveBlocks(ref TargetActiveBlockContext context)
            {
            }

            public override void GetFields(ref TargetFieldContext context)
            {
            }

            public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
            {
                context.AddHelpBox(MessageType.Warning, "Cannot find the code for this SubTarget, a package may be missing.");
            }

            public override bool IsActive() => false;

            public override void Setup(ref TargetSetupContext context)
            {
            }
        }

        internal class UnknownShaderPropertyType : AbstractShaderProperty
        {
            public string jsonData;

            public UnknownShaderPropertyType(string displayName, string jsonData) : base()
            {
                this.displayName = displayName;
                this.jsonData = jsonData;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
                base.Deserailize(typeInfo, jsonData);
            }

            public override string Serialize()
            {
                return jsonData.Trim();
            }

            internal override ConcreteSlotValueType concreteShaderValueType => ConcreteSlotValueType.Vector1;
            internal override bool isExposable => false;
            internal override bool isRenamable => false;
            internal override ShaderInput Copy()
            {
                // we CANNOT copy ourselves, as the serialized GUID in the jsonData would not match the json GUID
                return null;
            }

            public override PropertyType propertyType => PropertyType.Float;
            internal override void GetPropertyReferenceNames(List<string> result) { }
            internal override void GetPropertyDisplayNames(List<string> result) { }
            internal override string GetPropertyBlockString() { return ""; }
            internal override void AppendPropertyBlockStrings(ShaderStringBuilder builder)
            {
                builder.AppendLine("/* UNKNOWN PROPERTY: " + referenceName + " */");
            }

            internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => false;
            internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
            {
                action(new HLSLProperty(HLSLType._float, referenceName, HLSLDeclaration.Global, concretePrecision));
            }

            internal override string GetPropertyAsArgumentString(string precisionString) { return ""; }
            internal override AbstractMaterialNode ToConcreteNode() { return null; }

            internal override PreviewProperty GetPreviewMaterialProperty()
            {
                return new PreviewProperty(propertyType)
                {
                    name = referenceName,
                    floatValue = 0.0f
                };
            }

            public override string GetPropertyTypeString() { return ""; }
        }

        internal class UnknownMaterialSlotType : MaterialSlot
        {
            // used to deserialize some data out of an unknown MaterialSlot
            class SerializerHelper
            {
                [SerializeField]
                public string m_DisplayName = null;

                [SerializeField]
                public SlotType m_SlotType = SlotType.Input;

                [SerializeField]
                public bool m_Hidden = false;

                [SerializeField]
                public string m_ShaderOutputName = null;

                [SerializeField]
                public ShaderStageCapability m_StageCapability = ShaderStageCapability.All;
            }

            public string jsonData;

            public UnknownMaterialSlotType(string displayName, string jsonData) : base()
            {
                // copy some minimal information to try to keep the UI as similar as possible
                var helper = new SerializerHelper();
                JsonUtility.FromJsonOverwrite(jsonData, helper);
                this.displayName = helper.m_DisplayName;
                this.hidden = helper.m_Hidden;
                this.stageCapability = helper.m_StageCapability;
                this.SetInternalData(helper.m_SlotType, helper.m_ShaderOutputName);

                // save the original json for saving
                this.jsonData = jsonData;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
                base.Deserailize(typeInfo, jsonData);
            }

            public override string Serialize()
            {
                return jsonData.Trim();
            }

            public override bool isDefaultValue => true;

            public override SlotValueType valueType => SlotValueType.Vector1;

            public override ConcreteSlotValueType concreteValueType => ConcreteSlotValueType.Vector1;

            public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode) { }

            public override void CopyValuesFrom(MaterialSlot foundSlot)
            {
                // we CANNOT copy data from another slot, as the GUID in the serialized jsonData would not match our real GUID
                throw new NotSupportedException();
            }
        }

        [NeverAllowedByTarget]
        internal class UnknownNodeType : AbstractMaterialNode
        {
            public string jsonData;

            public UnknownNodeType() : base()
            {
                jsonData = null;
                isValid = false;
                SetOverrideActiveState(ActiveState.ExplicitInactive, false);
                SetActive(false, false);
            }

            public UnknownNodeType(string jsonData)
            {
                this.jsonData = jsonData;
                isValid = false;
                SetOverrideActiveState(ActiveState.ExplicitInactive, false);
                SetActive(false, false);
            }

            public override void OnAfterDeserialize(string json)
            {
                jsonData = json;
                base.OnAfterDeserialize(json);
            }

            public override string Serialize()
            {
                EnqueSlotsForSerialization();
                return jsonData.Trim();
            }

            public override void ValidateNode()
            {
                base.ValidateNode();
                owner.AddValidationError(objectId, "This node type could not be found. No function will be generated in the shader.", ShaderCompilerMessageSeverity.Warning);
            }

            // unknown node types cannot be copied, or else their GUID would not match the GUID in the serialized jsonDAta
            public override bool canCutNode => false;
            public override bool canCopyNode => false;
        }

        internal class UnknownGraphDataExtension : AbstractShaderGraphDataExtension
        {
            public string name;
            public string jsonData;
            internal override string displayName => name;

            internal UnknownGraphDataExtension() : base() { }

            internal UnknownGraphDataExtension(string displayName, string jsonData)
            {
                name = displayName;
                this.jsonData = jsonData;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
                base.Deserailize(typeInfo, jsonData);
            }

            public override string Serialize()
            {
                return jsonData.Trim();
            }

            internal override void OnPropertiesGUI(VisualElement context, Action onChange, Action<string> registerUndo, GraphData owner)
            {
                var helpBox = new HelpBoxRow(MessageType.Info);
                helpBox.Add(new Label("Cannot find the code for this data extension, a package may be missing."));
                context.hierarchy.Add(helpBox);
            }
        }
        #endregion //Unknown Data Handling

        static readonly Dictionary<string, Type> k_TypeMap = CreateTypeMap();

        internal static bool isDeserializing;

        internal static readonly Dictionary<string, JsonObject> valueMap = new Dictionary<string, JsonObject>();

        static List<MultiJsonEntry> s_Entries;

        internal static bool isSerializing;

        internal static readonly List<JsonObject> serializationQueue = new List<JsonObject>();

        internal static readonly HashSet<string> serializedSet = new HashSet<string>();

        static JsonObject currentRoot = null;

        static Dictionary<string, Dictionary<string, string>> jsonBlobs = new Dictionary<string, Dictionary<string, string>>();

        static Dictionary<string, Type> CreateTypeMap()
        {
            var map = new Dictionary<string, Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<JsonObject>())
            {
                if (type.FullName != null)
                {
                    map[type.FullName] = type;
                }
            }

            foreach (var type in TypeCache.GetTypesWithAttribute(typeof(FormerNameAttribute)))
            {
                if (type.IsAbstract || !typeof(JsonObject).IsAssignableFrom(type))
                {
                    continue;
                }

                foreach (var attribute in type.GetCustomAttributes(typeof(FormerNameAttribute), false))
                {
                    var legacyAttribute = (FormerNameAttribute)attribute;
                    map[legacyAttribute.fullName] = type;
                }
            }

            return map;
        }

        public static Type ParseType(string typeString)
        {
            k_TypeMap.TryGetValue(typeString, out var type);
            return type;
        }

        public static List<MultiJsonEntry> Parse(string str)
        {
            var result = new List<MultiJsonEntry>();
            const string separatorStr = "\n\n";
            var startIndex = 0;
            var raw = new FakeJsonObject();

            while (startIndex < str.Length)
            {
                var jsonBegin = str.IndexOf("{", startIndex, StringComparison.Ordinal);
                if (jsonBegin == -1)
                {
                    break;
                }

                var jsonEnd = str.IndexOf(separatorStr, jsonBegin, StringComparison.Ordinal);
                if (jsonEnd == -1)
                {
                    jsonEnd = str.IndexOf("\n\r\n", jsonBegin, StringComparison.Ordinal);
                    if (jsonEnd == -1)
                    {
                        jsonEnd = str.LastIndexOf("}", StringComparison.Ordinal) + 1;
                    }
                }

                var json = str.Substring(jsonBegin, jsonEnd - jsonBegin);

                JsonUtility.FromJsonOverwrite(json, raw);
                if (startIndex != 0 && string.IsNullOrWhiteSpace(raw.type))
                {
                    throw new InvalidOperationException($"Type is null or whitespace in JSON:\n{json}");
                }

                result.Add(new MultiJsonEntry(raw.type, raw.id, json));
                raw.Reset();

                startIndex = jsonEnd + separatorStr.Length;
            }

            return result;
        }

        public static void Enqueue(JsonObject jsonObject, string json)
        {
            if (s_Entries == null)
            {
                throw new InvalidOperationException("Can only Enqueue during JsonObject.OnAfterDeserialize.");
            }

            valueMap.Add(jsonObject.objectId, jsonObject);
            s_Entries.Add(new MultiJsonEntry(jsonObject.GetType().FullName, jsonObject.objectId, json));
        }

        public static JsonObject CreateInstanceForDeserialization(string typeString)
        {
            if (!k_TypeMap.TryGetValue(typeString, out var type))
            {
                return new UnknownJsonObject(typeString);
            }
            var output = (JsonObject)Activator.CreateInstance(type, true);
            //This CreateInstance function is supposed to essentially create a blank copy of whatever class we end up deserializing into.
            //when we typically create new JsonObjects in all other cases, we want that object to be assumed to be the latest version.
            //This doesn't work if any json object was serialized before we had the idea of version, as the blank copy would have the
            //latest version on creation and since the serialized version wouldn't have a version member, it would not get overwritten
            //and we would automatically upgrade all previously serialized json objects incorrectly and without user action. To avoid this,
            //we default jsonObject version to 0, and if the serialized value has a different saved version it gets changed and if the serialized
            //version does not have a different saved value it remains 0 (earliest version)
            output.ChangeVersion(0);
            output.OnBeforeDeserialize();
            return output;
        }

        private static FieldInfo s_ObjectIdField =
            typeof(JsonObject).GetField("m_ObjectId", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Deserialize(JsonObject root, List<MultiJsonEntry> entries, bool rewriteIds)
        {
            if (isDeserializing)
            {
                throw new InvalidOperationException("Nested MultiJson deserialization is not supported.");
            }

            try
            {
                isDeserializing = true;
                currentRoot = root;
                root.ChangeVersion(0); //Same issue as described in CreateInstance
                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    try
                    {
                        JsonObject value = null;
                        if (index == 0)
                        {
                            value = root;
                        }
                        else
                        {
                            value = CreateInstanceForDeserialization(entry.type);
                        }

                        var id = entry.id;

                        if (id != null)
                        {
                            // Need to make sure that references looking for the old ID will find it in spite of
                            // ID rewriting.
                            valueMap[id] = value;
                        }

                        if (rewriteIds || entry.id == null)
                        {
                            id = value.objectId;
                            entries[index] = new MultiJsonEntry(entry.type, id, entry.json);
                            valueMap[id] = value;
                        }

                        s_ObjectIdField.SetValue(value, id);
                    }
                    catch (Exception e)
                    {
                        // External code could throw exceptions, but we don't want that to fail the whole thing.
                        // Potentially, the fallback type should also be used here.
                        Debug.LogException(e);
                    }
                }

                s_Entries = entries;

                // Not a foreach because `entries` can be populated by calls to `Enqueue` as we go.
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    try
                    {
                        var value = valueMap[entry.id];
                        value.Deserailize(entry.type, entry.json);
                        // Set ID again as it could be overwritten from JSON.
                        s_ObjectIdField.SetValue(value, entry.id);
                        value.OnAfterDeserialize(entry.json);
                    }
                    catch (Exception e)
                    {
                        if (!String.IsNullOrEmpty(entry.id))
                        {
                            var value = valueMap[entry.id];
                            if (value != null)
                            {
                                Debug.LogError($"Exception thrown while deserialize object of type {entry.type}: {e.Message}");
                            }
                        }
                        Debug.LogException(e);
                    }
                }

                s_Entries = null;

                foreach (var entry in entries)
                {
                    try
                    {
                        var value = valueMap[entry.id];
                        value.OnAfterMultiDeserialize(entry.json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                valueMap.Clear();
                currentRoot = null;
                isDeserializing = false;
            }
        }

        public static string Serialize(JsonObject mainObject)
        {
            if (isSerializing)
            {
                throw new InvalidOperationException("Nested MultiJson serialization is not supported.");
            }

            try
            {
                isSerializing = true;

                serializedSet.Add(mainObject.objectId);
                serializationQueue.Add(mainObject);

                var idJsonList = new List<(string, string)>();

                // Not a foreach because the queue is populated by `JsonData<T>`s as we go.
                for (var i = 0; i < serializationQueue.Count; i++)
                {
                    var value = serializationQueue[i];
                    var json = value.Serialize();
                    idJsonList.Add((value.objectId, json));
                }

                if (jsonBlobs.TryGetValue(mainObject.objectId, out var blobs))
                {
                    foreach (var blob in blobs)
                    {
                        if (!idJsonList.Contains((blob.Key, blob.Value)))
                            idJsonList.Add((blob.Key, blob.Value));
                    }
                }


                idJsonList.Sort((x, y) =>
                    // Main object needs to be placed first
                    x.Item1 == mainObject.objectId ? -1 :
                    y.Item1 == mainObject.objectId ? 1 :
                    // We sort everything else by ID to consistently maintain positions in the output
                    x.Item1.CompareTo(y.Item1));


                const string k_NewLineString = "\n";
                var sb = new StringBuilder();
                foreach (var (id, json) in idJsonList)
                {
                    sb.Append(json);
                    sb.Append(k_NewLineString);
                    sb.Append(k_NewLineString);
                }

                return sb.ToString();
            }
            finally
            {
                serializationQueue.Clear();
                serializedSet.Clear();
                isSerializing = false;
            }
        }

        public static void PopulateValueMap(JsonObject mainObject)
        {
            if (isSerializing)
            {
                throw new InvalidOperationException("Nested MultiJson serialization is not supported.");
            }

            try
            {
                isSerializing = true;

                serializedSet.Add(mainObject.objectId);
                serializationQueue.Add(mainObject);

                // Not a foreach because the queue is populated by `JsonRef<T>`s as we go.
                for (var i = 0; i < serializationQueue.Count; i++)
                {
                    var value = serializationQueue[i];
                    value.Serialize();
                    valueMap[value.objectId] = value;
                }
            }
            finally
            {
                serializationQueue.Clear();
                serializedSet.Clear();
                isSerializing = false;
            }
        }
    }
}
