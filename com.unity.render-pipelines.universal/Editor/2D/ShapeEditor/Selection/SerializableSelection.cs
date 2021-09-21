using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal.Path2D
{
    [Serializable]
    internal abstract class SerializableSelection<T> : ISelection<T>, ISerializationCallbackReceiver
    {
        internal readonly static int kInvalidID = -1;

        [SerializeField]
        private T[] m_Keys = new T[0];

        private HashSet<T> m_Selection = new HashSet<T>();
        private HashSet<T> m_TemporalSelection = new HashSet<T>();
        private bool m_SelectionInProgress = false;

        public int Count
        {
            get { return m_Selection.Count + m_TemporalSelection.Count; }
        }

        public T activeElement
        {
            get { return First(); }
            set
            {
                Clear();
                Select(value, true);
            }
        }

        public T[] elements
        {
            get
            {
                var set = m_Selection;

                if (m_SelectionInProgress)
                {
                    var union = new HashSet<T>(m_Selection);
                    union.UnionWith(m_TemporalSelection);
                    set = union;
                }

                return new List<T>(set).ToArray();
            }
            set
            {
                Clear();
                foreach (var element in value)
                    Select(element, true);
            }
        }

        protected abstract T GetInvalidElement();

        public void Clear()
        {
            GetSelection().Clear();
        }

        public void BeginSelection()
        {
            m_SelectionInProgress = true;
            Clear();
        }

        public void EndSelection(bool select)
        {
            m_SelectionInProgress = false;

            if (select)
                m_Selection.UnionWith(m_TemporalSelection);
            else
                m_Selection.ExceptWith(m_TemporalSelection);

            m_TemporalSelection.Clear();
        }

        public bool Select(T element, bool select)
        {
            var changed = false;

            if (EqualityComparer<T>.Default.Equals(element, GetInvalidElement()))
                return changed;

            if (select)
                changed = GetSelection().Add(element);
            else if (Contains(element))
                changed = GetSelection().Remove(element);

            return changed;
        }

        public bool Contains(T element)
        {
            return m_Selection.Contains(element) || m_TemporalSelection.Contains(element);
        }

        private HashSet<T> GetSelection()
        {
            if (m_SelectionInProgress)
                return m_TemporalSelection;

            return m_Selection;
        }

        private T First()
        {
            T element = First(m_Selection);

            if (EqualityComparer<T>.Default.Equals(element, GetInvalidElement()))
                element = First(m_TemporalSelection);

            return element;
        }

        private T First(HashSet<T> set)
        {
            if (set.Count == 0)
                return GetInvalidElement();

            using (var enumerator = set.GetEnumerator())
            {
                Debug.Assert(enumerator.MoveNext());
                return enumerator.Current;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_Keys = new List<T>(m_Selection).ToArray();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            elements = m_Keys;
        }
    }
}
