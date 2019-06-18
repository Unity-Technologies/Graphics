using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class DecalProjectorComponent : IVersionable<DecalProjectorComponent.Version>
    {
        enum Version
        {
            Initial,
            UseZProjectionAxisAndScaleIndependance
        }

        static readonly MigrationDescription<Version, DecalProjectorComponent> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UseZProjectionAxisAndScaleIndependance, (DecalProjectorComponent decal) =>
            {
                // Update size for scale independence
                decal.m_Size.Scale(decal.transform.lossyScale);

                //Rotate so projection move from - Y to Z but childs keep same positions and rotations
                decal.transform.RotateAround(decal.transform.position, decal.transform.right, 90);
                foreach (Transform child in decal.transform)
                {
                    child.RotateAround(decal.transform.position, decal.transform.right, -90f);
                }

                // Inverse Y and Z in the size so we keep same aspect
                float newZ = decal.m_Size.y;
                decal.m_Size.y = decal.m_Size.z;
                decal.m_Size.z = newZ;

                // Inverse Y and Z in the offset so we keep same aspect and take into account new Scale independence
                newZ = -decal.m_Offset.y * decal.transform.lossyScale.y;
                decal.m_Offset.y = decal.m_Offset.z * decal.transform.lossyScale.z;
                decal.m_Offset.z = newZ;
                decal.m_Offset.x *= decal.transform.lossyScale.x;
                
                // Update decal system
                if (decal.m_Handle != null)
                    DecalSystem.instance.RemoveDecal(decal.m_Handle);
                Matrix4x4 sizeOffset = Matrix4x4.Translate(decal.decalOffset) * Matrix4x4.Scale(decal.decalSize);
                decal.m_Handle = DecalSystem.instance.AddDecal(decal.position, decal.rotation, Vector3.one, sizeOffset, decal.m_DrawDistance, decal.m_FadeScale, decal.uvScaleBias, decal.m_AffectsTransparency, decal.m_Material, decal.gameObject.layer, decal.m_FadeFactor);
            })
        );

        [SerializeField]
        Version m_Version;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void Awake() => k_Migration.Migrate(this);
    }
}
