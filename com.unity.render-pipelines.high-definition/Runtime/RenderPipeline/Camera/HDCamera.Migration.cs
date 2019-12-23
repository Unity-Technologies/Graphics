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
        
        internal static void ConvertCameraToHDCamera(Camera camera)
        {
            if (camera == null || camera is HDCamera)
                return;

            GameObject refGO = camera.gameObject;

#if UNITY_EDITOR
            UnityEditor.Undo.SetCurrentGroupName("Convert Camera to HDCamera");
#endif

            //1 - Create a temporary GameObject into the HDCamera
            HDCamera tmpHDCam = new GameObject("Temporary for creating HDCamera in place of Camera", new[] { typeof(HDCamera) }).GetComponent<HDCamera>();

            //2 - Copy the Camera values on this GameObject into the HDCamera
            tmpHDCam.CopyFrom(camera); //copy camera state

            //3 - Copy the HDAdditionalCameraData values into the HDCamera if any available
            HDAdditionalCameraData additionalData = camera.GetComponent<HDAdditionalCameraData>();
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
            result.CopyFrom(tmpHDCam); //copy camera state
            HDUtils.RuntimeCopyComponentValue(tmpHDCam, result);

            //6 - Destroy temporary GameObject
            GameObject.DestroyImmediate(tmpHDCam.gameObject);
        }
    }
}
