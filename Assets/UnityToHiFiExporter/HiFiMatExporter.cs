// ===============================================================================================
//	The MIT License (MIT) for UnityToHiFiExporter
//
//  UnityFBXExporter was created for Building Crafter (http://u3d.as/ovC) a tool to rapidly 
//	create high quality buildings right in Unity with no need to use 3D modeling programs.
//
//  Copyright (c) 2016 | 8Bit Goose Games, Inc.
//
//	UnityToHiFiExporter expanded upon the original code to export Unity scenes into 
//	High Fidelity (https://highfidelity.com/) an early-stage technology lab experimenting 
//	with Virtual Worlds and VR. Certain functions have been rewritten specificly to export 
//	into HiFi, so if you want a generalized Unity FBX exporter, please take a look at the 
//	original repo.
//
//	Copyright (c) 2018 | High Fidelity, Inc.
//		
//	Permission is hereby granted, free of charge, to any person obtaining a copy 
//	of this software and associated documentation files (the "Software"), to deal 
//	in the Software without restriction, including without limitation the rights 
//	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
//	of the Software, and to permit persons to whom the Software is furnished to do so, 
//	subject to the following conditions:
//		
//	The above copyright notice and this permission notice shall be included in all 
//	copies or substantial portions of the Software.
//		
//	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//	PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//	HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//	OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
//	OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ===============================================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;

namespace HiFiExporter 
{
	public class HiFiMatExporter
	{

		/// <summary>
		/// DO NOT USE: Working on, where every texture is grabbed from the object
		/// </summary>
		public static void ExportTextureFromAMesh(MeshRenderer meshRenderer, string materialFolder, string fullTextureFolder)
		{
			// 1. Need to copy the texture to the new path


			// Go through all materials in the mesh renderer and export them as some materials

			int length = meshRenderer.sharedMaterials.Length;

			for(int materialIndex = 0; materialIndex < length; materialIndex++)
			{
				Material currentMaterial = meshRenderer.sharedMaterials[materialIndex];
				Shader shader = currentMaterial.shader;

				int propertyCount = ShaderUtil.GetPropertyCount(shader);

				// Copies all the textures to the file
				for(int i = 0; i < propertyCount; i++)
				{
					ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, i);
					if(propType == ShaderUtil.ShaderPropertyType.TexEnv)
					{
						Texture texture = currentMaterial.GetTexture(ShaderUtil.GetPropertyName(shader, i));

						if(texture != null)
						{
							string texturePath = UtiltiesHiFi.GetAssetPathFolder() + AssetDatabase.GetAssetOrScenePath(texture);
							string newTextureName = UtiltiesHiFi.GetFileName(texturePath);
							string newTexturePathAndName = fullTextureFolder + newTextureName;
							File.Copy(texturePath, newTexturePathAndName);
						}
					}
				}
			}
		}

