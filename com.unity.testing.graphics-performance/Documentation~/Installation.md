# Installing the Graphics performance testing package
The Graphics performance test package should exist inside the same repository as your test projects. Therefore, you should install this package in the Graphics repository. To make sure that your test projects can use this package and that the tests compile correctly, do the following:

1. Include the Graphics performance testing package in the Graphics repository. To do this, add the following line in the manifest of your test project in the TestProjects/ folder: "com.unity.testing.graphics-performance": "file:../../../com.unity.testing.graphics-performance"
2. Add the following line to the testable section of your project manifest: "testables": ["com.unity.testing.graphics-performance"]

<a name="setup"></a>
## Setup
To use the Graphics performance test package, you need to set up a test directory, the assembly definition, and the test assets. You also need to download extra packages so that the shader analysis can function properly.

For static shader analysis to work, you need to add all of the packages for each of the NDA platforms to your manifest. You can download them all from [Scriptable Render Pipeline extension packages](https://github.cds.internal.unity3d.com/unity/com.unity.render-pipelines.nda) on github. These packages are required for the Graphics performance test package to work.

<a name="create-a-test-directory"></a>
## Create a test directory
To set up a new test directory:

1. Create a new test directory. To do this, go to **Create > Testing > Test Assembly** Folder. This automatically creates a folder named “**Tests**” and configures an assembly definition (.asmdef) file in test mode. 
2. Create an Editor folder within the new Test Assembly folder to store your tests from the Editor. Configure this folder with Editor Only settings in the Inspector, otherwise the project does not compile.
3. Create a Runtime folder to store your tests from runtime. 
4. Arrange the folders with the following file hierarchy:
   1. Assets/
      - Performance Tests
        - Editor
        - Runtime
      - Resources
      - Scenes

The **Resources** folder is the default directory for test assets that Unity loads at runtime.

<a name="set-up-the-assembly-definition"></a>
## Set up the assembly definition
Your assembly definition file needs to include the references you use to write your tests. To add these references, include the following lines in the corresponding assembly definitions file.

Add the following lines to your runtime assembly definitions file: 

```   
"references": [
        "GUID:91836b14885b8a34196f4aa8303d7793",
        "GUID:27619889b8ba8c24980f49ee34dbb44a",
        "GUID:0acc523941302664db1f4e527237feb3",
        "GUID:df380645f10b7bc4b97d4f5eb6303d95",
        "GUID:295068ed467c2ce4a9cda3833065f650"
    ],
```

Add the following lines to your Editor assembly definitions file:

 ```
 "references": [
        "GUID:91836b14885b8a34196f4aa8303d7793",
        "GUID:df380645f10b7bc4b97d4f5eb6303d95",
        "GUID:295068ed467c2ce4a9cda3833065f650",
        "GUID:27619889b8ba8c24980f49ee34dbb44a",
        "GUID:cbbcbe5a7206638449ebcb9382eeb3a8",
        "GUID:78bd2ddd6e276394a9615c203e574844"
    ],
 ```

You can also add the assembly definition files from the **Assembly Definition References** list in [AssemblyDefinitionFile Import Settings](https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html). Add the following functions for the runtime assembly definitions:

- `Unity.PerformanceTesting`
- `Unity.GraphicTests.Performance.Runtime`
- `Unity.RenderPipelines.Core.Runtime`

For the Editor assembly definitions, include all of the above functions and the following:

- `Unity.GraphicTests.Performance.Editor`

<a name="set-up-the-test-assets"></a>

## Set up the test assets

The Graphics performance testing package comes with two test assets; Performance Test Description and Static Shader Analysis. To use these assets in your tests, you need to set them up in Unity: 

1. Go to **Assets > Create > Testing**, and create the **Performance Test Description** and **Static Shader Analysis** assets. Create both assets inside your Resources folder so that Unity can load them at runtime. To learn how to set up, configure and use these assets, see [Performance Test Description](Performance-Test-Description.md) and [Static Shader Analysis](static-shader-analysis.md).
2. To instruct Unity to use these assets, go to **Project Settings > Performance Tests**. In the **Test Description Asset** field, add the Performance Test Description asset. In the **Static Analysis Asset** field, add the Static Shader Analysis asset.
3. Create a simple scene with a camera to use during the tests. You need at least one scene asset and one Render Pipeline Asset for your first runtime test. 
4. Open the **Performance Test Description** asset and click the `+` in the Scenes window to reference your scene. This generates a list of tests in the **Test Runner** window.
5. Click the `+` in the **SRP Assets** window of the Performance Test Description asset to reference the Scriptable Render Pipeline (SRP) asset Unity uses to render the scene. 