using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public class VolumeComponent : ScriptableObject
    {
        // Used to control the state of this override - handy to quickly turn a volume override
        // on & off in the editor
        public bool active = true;

        internal ReadOnlyCollection<VolumeParameter> parameters { get; private set; }

        void OnEnable()
        {
            // Automatically grab all fields of type VolumeParameter for this instance
            parameters = this.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                .OrderBy(t => t.MetadataToken) // Guaranteed order
                .Select(t => (VolumeParameter)t.GetValue(this))
                .ToList()
                .AsReadOnly();
        }

        public void SetAllOverridesTo(bool state)
        {
            SetAllOverridesTo(parameters, state);
        }

        void SetAllOverridesTo(IEnumerable<VolumeParameter> enumerable, bool state)
        {
            foreach (var prop in enumerable)
            {
                prop.overrideState = state;
                var t = prop.GetType();

                if (VolumeParameter.IsObjectParameter(t))
                {
                    // This method won't be called a lot but this is sub-optimal, fix me
                    var innerParams = (ReadOnlyCollection<VolumeParameter>)
                        t.GetProperty("parameters", BindingFlags.NonPublic | BindingFlags.Instance)
                         .GetValue(prop, null);

                    if (innerParams != null)
                        SetAllOverridesTo(innerParams, state);
                }
            }
        }

        // Custom hashing function used to compare the state of settings (it's not meant to be
        // unique but to be a quick way to check if two setting sets have the same state or not).
        // Hash collision rate should be pretty low.
        public override int GetHashCode()
        {
            unchecked
            {
                //return parameters.Aggregate(17, (i, p) => i * 23 + p.GetHash());

                int hash = 17;

                foreach (var p in parameters)
                    hash = hash * 23 + p.GetHashCode();

                return hash;
            }
        }
    }
}