		/// <summary>
		/// Grabs the normal / bump map and exports it for the gameObject in question
		/// </summary>
		public static void GetAllTopMaterialsToString(GameObject gameObj, string newPath, bool copyTextures, out Material[] materials, out string matObjects, out string connections)
		{
			StringBuilder tempObjectSb = new StringBuilder();
			StringBuilder tempConnectionsSb = new StringBuilder();

			Renderer meshRenderer = gameObj.GetComponent<Renderer>();

			if(meshRenderer == null)
			{
				matObjects = tempObjectSb.ToString();
				connections = tempConnectionsSb.ToString();
				materials = new Material[0];
				return;
			}

			List<Material> uniqueMaterials = new List<Material>();

			// Gets all the unique materials within this GameObject Hierarchy
			for(int n = 0; n < meshRenderer.sharedMaterials.Length; n++)
			{
				Material mat = meshRenderer.sharedMaterials[n];

				if(uniqueMaterials.Contains(mat) == false && mat != null)
					uniqueMaterials.Add(mat);
			}

			for (int i = 0; i < uniqueMaterials.Count; i++)
			{
				Material mat = uniqueMaterials[i];

				// We rename the material if it is being copied
				string materialName = mat.name;

				int referenceId = Mathf.Abs(mat.GetInstanceID());

				tempObjectSb.AppendLine();
				tempObjectSb.AppendLine("\tMaterial: " + referenceId + ", \"Material::" + materialName + "\", \"\" {");
				tempObjectSb.AppendLine("\t\tVersion: 102");
				tempObjectSb.AppendLine("\t\tShadingModel: \"phong\"");
				tempObjectSb.AppendLine("\t\tMultiLayer: 0");
				tempObjectSb.AppendLine("\t\tProperties70:  {");
				tempObjectSb.AppendFormat("\t\t\tP: \"Diffuse\", \"Vector3D\", \"Vector\", \"\",{0},{1},{2}", mat.color.r, mat.color.g, mat.color.b);
				tempObjectSb.AppendLine();
				tempObjectSb.AppendFormat("\t\t\tP: \"DiffuseColor\", \"Color\", \"\", \"A\",{0},{1},{2}", mat.color.r, mat.color.g, mat.color.b);
				tempObjectSb.AppendLine();

//				TODO: Figure out if this property can be written to the FBX file
//				if(mat.HasProperty("_MetallicGlossMap"))
//				{
//					Debug.Log("has metallic gloss map");
//					Color color = mat.GetColor("_Color");
//					tempObjectSb.AppendFormat("\t\t\tP: \"Specular\", \"Vector3D\", \"Vector\", \"\",{0},{1},{2}", color.r, color.g, color.r);
//					tempObjectSb.AppendLine();
//					tempObjectSb.AppendFormat("\t\t\tP: \"SpecularColor\", \"ColorRGB\", \"Color\", \" \",{0},{1},{2}", color.r, color.g, color.b);
//					tempObjectSb.AppendLine();
//				}

				if(mat.HasProperty("_SpecColor"))
				{
					Color color = mat.GetColor("_SpecColor");
					tempObjectSb.AppendFormat("\t\t\tP: \"Specular\", \"Vector3D\", \"Vector\", \"\",{0},{1},{2}", color.r, color.g, color.r);
					tempObjectSb.AppendLine();
					tempObjectSb.AppendFormat("\t\t\tP: \"SpecularColor\", \"ColorRGB\", \"Color\", \" \",{0},{1},{2}", color.r, color.g, color.b);
					tempObjectSb.AppendLine();
				}

				if(mat.HasProperty("_Mode"))
				{
					Color color = Color.white;

					switch((int)mat.GetFloat("_Mode"))
					{
					case 0: // Map is opaque
						tempObjectSb.AppendFormat("\t\t\tP: \"TransparentColor\", \"Color\", \"\", \"A\",{0},{1},{2}", color.r, color.g, color.b);
						tempObjectSb.AppendLine();
						tempObjectSb.AppendFormat("\t\t\tP: \"Opacity\", \"double\", \"Number\", \"\",{0}", 1);
						tempObjectSb.AppendLine();
						break;

					case 1: // Map is a cutout
						//  TODO: Add option if it is a cutout
						break;

					case 2: // Map is a fade
						color = mat.GetColor("_Color");

						tempObjectSb.AppendFormat("\t\t\tP: \"TransparentColor\", \"Color\", \"\", \"A\",{0},{1},{2}", color.r, color.g, color.b);
						tempObjectSb.AppendLine();
						tempObjectSb.AppendFormat("\t\t\tP: \"Opacity\", \"double\", \"Number\", \"\",{0}", color.a);
						tempObjectSb.AppendLine();
						break;

					case 3: // Map is transparent
						color = mat.GetColor("_Color");

						tempObjectSb.AppendFormat("\t\t\tP: \"TransparentColor\", \"Color\", \"\", \"A\",{0},{1},{2}", color.r, color.g, color.b);
						tempObjectSb.AppendLine();
						tempObjectSb.AppendFormat("\t\t\tP: \"Opacity\", \"double\", \"Number\", \"\",{0}", color.a);
						tempObjectSb.AppendLine();
						break;
					}
				}

				// NOTE: Unity doesn't currently import this information (I think) from an FBX file.
				if(mat.HasProperty("_EmissionColor"))
				{
					Color color = mat.GetColor("_EmissionColor");

					tempObjectSb.AppendFormat("\t\t\tP: \"Emissive\", \"Vector3D\", \"Vector\", \"\",{0},{1},{2}", color.r, color.g, color.b);
					tempObjectSb.AppendLine();

					float averageColor = (color.r + color.g + color.b) / 3f;

					tempObjectSb.AppendFormat("\t\t\tP: \"EmissiveFactor\", \"Number\", \"\", \"A\",{0}", averageColor);
					tempObjectSb.AppendLine();
				}

				// TODO: Add these to the file based on their relation to the PBR files
//				tempObjectSb.AppendLine("\t\t\tP: \"AmbientColor\", \"Color\", \"\", \"A\",0,0,0");
//				tempObjectSb.AppendLine("\t\t\tP: \"ShininessExponent\", \"Number\", \"\", \"A\",6.31179285049438");
//				tempObjectSb.AppendLine("\t\t\tP: \"Ambient\", \"Vector3D\", \"Vector\", \"\",0,0,0");
//				tempObjectSb.AppendLine("\t\t\tP: \"Shininess\", \"double\", \"Number\", \"\",6.31179285049438");
//				tempObjectSb.AppendLine("\t\t\tP: \"Reflectivity\", \"double\", \"Number\", \"\",0");

				tempObjectSb.AppendLine("\t\t}");
				tempObjectSb.AppendLine("\t}");

				string textureObjects;
				string textureConnections;

				SerializedTextures(gameObj, newPath, mat, materialName, copyTextures, out textureObjects, out textureConnections);

				tempObjectSb.Append(textureObjects);
				tempConnectionsSb.Append(textureConnections);
			}

			materials = uniqueMaterials.ToArray<Material>();

			matObjects = tempObjectSb.ToString();
			connections = tempConnectionsSb.ToString();
		}

