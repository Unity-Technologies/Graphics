using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Stores a relationship between two types by using a dynamic storage.
    ///
    /// This is an alternative to static storage (in generic type) which may not be available for AOT platforms.
    /// </summary>
    public class DynamicTypeRelation
    {
        Dictionary<Type, HashSet<Type>> m_Dictionary = new Dictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Set the value in a dictionary.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        public void RegisterRelation([DisallowNull] Type subject, [DisallowNull] Type target)
        {
            if (!m_Dictionary.TryGetValue(subject, out var targets))
            {
                targets = new HashSet<Type>();
                m_Dictionary.Add(subject, targets);
            }

            targets.Add(target);
        }

        /// <summary>
        /// Set the value in a dictionary.
        /// </summary>
        public bool AreRelated<TSubject, TTarget>() => AreRelated(typeof(TSubject), typeof(TTarget));

        /// <summary>
        /// Are the provided type related?
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool AreRelated([DisallowNull] Type subject, [DisallowNull] Type target)
        {
            var hasRelations = m_Dictionary.TryGetValue(subject, out var targets);
            return hasRelations && targets.Contains(target);
        }

        /// <summary>
        /// Does <typeparamref name="TSubject"/> have a relationship as a subject?
        /// </summary>
        /// <returns></returns>
        public bool HasRelations<TSubject>() => HasRelations(typeof(TSubject));

        /// <summary>
        /// Does <paramref name="subject"/> have a relationship as a subject?
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        public bool HasRelations([DisallowNull] Type subject)
        {
            return m_Dictionary.ContainsKey(subject);
        }
    }
}
