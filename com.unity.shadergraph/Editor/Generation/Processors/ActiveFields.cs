using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Internal
{
    internal interface IActiveFields : KeywordDependentCollection.IInstance, KeywordDependentCollection.ISet<IActiveFields>
    {
        IEnumerable<FieldDescriptor> fields { get; }

        bool Add(FieldDescriptor field);
        bool Contains(FieldDescriptor field);
        bool Contains(string value);
    }

    internal interface IActiveFieldsSet : KeywordDependentCollection.ISet<IActiveFields>
    {
        void AddAll(FieldDescriptor field);
    }

    internal class FieldNamePairStorage
    {
        private HashSet<FieldDescriptor> m_fieldDescriptors;
        private HashSet<string> m_fieldNames;

        public IEnumerable<FieldDescriptor> fields => m_fieldDescriptors;

        public FieldNamePairStorage()
        {
            m_fieldDescriptors = new HashSet<FieldDescriptor>();
            m_fieldNames = new HashSet<string>(StringComparer.Ordinal);
        }

        public IEnumerable<FieldDescriptor> Union(FieldNamePairStorage other)
        {
            var output = new HashSet<FieldDescriptor>(m_fieldDescriptors);
            output.UnionWith(other.m_fieldDescriptors);
            return output;
        }

        public bool Contains(FieldDescriptor fieldDescriptor)
        {
            return m_fieldDescriptors.Contains(fieldDescriptor);
        }

        public bool Contains(string fieldName)
        {
            return m_fieldNames.Contains(fieldName);
        }

        public bool Add(FieldDescriptor fieldDescriptor)
        {
            bool added = m_fieldDescriptors.Add(fieldDescriptor);
            if (added)
            {
                m_fieldNames.Add(fieldDescriptor.ToFieldString());
            }
            return added;
        }
    }

    internal sealed class ActiveFields : KeywordDependentCollection<
        FieldNamePairStorage,
        ActiveFields.All,
        ActiveFields.AllPermutations,
        ActiveFields.ForPermutationIndex,
        ActiveFields.Base,
        IActiveFields,
        IActiveFieldsSet
    >
    {
        public struct ForPermutationIndex : IActiveFields, IActiveFieldsSet
        {
            private ActiveFields m_Source;
            private int m_PermutationIndex;

            public KeywordDependentCollection.KeywordPermutationInstanceType type => KeywordDependentCollection.KeywordPermutationInstanceType.Permutation;
            public IEnumerable<IActiveFields> instances => Enumerable.Repeat<IActiveFields>(this, 1);
            public IEnumerable<FieldDescriptor> fields =>
                m_Source.baseStorage.Union(m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex));
            public int instanceCount => 1;
            public int permutationIndex => m_PermutationIndex;

            internal ForPermutationIndex(ActiveFields source, int index)
            {
                m_Source = source;
                m_PermutationIndex = index;
            }

            public bool Add(FieldDescriptor field)
                => m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex).Add(field);

            public bool Contains(FieldDescriptor field) =>
                m_Source.baseStorage.Contains(field)
                || m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex).Contains(field);

            public bool Contains(string value) => m_Source.baseStorage.Contains(value)
            || m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex).Contains(value);
            public void AddAll(FieldDescriptor field) => Add(field);
        }

        public struct Base : IActiveFields, IActiveFieldsSet
        {
            private ActiveFields m_Source;

            public IEnumerable<FieldDescriptor> fields => m_Source.baseStorage.fields;
            public int instanceCount => 1;
            public int permutationIndex => -1;
            public KeywordDependentCollection.KeywordPermutationInstanceType type => KeywordDependentCollection.KeywordPermutationInstanceType.Base;
            public IEnumerable<IActiveFields> instances => Enumerable.Repeat<IActiveFields>(this, 1);

            internal Base(ActiveFields source)
            {
                m_Source = source;
            }

            public bool Add(FieldDescriptor field) => m_Source.baseStorage.Add(field);
            public bool Contains(FieldDescriptor field) => m_Source.baseStorage.Contains(field);
            public bool Contains(string value) => m_Source.baseStorage.Contains(value);
            public void AddAll(FieldDescriptor field) => Add(field);
        }

        public struct All : IActiveFieldsSet
        {
            private ActiveFields m_Source;
            public int instanceCount => m_Source.permutationCount + 1;

            internal All(ActiveFields source)
            {
                m_Source = source;
            }

            public void AddAll(FieldDescriptor field)
            {
                m_Source.baseInstance.Add(field);
                for (var i = 0; i < m_Source.permutationCount; ++i)
                    m_Source.GetOrCreateForPermutationIndex(i).Add(field);
            }

            public IEnumerable<IActiveFields> instances
            {
                get
                {
                    var self = this;
                    return m_Source.permutationStorages
                        .Select((v, i) => new ForPermutationIndex(self.m_Source, i) as IActiveFields)
                        .Union(Enumerable.Repeat((IActiveFields)m_Source.baseInstance, 1));
                }
            }
        }

        public struct AllPermutations : IActiveFieldsSet
        {
            private ActiveFields m_Source;
            public int instanceCount => m_Source.permutationCount;

            internal AllPermutations(ActiveFields source)
            {
                m_Source = source;
            }

            public void AddAll(FieldDescriptor field)
            {
                for (var i = 0; i < m_Source.permutationCount; ++i)
                    m_Source.GetOrCreateForPermutationIndex(i).Add(field);
            }

            public IEnumerable<IActiveFields> instances
            {
                get
                {
                    var self = this;
                    return m_Source.permutationStorages
                        .Select((v, i) => new ForPermutationIndex(self.m_Source, i) as IActiveFields);
                }
            }
        }

        public struct NoPermutation : IActiveFields, IActiveFieldsSet
        {
            private ActiveFields m_Source;

            public IEnumerable<FieldDescriptor> fields => m_Source.baseStorage.fields;
            public int instanceCount => 1;
            public int permutationIndex => -1;
            public KeywordDependentCollection.KeywordPermutationInstanceType type => KeywordDependentCollection.KeywordPermutationInstanceType.Base;

            internal NoPermutation(ActiveFields source)
            {
                m_Source = source;
            }

            public bool Add(FieldDescriptor field) => m_Source.baseInstance.Add(field);
            public bool Contains(FieldDescriptor field) => m_Source.baseStorage.Contains(field);
            public bool Contains(string value) => m_Source.baseStorage.Contains(value);
            public void AddAll(FieldDescriptor field) => Add(field);
            public IEnumerable<IActiveFields> instances => Enumerable.Repeat<IActiveFields>(this, 1);
        }

        protected override All CreateAllSmartPointer() => new All(this);
        protected override AllPermutations CreateAllPermutationsSmartPointer() => new AllPermutations(this);
        protected override ForPermutationIndex CreateForPermutationSmartPointer(int index) => new ForPermutationIndex(this, index);
        protected override Base CreateBaseSmartPointer() => new Base(this);
    }
}
