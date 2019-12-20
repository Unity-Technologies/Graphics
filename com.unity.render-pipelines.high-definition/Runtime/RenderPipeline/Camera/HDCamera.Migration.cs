namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDCamera : IVersionable<HDCamera.Version>
    {
        enum Version
        {
            MergeHDAdditionalCameraDataIntoHDCamera, //see HDAdditionalCameraData for migration step prior merge
        }

        [SerializeField]
        Version m_Version;

        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        //Uncomment the following when new migration step will occures
        //static readonly MigrationDescription<Version, HDCamera> k_Migration = MigrationDescription.New<Version, HDCamera>(
        //    //Add migration step here;
        //);

        //void Awake() => k_Migration.Migrate(this);
        
            if (additionalData != null)
                HDUtils.RuntimeCopyComponentValue(additionalData, tmpHDCam);

            //4 - Remove Camera and HDAdditionalCameraData if any available
#if UNITY_EDITOR
            if (additionalData)
                UnityEditor.Undo.DestroyObjectImmediate(additionalData);
            UnityEditor.Undo.DestroyObjectImmediate(camera);
#else
            if (additionalData)
                GameObject.DestroyImmediate(additionalData);
            GameObject.DestroyImmediate(camera);
#endif

            //5 - Add HDCamera and copy evrything from the temporary one
            HDCamera result =
#if UNITY_EDITOR
                UnityEditor.Undo.AddComponent<HDCamera>(refGO);
#else
                refGO.AddComponent<HDCamera>();
#endif
            HDUtils.RuntimeCopyComponentValue(tmpHDCam, result);

            //6 - Destroy temporary GameObject
            GameObject.DestroyImmediate(tmpHDCam.gameObject);
        }
    }
}
