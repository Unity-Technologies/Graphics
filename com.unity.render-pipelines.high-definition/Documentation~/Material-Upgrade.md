# Upgrading Materials between HDRP versions

Between different versions of the High Definition Render Pipeline (HDRP), you might need to upgrade materials for them to work. This page describes how to upgrade materials and how to fix issues.

## Automatic Material Upgrade

To determine whether you need to upgrade a material, HDRP checks if the value `m_LastMaterialVersion` in the file ProjectSettings/HDRPProjectSettings.asset is the latest required.
If it's not, HDRP reimports all materials in the project and saves them to disk if they changed change.

**Note**: If you use a version control system, the materials will also be checked out before saving their content to disk. If your version control system requires checkout operations (for example, Perforce), you must set it up correctly in your project before upgrading your materials. Otherwise, the material will be upgraded and the file marked as non read-only, but they won't be checked out by the VCS.

When the upgrade has finished and the materials has been saved to disk, HDRP updates the value of `m_LastMaterialVersion` and writes the new value to HDRPProjectSettings.asset.

When you import a material saved with an older version of HDRP, this material will also be upgraded automatically and written to disk.

## Manual Material Upgrade

In case the automatic material upgrade fails and a material isn't working as expected when upgrading an HDRP version, run the upgrade process manually. To do so, you can either:

- Use the [Render Pipeline Wizard](Render-Pipeline-Wizard.md)
    1. Open the Render Pipeline Wizard.
    2. Go to **Project Migration Quick-links**
    3. Click **Upgrade HDRP Materials to Latest Version**.
- Select **Edit** > **Render Pipelines** > **HD Render Pipeline** > **Upgrade from Previous Version** > **Upgrade HDRP Materials to Latest Version**.
