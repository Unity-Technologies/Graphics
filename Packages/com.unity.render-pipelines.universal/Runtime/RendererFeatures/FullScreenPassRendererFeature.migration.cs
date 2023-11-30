using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public partial class FullScreenPassRendererFeature : ISerializationCallbackReceiver
{
    private enum Version
    {
        // * Uninitialised is a special last entry that will only ever be set on newly created objects or objects
        //   which were previously serialised with no Version member at all.
        // * We distinguish between new objects and the unversioned objects based on if we first see this value during
        //   serialization or during deserialization respectively.
        Uninitialised = -1,

        Initial,
        AddFetchColorBufferCheckbox,

        // These two entries should remain at the end of the enum and new version should be added before Count
        Count,
        Latest = Count - 1,
    }

    [SerializeField]
    [HideInInspector]
    private Version m_Version = Version.Uninitialised;

    private void UpgradeIfNeeded()
    {
        // As we rely on serialization/deserialization order to initialize the version as player might have restricions
        // on when and if serialization is done skipping any upgrading at runtime to avoid accidentally doing the
        // upgrade on the latest version. Upgrading at runtime does not really have much utility as it would mean
        // that the asset would need to have been produced in an editor which is an earlier build version than the player
#if UNITY_EDITOR
        if (m_Version == Version.Latest)
            return;

        if(m_Version == Version.Initial)
        {
            // * Previously the ScriptableRenderPassInput.Color requirement was repurposed to mean "copy the active
            //   color target" even though it is typically used to request '_CameraOpaqueTexture' and the copy color pass.
            // * From now on, the "Fetch Color Buffer" choice will be a separate checkbox to remove the inconsistent
            //   meaning and to allow using the '_CameraOpaqueTexture' if one wants to as well.
            fetchColorBuffer = requirements.HasFlag(ScriptableRenderPassInput.Color);

            // As the Color flag was being masked out during actual rendering we can safely disable it.
            requirements &= ~ScriptableRenderPassInput.Color;

            m_Version++;
        }
        // Put the next upgrader in an "if" here (not "else if" as they migh all need to run)

        // Making sure SetDirty is called once after deserialization
        EditorApplication.delayCall += () =>
        {
            if (this)
                EditorUtility.SetDirty(this);
        };
#endif
    }

    /// <inheritdoc/>
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        // This should only ever be true the first time we're serializing a newly created object
        if (m_Version == Version.Uninitialised)
            m_Version = Version.Latest;
    }

    /// <inheritdoc/>
    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        // The 'Uninitialised' version is expected to only occur during deserialization for objects that were previously
        // serialized before we added the m_Version field
        if (m_Version == Version.Uninitialised)
            m_Version = Version.Initial;

        UpgradeIfNeeded();
    }
}
