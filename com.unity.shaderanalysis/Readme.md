# Features
## Generate a performance report

1. Select the target platform in the Tool
1. Select a shader / compute shader / material in the project view
1. Click "Build Report"

## Export to Excel / CSV

1. Once a report is built
1. Click on "=> Excel" or "=> CSV" to generate a Excel/CSV report

Unity will open the generated report

## Open the intermediate files

> Intermediate files are generated during the build report (compiled shader, raw performance reports, ...) in the Temp folder of the current Unity project.

> These files are deleted when the Unity project is closed.

1. Once a report is built
1. Click on "Open Temp Dir"

## Diff to a reference report

1. Make sur your reference folder is set (or pick one)
1. Generate a performance report for your asset
1. Click on "Diff with ref"
    1. The diff is only available if a report for the same asset GUID exist in the reference folder

Unity will perform the diff and open it in Excel

## Set a report as reference

1. Once a report is built and the reference folder is set
1. Click on "Set as reference"

The report will be written in the reference folder with the current asset's guid

## Open the source code for a pass/kernel

1. Once a report is built
1. Click on the "Open" button for the desired pass/kernel

## Save the reports in a SCM

The reports are stored in $UNITY_PROJECT/Library/ShaderPerformanceReports.

There will be a file per platform (like: AssetMetaData_PS4.asset for PS4).

This is the file to save in a SCM.

# Additional notes
## Compilation units

For a shader, a report will be generated for each multicompile for each pass.

For a compute shader, a report will be generated per kernel

For a material, a report will be generated for each multicompile for each pass. It wil take into account shader features, actived or disabled passes as well.