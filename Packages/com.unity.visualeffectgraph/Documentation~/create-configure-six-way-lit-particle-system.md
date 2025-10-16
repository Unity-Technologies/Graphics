# Create a six-way lit particle system in Visual Effect Graph 

Set up a particle system in Visual Effect Graph that uses six-way lighting to achieve enhanced realism for smoke or explosion effects.

To create and configure a six-way lit particle system, follow these steps:

1. Open a **Visual Effect Graph** or create a new one.

1. Set the system to burst a single particle, and allocate memory for one particle.

1. Right-click the **Output Particle** context, then convert the output to a lit quad.

1. Set **Material Type** to **Six-Way Smoke Lit** in the Inspector window.

1. Drag and drop the two six-way lightmap textures from your `Assets` folder to the **Output Particle** context positive and negative axes lightmap slots, respectively.

1. Set the **Output Particle** context **UV Mode** to **Flipbook**.

1. Set the **Output Particle** context **Flip Book size** to match the number of frames.

1. Configure the emissive settings in the Inspector window:
    - Set **Emissive Mode** to **Single Channel**. This mode uses the alpha channel of the second texture.
    - Adjust **Exposure Weight** and **Emissive Multiplier** as needed.

1. Adjust six-way lighting settings as needed to achieve the desired effect.

