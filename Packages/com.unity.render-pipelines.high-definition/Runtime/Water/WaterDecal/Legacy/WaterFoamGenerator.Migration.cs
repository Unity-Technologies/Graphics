
namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterFoamGenerator : IVersionable<WaterFoamGenerator.Version>
    {
        enum Version
        {
            First,
            FoamRemap,
            ConvertToDecal,

            Count,
        }

        [SerializeField]
        Version m_Version = Version.First;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        static readonly MigrationDescription<Version, WaterFoamGenerator> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.FoamRemap, (WaterFoamGenerator s) =>
            {
                s.surfaceFoamDimmer = Mathf.Min(s.surfaceFoamDimmer * 3.0f, 1.0f);
                s.deepFoamDimmer = Mathf.Min(s.deepFoamDimmer * 3.0f, 1.0f);
            }),
            MigrationStep.New(Version.ConvertToDecal, (WaterFoamGenerator s) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (s.type == WaterFoamGeneratorType.Material)
                    return;

            #if UNITY_EDITOR
                int type = 0;
                s.resolution.Set(64, 64);
                s.updateMode = CustomRenderTextureUpdateMode.OnLoad;

                s.material = new Material(GraphicsSettings.GetRenderPipelineSettings<WaterSystemRuntimeResources>().waterDecalMigrationShader);
                s.material.name = s.gameObject.name;
                s.material.SetFloat("_AffectDeformation", 0.0f);

                if (s.type == WaterFoamGeneratorType.Disk)
                {
                    type = 0;
                }
                if (s.type == WaterFoamGeneratorType.Rectangle)
                {
                    type = 1;
                    s.material.SetVector("_Blend_Distance", Vector4.zero);
                }
                if (s.type == WaterFoamGeneratorType.Texture)
                {
                    type = 4;
                    s.material.SetTexture("_Foam_Texture", s.texture);
                    s.material.SetTexture("_Deformation_Texture", Texture2D.blackTexture);

                    if (s.texture is CustomRenderTexture crt)
                        s.updateMode = crt.updateMode;

                    // Clear ref to separate asset
                    s.texture = null;
                }

                s.material.SetFloat("_TYPE", type);
                s.type = WaterFoamGeneratorType.Material;

                UnityEditor.EditorUtility.SetDirty(s.gameObject);
                UnityEditor.MaterialEditor.ApplyMaterialPropertyDrawers(s.material);
                HDMaterial.ValidateMaterial(s.material);
            #else
                Debug.LogError($"Water Foam Generator '{s.gameObject.name}' was not migrated. It will not render correctly.");
            #endif
#pragma warning restore 618
            })
        );
    }
}
