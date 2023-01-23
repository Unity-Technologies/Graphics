using System;

namespace UnityEngine.Rendering
{
    public partial class ProbeVolume : MonoBehaviour
    {
        enum Version
        {
            Initial,
            LocalMode,

            Count
        }

        [SerializeField]
        Version version = Version.Initial;

        void Awake()
        {
            if (version == Version.Count)
                return;

            if (version == Version.Initial)
            {
#pragma warning disable 618 // Type or member is obsolete
                mode = globalVolume ? Mode.Scene : Mode.Local;
#pragma warning restore 618

                version++;
            }
        }

        /// <summary>
        /// If is a global bolume
        /// </summary>
        [SerializeField, Obsolete("Use mode instead")]
        public bool globalVolume = false;

    }
}
