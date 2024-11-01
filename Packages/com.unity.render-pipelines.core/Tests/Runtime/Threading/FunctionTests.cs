using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

class ThreadingEmulationFunctionTests : IPrebuildSetup
{
    private const string kPathToWaveShader = "Packages/com.unity.render-pipelines.core/Tests/Runtime/Threading/FunctionTestsWave.compute";
    private const string kPathToGroupShader = "Packages/com.unity.render-pipelines.core/Tests/Runtime/Threading/FunctionTestsGroup.compute";

    private static int kMaxTestBlockSize = 1024;

    private ComputeShader m_waveCs;
    private ComputeShader m_groupCs;

    private GraphicsBuffer m_inputBuffer;
    private GraphicsBuffer m_outputBuffer;

    private int[] m_sequenceBufferData;
    private int[] m_zeroedBufferData;

    public enum Kernel
    {
        AllEqual,
        AllTrue,
        AnyTrue,
        Ballot,
        BitAnd,
        BitOr,
        BitXor,
        CountBits,
        Max,
        Min,
        Product,
        Sum,
        GetCount,
        GetIndex,
        IsFirst,
        PrefixCountBits,
        PrefixProduct,
        PrefixSum,
        ReadAtBroadcast,
        ReadAtShuffle,
        ReadFirst,
    }

    public enum WaveSizeKeyword
    {
        EMULATE_WAVE_SIZE_8,
        EMULATE_WAVE_SIZE_16,
        EMULATE_WAVE_SIZE_32,
        EMULATE_WAVE_SIZE_64,
        EMULATE_WAVE_SIZE_128,
        WAVE_SIZE_NATIVE
    }

    public enum GroupSizeKeyword
    {
        GROUP_SIZE_1 = 1,
        GROUP_SIZE_2 = 2,
        GROUP_SIZE_4 = 4,
        GROUP_SIZE_8 = 8,
        GROUP_SIZE_16 = 16,
        GROUP_SIZE_32 = 32,
        GROUP_SIZE_64 = 64,
        GROUP_SIZE_128 = 128,
        GROUP_SIZE_256 = 256,
        GROUP_SIZE_512 = 512,
        GROUP_SIZE_1024 = 1024
    }

    string GetTestResourceName(string srcPath)
    {
        return "_" + Path.GetFileNameWithoutExtension(srcPath);
    }

#if UNITY_EDITOR
    void CopyTestResource(string srcPath)
    {
        string dstPath = Path.Join("Assets/Resources", GetTestResourceName(srcPath) + Path.GetExtension(srcPath));

        AssetDatabase.CopyAsset(srcPath, dstPath);
    }
#endif

