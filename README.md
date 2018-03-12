=========================
Unity FBX Exporter (1.2.0)
-------------------------

GitHub: https://github.com/highfidelity/unity-to-hifi-exporter/

Contact: info@highfidelity.io

The Unity to High Fidelity JSON/FBX Exporter is adapted from the Unity FBX Exporter by Kellan Higgins(https://github.com/KellanHiggins/UnityFBXExporter).  

It is a simple JSON and FBX writer designed to export static objects (and render skinned meshes to static poses) from Unity into the FBX format, preserving the materials, game object hierarchy and textures attached.

It was written for High Fidelity - An early-stage technology lab experimenting with Virtual Worlds and VR.  https://github.com/highfidelity/hifi


Features
-------------------------

1. Can export any GameObject into FBX format.

2. Supports FBX format 7.3, around 2013.

3. Exports materials into the FBX file.

4. Exports texture references into the FBX file.

5. Exports will import into HiFi with one click of the menu


Known limitations
-------------------------

1. FBX format will only recognize diffuse maps and normal maps when exporting. Can not include height maps, for example.

2. Textures only support PBR Unity 5 shaders.

3. Only exports one UV map, not a AO UV 2 map.

4. Parents which skew their children will not import correctly. This is a different from how HiFi versus Unity handles scaling.

5. Things that are not supported include partical effects, terrains, scripts, and cameras.

Tutorial
------------------------
It is very simple to use this exporter. You shouldn't have any problems, and if you do, please add an issue to the Github project

1. Import the github project files into your asset folder or bring in the Unity package
1. Select any GameObject in the scene or export the scene whole.
2. Go to the GameObjects menu and select Hifi FBX Exporter
3. Choose the folder to export to
4. There will be a prompt asking about the base URL to place on the JSON file.  If you are testing locally, choose do not use and this will export with local file references.  If you know the remote directly you are uploading to, then copy that root url and paste it here.
5. If you are using a remote directory, upload into it the folders created for the models/materials/textures.
6. Drop the JSON into High Fidelity and you are good to go!


Export to Blender
------------------------

Blender 2.70 doesn't take ASCII FBX files. So you'll need to download the converter from the FBX site. Then convert it to a binary file and then import it into blender. Because the relative texture names are correct, blender will import your albedo and normal texture. Pretty neat!

Known Issues
------------------------

1. Skinned mesh renderers may or may not be exporting materials correctly. Did not work on my test object.
2. Standard objects throw an error if you try and export them (like the sphere). Still seems to export fine. Also why are you exporting Unity's sphere???

Crediting This Project
------------------------

As a note, this project is an MIT license. Which means you can take this code, upload it to the Unity Asset store and charge money for it. BUT, you must include the license (including the bit about Building Crafter) in your project. If you have any questions about this, hit me up.

If you compact it into a DLL and hide all the code away, you still have to include the license somewhere. I'd much rather you come to me then find out you've taken in 4 months later. Don't be a dick, give back to the community!
