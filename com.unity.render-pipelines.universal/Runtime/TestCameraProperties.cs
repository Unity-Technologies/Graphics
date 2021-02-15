using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;

// TODO: Remove after the testing
internal static class TestCameraProperties
{
    private static Mesh SomeMesh;
    private static Material FetchCameraPropertiesMaterialOld;
    private static Material FetchCameraPropertiesMaterialNew;
    private static ComputeBuffer CameraPropertiesDataBufferOld;
    private static ComputeBuffer CameraPropertiesDataBufferNew;
    private static CameraPropertiesData DataOld;
    private static CameraPropertiesData DataNew;

    public struct CameraPropertiesData
    {
        public float4 _WorldSpaceCameraPos;
        public float4x4 _Reflection;
        public float4 _ScreenParams;
        public float4 _ProjectionParams;
        public float4 _ZBufferParams;
        public float4 unity_OrthoParams;
        public float4 unity_HalfStereoSeparation;
        public float4 unity_CameraWorldClipPlanes0;
        public float4 unity_CameraWorldClipPlanes1;
        public float4 unity_CameraWorldClipPlanes2;
        public float4 unity_CameraWorldClipPlanes3;
        public float4 unity_CameraWorldClipPlanes4;
        public float4 unity_CameraWorldClipPlanes5;
        public float4 unity_BillboardNormal;
        public float4 unity_BillboardTangent;
        public float4 unity_BillboardCameraParams;
    }

    public static void SampleCameraPropertiesOld(CommandBuffer cmd)
    {
        if (SomeMesh == null)
            SomeMesh = CoreUtils.CreateCubeMesh(-Vector3.one, Vector3.one);

        unsafe
        {
            if (FetchCameraPropertiesMaterialOld == null)
            {
                FetchCameraPropertiesMaterialOld = new Material(Shader.Find("Hidden/FetchCameraPropertiesShader"));
                CameraPropertiesDataBufferOld = new ComputeBuffer(1, sizeof(CameraPropertiesData));
                cmd.SetRandomWriteTarget(1, CameraPropertiesDataBufferOld);
            }
        }
        cmd.DrawMesh(SomeMesh, Matrix4x4.Translate(Vector3.zero), FetchCameraPropertiesMaterialOld);

        CameraPropertiesData[] array = new CameraPropertiesData[1];
        CameraPropertiesDataBufferOld.GetData(array);
        DataOld = array[0];
    }

    public static void SampleCameraPropertiesNew(CommandBuffer cmd)
    {
        if (SomeMesh == null)
            SomeMesh = CoreUtils.CreateCubeMesh(-Vector3.one, Vector3.one);

        unsafe
        {
            if (FetchCameraPropertiesMaterialNew == null)
            {
                FetchCameraPropertiesMaterialNew = new Material(Shader.Find("Hidden/FetchCameraPropertiesShader"));
                CameraPropertiesDataBufferNew = new ComputeBuffer(1, sizeof(CameraPropertiesData));
                cmd.SetRandomWriteTarget(1, CameraPropertiesDataBufferNew);
            }
        }
        cmd.DrawMesh(SomeMesh, Matrix4x4.Translate(Vector3.zero), FetchCameraPropertiesMaterialNew);

        CameraPropertiesData[] array = new CameraPropertiesData[1];
        CameraPropertiesDataBufferNew.GetData(array);
        DataNew = array[0];
    }

