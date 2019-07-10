using System.Collections.Generic;
using System.Linq;

namespace Data.Util
{
    public enum ActiveFieldInstanceType
    {
        Base,
        Permutation
    }

    public interface IActiveFields
    {
        ActiveFieldInstanceType type { get; }
        int permutationIndex { get; }
        IEnumerable<string> fields { get; }

        bool Add(string field);
        bool Contains(string field);
    }

    public interface IActiveFieldsSet
    {
        int count { get; }
        IEnumerable<IActiveFields> instances { get; }

        void AddAll(string field);
    }

    public class ActiveFields
    {
        public struct ForPermutationIndex: IActiveFields, IActiveFieldsSet
        {
            private ActiveFields m_Source;
            private int m_PermutationIndex;

            public IEnumerable<string> fields => m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex);
            public int count => 1;
            public int permutationIndex => m_PermutationIndex;
            public ActiveFieldInstanceType type => ActiveFieldInstanceType.Permutation;

            internal ForPermutationIndex(ActiveFields source, int index)
            {
                m_Source = source;
                m_PermutationIndex = index;
            }

            public bool Add(string field)
             => m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex).Add(field);

            public bool Contains(string field) =>
                m_Source.m_Base.Contains(field)
                || m_Source.GetOrCreateForPermutationIndex(m_PermutationIndex).Contains(field);

            public void AddAll(string field) => Add(field);
            public IEnumerable<IActiveFields> instances => Enumerable.Repeat<IActiveFields>(this, 1);
        }

        public struct Base : IActiveFields, IActiveFieldsSet
        {
            private ActiveFields m_Source;

            public IEnumerable<string> fields => m_Source.m_Base;
            public int count => 1;
            public int permutationIndex => -1;
            public ActiveFieldInstanceType type => ActiveFieldInstanceType.Base;

            internal Base(ActiveFields source)
            {
                m_Source = source;
            }

            public bool Add(string field) => m_Source.m_Base.Add(field);
            public bool Contains(string field) => m_Source.m_Base.Contains(field);

            public void AddAll(string field) => Add(field);
            public IEnumerable<IActiveFields> instances => Enumerable.Repeat<IActiveFields>(this, 1);
        }

        public struct All : IActiveFieldsSet
        {
            private ActiveFields m_Source;
            public int count => m_Source.m_PerPermutationIndex.Count + 1;

            internal All(ActiveFields source)
            {
                m_Source = source;
            }

            public void AddAll(string field)
            {
                m_Source.m_Base.Add(field);
                for (var i = 0; i < m_Source.m_PerPermutationIndex.Count; ++i)
                    m_Source.m_PerPermutationIndex[i].Add(field);
            }

            public IEnumerable<IActiveFields> instances
            {
                get
                {
                    var self = this;
                    return m_Source.m_PerPermutationIndex
                        .Select((v, i) => new ForPermutationIndex(self.m_Source, i) as IActiveFields)
                        .Union(Enumerable.Repeat((IActiveFields)m_Source.baseInstance, 1));
                }
            }
        }

        public struct AllPermutations : IActiveFieldsSet
        {
            private ActiveFields m_Source;
            public int count => m_Source.m_PerPermutationIndex.Count;

            internal AllPermutations(ActiveFields source)
            {
                m_Source = source;
            }

            public void AddAll(string field)
            {
                for (var i = 0; i < m_Source.m_PerPermutationIndex.Count; ++i)
                    m_Source.m_PerPermutationIndex[i].Add(field);
            }

            public IEnumerable<IActiveFields> instances
            {
                get
                {
                    var self = this;
                    return m_Source.m_PerPermutationIndex
                        .Select((v, i) => new ForPermutationIndex(self.m_Source, i) as IActiveFields);
                }
            }
        }

        HashSet<string> m_Base = new HashSet<string>();
        List<HashSet<string>> m_PerPermutationIndex = new List<HashSet<string>>();

        public ForPermutationIndex this[int index]
        {
            get
            {
                GetOrCreateForPermutationIndex(index);
                return new ForPermutationIndex(this, index);
            }
        }

        public All all => new All(this);
        public AllPermutations allPermutations => new AllPermutations(this);

        /// <summary>
        /// All permutation will inherit from base's active fields
        /// </summary>
        public Base baseInstance => new Base(this);

        HashSet<string> GetOrCreateForPermutationIndex(int index)
        {
            while(index >= m_PerPermutationIndex.Count)
                m_PerPermutationIndex.Add(new HashSet<string>());

            return m_PerPermutationIndex[index];
        }
    }
}
