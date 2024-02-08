using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    class CustomPostProcessVolumeComponentList : ISerializationCallbackReceiver
    {
        [SerializeField] private CustomPostProcessInjectionPoint m_InjectionPoint;
        public CustomPostProcessInjectionPoint injectionPoint => m_InjectionPoint;

        [SerializeField] private List<string> m_CustomPostProcessTypesAsString;
        private List<Type> m_CustomPostProcessTypes;

        public CustomPostProcessVolumeComponentList(CustomPostProcessInjectionPoint injectionPoint)
        {
            m_CustomPostProcessTypes = new();
            m_CustomPostProcessTypesAsString = new();
            m_InjectionPoint = injectionPoint;
        }

        public IEnumerator<Type> GetEnumerator()
        {
            if (m_CustomPostProcessTypes == null)
                SyncCustomPostProcessTypes();

            return m_CustomPostProcessTypes.GetEnumerator();
        }

        public int Count
        {
            get
            {
                if (m_CustomPostProcessTypes == null)
                    SyncCustomPostProcessTypes();

                return m_CustomPostProcessTypes.Count;
            }
        }

        public Type this[int index]
        {
            get
            {
                if (m_CustomPostProcessTypes == null)
                    SyncCustomPostProcessTypes();

                return m_CustomPostProcessTypes[index];
            }

            set
            {
                if (m_CustomPostProcessTypes == null)
                    SyncCustomPostProcessTypes();

                m_CustomPostProcessTypes[index] = value;
            }
        }

        private void SyncCustomPostProcessTypes()
        {
            if (m_CustomPostProcessTypes == null)
                m_CustomPostProcessTypes = new();
            else
                m_CustomPostProcessTypes.Clear();

            for (int i = 0; i < m_CustomPostProcessTypesAsString.Count; ++i)
            {
                //UUM-60204: Script can have been deleted by user. We cannot assum the type can still exists.
                Type type = null;
                try { type = Type.GetType(m_CustomPostProcessTypesAsString[i]); }
                catch(Exception) { }
                if (type == null || !typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(type))
                {
                    m_CustomPostProcessTypesAsString.RemoveAt(i--);
                    continue;
                }

                m_CustomPostProcessTypes.Add(type);
            }
        }

        public void OnAfterDeserialize()
        {
            SyncCustomPostProcessTypes();
        }

        public void OnBeforeSerialize()
        {
        }

        public bool Contains(string typeString) => m_CustomPostProcessTypesAsString.Contains(typeString);
        public bool Contains<T>() where T : CustomPostProcessVolumeComponent => m_CustomPostProcessTypesAsString.Contains(typeof(T).AssemblyQualifiedName);

        public bool Add(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                throw new ArgumentNullException(nameof(typeString));

            if (!Contains(typeString))
            {
                var type = Type.GetType(typeString);
                if (typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(type))
                {
                    m_CustomPostProcessTypesAsString.Add(typeString);
                    SyncCustomPostProcessTypes();
                    return true;
                }
            }

            return false;
        }

        public bool Add<T>() where T : CustomPostProcessVolumeComponent => Add(typeof(T).AssemblyQualifiedName);

        public bool AddRange(List<string> typesString)
        {
            if (typesString == null)
                throw new ArgumentNullException(nameof(typesString));

            bool changed = false;
            foreach (var typeString in typesString)
            {
                if (!Contains(typeString))
                {
                    var type = Type.GetType(typeString);
                    if (typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(type))
                    {
                        m_CustomPostProcessTypesAsString.Add(typeString);
                        changed = true;
                    }
                }
            }

            if (changed)
                SyncCustomPostProcessTypes();

            return changed;
        }

        public bool Remove(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                throw new ArgumentNullException(nameof(typeString));

            if (!Contains(typeString))
                return false;

            if (m_CustomPostProcessTypesAsString.Remove(typeString))
            {
                SyncCustomPostProcessTypes();
                return true;
            }

            return false;
        }

        public bool Remove<T>() where T : CustomPostProcessVolumeComponent => Remove(typeof(T).AssemblyQualifiedName);
    }
}
