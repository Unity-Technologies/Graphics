# Problems that the Scriptable Render Pipeline solves

If a Render Pipeline is a number of steps that an engine performs to render onto the screen, a Scriptable Render Pipeline is a pipeline that you can control from Unity scripting code to render the way you want it to.

## The Problem
Traditionally, Unity provided a number of built-in pipelines that you can use. This includes the Forward renderer which is better for mobile and Virtual Reality, and the Deferred renderer which is better for more high-end applications. These out of the box rendering solutions are very general black boxes, that comes with the following downsides.

* They only do what they are designed to do.
* They are general, which means that, because they need to do everything, they are masters at nothing.
* They are not very configurable. They are black boxes that you can inject rendering commands to at pre-defined points.
* Extension and modification is prone to error because small internal changes can have large outward ramifications.
* Unity can not fix many of the bugs because this changes behaviour, which can break Projects.

## The Solution
The SRP Core API resolves the problems described above. It changes rendering from being an inbuilt black box to a controllable, per project, scriptable concept. You can use the SRP Core API to control how Unity renders to the screen, from low to high level. 
