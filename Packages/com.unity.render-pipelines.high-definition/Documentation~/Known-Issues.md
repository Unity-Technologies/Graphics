# Known issues

This page contains information on known about issues you may encounter while using the High Definition Render Pipeline (HDRP). Each entry describes the issue and then details the steps to follow in order to resolve the issue.

## Material array size

If you upgrade your HDRP Project to a later version, you may encounter an error message similar to:

```
Property (_Env2DCaptureForward) exceeds previous array size (48 vs 6). Cap to previous size.

UnityEditor.EditorApplication:Internal_CallGlobalEventHandler()
```

To fix this issue, restart the Unity editor.

## Working with Collaborate and a local HDRP config package

If you installed the [config package](configure-a-project-using-the-hdrp-config-package.md) locally using the [HDRP Wizard](Render-Pipeline-Wizard.md), Unity may have placed it in `LocalPackages/com.unity.render-pipelines.high-definition-config` depending on the HDRP version your project used at that time.

In this case, Collaborate does not track changes you make to the local HDRP config package files. To fix this, move the local config package from `LocalPackages/com.unity.render-pipelines.high-definition-config` to `Packages/com.unity.render-pipelines.high-definition-config`. This embeds it in your project and allows Collaborate to tracks and version changes you make.
