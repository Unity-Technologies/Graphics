using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.UIGen
{
    public static class UIGeneratedPropertyMutatorSet
    {
        static Dictionary<Type, IGeneratedPropertyMutator> m_MutatorSet = new();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        static void GatherAllMutators()
        {
            var mutatorTypes = TypeCache.GetTypesDerivedFrom<IGeneratedPropertyMutator>();

            foreach(var type in mutatorTypes)
            {
                if (type.IsAbstract)
                    continue;

                m_MutatorSet.Add(type, (IGeneratedPropertyMutator)Activator.CreateInstance(type));
            }
        }
#endif
        //UIDefinition.IFeatureParameter
        public static bool TryFindMutatorForFeature<T>([NotNullWhen(true)] out IGeneratedPropertyMutator mutator, [NotNullWhen(false)] out Exception error)
        {
            if (!m_MutatorSet.ContainsKey(typeof(T)))
            {
                mutator = null;
                error = new Exception($"Unknown type Feature type. No Mutator registered for it.");
                return false;
            }

            mutator = m_MutatorSet[typeof(T)];
            error = null;
            return true;
        }
    }

    public interface IGeneratedPropertyMutator
    {
        Type featureParameterSupported { get; }

        //bool TryMutate(ref )
    }

    public abstract class GeneratedPropertyMutator<T> : IGeneratedPropertyMutator
        where T : UIDefinition.IFeatureParameter
    {
        public Type featureParameterSupported => typeof(T);
    }
}
