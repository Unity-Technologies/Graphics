# Cases tested in this scene:

* One camera watching diffusion profiles (referenced inside the HDRP asset)
* One camera inside a volume with a diffusion profile volume override using a list of profile that are not inside the HDRP asset
* One object using layered lit with transmission referencing diffusion profile in the material UI
* One shader graph hair shader with transmission referencing diffusion profile in the shader graph material UI
* One shader graph fabric with transmission referencing a diffusion profile saved inside the shader graph diffusion profile slot
* One shader graph stacklit with transmission referencing a diffusion profile saved inside a shader graph diffusion profile node
* One shader graph lit with a checkerboard pattern to choose the diffusion profile referencing diffusion profile saved inside two nodes.
* One lit shader without diffusion profile but transmission enabled (it uses the default diffusion profile)