		/// <summary>
		/// Serializes textures to FBX format.
		/// </summary>
		/// <param name="gameObj">Parent GameObject being exported.</param>
		/// <param name="newPath">The path to export to.</param>
		/// <param name="materials">Materials that holds all the textures.</param>
		/// <param name="matObjects">The string with the newly serialized texture file.</param>
		/// <param name="connections">The string to connect this to the  material.</param>
		private static void SerializedTextures(GameObject gameObj, string newPath, Material material, string materialName, bool copyTextures, out string objects, out string connections)
		{
			// TODO: FBX import currently only supports Diffuse Color and Normal Map
			// Because it is undocumented, there is no way to easily find out what other textures
			// can be attached to an FBX file so it is imported into the PBR shaders at the same time.
			// Also NOTE, Unity 5.1.2 will import FBX files with legacy shaders. This is fix done
			// in at least 5.3.4.

			StringBuilder objectsSb = new StringBuilder();
			StringBuilder connectionsSb = new StringBuilder();

			int materialId = Mathf.Abs(material.GetInstanceID());

			Texture mainTexture = material.GetTexture("_MainTex");

			string newObjects = null;
			string newConnections = null;

			// Serializeds the Main Texture, one of two textures that can be stored in FBX's sysytem
			if(mainTexture != null)
			{
				SerializeOneTexture(gameObj, newPath, material, materialName, materialId, copyTextures, "_MainTex", "DiffuseColor", out newObjects, out newConnections);
				objectsSb.AppendLine(newObjects);
				connectionsSb.AppendLine(newConnections);
			}

			if(SerializeOneTexture(gameObj, newPath, material, materialName, materialId, copyTextures, "_BumpMap", "NormalMap", out newObjects, out newConnections))
			{
				objectsSb.AppendLine(newObjects);
				connectionsSb.AppendLine(newConnections);
			}

			connections = connectionsSb.ToString();
			objects = objectsSb.ToString();
		}

