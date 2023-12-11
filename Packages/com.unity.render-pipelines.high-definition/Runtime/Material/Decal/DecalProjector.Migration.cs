namespace UnityEngine.Rendering.HighDefinition
{
    public partial class DecalProjector : IVersionable<DecalProjector.Version>
    {
        enum Version
        {
            Initial,
            UseZProjectionAxisAndScaleIndependance,
            FixPivotPosition,
            GenericRenderingLayers,
        }

        static readonly MigrationDescription<Version, DecalProjector> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UseZProjectionAxisAndScaleIndependance, (DecalProjector decal) =>
            {
                // Update size for scale independence
                decal.m_Size.Scale(decal.transform.lossyScale);

                //Rotate so projection move from -Y to Z but childs keep same positions and rotations
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

                decal.m_Handle = DecalSystem.instance.AddDecal(decal);
            }),
            MigrationStep.New(Version.FixPivotPosition, (DecalProjector decal) =>
            {
                // Translate the decal to half its size in z so it will remain at same position
                Vector3 translationValue = (decal.m_Offset - new Vector3(0f, 0f, decal.m_Size.z * 0.5f));
                decal.transform.Translate(translationValue);

                // Update pivot position to -z face
                decal.m_Offset.x = 0f;
                decal.m_Offset.y = 0f;
                decal.m_Offset.z = decal.m_Size.z * 0.5f;

                // move childs to keep concistency
                // be carefull as we changed space for child to move things relatively, so in world space
                Transform parent = decal.transform.parent;
                if (parent != null)
                {
                    translationValue.x *= parent.transform.lossyScale.x;
                    translationValue.y *= parent.transform.lossyScale.y;
                    translationValue.z *= parent.transform.lossyScale.z;
                    translationValue = decal.transform.rotation * -translationValue;
                }
                foreach (Transform child in decal.transform)
                {
                    child.Translate(translationValue, Space.World);
                }

                // Update decal system
                if (decal.m_Handle != null)
                    DecalSystem.instance.RemoveDecal(decal.m_Handle);

                decal.m_Handle = DecalSystem.instance.AddDecal(decal);
            }),
            MigrationStep.New(Version.GenericRenderingLayers, (DecalProjector decal) =>
            {
                // Decal and light layers are now shared on 16 bits instead of using 8 separate bits
                // Decal use the last 8 bits so they need to be shifted
                // If a decal projector was created before decal layer feature, just keep default value
                if (decal.m_DecalLayerMask != (RenderingLayerMask) (uint) UnityEngine.RenderingLayerMask.defaultRenderingLayerMask)
                    decal.m_DecalLayerMask = (RenderingLayerMask)((int)decal.m_DecalLayerMask << 8);
            })
        );

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void Awake() => k_Migration.Migrate(this);
    }
}
