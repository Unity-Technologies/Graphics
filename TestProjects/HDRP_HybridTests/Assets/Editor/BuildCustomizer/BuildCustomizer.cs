using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Build.Classic;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEngine;

class BuildConfigurationCustomizer : ClassicBuildPipelineCustomizer
{
    public override string[] ProvidePlayerScriptingDefines() => new[] {"MY_CUSTOM_DEFINE"};

    public override void RegisterAdditionalFilesToDeploy(Action<string, string> registerAdditionalFileToDeploy)
    {
        var file = "RandomDataFile.txt";
        registerAdditionalFileToDeploy(Path.Combine(Application.dataPath, file), Path.Combine(StreamingAssetsDirectory, file));
    }
}