    // Note: We don't currently attempt to clean up these resources because other tests could be using
    //       the resources folder.
    public void Setup()
    {
#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        CopyTestResource(kPathToWaveShader);
        CopyTestResource(kPathToGroupShader);

        AssetDatabase.Refresh();
#endif
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        if (!SystemInfo.supportsComputeShaders)
            Assert.Ignore("Compute shaders are a prerequisite for the threading tests and they aren't supported on the current device");

        m_waveCs = Resources.Load<ComputeShader>(GetTestResourceName(kPathToWaveShader));
        Assert.IsNotNull(m_waveCs, "Failed to find threading wave test compute shader kernel");

        m_groupCs = Resources.Load<ComputeShader>(GetTestResourceName(kPathToGroupShader));
        Assert.IsNotNull(m_groupCs, "Failed to find threading group test compute shader kernel");

        m_sequenceBufferData = Enumerable.Range(0, kMaxTestBlockSize).ToArray();
        m_zeroedBufferData = Enumerable.Repeat(0, kMaxTestBlockSize).ToArray();

        m_inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, kMaxTestBlockSize, 4);
        m_outputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, kMaxTestBlockSize, 4);

        m_inputBuffer.SetData(m_sequenceBufferData);
    }

    [OneTimeTearDown]
    public void OneTimeCleanUp()
    {
        m_inputBuffer.Release();
        m_outputBuffer.Release();
    }

    [SetUp]
    public void SetUp()
    {
        // Zero the output buffer before each test case
        m_outputBuffer.SetData(m_zeroedBufferData);
    }

    private static int[] GetBufferData(GraphicsBuffer buffer)
    {
        int[] result = null;

        var cmdReadBack = CommandBufferPool.Get("Readback");

        cmdReadBack.RequestAsyncReadback(buffer, req =>
        {
            if (req.done)
            {
                var data = req.GetData<int>();
                result = data.ToArray();
            }
        });
        cmdReadBack.WaitAllAsyncReadbackRequests();

        Graphics.ExecuteCommandBuffer(cmdReadBack);
        CommandBufferPool.Release(cmdReadBack);

        return result;
    }

    private void ValidateDeviceComputeGroupSize(int requiredComputeGroupSizeX)
    {
        int deviceMaxComputeGroupSizeX = SystemInfo.maxComputeWorkGroupSizeX;
        if (deviceMaxComputeGroupSizeX < requiredComputeGroupSizeX)
            Assert.Ignore($"The device's max compute group size X dimension ({deviceMaxComputeGroupSizeX}) does not meet the minimum requirement ({requiredComputeGroupSizeX}) for this test");
    }

    [Test]
    [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.WSAPlayerX64, RuntimePlatform.WindowsPlayer })] //https://jira.unity3d.com/browse/UUM-78016
    public void WaveTest([Values]Kernel kernel, [Values]WaveSizeKeyword waveSizeKeyword)
    {
        int groupSize = (int)GroupSizeKeyword.GROUP_SIZE_128;

        // Ensure that the wave tests are capable of running on the current device
        ValidateDeviceComputeGroupSize(groupSize);

        // When emulating wave sizes, a special shader keyword must be activated.
        bool hasEmulatedWaveSize = waveSizeKeyword != WaveSizeKeyword.WAVE_SIZE_NATIVE;

        var cmd = CommandBufferPool.Get("Threading Wave Test");

        if (hasEmulatedWaveSize)
        {
            cmd.SetKeyword(m_waveCs, new LocalKeyword(m_waveCs, waveSizeKeyword.ToString()), true);
        }

        int kernelIndex = (int)kernel;

        cmd.SetComputeBufferParam(m_waveCs, kernelIndex, "_Input", m_inputBuffer);
        cmd.SetComputeBufferParam(m_waveCs, kernelIndex, "_Output", m_outputBuffer);
        cmd.DispatchCompute(m_waveCs, kernelIndex, 1, 1, 1);

        if (hasEmulatedWaveSize)
        {
            cmd.SetKeyword(m_waveCs, new LocalKeyword(m_waveCs, waveSizeKeyword.ToString()), false);
        }

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

        // Read back and compare the results.
        var result = GetBufferData(m_outputBuffer);

        Assert.That(result, Has.Exactly(groupSize).EqualTo(1).And.Exactly(kMaxTestBlockSize - groupSize).EqualTo(0));
    }

    [Test]
    public void GroupTest([Values]Kernel kernel, [Values]GroupSizeKeyword groupSizeKeyword)
    {
        int groupSize = (int)groupSizeKeyword;

        // Ensure that the group tests are capable of running on the current device
        ValidateDeviceComputeGroupSize(groupSize);

        var cmd = CommandBufferPool.Get("Threading Group Test");

        cmd.SetKeyword(m_groupCs, new LocalKeyword(m_groupCs, groupSizeKeyword.ToString()), true);

        int kernelIndex = (int)kernel;

        cmd.SetComputeBufferParam(m_groupCs, kernelIndex, "_Input", m_inputBuffer);
        cmd.SetComputeBufferParam(m_groupCs, kernelIndex, "_Output", m_outputBuffer);
        cmd.DispatchCompute(m_groupCs, kernelIndex, 1, 1, 1);

        cmd.SetKeyword(m_groupCs, new LocalKeyword(m_groupCs, groupSizeKeyword.ToString()), false);

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

        // Read back and compare the results.
        var result = GetBufferData(m_outputBuffer);

        Assert.That(result, Has.Exactly(groupSize).EqualTo(1).And.Exactly(kMaxTestBlockSize - groupSize).EqualTo(0));
    }
}
