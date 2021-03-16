# Known issues

This page contains information on known about issues you may encounter while using HDRP. Each entry describes the issue and then details the steps to follow in order to resolve the issue.

## Material array size

If you upgrade your HDRP Project to a later version, you may encounter an error message similar to:

```
Property (_Env2DCaptureForward) exceeds previous array size (48 vs 6). Cap to previous size.

UnityEditor.EditorApplication:Internal_CallGlobalEventHandler()
```

To fix this issue, restart the Unity editor.

## Collaborate and local HDRP config package

If you installed the local config package with the wizard, it may have been placed in `ROOT/LocalPackages/com.unity.render-pipelines.high-definition-config` depending on the HDRP version used at that moment.

In that case, you can move the local config package from `ROOT/LocalPackages/com.unity.render-pipelines.high-definition-config` to `ROOT/Packages/com.unity.render-pipelines.high-definition-config` and update your `Packages/manifest.json` files accordingly.

Now the local config package files will be versionned.