    public static void LogDifference()
    {
        CameraPropertiesData oldData = DataOld;
        CameraPropertiesData newData = DataNew;

        string message = "";
        message += $"{nameof(oldData._WorldSpaceCameraPos)}: {oldData._WorldSpaceCameraPos == newData._WorldSpaceCameraPos}\n";
        message += $"{nameof(oldData._Reflection)}: {oldData._Reflection == newData._Reflection}\n";
        message += $"{nameof(oldData._ScreenParams)}: {oldData._ScreenParams == newData._ScreenParams}\n";
        message += $"{nameof(oldData._ProjectionParams)}: {oldData._ProjectionParams == newData._ProjectionParams}\n";
        message += $"{nameof(oldData._ZBufferParams)}: {oldData._ZBufferParams == newData._ZBufferParams}\n";
        message += $"{nameof(oldData.unity_OrthoParams)}: {oldData.unity_OrthoParams == newData.unity_OrthoParams}\n";
        message += $"{nameof(oldData.unity_HalfStereoSeparation)}: {oldData.unity_HalfStereoSeparation == newData.unity_HalfStereoSeparation}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes0)}: {oldData.unity_CameraWorldClipPlanes0 == newData.unity_CameraWorldClipPlanes0}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes1)}: {oldData.unity_CameraWorldClipPlanes1 == newData.unity_CameraWorldClipPlanes1}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes2)}: {oldData.unity_CameraWorldClipPlanes2 == newData.unity_CameraWorldClipPlanes2}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes3)}: {oldData.unity_CameraWorldClipPlanes3 == newData.unity_CameraWorldClipPlanes3}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes4)}: {oldData.unity_CameraWorldClipPlanes4 == newData.unity_CameraWorldClipPlanes4}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes5)}: {oldData.unity_CameraWorldClipPlanes5 == newData.unity_CameraWorldClipPlanes5}\n";
        message += $"{nameof(oldData.unity_BillboardNormal)}: {oldData.unity_BillboardNormal == newData.unity_BillboardNormal}\n";
        message += $"{nameof(oldData.unity_BillboardTangent)}: {oldData.unity_BillboardTangent == newData.unity_BillboardTangent}\n";
        message += $"{nameof(oldData.unity_BillboardCameraParams)}: {oldData.unity_BillboardCameraParams == newData.unity_BillboardCameraParams}\n";

        message += $"{nameof(oldData._WorldSpaceCameraPos)}: {oldData._WorldSpaceCameraPos} == {newData._WorldSpaceCameraPos}\n";
        message += $"{nameof(oldData._Reflection)}: {oldData._Reflection} {newData._Reflection}\n";
        message += $"{nameof(oldData._ScreenParams)}: {oldData._ScreenParams} {newData._ScreenParams}\n";
        message += $"{nameof(oldData._ProjectionParams)}: {oldData._ProjectionParams} {newData._ProjectionParams}\n";
        message += $"{nameof(oldData._ZBufferParams)}: {oldData._ZBufferParams} {newData._ZBufferParams}\n";
        message += $"{nameof(oldData.unity_OrthoParams)}: {oldData.unity_OrthoParams} {newData.unity_OrthoParams}\n";
        message += $"{nameof(oldData.unity_HalfStereoSeparation)}: {oldData.unity_HalfStereoSeparation} {newData.unity_HalfStereoSeparation}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes0)}: {oldData.unity_CameraWorldClipPlanes0} {newData.unity_CameraWorldClipPlanes0}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes1)}: {oldData.unity_CameraWorldClipPlanes1} {newData.unity_CameraWorldClipPlanes1}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes2)}: {oldData.unity_CameraWorldClipPlanes2} {newData.unity_CameraWorldClipPlanes2}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes3)}: {oldData.unity_CameraWorldClipPlanes3} {newData.unity_CameraWorldClipPlanes3}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes4)}: {oldData.unity_CameraWorldClipPlanes4} {newData.unity_CameraWorldClipPlanes4}\n";
        message += $"{nameof(oldData.unity_CameraWorldClipPlanes5)}: {oldData.unity_CameraWorldClipPlanes5} {newData.unity_CameraWorldClipPlanes5}\n";
        message += $"{nameof(oldData.unity_BillboardNormal)}: {oldData.unity_BillboardNormal} {newData.unity_BillboardNormal}\n";
        message += $"{nameof(oldData.unity_BillboardTangent)}: {oldData.unity_BillboardTangent} {newData.unity_BillboardTangent}\n";
        message += $"{nameof(oldData.unity_BillboardCameraParams)}: {oldData.unity_BillboardCameraParams} {newData.unity_BillboardCameraParams}\n";
        Debug.Log(message);
    }

