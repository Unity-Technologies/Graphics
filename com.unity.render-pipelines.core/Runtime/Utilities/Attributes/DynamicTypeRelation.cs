using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Stores a relationship between two types by using a dynamic storage.
    ///
    /// This is an alternative to static storage (in generic type) which may not be available for AOT platforms.
    /// </summary>
    class DynamicTypeRelation
    {
        Dictionary<Type, HashSet<Type>> m_Dictionary = new Dictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Set the value in a dictionary.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        internal void RegisterRelation(Type subject, Type target)
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
        internal bool IsRelated<TSubject, TTarget>() => IsRelated(typeof(TSubject), typeof(TTarget));

        /// <summary>
        /// Are the provided type related?
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        internal bool IsRelated(Type subject, Type target)
        {
            var hasRelations = m_Dictionary.TryGetValue(subject, out var targets);
            return hasRelations && targets.Contains(target) || !hasRelations;
        }

        /// <summary>
        /// Does <typeparamref name="TSubject"/> have a relationship as a subject?
        /// </summary>
        /// <returns></returns>
        internal bool HasRelations<TSubject>() => HasRelations(typeof(TSubject));

        /// <summary>
        /// Does <paramref name="subject"/> have a relationship as a subject?
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        internal bool HasRelations(Type subject)
        {
            return m_Dictionary.ContainsKey(subject);
        }
    }
}
