#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CustomBuildSceneIterator {
    public const int DEFAULT_SCENES_PER_BUILD = 25;
    public const string ITER_ENV_VAR_NAME = "GRAPHICS_TEST_ITERATOR";
    public const string SCENES_PER_BUILD_ENV_VAR_NAME = "SCENES_PER_BUILD";
    public const string TESTS_DONE_ENV_VAR = "GRAPHICS_TESTS_DONE";
    public const string TEMP_SCENES_FILE_NAME = "\\tempSceneStorage.txt";
    public const string CUR_STATE_FILE_NAME = "\\CurTestState.txt";
    public const string TESTS_DONE_FILE_NAME = "\\TestsDone.txt";
    public const string LOG_PATH = "\\SetupLog.txt";
    private static StreamWriter logWriter;

    private static int GetCurrentIteration() {
        if (!File.Exists(Directory.GetCurrentDirectory() + CUR_STATE_FILE_NAME)) {
            return 0;
        }
        StreamReader tempSceneFile = new StreamReader(Directory.GetCurrentDirectory() + CUR_STATE_FILE_NAME);

        logWriter.WriteLine(Directory.GetCurrentDirectory() + CUR_STATE_FILE_NAME);

        int curIter = int.Parse(tempSceneFile.ReadLine());
        logWriter.WriteLine("curIter: " + curIter);
        tempSceneFile.Close();
        return curIter;
    }

    private static void SetCurrentIteration(int curIter) {
        if (File.Exists(Directory.GetCurrentDirectory() + CUR_STATE_FILE_NAME)) {
            File.Delete(Directory.GetCurrentDirectory() + CUR_STATE_FILE_NAME);
        }
        StreamWriter stateFile = new StreamWriter(Directory.GetCurrentDirectory() + CUR_STATE_FILE_NAME, true);
        stateFile.WriteLine(curIter.ToString());
        stateFile.Close();
    }

    public static void SelectIterativeScenesToBuild() {
        string tempScenePath = Directory.GetCurrentDirectory() + TEMP_SCENES_FILE_NAME;
        string desktopLogPath = Directory.GetCurrentDirectory() + LOG_PATH;
        logWriter = new StreamWriter(desktopLogPath, true);
        string scenesPerBuildEnvVal = Environment.GetEnvironmentVariable(SCENES_PER_BUILD_ENV_VAR_NAME, EnvironmentVariableTarget.Machine);

        string dataPath = Application.persistentDataPath;

        List<EditorBuildSettingsScene> allScenes;
        if (File.Exists(tempScenePath)) {
            // Restore enabled scenes state from the temp scene file before filtering by SCENES_PER_BUILD
            logWriter.WriteLine("temp scene file exists, restoring...");
            allScenes = new List<EditorBuildSettingsScene>();
            StreamReader tempSceneFile = new StreamReader(@tempScenePath);
            string line;
            while ((line = tempSceneFile.ReadLine()) != null) {
                bool.TryParse(tempSceneFile.ReadLine(), out bool sceneEnabled);
                allScenes.Add(new EditorBuildSettingsScene(new GUID(line), sceneEnabled));
            }
            tempSceneFile.Close();
        } else {
            // Write scene list (and enabled state) to temp save location, so it can be referenced the next iteration
            StreamWriter sceneWriter = new StreamWriter(tempScenePath, true);
            allScenes = EditorBuildSettings.scenes.ToList();
            logWriter.WriteLine("No temp scene file, writing " + allScenes.Count + " scenes");
            foreach (EditorBuildSettingsScene scene in allScenes) {
                sceneWriter.WriteLine(scene.guid);
                sceneWriter.WriteLine(scene.enabled);
            }
            sceneWriter.Close();
        }

        // Sort by enabled scenes
        List<EditorBuildSettingsScene> enabledScenes = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene scene in allScenes) {
            if (scene.enabled) {
                enabledScenes.Add(scene);
            }
        }

        int maxScenesPerBuild = scenesPerBuildEnvVal != null ? int.Parse(scenesPerBuildEnvVal) : DEFAULT_SCENES_PER_BUILD;
        int scenesInBuild = maxScenesPerBuild;
        int curIter = GetCurrentIteration();
        logWriter.WriteLine("Iteration: " + curIter);

        // Handles the case of hitting the end of the scene list, and not having enough scenes do run the whole SCENES_PER_BUILD quantity
        if ((curIter + 1) * maxScenesPerBuild >= enabledScenes.Count) {
            StreamWriter testsCompleteFile = new StreamWriter(Directory.GetCurrentDirectory() + TESTS_DONE_FILE_NAME, true);
            testsCompleteFile.WriteLine("All tests complete. Note: This is a temp file, and may be safely deleted");
            testsCompleteFile.Close();
            scenesInBuild = (maxScenesPerBuild * (curIter + 1)) - enabledScenes.Count;
        }

        EditorBuildSettings.scenes = enabledScenes.GetRange(curIter * maxScenesPerBuild, scenesInBuild).ToArray();  // Breaks here
        logWriter.WriteLine("Attempting to run " + EditorBuildSettings.scenes.Length.ToString() + " scenes");
        SetCurrentIteration(curIter + 1);

        logWriter.Close();
    }

}

#endif
