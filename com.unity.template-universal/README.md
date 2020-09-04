## CI Status

master (2020.1): [![](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/build-badge.svg?branch=master)](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/build-info?branch=master)
[![](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/dependencies-badge.svg?branch=master)](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/dependencies-info?branch=master)

2019.3: [![](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/build-badge.svg?branch=2019.3)](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/build-info?branch=2019.3)

2019.2: [![](https://badges.cds.internal.unity3d.com/packages/com.unity.template.lightweight/build-badge.svg?branch=2019.2)](https://badges.cds.internal.unity3d.com/packages/com.unity.template.lightweight/build-info?branch=2019.2)

2019.1: [![](https://badges.cds.internal.unity3d.com/packages/com.unity.template.lightweight/build-badge.svg?branch=2019.1)](https://badges.cds.internal.unity3d.com/packages/com.unity.template.lightweight/build-info?branch=2019.1)

## Public Package Versions

[![ReleaseBadge](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/release-badge.svg)]()
[![ReleaseBadge](https://badges.cds.internal.unity3d.com/packages/com.unity.template.universal/candidates-badge.svg)]()


# About _Universal Project Template_

The Universal Project Template configures Project settings for Projects where performance and wide platform support are the primary considerations.

This Project Template uses the following features:

* Universal Render Pipeline (URP) - A prebuilt Scriptable Render Pipeline that is quick and easy to customize, and lets you create optimized graphics across a wide range of platforms. URP also includes an optimized 2D renderer complete with 2D lights and pixel perfect rendering, and an integrated post-processing solution. For more information, see the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest">URP documentation</a>.
* Shader Graph - This tool allows you to create shaders using a visual node editor instead of writing code. For more information on Shader Graph, see the <a href="https://docs.unity3d.com/Packages/com.unity.shadergraph@latest">Shader Graph documentation</a>.

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

The first step to get your package published to production for public consumption is to send it to the candidates repository, where it can be evaluated by QA and Release Management.  You can publish your template to the candidates repository through the added CI, which is the **recommended** approach.

1. Once you are ready to publish a new version, say version `1.0.0`, you can add a git tag `rc-1.0.0` to the commit you want to publish. The CI will validate and then publish your template to `candidates`.

1. Request that your template package be published to production by [filling out the following form](https://docs.google.com/forms/d/e/1FAIpQLSeEOeWszG7F5mx_VEYm8SrjcIajxa5WoLXh-yhLvw8odsEnaQ/viewform)

1. Once your template is published to production, the last step is to create the Ono PR to include your template with a Unity Release, and have it be discovered in the Hub. To do so, create a branch that includes your template in `External/PackageManager/Editor/editor_installer.json`

`Note`: You can retrieve a version of your template package as an artifact from CI pipelines following any commit made to your repository.  This will allow you to easily test a change at any point during your development.
