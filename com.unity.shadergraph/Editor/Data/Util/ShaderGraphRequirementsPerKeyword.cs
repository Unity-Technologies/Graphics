using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Internal
{
    sealed class ShaderGraphRequirementsPerKeyword: KeywordDependentCollection<
        ShaderGraphRequirements,
        ShaderGraphRequirementsPerKeyword.All,
        ShaderGraphRequirementsPerKeyword.AllPermutations,
        ShaderGraphRequirementsPerKeyword.ForPermutationIndex,
        ShaderGraphRequirementsPerKeyword.Base,
        ShaderGraphRequirementsPerKeyword.IRequirements,
        ShaderGraphRequirementsPerKeyword.IRequirementsSet
    >
    {
        public interface IRequirements: KeywordDependentCollection.IInstance, KeywordDependentCollection.ISet<IRequirements>
        {
            void SetRequirements(ShaderGraphRequirements value);

            ShaderGraphRequirements requirements { get; set;  }
        }

        public interface IRequirementsSet: KeywordDependentCollection.ISet<IRequirements>
        {
        }

        public struct ForPermutationIndex: IRequirements, IRequirementsSet
        {
            private ShaderGraphRequirementsPerKeyword m_Source;
            private int m_PermutationIndex;

            public KeywordDependentCollection.KeywordPermutationInstanceType type => KeywordDependentCollection.KeywordPermutationInstanceType.Permutation;
            public IEnumerable<IRequirements> instances => Enumerable.Repeat<IRequirements>(this, 1);
            public int instanceCount => 1;
            public int permutationIndex => m_PermutationIndex;

            public ShaderGraphRequirements requirements
            {
                get => m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex);
                set => m_Source.SetForPermutationIndex(m_PermutationIndex, value);
            }

            public void SetRequirements(ShaderGraphRequirements value) => requirements = value;

            internal ForPermutationIndex(ShaderGraphRequirementsPerKeyword source, int index)
            {
                m_Source = source;
                m_PermutationIndex = index;
            }
        }

        public struct Base : IRequirements, IRequirementsSet
        {
            private ShaderGraphRequirementsPerKeyword m_Source;

            public int instanceCount => 1;
            public int permutationIndex => -1;
            public KeywordDependentCollection.KeywordPermutationInstanceType type => KeywordDependentCollection.KeywordPermutationInstanceType.Base;
            public IEnumerable<IRequirements> instances => Enumerable.Repeat<IRequirements>(this, 1);

            public ShaderGraphRequirements requirements
            {
                get => m_Source.baseStorage;
                set => m_Source.baseStorage = value;
            }

            public void SetRequirements(ShaderGraphRequirements value) => requirements = value;

            internal Base(ShaderGraphRequirementsPerKeyword source)
            {
                m_Source = source;
            }
        }

        public struct All : IRequirementsSet
        {
            private ShaderGraphRequirementsPerKeyword m_Source;
            public int instanceCount => m_Source.permutationCount + 1;

            internal All(ShaderGraphRequirementsPerKeyword source)
            {
                m_Source = source;
            }

            public IEnumerable<IRequirements> instances
            {
                get
                {
                    var self = this;
                    return m_Source.permutationStorages
                        .Select((v, i) => new ForPermutationIndex(self.m_Source, i) as IRequirements)
                        .Union(Enumerable.Repeat((IRequirements)m_Source.baseInstance, 1));
                }
            }
        }

        public struct AllPermutations : IRequirementsSet
        {
            private ShaderGraphRequirementsPerKeyword m_Source;
            public int instanceCount => m_Source.permutationCount;

            internal AllPermutations(ShaderGraphRequirementsPerKeyword source)
            {
                m_Source = source;
            }

            public IEnumerable<IRequirements> instances
            {
                get
                {
                    var self = this;
                    return m_Source.permutationStorages
                        .Select((v, i) => new ForPermutationIndex(self.m_Source, i) as IRequirements);
                }
            }
        }

        public void UnionWith(ShaderGraphRequirementsPerKeyword other)
        {
            baseStorage = baseStorage.Union(other.baseStorage);
            for (var i = 0; i < other.permutationCount; ++i)
                SetForPermutationIndex(i,
                    GetOrCreateForPermutationIndex(i).Union(other.GetOrCreateForPermutationIndex(i)));
        }

        protected override All CreateAllSmartPointer() => new All(this);
        protected override AllPermutations CreateAllPermutationsSmartPointer() => new AllPermutations(this);
        protected override ForPermutationIndex CreateForPermutationSmartPointer(int index) => new ForPermutationIndex(this, index);
        protected override Base CreateBaseSmartPointer() => new Base(this);
    }
}
