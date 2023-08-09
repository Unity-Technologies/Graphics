using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.VFX;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    // TODO Move this
    // Must match enum in C++
    enum VFXCoordinateSpace
    {
        Local = 0,
        World = 1,
        None = int.MaxValue
    }

    // TODO Move this
    interface ISpaceable
    {
        VFXCoordinateSpace space { get; set; }
    }

    interface IVFXDataGetter
    {
        VFXData GetData();
    }

    abstract class VFXData : VFXModel
    {
        public abstract VFXDataType type { get; }

        public virtual uint staticSourceCount
        {
            get
            {
                return 0u;
            }
        }

        public IEnumerable<VFXContext> owners
        {
            get { return m_Owners; }
        }

        public IEnumerable<VFXContext> compilableOwners
        {
            get { return owners.Where(o => o.CanBeCompiled()); }
        }

        public string title;

        public virtual IEnumerable<string> additionalHeaders
        {
            get { return Enumerable.Empty<string>(); }
        }

        public static VFXData CreateDataType(VFXDataType type)
        {
            switch (type)
            {
                case VFXDataType.Particle:
                    return ScriptableObject.CreateInstance<VFXDataParticle>();
                case VFXDataType.ParticleStrip:
                    {
                        var data = ScriptableObject.CreateInstance<VFXDataParticle>();
                        data.SetSettingValue("dataType", VFXDataParticle.DataType.ParticleStrip);
                        return data;
                    }
                case VFXDataType.Mesh:
                    return ScriptableObject.CreateInstance<VFXDataMesh>();
                case VFXDataType.SpawnEvent:
                    return ScriptableObject.CreateInstance<VFXDataSpawner>();
                case VFXDataType.OutputEvent:
                    return ScriptableObject.CreateInstance<VFXDataOutputEvent>();
                default: return null;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_Owners == null)
                m_Owners = new List<VFXContext>();
            else
            {
                // Remove bad references if any
                // The code below was replaced because it caused some strange crashes for unknown reasons
                //int nbRemoved = m_Owners.RemoveAll(o => o == null);
                int nbRemoved = 0;
                for (int i = 0; i < m_Owners.Count; ++i)
                    if (m_Owners[i] == null)
                    {
                        m_Owners.RemoveAt(i--);
                        ++nbRemoved;
                    }

                if (nbRemoved > 0)
                    Debug.LogWarning(String.Format("Remove {0} owners that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            if (cause == InvalidationCause.kSettingChanged) // As data settings are supposed to be implicitely context settings at the same time, throw an invalidate for each contexts
                foreach (VFXContext owner in owners)
                    owner.Invalidate(owner, cause);
        }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);

            if (m_Parent != null)
            {
                Detach();
            }
        }

        protected override void OnAdded()
        {
            throw new InvalidOperationException("VFXData cannot be attached to a VFXModel but are referenced in VFXContext");
        }

        public abstract void CopySettings<T>(T dst) where T : VFXData;

        public virtual bool CanBeCompiled()
        {
            return true;
        }

        public virtual void FillDescs(
            VFXCompileErrorReporter reporter,
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXTemporaryGPUBufferDesc> outTemporaryBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            VFXDependentBuffersData dependentBuffers,
            Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks,
            Dictionary<VFXData, uint> dataToSystemIndex,
            VFXSystemNames systemNames = null)
        {
            // Empty implementation by default
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextAdded(VFXContext context)
        {
            if (context == null)
                throw new ArgumentNullException();
            if (m_Owners.Contains(context))
                throw new ArgumentException(string.Format("{0} is already in the owner list of {1}", context, this));

            m_Owners.Add(context);
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextRemoved(VFXContext context)
        {
            if (!m_Owners.Remove(context))
                throw new ArgumentException(string.Format("{0} is not in the owner list of {1}", context, this));
        }

        public bool IsCurrentAttributeRead(VFXAttribute attrib) { return (GetAttributeMode(attrib) & VFXAttributeMode.Read) != 0; }
        public bool IsCurrentAttributeWritten(VFXAttribute attrib) { return (GetAttributeMode(attrib) & VFXAttributeMode.Write) != 0; }

        public bool IsCurrentAttributeRead(VFXAttribute attrib, VFXContext context) { return (GetAttributeMode(attrib, context) & VFXAttributeMode.Read) != 0; }
        public bool IsCurrentAttributeWritten(VFXAttribute attrib, VFXContext context) { return (GetAttributeMode(attrib, context) & VFXAttributeMode.Write) != 0; }

        public bool IsAttributeUsed(VFXAttribute attrib) { return GetAttributeMode(attrib) != VFXAttributeMode.None; }
        public bool IsAttributeUsed(VFXAttribute attrib, VFXContext context) { return GetAttributeMode(attrib, context) != VFXAttributeMode.None; }

        public bool IsCurrentAttributeUsed(VFXAttribute attrib) { return (GetAttributeMode(attrib) & VFXAttributeMode.ReadWrite) != 0; }
        public bool IsCurrentAttributeUsed(VFXAttribute attrib, VFXContext context) { return (GetAttributeMode(attrib, context) & VFXAttributeMode.ReadWrite) != 0; }

        public bool IsSourceAttributeUsed(VFXAttribute attrib) { return (GetAttributeMode(attrib) & VFXAttributeMode.ReadSource) != 0; }
        public bool IsSourceAttributeUsed(VFXAttribute attrib, VFXContext context) { return (GetAttributeMode(attrib, context) & VFXAttributeMode.ReadSource) != 0; }

        public bool IsAttributeLocal(VFXAttribute attrib) { return m_LocalCurrentAttributes.Contains(attrib); }
        public bool IsAttributeStored(VFXAttribute attrib) { return m_StoredCurrentAttributes.ContainsKey(attrib); }

        public VFXAttributeMode GetAttributeMode(VFXAttribute attrib, VFXContext context)
        {
            Dictionary<VFXContext, VFXAttributeMode> contexts;
            if (m_AttributesToContexts.TryGetValue(attrib, out contexts))
            {
                foreach (var c in contexts)
                    if (c.Key == context)
                        return c.Value;
            }

            return VFXAttributeMode.None;
        }

        public VFXAttributeMode GetAttributeMode(VFXAttribute attrib)
        {
            VFXAttributeMode mode = VFXAttributeMode.None;
            Dictionary<VFXContext, VFXAttributeMode> contexts;
            if (m_AttributesToContexts.TryGetValue(attrib, out contexts))
            {
                foreach (var context in contexts)
                    mode |= context.Value;
            }

            return mode;
        }

        public int GetNbAttributes()
        {
            return m_AttributesToContexts.Count;
        }

        public IEnumerable<VFXAttributeInfo> GetAttributes()
        {
            foreach (var attrib in m_AttributesToContexts)
            {
                VFXAttributeInfo info;
                info.attrib = attrib.Key;
                info.mode = VFXAttributeMode.None;

                foreach (var context in attrib.Value)
                    info.mode |= context.Value;

                yield return info;
            }
        }

        public IEnumerable<VFXAttributeInfo> GetAttributesForContext(VFXContext context)
        {
            Dictionary<VFXAttribute, VFXAttributeMode> attribs;
            if (m_ContextsToAttributes.TryGetValue(context, out attribs))
            {
                foreach (var attrib in attribs)
                {
                    VFXAttributeInfo info;
                    info.attrib = attrib.Key;
                    info.mode = attrib.Value;
                    yield return info;
                }
            }
            else
                throw new ArgumentException("Context does not exist");
        }

        private struct VFXAttributeInfoContext
        {
            public VFXAttributeInfo[] attributes;
            public VFXContext context;
        }

        public abstract VFXDeviceTarget GetCompilationTarget(VFXContext context);

        // Create implicit contexts and initialize cached contexts list
        public virtual IEnumerable<VFXContext> InitImplicitContexts()
        {
            m_Contexts = compilableOwners.ToList();
            return Enumerable.Empty<VFXContext>();
        }

        public void CollectAttributes()
        {
            if (m_Contexts == null) // Context hasnt been initialized (may happen in unity tests but not during actual compilation)
                InitImplicitContexts();

            var allDepenciesIn =
                m_Contexts.Where(c => c.contextType == VFXContextType.Init)
                    .SelectMany(c => c.inputContexts.Where(i => i.contextType == VFXContextType.SpawnerGPU))
                    .SelectMany(c => c.allLinkedInputSlot)
                    .Where(s =>
                    {
                        if (s.owner is VFXBlock)
                        {
                            VFXBlock block = (VFXBlock)(s.owner);
                            if (block.enabled)
                                return true;
                        }
                        else if (s.owner is VFXContext)
                        {
                            return true;
                        }

                        return false;
                    })
                    .Select(s => ((VFXModel)s.owner).GetFirstOfType<VFXContext>())
                    .Select(c => c.GetData());

            m_DependenciesIn = new HashSet<VFXData>(allDepenciesIn.Where(c => c.CanBeCompiled()));
            m_DependenciesInNotCompilable = new HashSet<VFXData>(allDepenciesIn.Where(c => !c.CanBeCompiled()));

            var allDependenciesOut =
                owners.SelectMany(o => o.allLinkedOutputSlot)
                    .Select(s => (VFXContext)s.owner)
                    .SelectMany(c => c.outputContexts)
                    .Select(c => c.GetData());

            m_DependenciesOut = new HashSet<VFXData>(allDependenciesOut.Where(c => c.CanBeCompiled()));
            m_DependenciesOutNotCompilable = new HashSet<VFXData>(allDependenciesOut.Where(c => !c.CanBeCompiled()));

            m_ContextsToAttributes.Clear();
            m_AttributesToContexts.Clear();
            var processedExp = new HashSet<VFXExpression>();

            bool changed = true;
            int count = 0;
            while (changed)
            {
                ++count;
                var attributeContexts = new List<VFXAttributeInfoContext>();
                foreach (var context in m_Contexts)
                {
                    processedExp.Clear();

                    var attributes = Enumerable.Empty<VFXAttributeInfo>();
                    attributes = attributes.Concat(context.attributes);
                    foreach (var block in context.activeFlattenedChildrenWithImplicit)
                        attributes = attributes.Concat(block.attributes);

                    var mapper = context.GetExpressionMapper(GetCompilationTarget(context));
                    if (mapper != null)
                        foreach (var exp in mapper.expressions)
                            attributes = attributes.Concat(CollectInputAttributes(exp, processedExp));

                    attributeContexts.Add(new VFXAttributeInfoContext
                    {
                        attributes = attributes.ToArray(),
                        context = context
                    });
                }

                changed = false;
                foreach (var context in attributeContexts)
                {
                    foreach (var attribute in context.attributes)
                    {
                        if (AddAttribute(context.context, attribute))
                        {
                            changed = true;
                        }
                    }
                }
            }

            ProcessAttributes();

            //TMP Debug only
            DebugLogAttributes();
        }

        public void ProcessDependencies()
        {
            ComputeLayer();

            // Update attributes
            foreach (var childData in m_DependenciesOut)
            {
                foreach (var attrib in childData.m_ReadSourceAttributes)
                {
                    if (!m_StoredCurrentAttributes.ContainsKey(attrib))
                    {
                        m_LocalCurrentAttributes.Remove(attrib);
                        m_StoredCurrentAttributes.Add(attrib, 0);
                    }
                }
            }
        }

        private static uint ComputeLayer(IEnumerable<VFXData> dependenciesIn)
        {
            if (dependenciesIn.Any())
            {
                return 1u + ComputeLayer(dependenciesIn.SelectMany(o => o.m_DependenciesIn));
            }
            return 0u;
        }

        private void ComputeLayer()
        {
            if (!m_DependenciesIn.Any() && !m_DependenciesOut.Any())
            {
                m_Layer = 0; //Independent system, choose layer 0 anyway.
            }
            else
            {
                m_Layer = ComputeLayer(m_DependenciesIn);
            }
        }

        protected bool HasImplicitInit(VFXAttribute attrib)
        {
            return attrib.Equals(VFXAttribute.Seed)
                || attrib.Equals(VFXAttribute.ParticleId)
                || attrib.Equals(VFXAttribute.SpawnIndex)
                || attrib.Equals(VFXAttribute.SpawnIndexInStrip);
        }

        private void ProcessAttributes()
        {
            m_StoredCurrentAttributes.Clear();
            m_LocalCurrentAttributes.Clear();
            m_ReadSourceAttributes.Clear();
            int contextCount = m_Contexts.Count;
            if (contextCount > 16)
                throw new InvalidOperationException(string.Format("Too many contexts that use particle data {0} > 16", contextCount));

            foreach (var kvp in m_AttributesToContexts)
            {
                bool local = false;
                var attribute = kvp.Key;
                int key = 0;

                bool onlyInit = true;
                bool onlyOutput = true;
                bool onlyUpdateRead = true;
                bool onlyUpdateWrite = true;
                bool needsSpecialInit = HasImplicitInit(attribute);
                bool writtenInInit = needsSpecialInit;
                bool readSourceInInit = false;

                foreach (var kvp2 in kvp.Value)
                {
                    var context = kvp2.Key;
                    if (context.contextType == VFXContextType.Init
                        && (kvp2.Value & VFXAttributeMode.ReadSource) != 0)
                    {
                        readSourceInInit = true;
                    }

                    if (kvp2.Value == VFXAttributeMode.None)
                    {
                        throw new InvalidOperationException("Unexpected attribute mode : " + attribute);
                    }

                    if (kvp2.Value == VFXAttributeMode.ReadSource)
                    {
                        continue;
                    }

                    if (context.contextType != VFXContextType.Init)
                        onlyInit = false;
                    if (context.contextType != VFXContextType.Output && context.contextType != VFXContextType.Filter)
                        onlyOutput = false;
                    if (context.contextType != VFXContextType.Update)
                    {
                        onlyUpdateRead = false;
                        onlyUpdateWrite = false;
                    }
                    else
                    {
                        if ((kvp2.Value & VFXAttributeMode.Read) != 0)
                            onlyUpdateWrite = false;
                        if ((kvp2.Value & VFXAttributeMode.Write) != 0)
                            onlyUpdateRead = false;
                    }

                    if (context.contextType != VFXContextType.Init) // Init isnt taken into account for key computation
                    {
                        int shift = m_Contexts.IndexOf(context) << 1;
                        int value = 0;
                        if ((kvp2.Value & VFXAttributeMode.Read) != 0)
                            value |= 0x01;
                        if (((kvp2.Value & VFXAttributeMode.Write) != 0) && context.contextType == VFXContextType.Update)
                            value |= 0x02;
                        key |= (value << shift);
                    }
                    else if ((kvp2.Value & VFXAttributeMode.Write) != 0)
                        writtenInInit = true;
                }

                if ((key & ~0xAAAAAAAA) == 0) // no read
                    local = true;
                if (onlyUpdateWrite || onlyInit || (!needsSpecialInit && (onlyUpdateRead || onlyOutput))) // no shared atributes
                    local = true;
                if (!writtenInInit && (key & 0xAAAAAAAA) == 0) // no write mask
                    local = true;
                if (VFXAttribute.AllAttributeLocalOnly.Contains(attribute))
                    local = true;

                if (local)
                    m_LocalCurrentAttributes.Add(attribute);
                else
                    m_StoredCurrentAttributes.Add(attribute, key);

                if (readSourceInInit)
                    m_ReadSourceAttributes.Add(attribute);
            }
        }

        public abstract void GenerateAttributeLayout(Dictionary<VFXContext, List<VFXContextLink>[]> effectiveFlowInputLinks);

        public abstract string GetAttributeDataDeclaration(VFXAttributeMode mode);
        public abstract string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location);
        public abstract string GetStoreAttributeCode(VFXAttribute attrib, string value);

        private bool AddAttribute(VFXContext context, VFXAttributeInfo attribInfo)
        {
            if (attribInfo.mode == VFXAttributeMode.None)
                throw new ArgumentException("Cannot add an attribute without mode");

            Dictionary<VFXAttribute, VFXAttributeMode> attribs;
            if (!m_ContextsToAttributes.TryGetValue(context, out attribs))
            {
                attribs = new Dictionary<VFXAttribute, VFXAttributeMode>();
                m_ContextsToAttributes.Add(context, attribs);
            }

            var attrib = attribInfo.attrib;
            var mode = attribInfo.mode;

            bool hasChanged = false;
            if (attribs.ContainsKey(attrib))
            {
                var oldMode = attribs[attrib];
                mode |= attribs[attrib];
                if (mode != oldMode)
                {
                    attribs[attrib] = mode;
                    hasChanged = true;
                }
            }
            else
            {
                attribs[attrib] = mode;
                hasChanged = true;
            }

            if (hasChanged)
            {
                Dictionary<VFXContext, VFXAttributeMode> contexts;
                if (!m_AttributesToContexts.TryGetValue(attrib, out contexts))
                {
                    contexts = new Dictionary<VFXContext, VFXAttributeMode>();
                    m_AttributesToContexts.Add(attrib, contexts);
                }
                contexts[context] = mode;
            }

            return hasChanged;
        }

        // Collect attribute expressions recursively
        private IEnumerable<VFXAttributeInfo> CollectInputAttributes(VFXExpression exp, HashSet<VFXExpression> processed)
        {
            if (!processed.Contains(exp) && exp.Is(VFXExpression.Flags.PerElement)) // Testing per element allows to early out as it is propagated
            {
                processed.Add(exp);

                foreach (var info in exp.GetNeededAttributes())
                    yield return info;

                foreach (var parent in exp.parents)
                {
                    foreach (var info in CollectInputAttributes(parent, processed))
                        yield return info;
                }
            }
        }

        private void DebugLogAttributes()
        {
            if (!VFXViewPreference.advancedLogs)
                return;

            var builder = new StringBuilder();

            builder.AppendLine(string.Format("Attributes for data {0} of type {1}", GetHashCode(), GetType()));
            foreach (var context in m_Contexts)
            {
                Dictionary<VFXAttribute, VFXAttributeMode> attributeInfos;
                if (m_ContextsToAttributes.TryGetValue(context, out attributeInfos))
                {
                    builder.AppendLine(string.Format("\tContext {1} {0}", context.GetHashCode(), context.contextType));
                    foreach (var kvp in attributeInfos)
                        builder.AppendLine(string.Format("\t\tAttribute {0} {1} {2}", kvp.Key.name, kvp.Key.type, kvp.Value));
                }
            }

            if (m_StoredCurrentAttributes.Count > 0)
            {
                builder.AppendLine("--- STORED CURRENT ATTRIBUTES ---");
                foreach (var kvp in m_StoredCurrentAttributes)
                    builder.AppendLine(string.Format("\t\tAttribute {0} {1} {2}", kvp.Key.name, kvp.Key.type, kvp.Value));
            }

            if (m_AttributesToContexts.Count > 0)
            {
                builder.AppendLine("--- LOCAL CURRENT ATTRIBUTES ---");
                foreach (var attrib in m_LocalCurrentAttributes)
                    builder.AppendLine(string.Format("\t\tAttribute {0} {1}", attrib.name, attrib.type));
            }

            Debug.Log(builder.ToString());
        }

        public uint layer
        {
            get
            {
                return m_Layer;
            }
        }

        //Doesn't include not comilable context
        public IEnumerable<VFXData> dependenciesIn
        {
            get
            {
                return m_DependenciesIn;
            }
        }

        public IEnumerable<VFXData> dependenciesOut
        {
            get
            {
                return m_DependenciesOut;
            }
        }

        public IEnumerable<VFXData> allDependenciesIncludingNotCompilable
        {
            get
            {
                var all = Enumerable.Empty<VFXData>();
                all = all.Concat(m_DependenciesIn);
                all = all.Concat(m_DependenciesOut);
                all = all.Concat(m_DependenciesInNotCompilable);
                all = all.Concat(m_DependenciesOutNotCompilable);
                return all;
            }
        }

        [SerializeField]
        protected List<VFXContext> m_Owners;

        [NonSerialized]
        protected List<VFXContext> m_Contexts;

        [NonSerialized]
        protected Dictionary<VFXContext, Dictionary<VFXAttribute, VFXAttributeMode>> m_ContextsToAttributes = new Dictionary<VFXContext, Dictionary<VFXAttribute, VFXAttributeMode>>();
        [NonSerialized]
        protected Dictionary<VFXAttribute, Dictionary<VFXContext, VFXAttributeMode>> m_AttributesToContexts = new Dictionary<VFXAttribute, Dictionary<VFXContext, VFXAttributeMode>>();

        [NonSerialized]
        protected Dictionary<VFXAttribute, int> m_StoredCurrentAttributes = new Dictionary<VFXAttribute, int>();
        [NonSerialized]
        protected HashSet<VFXAttribute> m_LocalCurrentAttributes = new HashSet<VFXAttribute>();

        [NonSerialized]
        protected HashSet<VFXAttribute> m_ReadSourceAttributes = new HashSet<VFXAttribute>();

        [NonSerialized]
        protected HashSet<VFXData> m_DependenciesIn = new HashSet<VFXData>();

        [NonSerialized]
        protected HashSet<VFXData> m_DependenciesInNotCompilable = new HashSet<VFXData>();

        [NonSerialized]
        protected HashSet<VFXData> m_DependenciesOut = new HashSet<VFXData>();

        [NonSerialized]
        protected HashSet<VFXData> m_DependenciesOutNotCompilable = new HashSet<VFXData>();

        [NonSerialized]
        protected uint m_Layer;
    }
}
