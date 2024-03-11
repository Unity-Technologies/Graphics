using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;

public class SVLChecker : MonoBehaviour
{
    public GameObject sphere;

    public void Check()
    {
#if !UNITY_EDITOR
        GraphicsSettings.logWhenShaderIsCompiled = true;
        sphere.SetActive(true);
        Camera.main.Render();
        GraphicsSettings.logWhenShaderIsCompiled = false;

        // Read log while the handle is still controlled by Unity
        using (var logFile = new FileStream(Application.consoleLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (var reader = new StreamReader(logFile, Encoding.Default))
                CheckVariantLog(reader.ReadToEnd());
        }
#else
        sphere.SetActive(true);
#endif
    }

    void CheckVariantLog(string logFileContent)
    {
        var match = GenerateShaderVariantList.s_CompiledShaderRegex.Match(logFileContent);
        if (!match.Success)
        {
            Debug.LogError("Shader variant logging info not found!");
            sphere.SetActive(false); // Also make the GFX test fail
        }
        else
        {
            Debug.Log("SVL Check OK!");
        }
    }
}
