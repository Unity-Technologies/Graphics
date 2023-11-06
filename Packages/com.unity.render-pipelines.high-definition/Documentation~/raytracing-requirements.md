## Ray tracing hardware requirements

Full ray tracing hardware acceleration is available on the following GPUs:

- NVIDIA GeForce 20 series:
  - RTX 2060
  - RTX 2060 Super
  - RTX 2070
  - RTX 2070 Super
  - RTX 2080
  - RTX 2080 Super
  - RTX 2080 Ti
  - NVIDIA TITAN RTX
- NVIDIA GeForce 30 series:
  - RTX 3060
  - RTX 3060Ti
  - RTX 3070
  - RTX 3080
  - RTX 3090
- NVIDIA Quadro:
  - RTX 3000 (laptop only)
  - RTX 4000
  - RTX 5000
  - RTX 6000
  - RTX 8000
- AMD RX series:
  - RX 6600
  - RX 6600 XT
  - RX 6700
  - RX 6700 XT
  - RX 6800
  - RX 6800 XT
  - RX 6900 XT
- AMD Radeon Pro series:
  - Pro W6600
  - Pro W6800

NVIDIA also provides a ray tracing fallback for some previous generation graphics cards:

- NVIDIA GeForce GTX
  - Turing generation: GTX 1660 Super, GTX 1660 Ti
  - Pascal generation: GTX 1060 6GB, GTX 1070, GTX 1080, GTX 1080 Ti
- NVIDIA TITAN V
- NVIDIA Quadro: P4000, P5000, P6000, V100

If your computer has one of these graphics cards, it can run ray tracing in Unity.

Before you open Unity, make sure to update your NVIDIA drivers to the latest version, and make sure your Windows version is at least 1809.

Ray tracing is also supported on specific console platforms. Consult console-specific documentation for more information.

You can use the Boolean [`SystemInfo.supportsRayTracing`](https://docs.unity3d.com/ScriptReference/SystemInfo-supportsRayTracing.html) to check if the current system supports ray tracing. This function checks the operating system, GPU, graphics driver and API.