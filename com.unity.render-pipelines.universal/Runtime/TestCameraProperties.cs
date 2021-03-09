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
    private const float CompareBias = 0.00001f;

    public static bool isActive { get; set; }

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
        if (!isActive)
            return;

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
        if (!isActive)
            return;

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
            PropertyGUI(nameof(oldData._WorldSpaceCameraPos),
                AreApproximatelySame(oldData._WorldSpaceCameraPos, newData._WorldSpaceCameraPos),
                !IsAllZeroes(oldData._WorldSpaceCameraPos, newData._WorldSpaceCameraPos),
                style);
            PropertyGUI(nameof(oldData._Reflection),
                AreApproximatelySame(oldData._Reflection, newData._Reflection),
                !IsAllZeroes(oldData._Reflection, newData._Reflection),
                style);
            PropertyGUI(nameof(oldData._ScreenParams),
                AreApproximatelySame(oldData._ScreenParams, newData._ScreenParams),
                !IsAllZeroes(oldData._ScreenParams, newData._ScreenParams),
                style);
            PropertyGUI(nameof(oldData._ProjectionParams),
                AreApproximatelySame(oldData._ProjectionParams, newData._ProjectionParams),
                !IsAllZeroes(oldData._ProjectionParams, newData._ProjectionParams),
                style);
            PropertyGUI(nameof(oldData._ZBufferParams),
                AreApproximatelySame(oldData._ZBufferParams, newData._ZBufferParams),
                !IsAllZeroes(oldData._ZBufferParams, newData._ZBufferParams),
                style);
            PropertyGUI(nameof(oldData.unity_OrthoParams),
                AreApproximatelySame(oldData.unity_OrthoParams, newData.unity_OrthoParams),
                !IsAllZeroes(oldData.unity_OrthoParams, newData.unity_OrthoParams),
                style);
            PropertyGUI(nameof(oldData.unity_HalfStereoSeparation),
                AreApproximatelySame(oldData.unity_HalfStereoSeparation, newData.unity_HalfStereoSeparation),
                !IsAllZeroes(oldData.unity_HalfStereoSeparation, newData.unity_HalfStereoSeparation),
                style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes0),
                AreApproximatelySame(oldData.unity_CameraWorldClipPlanes0, newData.unity_CameraWorldClipPlanes0),
                !IsAllZeroes(oldData.unity_CameraWorldClipPlanes0, newData.unity_CameraWorldClipPlanes0),
                style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes1),
                AreApproximatelySame(oldData.unity_CameraWorldClipPlanes1, newData.unity_CameraWorldClipPlanes1),
                !IsAllZeroes(oldData.unity_CameraWorldClipPlanes1, newData.unity_CameraWorldClipPlanes1),
                style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes2),
                AreApproximatelySame(oldData.unity_CameraWorldClipPlanes2, newData.unity_CameraWorldClipPlanes2),
                !IsAllZeroes(oldData.unity_CameraWorldClipPlanes2, newData.unity_CameraWorldClipPlanes2),
                style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes3),
                AreApproximatelySame(oldData.unity_CameraWorldClipPlanes3, newData.unity_CameraWorldClipPlanes3),
                !IsAllZeroes(oldData.unity_CameraWorldClipPlanes3, newData.unity_CameraWorldClipPlanes3),
                style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes4),
                AreApproximatelySame(oldData.unity_CameraWorldClipPlanes4, newData.unity_CameraWorldClipPlanes4),
                !IsAllZeroes(oldData.unity_CameraWorldClipPlanes4, newData.unity_CameraWorldClipPlanes4),
                style);
            PropertyGUI(nameof(oldData.unity_CameraWorldClipPlanes5),
                AreApproximatelySame(oldData.unity_CameraWorldClipPlanes5, newData.unity_CameraWorldClipPlanes5),
                !IsAllZeroes(oldData.unity_CameraWorldClipPlanes5, newData.unity_CameraWorldClipPlanes5),
                style);
            PropertyGUI(nameof(oldData.unity_BillboardNormal),
                AreApproximatelySame(oldData.unity_BillboardNormal, newData.unity_BillboardNormal),
                !IsAllZeroes(oldData.unity_BillboardNormal, newData.unity_BillboardNormal),
                style);
            PropertyGUI(nameof(oldData.unity_BillboardTangent),
                AreApproximatelySame(oldData.unity_BillboardTangent, newData.unity_BillboardTangent),
                !IsAllZeroes(oldData.unity_BillboardTangent, newData.unity_BillboardTangent),
                style);
            PropertyGUI(nameof(oldData.unity_BillboardCameraParams),
                AreApproximatelySame(oldData.unity_BillboardCameraParams, newData.unity_BillboardCameraParams),
                !IsAllZeroes(oldData.unity_BillboardCameraParams, newData.unity_BillboardCameraParams),
                style);
        }

        GUILayout.EndVertical();

        return isExpended;
    }

    private static bool IsAllZeroes(float4 first, float4 second)
    {
        return math.length(first) == 0 && math.length(second) == 0;
    }

    private static bool IsAllZeroes(float4x4 first, float4x4 second)
    {
        return
            math.length(first.c0) == 0 &&
            math.length(first.c1) == 0 &&
            math.length(first.c2) == 0 &&
            math.length(first.c3) == 0 &&

            math.length(second.c0) == 0 &&
            math.length(second.c1) == 0 &&
            math.length(second.c2) == 0 &&
            math.length(second.c3) == 0;

    }

    private static bool AreApproximatelySame(float4 first, float4 second)
    {
        return math.all(math.abs(first - second) < CompareBias);
    }

    private static bool AreApproximatelySame(float4x4 first, float4x4 second)
    {
        var mask = (first - second);
        return
            math.all(math.abs(mask.c0) < CompareBias) &&
            math.all(math.abs(mask.c1) < CompareBias) &&
            math.all(math.abs(mask.c2) < CompareBias) &&
            math.all(math.abs(mask.c3) < CompareBias);
    }

    private static void PropertyGUI(string name, bool condition, bool valid, GUIStyle style)
    {
        string conditionText = condition ? "<color=#00ff00>Green</color>" : "<color=#ff0000>Red</color>";
        string validText = valid ? "<color=#00ff00>Valid</color>" : "<color=#ffff00>All Zeroes</color>";
        GUILayout.Label($"{name} is {conditionText} is {validText}", style);
    }
}
