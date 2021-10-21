using System;
using System.Linq;

namespace UnityEngine.Rendering
{
    public sealed class VisibleVolumeComponentFilter : IFilter<VolumeComponentType>
    {
        bool m_Visible;

        public VisibleVolumeComponentFilter(bool visible)
        {
            m_Visible = visible;
        }

        public bool IsAccepted(VolumeComponentType subjectType)
        {
            var isHidden = subjectType.AsType().GetCustomAttributes(true)
                .Any(attr => attr is HideInInspector or ObsoleteAttribute);
            return m_Visible && !isHidden || !m_Visible && isHidden;
        }

        bool Equals(VisibleVolumeComponentFilter other) => true;

        public bool Equals(IFilter<VolumeComponentType> other)
        {
            return other is VisibleVolumeComponentFilter filter && Equals(filter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VisibleVolumeComponentFilter)obj);
        }

        public override int GetHashCode() => 0;
    }
}
