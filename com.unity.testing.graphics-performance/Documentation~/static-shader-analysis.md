# Static Shader Analysis

## Overview

Static shader analysis is the analysis of the compiled shader byte code. This analysis is per gpu program (so per shader variant) and is specific per platform.

You will probably have to add additional packages to support the platform you intend to analyze for.

We use the Unity Test Runner to execute the static analysis, it will send the analysis measurements to the performance database.

_Important note: It is not a regression tests, we only use the test to execute the analysis and upload the measurements. A failure means that the analysis failed to execute_

## Defining static analysis

Analysis are defined in an `EditorShaderStaticAnalysisAsset`, it looks like this:

![Editor shader static analysis asset](Images/EditorShaderStaticAnalysisAsset.png)

To define an analysis, you will need to add an "Asset Definition".

### Asset Definition
![Asset definition](Images/AssetDefinition.png)

| **Property** | **Description** |
| --- | --- |
| **Asset Alias** | An alias for the asset. Use it to filter the reported metric. |
| **Asset Category** | The category for the asset. Use it to filter the reported metric. |
| **Test Name** | The name of the test. Use it to filter the reported metric. |
| **Filter** | A filter to use to select the shader passes and variant to analyse. (See below) |
| **Filter.Filter Type** | The type of the filter. Use `Reference` and fill the filter name in `Reference Name` to use the shared filter. Or use `Definition` and define the filter below. |
| **Include in Targets** | The analysis will be performed for the selected platforms. |

### Shared Filter

You can define shared filter so you can reuse it across multiple asset definitions.

![Filter](Images/GPUProgramFilter.png)

| **Property** | **Description** |
| --- | --- |
| **Name** | The name of the filter, use this name to reference a filter. |
| **Category** | The category of the filter. |
| **Keyword filter** | A keyword filter. For instance `A&B|A&C` will include all variants with keyword A and keyword B or C. |
| **Pass Name Filter** | A pass name filter. Use `+` to include, or `-` to exclude. For instance: `+GBuffer` will analyze only the GBuffer pass, or `-META,Shadow` will analyze all passes except META and Shadow. |

## Executing static analysis

Static analysis will appear in the test runner and you can execute them like a standard editor unit test.

_Note: The test only appears in the test runner for the supported platforms, you will require the additional platform package._