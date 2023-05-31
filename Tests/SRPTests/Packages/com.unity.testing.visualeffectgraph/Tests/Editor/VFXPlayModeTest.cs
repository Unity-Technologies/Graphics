using NUnit.Framework;

namespace UnityEditor.VFX.Test
{
    public class VFXPlayModeTest
    {
        private bool m_OriginalPlayModeOptionEnabled;
        private EnterPlayModeOptions m_OriginalPlayModeOption;

        [OneTimeSetUp]
        public void PlayModeInit()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            m_OriginalPlayModeOptionEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            m_OriginalPlayModeOption = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSettings.enterPlayModeOptionsEnabled = true;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    NotifyEnterPlayMode();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    NotifyExitedPlayMode();
                    break;
            }
        }

        [OneTimeTearDown]
        public void PlayModeCleanup()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSettings.enterPlayModeOptions = m_OriginalPlayModeOption;
            EditorSettings.enterPlayModeOptionsEnabled = m_OriginalPlayModeOptionEnabled;
        }

        protected virtual void NotifyEnterPlayMode() {}
        protected virtual void NotifyExitedPlayMode() {}
    }
}
