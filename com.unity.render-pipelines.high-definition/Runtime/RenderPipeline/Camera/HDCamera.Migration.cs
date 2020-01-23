namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDCamera : IVersionable<HDCamera.Version>
    {
        enum Version
        {
            MergeHDAdditionalCameraDataIntoHDCamera, //see HDAdditionalCameraData for migration step prior merge
        }

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();

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

            //1 - Create a temporary GameObject into the HDCamera
            HDCamera tmpHDCam = new GameObject("Temporary for creating HDCamera in place of Camera", new[] { typeof(HDCamera) }).GetComponent<HDCamera>();

            //2 - Copy the Camera values on this GameObject into the HDCamera
            tmpHDCam.CopyFrom(camera); //copy camera state

            //3 - Copy the HDAdditionalCameraData values into the HDCamera if any available
            HDAdditionalCameraData additionalData = camera.GetComponent<HDAdditionalCameraData>();
            if (additionalData != null)
                HDUtils.RuntimeCopyComponentValue(additionalData, tmpHDCam);

            //4 - Remove Camera and HDAdditionalCameraData if any available
            if (additionalData)
                GameObject.DestroyImmediate(additionalData);
            GameObject.DestroyImmediate(camera);

            //5 - Add HDCamera and copy evrything from the temporary one
            HDCamera result = refGO.AddComponent<HDCamera>();
            result.CopyFrom(tmpHDCam); //copy camera state
            HDUtils.RuntimeCopyComponentValue(tmpHDCam, result);

            //6 - Destroy temporary GameObject
            GameObject.DestroyImmediate(tmpHDCam.gameObject);
        }

#if UNITY_EDITOR
        internal static void ConvertCameraToHDCameraWithUndo(Camera camera)
        {
            if (camera == null || camera is HDCamera)
                return;

            GameObject refGO = camera.gameObject;
            
            UnityEditor.Undo.SetCurrentGroupName("Convert Camera to HDCamera");

            //1 - Create a temporary GameObject into the HDCamera
            HDCamera tmpHDCam = new GameObject("Temporary for creating HDCamera in place of Camera", new[] { typeof(HDCamera) }).GetComponent<HDCamera>();

            //2 - Copy the Camera values on this GameObject into the HDCamera
            tmpHDCam.CopyFrom(camera); //copy camera state

            //3 - Copy the HDAdditionalCameraData values into the HDCamera if any available
            HDAdditionalCameraData additionalData = camera.GetComponent<HDAdditionalCameraData>();
            if (additionalData != null)
                HDUtils.RuntimeCopyComponentValue(additionalData, tmpHDCam);

            //4 - Remove Camera and HDAdditionalCameraData if any available
            if (additionalData)
                UnityEditor.Undo.DestroyObjectImmediate(additionalData);
            UnityEditor.Undo.DestroyObjectImmediate(camera);

            //5 - Add HDCamera and copy evrything from the temporary one
            HDCamera result = UnityEditor.Undo.AddComponent<HDCamera>(refGO);
            result.CopyFrom(tmpHDCam); //copy camera state
            HDUtils.RuntimeCopyComponentValue(tmpHDCam, result);

            //6 - Destroy temporary GameObject
            GameObject.DestroyImmediate(tmpHDCam.gameObject);
        }
#endif
    }
}
