# Getting started

To use the Universal Render Pipeline (URP), you can start a new Project or upgrade an existing Project. You can do this in the following ways:

- [Create a new URP Project from a Template](creating-a-new-project-with-urp.md). If you are starting a new Project from scratch, this is the best choice. When you do this, Unity automatically installs and configures URP for you.
- [Install URP into an existing Unity Project](InstallURPIntoAProject.md). If you have started a Project using the Built-in Render Pipeline, you can install URP and configure your Project to use URP. When you do this, you must configure URP yourself. You will need to manually convert or recreate parts of your Project (such as lit shaders or post-processing effects) to be compatible with URP.

**Note:** URP's integrated post-processing solution does not currently support custom post-processing effects. If your Project uses custom post-processing effects, these cannot currently be recreated in URP's integrated post-processing solution. Custom post-processing effects will be supported in a forthcoming release of URP.

**Note:** Projects made using URP are not compatible with the High Definition Render Pipeline (HDRP) or the Built-in Render Pipeline. Before you start development, you must decide which render pipeline to use in your Project. For information on choosing a render pipeline, see [the Render Pipelines section of the Unity Manual](https://docs.unity3d.com/2019.3/Documentation/Manual/render-pipelines.html).
