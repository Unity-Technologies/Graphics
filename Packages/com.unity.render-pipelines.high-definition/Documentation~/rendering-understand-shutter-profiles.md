## Understand shutter profiles

In the [multiframe rendering API](rendering-multiframe-recording-api.md), the `BeginRecording` call allows you to specify how fast the camera shutter opens and closes. The speed of the camera shutter defines the so called “shutter profile”. The following image demonstrates how different shutter profiles affect the appearance of motion blur on a blue sphere moving from left to right.

![Different shutter profiles and their impact on image blur caused by motion or exposure changes. A Uniform Shutter opens fully for an even duration, creating sharp-edged, uniform blur. A Slow Open Shutter gradually opens and briefly remains open, generating a blur that fades progressively on one side. A Linear Open and Close Shutter moves at a consistent rate, producing tapered edges with a linear gradient effect. Finally, a Smooth Open and Close Shutter opens slowly, reaches full exposure, and closes smoothly, resulting in a blur with soft transitions on both sides. Each profile shapes how light reaches the sensor, directly influencing motion blur and gradient effects in the image.](Images/shutter_profiles.png)

In all cases, the speed of the sphere is the same. The only change is the shutter profile. The horizontal axis of the profile diagram corresponds to time, and the vertical axis corresponds to the openning of the shutter.

You can easily define the first three profiles without using an animation curve by setting the open, close parameters to (0,1), (1,1), and (0.25, 0.75) respectively. The last profile requires the use of an animation curve.

In this example, you can see that the slow open profile creates a motion trail appearance for the motion blur, which might be more desired for artists. Although, the smooth open and close profile creates smoother animations than the slow open or uniform profiles.
