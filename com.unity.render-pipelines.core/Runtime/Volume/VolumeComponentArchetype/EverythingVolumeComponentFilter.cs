using System;

namespace UnityEngine.Rendering
{
    public sealed class EverythingVolumeComponentFilter : IFilter<VolumeComponentType>
    {
        public bool IsAccepted(VolumeComponentType subjectType) => true;

        bool Equals(EverythingVolumeComponentFilter other) => true;

        public bool Equals(IFilter<VolumeComponentType> other)
        {
            return other is EverythingVolumeComponentFilter filter && Equals(filter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EverythingVolumeComponentFilter)obj);
        }

        public override int GetHashCode() => 0;
    }
}
