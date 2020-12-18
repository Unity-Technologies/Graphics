using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.Internal
{
    public static class KeywordDependentCollection
    {
        public enum KeywordPermutationInstanceType
        {
            Base,
            Permutation,
        }

        public interface ISet<IInstance>
        {
            int instanceCount { get; }
            IEnumerable<IInstance> instances { get; }
        }

        public interface IInstance
        {
            KeywordPermutationInstanceType type { get; }
            int permutationIndex { get; }
        }
    }

    public abstract class KeywordDependentCollection<TStorage, TAll, TAllPermutations, TForPermutation, TBase, TIInstance, TISet>
        where TAll: TISet
        where TAllPermutations: TISet
        where TForPermutation: TISet, TIInstance
        where TBase: TISet, TIInstance
        where TISet: KeywordDependentCollection.ISet<TIInstance>
        where TIInstance: KeywordDependentCollection.IInstance
        where TStorage: new()
    {
        TStorage m_Base = new TStorage();
        List<TStorage> m_PerPermutationIndex = new List<TStorage>();

        public int permutationCount => m_PerPermutationIndex.Count;

        public TForPermutation this[int index]
        {
            get
            {
                GetOrCreateForPermutationIndex(index);
                return CreateForPermutationSmartPointer(index);
            }
        }

        public TAll all => CreateAllSmartPointer();
        public TAllPermutations allPermutations => CreateAllPermutationsSmartPointer();

        /// <summary>
        /// All permutation will inherit from base's active fields
        /// </summary>
        public TBase baseInstance => CreateBaseSmartPointer();

        protected TStorage baseStorage
        {
            get => m_Base;
            set => m_Base = value;
        }

        protected IEnumerable<TStorage> permutationStorages => m_PerPermutationIndex;

        protected TStorage GetOrCreateForPermutationIndex(int index)
        {
            while(index >= m_PerPermutationIndex.Count)
                m_PerPermutationIndex.Add(new TStorage());

            return m_PerPermutationIndex[index];
        }

        protected void SetForPermutationIndex(int index, TStorage value)
        {
            while(index >= m_PerPermutationIndex.Count)
                m_PerPermutationIndex.Add(new TStorage());

            m_PerPermutationIndex[index] = value;
        }

        protected abstract TAll CreateAllSmartPointer();
        protected abstract TAllPermutations CreateAllPermutationsSmartPointer();
        protected abstract TForPermutation CreateForPermutationSmartPointer(int index);
        protected abstract TBase CreateBaseSmartPointer();
    }
}
