using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<ProxyVolumeComponentUI, SerializedProxyVolumeComponent>;

    class ProxyVolumeComponentUI : BaseUI<SerializedProxyVolumeComponent>
    {
        public static readonly CED.IDrawer Inspector;

        static ProxyVolumeComponentUI()
        {
            Inspector = CED.Select(
                (s, d, o) => s.proxyVolume,
                (s, d, o) => d.proxyVolume,
                ProxyVolumeUI.SectionShape
            );
        }

        public ProxyVolumeUI proxyVolume = new ProxyVolumeUI();

        public ProxyVolumeComponentUI()
            : base(0)
        {
            
        }

        public override void Reset(SerializedProxyVolumeComponent data, UnityAction repaint)
        {
            proxyVolume.Reset(data.proxyVolume, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            proxyVolume.Update();
            base.Update();
        }

        public static void DrawHandles_EditBase(ProxyVolumeComponentUI ui, ReflectionProxyVolumeComponent target)
        {
            ProxyVolumeUI.DrawHandles_EditBase(target.transform, target.proxyVolume, ui.proxyVolume, target);
        }

        public static void DrawHandles_EditNone(ProxyVolumeComponentUI ui, ReflectionProxyVolumeComponent target)
        {
            ProxyVolumeUI.DrawHandles_EditNone(target.transform, target.proxyVolume, ui.proxyVolume, target);
        }

        public static void DrawGizmos_EditNone(ProxyVolumeComponentUI ui, ReflectionProxyVolumeComponent target)
        {
            ProxyVolumeUI.DrawGizmos_EditNone(target.transform, target.proxyVolume, ui.proxyVolume, target);
        }
    }
}
