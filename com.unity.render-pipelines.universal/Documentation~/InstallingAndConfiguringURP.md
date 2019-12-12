# Getting started

To use the Universal Render Pipeline (URP), you can start a new Project or upgrade an existing Project. You can do this in the following ways:

- [Create a new URP Project from a Template](creating-a-new-project-with-urp.md). If you are starting a new Project from scratch, this is the best choice. When you do this, Unity automatically installs and configures URP for you.
- [Upgrade a Project that uses the Built-in Render Pipeline](InstallURPIntoAProject.md). If your Project uses the Built-in Render Pipeline, you can upgrade it to use URP. When you do this, you must configure URP yourself. You may also need to manually convert or recreate parts of your Project (such as lit shaders or post-processing effects) to be compatbible with URP.
- [Upgrade a Project that uses the Lightweight Render Pipeline (LWRP)](https://docs.google.com/document/d/1Xd5bZa8pYZRHri-EnNkyhwrWEzSa15vtnpcg--xUCIs). If your Project uses LWRP, you can upgrade it to use URP. When you do this, Unity automatically upgrades most of your Project settings, but you need to perform some manual steps.

**Note:** URP does not currently support custom post-processing effects. If your Project uses custom post-processing effects, these cannot currently be recreated in URP. Custom post-processing effects will be supported in a forthcoming release of URP.

**Note:** Projects made using URP are not compatible with the High Definition Render Pipeline (HDRP) or the Built-in Render Pipeline. Before you start development, you must decide which render pipeline to use in your Project. For information on choosing a render pipeline, see [the Render Pipelines section of the Unity Manual](https://docs.unity3d.com/2019.3/Documentation/Manual/render-pipelines.html).