using UnityEditor;
using UnityEngine;

namespace UnityEditor.VFX
{
    // This ImporterGlobalSettings acts as a middleman for the VFX importer settings that are EditorPrefs based and need to be synched on import workers.
    // They are still accessed through VFXViewPreference like any other preferences.
    class VFXGlobalImportSettings : ImporterGlobalSettings<VFXGlobalImportSettings>
    {
        public const string generateShadersWithDebugSymbolsKey = "VFX.generateShadersWithDebugSymbols";
        public const string forceEditionCompilationKey = "VFX.ForceEditionCompilation";
        public const string useNewCompilerKey = "VFX.UseNewCompiler";

        internal const string k_CustomDependencyName = "VFX/GlobalImportSettings";

        [SerializeField]
        bool m_GenerateShadersWithDebugSymbols;
        [SerializeField]
        bool m_ForceEditionCompilation;
        [SerializeField]
        bool m_UseNewCompiler;

        public bool generateShadersWithDebugSymbols
        {
            get => m_GenerateShadersWithDebugSymbols;
            set => WriteAndPublish(ref m_GenerateShadersWithDebugSymbols, value, generateShadersWithDebugSymbolsKey);
        }

        public bool forceEditionCompilation
        {
            get => m_ForceEditionCompilation;
            set => WriteAndPublish(ref m_ForceEditionCompilation, value, forceEditionCompilationKey);
        }

        public bool useNewCompiler
        {
            get => m_UseNewCompiler;
            set => Write(ref m_UseNewCompiler, value, useNewCompilerKey);
        }

        static void Write(ref bool field, bool value, string editorPrefsKey)
        {
            if (field == value)
                return;

            field = value;
            EditorPrefs.SetBool(editorPrefsKey, value);
        }

        static void WriteAndPublish(ref bool field, bool value, string editorPrefsKey)
        {
            if (field == value)
                return;

            Write(ref field, value, editorPrefsKey);
            PublishCustomDependency();

            // Applies the new value now; without it the assets stay stale until something else refreshes.
            AssetDatabase.Refresh();
        }

        Hash128 ComputeValueHash()
        {
            var hash = new Hash128();
            hash.Append(m_GenerateShadersWithDebugSymbols ? 1 : 0);
            hash.Append(m_ForceEditionCompilation ? 1 : 0);
            return hash;
        }

        // No-op on import workers, which record the dependency during import rather than publishing its value.
        static void PublishCustomDependency()
        {
            AssetDatabase.RegisterCustomDependency(k_CustomDependencyName, instance.ComputeValueHash());
        }

        void OnEnable()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            Read();
        }

        void Read()
        {
            m_GenerateShadersWithDebugSymbols = EditorPrefs.GetBool(generateShadersWithDebugSymbolsKey, false);
            m_ForceEditionCompilation = EditorPrefs.GetBool(forceEditionCompilationKey, false);
            m_UseNewCompiler = EditorPrefs.GetBool(useNewCompilerKey, false);
        }

        // Picks up EditorPrefs written behind our back. Has to reload now rather than on the next read: these
        // values are captured for the import workers when an import starts, and a deferred reload would let
        // that capture publish the stale value.
        internal static void Reload()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            instance.Read();
            PublishCustomDependency();
        }

        // ScriptableSingletons are lazily loaded so force a load on the editor thread to ensure they are synched on import workers.
        [InitializeOnLoadMethod]
        static void CreateBeforeAnyImport()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            _ = instance;
            PublishCustomDependency();
        }
    }
}
