using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteInEditMode]
    public class Volume : MonoBehaviour
    {
        [Tooltip("A global volume is applied to the whole scene.")]
        public bool isGlobal = false;

        [Tooltip("Volume priority in the stack. Higher number means higher priority. Negative values are supported.")]
        public float priority = 0f;

        [Tooltip("Outer distance to start blending from. A value of 0 means no blending and the volume overrides will be applied immediatly upon entry.")]
        public float blendDistance = 0f;

        [Range(0f, 1f), Tooltip("Total weight of this volume in the scene. 0 means it won't do anything, 1 means full effect.")]
        public float weight = 1f;

        public List<VolumeComponent> components = new List<VolumeComponent>();

        // Editor-only
        [NonSerialized]
        public bool isDirty;

        // Needed for state tracking (see the comments in Update)
        int m_PreviousLayer;
        float m_PreviousPriority;

        void OnEnable()
        {
            // Make sure every setting is valid. If a profile holds a script that doesn't exist
            // anymore, nuke it to keep the volume clean. Note that if you delete a script that is
            // currently in use in a volume you'll still get a one-time error in the console, it's
            // harmless and happens because Unity does a redraw of the editor (and thus the current
            // frame) before the recompilation step.
            components.RemoveAll(x => x == null);

            m_PreviousLayer = gameObject.layer;
            VolumeManager.instance.Register(this, m_PreviousLayer);
        }

        void OnDisable()
        {
            VolumeManager.instance.Unregister(this, gameObject.layer);
        }

        void Reset()
        {
            isDirty = true;
        }

        void Update()
        {
            // Unfortunately we need to track the current layer to update the volume manager in
            // real-time as the user could change it at any time in the editor or at runtime.
            // Because no event is raised when the layer changes, we have to track it on every
            // frame :/
            int layer = gameObject.layer;
            if (layer != m_PreviousLayer)
            {
                VolumeManager.instance.UpdateVolumeLayer(this, m_PreviousLayer, layer);
                m_PreviousLayer = layer;
            }

            // Same for priority. We could use a property instead, but it doesn't play nice with the
            // serialization system. Using a custom Attribute/PropertyDrawer for a property is
            // possible but it doesn't work with Undo/Redo in the editor, which makes it useless for
            // our case.
            if (priority != m_PreviousPriority)
            {
                VolumeManager.instance.SetLayerDirty(layer);
                m_PreviousPriority = priority;
            }
        }

        public T Add<T>(bool overrides = false)
            where T : VolumeComponent
        {
            if (Has<T>())
                throw new InvalidOperationException("Component already exists in the volume");

            var component = ScriptableObject.CreateInstance<T>();
            component.SetAllOverridesTo(overrides);
            isDirty = true;
            return component;
        }

        public void Remove<T>()
            where T : VolumeComponent
        {
            int toRemove = -1;
            var type = typeof(T);

            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].GetType() == type)
                {
                    toRemove = i;
                    break;
                }
            }

            if (toRemove >= 0)
            {
                components.RemoveAt(toRemove);
                isDirty = true;
            }
        }

        public bool Has<T>()
            where T : VolumeComponent
        {
            var type = typeof(T);

            foreach (var component in components)
            {
                if (component.GetType() == type)
                    return true;
            }

            return false;
        }

        public bool TryGet<T>(out T component)
            where T : VolumeComponent
        {
            var type = typeof(T);
            component = null;

            foreach (var comp in components)
            {
                if (comp.GetType() == type)
                {
                    component = (T)comp;
                    return true;
                }
            }

            return false;
        }
    }
}
