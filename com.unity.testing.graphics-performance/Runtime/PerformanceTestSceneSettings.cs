using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class PerformanceTestSceneSettings : MonoBehaviour
{
    public enum ColorBufferFormat
    {
        R8G8B8A8 = GraphicsFormat.R8G8B8A8_SNorm,
        R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,
        R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
    }

    [HideInInspector, System.NonSerialized]
    public Camera testCamera;

    [Header("Camera Settings")]
    [Tooltip("Width of the render texture assigned to the camera during the test execution.")]
    public int cameraWidth = 1920;
    [Tooltip("Height of the render texture assigned to the camera during the test execution.")]
    public int cameraHeight = 1080;
    [Tooltip("Graphics Format of the render texture assigned to the camera during the test execution.")]
    public ColorBufferFormat colorBufferFormat = ColorBufferFormat.R8G8B8A8;

    [Header("Performance Counters Settings")]
    [Tooltip("Number of frame the performance counter test will use to record the timings.")]
    public int measurementCount = 30;

    [Header("Memory Settings")]
    [Tooltip("Minimum size in byte for an object to be detected by the performance test.")]
    public int minObjectSize = 1024 * 64;
}
