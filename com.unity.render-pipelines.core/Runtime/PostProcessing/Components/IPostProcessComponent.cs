using System;

namespace UnityEngine.Rendering
{
    public interface IPostProcessComponent
    {
        bool IsActive();

        bool IsTileCompatible()
        {
            return false;
        }
    }

    public interface IDeprecatedVolumeComponent
    {
        public void CopyToNewComponent(VolumeComponent old);
        public Type GetNewComponentType();
    }
}