    public static bool GUIDifference(bool isExpended)
    {
        CameraPropertiesData oldData = DataOld;
        CameraPropertiesData newData = DataNew;

        GUILayout.BeginVertical();
        isExpended = GUILayout.Toggle(isExpended, "Difference");
        GUIStyle style = new GUIStyle(GUI.skin.label);

        if (isExpended)
        {
            PropertyGUI(nameof(oldData._WorldSpaceCameraPos), math.all(oldData._WorldSpaceCameraPos == newData._WorldSpaceCameraPos), math.length(oldData._WorldSpaceCameraPos) != 0, style);
            PropertyGUI(nameof(oldData._Reflection),
                math.all(oldData._Reflection.c0 == newData._Reflection.c0) &&
                math.all(oldData._Reflection.c1 == newData._Reflection.c1) &&
                math.all(oldData._Reflection.c2 == newData._Reflection.c2) &&
                math.all(oldData._Reflection.c3 == newData._Reflection.c3),
                math.length(oldData._Reflection.c0) + math.length(oldData._Reflection.c1) + math.length(oldData._Reflection.c2) + math.length(oldData._Reflection.c3) != 0, style);
            PropertyGUI(nameof(oldData._ScreenParams), math.all(oldData._ScreenParams == newData._ScreenParams), math.length(oldData._ScreenParams) != 0, style);
            PropertyGUI(nameof(oldData._ProjectionParams), math.all(oldData._ProjectionParams == newData._ProjectionParams), math.length(oldData._ProjectionParams) != 0, style);
            PropertyGUI(nameof(oldData._ZBufferParams), math.all(oldData._ZBufferParams == newData._ZBufferParams), math.length(oldData._ZBufferParams) != 0, style);
            PropertyGUI(nameof(oldData.unity_OrthoParams), math.all(oldData.unity_OrthoParams == newData.unity_OrthoParams), math.length(oldData.unity_OrthoParams) != 0, style);
            PropertyGUI(nameof(oldData.unity_HalfStereoSeparation), math.all(oldData.unity_HalfStereoSeparation == newData.unity_HalfStereoSeparation), math.length(oldData.unity_HalfStereoSeparation) != 0, style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes0), math.all(oldData.unity_CameraWorldClipPlanes0 == newData.unity_CameraWorldClipPlanes0), math.length(oldData.unity_CameraWorldClipPlanes0) != 0, style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes1), math.all(oldData.unity_CameraWorldClipPlanes1 == newData.unity_CameraWorldClipPlanes1), math.length(oldData.unity_CameraWorldClipPlanes1) != 0, style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes2), math.all(oldData.unity_CameraWorldClipPlanes2 == newData.unity_CameraWorldClipPlanes2), math.length(oldData.unity_CameraWorldClipPlanes2) != 0, style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes3), math.all(oldData.unity_CameraWorldClipPlanes3 == newData.unity_CameraWorldClipPlanes3), math.length(oldData.unity_CameraWorldClipPlanes3) != 0, style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes4), math.all(oldData.unity_CameraWorldClipPlanes4 == newData.unity_CameraWorldClipPlanes4), math.length(oldData.unity_CameraWorldClipPlanes4) != 0, style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes5), math.all(oldData.unity_CameraWorldClipPlanes5 == newData.unity_CameraWorldClipPlanes5), math.length(oldData.unity_CameraWorldClipPlanes5) != 0, style);
            PropertyGUI(nameof(oldData.unity_BillboardNormal), math.all(oldData.unity_BillboardNormal == newData.unity_BillboardNormal), math.length(oldData.unity_BillboardNormal) != 0, style);
            PropertyGUI(nameof(oldData.unity_BillboardTangent), math.all(oldData.unity_BillboardTangent == newData.unity_BillboardTangent), math.length(oldData.unity_BillboardTangent) != 0, style);
            PropertyGUI(nameof(oldData.unity_BillboardCameraParams), math.all(oldData.unity_BillboardCameraParams == newData.unity_BillboardCameraParams), math.length(oldData.unity_BillboardCameraParams) != 0, style);
        }

        GUILayout.EndVertical();

        return isExpended;
    }

    private static void PropertyGUI(string name, bool condition, bool valid, GUIStyle style)
    {
        string conditionText = condition ? "<color=#00ff00>Green</color>" : "<color=#ff0000>Red</color>";
        string validText = valid ? "<color=#00ff00>Valid</color>" : "<color=#ffff00>All Zeroes</color>";
        GUILayout.Label($"{name} is {conditionText} is {validText}", style);
    }
}
