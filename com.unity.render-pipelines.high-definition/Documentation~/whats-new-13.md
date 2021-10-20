# What's new in HDRP version 13 / Unity 2022.1

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Added

### Material Runtime API

To enable or disable most HDRP shader features on a material, changes need to be done on the keyword state and sometimes on one or more properties. This is done automatically when editing values though the inspector, but from HDRP 13.0, new APIs are also available in order to run the validation steps from script, both in the editor and in standalone builds.

For more information, see the [Material Scripting API documentation](Material-API.md).

### Access main directional light from ShaderGraph

From HDRP 13, you can access the main light direction from a ShaderGraph using the *Main Light Direction* node.
For more information, see the [node documentation](https://docs.unity3d.com/Packages/com.unity.shadergraph@13.1/manual/Main-Light-Direction-Node.html).

## Updated
