using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<ReflectionProxyVolumeComponentUI, SerializedReflectionProxyVolumeComponent>;

    class ReflectionProxyVolumeComponentUI : IUpdateable<SerializedReflectionProxyVolumeComponent>
    {
        #pragma warning disable 618
        public static readonly CED.IDrawer Inspector = CED.Action((s, d, o)
            => ProxyVolumeUI.SectionShape.Draw(s.proxyVolume, d.proxyVolume, o));
        #pragma warning restore 618

        public ProxyVolumeUI proxyVolume = new ProxyVolumeUI();

        public void Update(SerializedReflectionProxyVolumeComponent s)
            => proxyVolume.Update(s.proxyVolume);
    }
}
