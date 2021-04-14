using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDAssetFactory
    {
        internal class VolumeProfileCreator : ProjectWindowCallback.EndNameEditAction
        {
            public enum Kind { Default, LookDev }
            Kind m_Kind;

            void SetKind(Kind kind) => m_Kind = kind;

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var profile = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName);
                ProjectWindowUtil.ShowCreatedAsset(profile);
                Assign(profile);
            }

            void Assign(VolumeProfile profile)
            {
                switch (m_Kind)
                {
                    case Kind.Default:
                        settings.volumeProfile = profile;
                        break;
                    case Kind.LookDev:
                        settings.lookDevVolumeProfile = profile;
                        break;
                }
                EditorUtility.SetDirty(settings);
            }

            static string GetDefaultName(Kind kind)
            {
                string defaultName;
                switch (kind)
                {
                    case Kind.Default:
                        defaultName = "VolumeProfile_Default";
                        break;
                    case Kind.LookDev:
                        defaultName = "LookDevProfile_Default";
                        break;
                    default:
                        defaultName = "N/A";
                        break;
                }
                return defaultName;
            }

            static HDRenderPipelineGlobalSettings settings;
            public static void CreateAndAssign(Kind kind, HDRenderPipelineGlobalSettings globalSettings)
            {
                settings = globalSettings;

                if (settings == null)
                {
                    Debug.LogError("Trying to create a Volume Profile for a null HDRP Global Settings. Operation aborted.");
                    return;
                }
                var assetCreator = ScriptableObject.CreateInstance<VolumeProfileCreator>();
                assetCreator.SetKind(kind);

                if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                    AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, $"Assets/{HDProjectSettings.projectSettingsFolderPath}/{globalSettings.name}_{GetDefaultName(kind)}.asset", null, null);
            }
        }
    }
}
