# Upgrading Materials between HDRP versions

Between different High Definition Render Pipeline (HDRP) versions materials might need an upgrade to work properly. This page describes how the process works and how you can remedy potential issues.

## Automatic Material Upgrade

To determine whether a material upgrade is needed or not, HDRP checks if the value *m_LastMaterialVersion* in the file ProjectSettings/HDRPProjectSettings.asset is the latest required.
If it is not,  all materials in the project will be reimported and saved to disk if they needed change. Note that if a version control system is used, then the materials will also be checked out before saving their content to disk.

When the upgrade is done and the materials are written to disk, the value of *m_LastMaterialVersion* is updated and written to HDRPProjectSettings.asset.

When a material saved with an older version of HDRP is imported, then this will also be upgraded automatically and written to disk when done.

Please note that if a version control system that requires check out operations is in use (e.g. Perforce), it is important that it is correctly setup in the project before the upgrade operation happens. If this is not the case, the material will be upgraded and the file marked as non read-only, but they will not be checked out by the VCS.

## Manual Material Upgrade

In case the above process fails and a material is not working as expected when upgrading HDRP version, it is suggested to run the upgrade process manually. To do so, you can either:

- Open the [Render Pipeline Wizard](Render-Pipeline-Wizard.md) and under the **Project Migration Quick-links** click on the Upgrade HDRP Materials to Latest Version button. Or:
- Select **Edit > Render Pipelines > HD Render Pipeline > Upgrade from Previous Version > Upgrade HDRP Materials to Latest Version**
