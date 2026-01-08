# About _High Definition Project Template_

This template is a good starting point for people focused on high-end graphics that want to develop games for platforms that support Shader Model 5.0 (DX11 and above).
This template utilizes the High Definition Render Pipeline, a modern rendering pipeline that includes advanced material types and a configurable hybrid tile/cluster deferred/forward lighting architecture.
This template also includes the new Shadergraph tool, Post-Processing stack, several Presets to jump start development, and example content.

This Project Template uses the following features:

* High Definition Render Pipeline - For more information, see the [HDRP documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html).
* Shader Graph tool - This tool allows you to create shaders using a visual node editor instead of writing code. For more information on the Shader Graph, see the [Shader Graph documentation](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html).

*Note:* The High Definition Render Pipeline is currently in development, so consider it incomplete and subject to change (API, UX, scope). As such, it is not covered by regular Unity support. Unity is seeking feedback on the feature. To ask questions about the feature, visit the <a href="https://forum.unity.com/forums/graphics-experimental-previews.110/?_ga=2.9250101.1048197026.1528313382-1647295365.1509665782">Unity preview forum</a>.

## Template CI
CI has been added to the project and it will test your template on every commit on `Yamato`.
This will validate that the template package as well as embedded packages (if any) have the right structure, have tests and do not create console logs when opened with Unity.
The CI will also automatically test the template as it would be used by a user on multiple editor versions and OS.
You might need to tweak the list of editors and OS you want to test the template on. For more information, please [go here](https://confluence.hq.unity3d.com/pages/viewpage.action?spaceKey=PAK&title=Setting+up+your+package+CI)

`Note`: To make use of the CI, your repository must be added to Yamato.
Log in to [Yamato](https://yamato.cds.internal.unity3d.com/) and click on the Project + button on the top right.  This will open a dialog asking for you to specify a git url and project name.

## Trying out your template locally.

If you want to test your template locally from a user's perspective, you will need to make it available to a Unity Editor. This can be accomplished by following these steps:

1. Use upm-ci tools to test your template

    You need to make sure you have `Node.js` and `npm` _(install it from [here](https://nodejs.org/en/))_ installed on your machine to package successfully, as the script calls `npm` under the hood for packaging and publishing. The script is tested with `node@v10.16.0` and `npm@5.6.0`.
    Install globally the upm-ci package:

    ```npm install upm-ci-utils -g --registry https://api.bintray.com/npm/unity/unity-npm```

    1. **To run all your template tests**
        1. Open a console (or terminal) window and cd your way inside your template project folder

            ```upm-ci template test -u 2018.3```

            You can test against many versions of Unity with the -u parameter:

            - Testing on a specific version: use `-u 2019.1.0a13`
            - Testing on a latest release of a version: use `-u 2019.1`
            - Testing on the latest available trunk build: use `-u trunk`
            - Testing on a specific branch: use `-u team-name/my-branch`
            - Testing on a specific revision: use `-u 3de2277bb0e6`
            - Testing with an editor installed on your machine: use `-u /absolute/path/to/the/folder/containing/Unity.app/or/Unity.exe`

            By default, this will download the desired version of the editor in a .Editor folder created in the current working directory.

    1. **To test what a user would see**
        1. Open a console (or terminal) window and cd your way inside your template project folder

            ```upm-ci template pack```
            This will generate a folder /upm-ci~/templates/ containing a .tgz file of your converted template.

        1. Include the tarballed template package in Unity editor

            You can then copy the template's `tgz` package file in Unity in one of these paths to make it available in the editor when creating new projects:

            1. Mac: `<Unity Editor Root>/Contents/Resources/PackageManager/ProjectTemplates`

            1. Windows: `<Unity Editor Root>/Data/Resources/PackageManager/ProjectTemplates`

        1. Preview your project template

            Open Unity Hub. Locate the editor to which you added your template to.
            When creating a new project, you should see your template in the templates list:

            ![Template in new project](Packages/com.unity.template.mytemplate/Documentation~/images/template_in_new_project.png)

            Note: f you are launching the Unity editor without the hub, you will not see additional templates in the list.

## Publishing your template for use in the Editor

Currently, in trunk, we maintain the HD Template version in the last 6.0 LTS. 
This is then published and promoted as a package used for ALL 6.X versions.

1. Change versions and changelog like in this [PR](https://github.com/Unity-Technologies/Graphics/pull/8202) 
2. If you upgraded the project to a new Unity version, make sure to update dependencies in package.json and make them match in manifest.json of the project itself like in this [PR](https://github.com/Unity-Technologies/Graphics/pull/8212).
3. Launch Yamato job in the trunk: "Publish HDRP Template trunk (DRY RUN)"
4. Download the artifact zip and unzip it to find com.unity.template.hd-YOURVERSION.tgz
5. Test it locally, by adding it to Data\Resources\PackageManager\ProjectTemplate of your current editor and restart Unity Hub.
6. If everything goes well, you can launch the real job : "Publish HDRP Template trunk" (Warning : you can only do it once for each version) 
7. Create a workingset [PR](https://github.cds.internal.unity3d.com/unity/com.unity.working-set.templates/pull/444):  targeting one of the release/templates branch
8. If the tests do not pass, make sure you updated the pvp-exemptions file like [this](https://github.cds.internal.unity3d.com/unity/com.unity.working-set.templates/blob/7b35200cd55f2c1bc48930b2075b090459248f82/pvp-exemptions.json#L870)
9. When all the tests pass, add @unity/package-release-managers as reviewer
10. When the PR lands, the new template should be available in all the versions more or less instantly. 

More complete docs [here](https://internaldocs.unity.com/package_development/template-starter-kit/release-template/#4-create-a-workingset-pr)