		private static bool SerializeOneTexture(
			GameObject gameObj, 
			string newPath, 
			Material material, 
			string materialName,
			int materialId,
			bool copyTextures, 
			string unityExtension, 
			string textureType, 
			out string objects, 
			out string connections)
		{
			string texturesFolderName = @"/Textures/";
			string texturesFolderNameNoFrontSlash = @"Textures/";
			texturesFolderName = "";
			texturesFolderNameNoFrontSlash = "";

			StringBuilder objectsSb = new StringBuilder();
			StringBuilder connectionsSb = new StringBuilder();

			Texture texture = material.GetTexture(unityExtension);

			if(texture == null)
			{
				objects = "";
				connections = "";
				return false;
			}

			string originalAssetPath = AssetDatabase.GetAssetPath(texture);
			string fullDataFolderPath = Application.dataPath;
			string textureFilePathFullName = originalAssetPath;
			string textureName = Path.GetFileNameWithoutExtension(originalAssetPath);
			string textureExtension = Path.GetExtension(originalAssetPath);

			// TODO - We have to change this to relative to the file being exported
//			if(copyTextures)
			{
				textureFilePathFullName = UtiltiesHiFi.GetFolderFromPath(newPath) + texturesFolderNameNoFrontSlash + textureName + unityExtension + textureExtension;
			}


			// copy the textures to the new folder went want to copy it to
			string fullOriginalPath = UtiltiesHiFi.GetAssetPathFolder() + originalAssetPath;

			if(Directory.Exists(UtiltiesHiFi.GetFolderFromPath(newPath) + texturesFolderName) == false)
				Directory.CreateDirectory(UtiltiesHiFi.GetFolderFromPath(newPath) + texturesFolderName);

			// TODO - prevent overwrites of different files
			File.Copy(fullOriginalPath, textureFilePathFullName, true);

			long textureReference = HighFidelityFBXExporter.GetRandomFBXId();

			// TODO - test out different reference names to get one that doesn't load a _MainTex when importing.

			objectsSb.AppendLine("\tTexture: " + textureReference + ", \"Texture::" + materialName + unityExtension + "\", \"\" {");
			objectsSb.AppendLine("\t\tType: \"TextureVideoClip\"");
			objectsSb.AppendLine("\t\tVersion: 202");
			objectsSb.AppendLine("\t\tTextureName: \"Texture::" + materialName + unityExtension + "\"");
			objectsSb.AppendLine("\t\tProperties70:  {");
			objectsSb.AppendLine("\t\t\tP: \"CurrentTextureBlendMode\", \"enum\", \"\", \"\",0");
			objectsSb.AppendLine("\t\t\tP: \"UVSet\", \"KString\", \"\", \"\", \"map1\"");
			objectsSb.AppendLine("\t\t\tP: \"UseMaterial\", \"bool\", \"\", \"\",1");
			objectsSb.AppendLine("\t\t}");
			objectsSb.AppendLine("\t\tMedia: \"Video::" + materialName + unityExtension + "\"");

			// Sets the absolute path for the copied texture
			objectsSb.Append("\t\tFileName: \"");
			objectsSb.Append(textureFilePathFullName);
			objectsSb.AppendLine("\"");

			objectsSb.AppendLine("\t\tRelativeFilename: \"" + texturesFolderNameNoFrontSlash + textureName + unityExtension + textureExtension + "\"");
			
			objectsSb.AppendLine("\t\tModelUVTranslation: 0,0"); // TODO: Figure out how to get the UV translation into here
			objectsSb.AppendLine("\t\tModelUVScaling: 1,1"); // TODO: Figure out how to get the UV scaling into here
			objectsSb.AppendLine("\t\tTexture_Alpha_Source: \"None\""); // TODO: Add alpha source here if the file is a cutout.
			objectsSb.AppendLine("\t\tCropping: 0,0,0,0");
			objectsSb.AppendLine("\t}");

			connectionsSb.AppendLine("\t;Texture::" + textureName + ", Material::" + materialName + "\"");
			connectionsSb.AppendLine("\tC: \"OP\"," + textureReference + "," + materialId + ", \"" + textureType + "\""); 

			connectionsSb.AppendLine();

			objects = objectsSb.ToString();
			connections = connectionsSb.ToString();

			return true;
		}
	}
}