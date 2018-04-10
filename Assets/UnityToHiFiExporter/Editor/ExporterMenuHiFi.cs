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
using System.Text;
using System.IO;
using HiFiExporter;
using System.Linq;


namespace HiFiExporter 
{
	/// <summary>
	/// Used to tell what type of object is being exported
	/// Curently only Prcoedural objects are being exported
	/// </summary>
	enum ExportType
	{
		Procedural,
		SkinnedMesh,
		PlaneOrQuad,
		Fbx, // NOTE: Currently not used
		Obj // NOTE: Currently not used
	}

    public class ExporterMenuHiFi : Editor 
	{
        // Global Variables
		/// <summary>Sets up a list of GUIDs for all the gameObjects to be exported, for parenting and id tagging reasons</summary>
		public static Dictionary<GameObject, string> GUIDReference = new Dictionary<GameObject, string>();
		/// <summary>The path to export the json file to</summary>
		private static string lastJsonPath = "";
		private static string lastFbxFileName = "";

        // Dropdown
		[MenuItem("GameObject/Export Scene or Selected Objs to HiFi %#e", false, 40)]
        public static void ExportDropdownGameObjectToFBX() 
		{
			EditorUtility.ClearProgressBar(); // Ensures that any progress bar has been cleared before starting

			if(Application.isPlaying)
			{
				Debug.LogError("Can't export while in play mode. If you would like this, please visit High Fidelity's github repo / forums and leave a comment.");
				return;
			}

			if(lastJsonPath == "")
				lastJsonPath = Application.dataPath;
			else
				lastJsonPath = FindRootPath(lastJsonPath);

			if(lastFbxFileName == "")
				lastFbxFileName = "UnityHiFiExport.json";
			
			string newJsonPath = EditorUtility.SaveFilePanel("Save as JSON filename", lastJsonPath, lastFbxFileName, "json");

			if(newJsonPath == "")
				return;

			lastJsonPath = newJsonPath;

			lastFbxFileName = GetFileName(lastJsonPath);

			// Note, we have to pause before we execute the export current game object
			// So we call on this separate window to help choose where the URL should go.
			// Once that is not needed, delete this, and execute the code commented out below
			URLPopupWindow.Init();

//          ExportCurrentGameObject(false, false);
        }


		#region Main Exporter Methods

		/// <summary>
		/// Exports the selected (or all) game objects to the json path which is saved as a static path
		/// </summary>
		public static void ExportCurrentGameObjects()
		{
			EditorUtility.DisplayProgressBar("Exporting FBX files", "Progress", 0);

			// Destroy all the FBX files at the path so we don't constantly save over everything
			CleanUpAnyOldFiles(lastJsonPath);

			GUIDReference.Clear();

			string fileName = lastJsonPath.Remove(0, lastJsonPath.LastIndexOf('/') + 1);
			string rootfolderPath = lastJsonPath.Remove(lastJsonPath.LastIndexOf('/'), lastJsonPath.Length - lastJsonPath.LastIndexOf('/')) + "/";

			List<GameObject> gameObjectsToExport = new List<GameObject>();
			List<GameObject> gameObjectsSelected = new List<GameObject>();

			// Find which objects are selected
			if(Selection.activeGameObject != null)
			{
				// Now we select the object and all its children
				GameObject[] selected = Selection.gameObjects;

				for(int selectedIndex = 0; selectedIndex < selected.Length; selectedIndex++)
				{
					Transform[] childTransforms = selected[selectedIndex].GetComponentsInChildren<Transform>();
					for(int i = 0; i < childTransforms.Length; i++)
					{
						if(gameObjectsSelected.Contains(childTransforms[i].gameObject) == false)
							gameObjectsSelected.Add(childTransforms[i].gameObject);
					}
						
				}
			} // If there are no objects selected, export the entire scene
			else
			{
				GameObject[] gameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
				gameObjectsSelected.AddRange(gameObjects);
			}

			for (int i = 0; i < gameObjectsSelected.Count; i++) 
			{
				// skip if gameobject is a camera
				// ALL THE EXCEPTIONS 
				if (gameObjectsSelected[i].GetComponent<Camera>()
					|| gameObjectsSelected[i].transform.root.GetComponent<Canvas>()// HACK to make sure we don't export the huge canvas
				)
					continue;
				else if(gameObjectsSelected[i].GetComponent<Light> ()) // TODO - reintroduce exporting lights
					gameObjectsToExport.Add(gameObjectsSelected[i]);
				else if (gameObjectsSelected[i].activeInHierarchy)
					gameObjectsToExport.Add(gameObjectsSelected[i]);
			}

			// Create guids for all unique game objects
			// These help HF take in all the information
			for(int i = 0; i < gameObjectsToExport.Count; i++)
			{
				if(GUIDReference.ContainsKey(gameObjectsToExport[i]) == false)
					GUIDReference.Add(gameObjectsToExport[i], CreateNewGUID());
				else
					Debug.LogError("Already contains a gameObject");
			}

			if(gameObjectsToExport.Count < 1)
			{
				Debug.LogError("No gameObjects selected, did not export anything");
				return;
			}
				
			// Use a ref to update the jsonObject so it is ready for export when it gets in
			ExportGameObjectAsJsonToHF(gameObjectsToExport, rootfolderPath, fileName);
		}


		public static void ExportGameObjectAsJsonToHF(List<GameObject> gameObjectsToExport, string rootPath, string fileName) 
		{
			// Used to prevent duplicate meshes being created
			Dictionary<string, string> meshReference = new Dictionary<string, string>();

			// Set up the directory for the fbx files
			string fbxExportPath = rootPath + @"FBXObjects/";
			if(Directory.Exists(fbxExportPath) == false)
				Directory.CreateDirectory(fbxExportPath);

			// We go through all the parent gameObjects and find the center of the bounding box. We will then offset
			// by this amount so when you import something into HiFi it doesn't get placed very far away

			Bounds boundsForAllObjects = new Bounds();
			bool boundsInit = false;
			for(int i = 0; i < gameObjectsToExport.Count; i++)
			{


				Transform trans = gameObjectsToExport[i].transform;

				if(trans.parent != null && GUIDReference.ContainsKey(trans.parent.gameObject) == true)
					continue;

				// HACK - skip the directional light and area lights since we skip them below
				// When area / directional lights are added, remove the 3 lines.
				Light light = trans.gameObject.GetComponent<Light>();
				if(light != null && (light.type == LightType.Directional || light.type == LightType.Area))
					continue;
				// END HACK

				// NOTE: If you use "continue" in the loop to NOT write out any objects to the json component, we have to add an exception here.

				// We no
				if(boundsInit == false)
				{
					boundsForAllObjects = new Bounds(trans.position, Vector3.zero);
					boundsInit = true;
				}
				else
					boundsForAllObjects.Encapsulate(trans.position);
			}

			StringBuilder jsonOutput = new StringBuilder("{\"Entities\":[");
			for (int gameObjectIndex = 0; gameObjectIndex < gameObjectsToExport.Count; gameObjectIndex++)
			{
				EditorUtility.DisplayProgressBar("Exporting FBX files", "Progress", (float)gameObjectIndex / (float)gameObjectsToExport.Count);

				// First we make sure to export the rotation, position and scale correctly for all the objects
				GameObject gameObj = gameObjectsToExport[gameObjectIndex];
				HiFiJsonObject jsonObject = new HiFiJsonObject();

				// Set up ids and parent ids if applicable
				jsonObject.parentID = FindProperParentGUID(gameObj, GUIDReference);
				jsonObject.id = GUIDReference[gameObj];
				bool hasParent = jsonObject.parentID != GetBlankID();

				// Calculate position, rotation, scale as if the objects are empty objects
				// In the case of meshes, this will be overwritten later

				// Registration of all objects differ from their Unity parts, they always rotate in the middle
				jsonObject.registrationPoint.x = .5f;
				jsonObject.registrationPoint.y = .5f;
				jsonObject.registrationPoint.z = .5f;

				// Set small dimensions in case this gameObject is just a holder
				jsonObject.dimensions = Vector3.one * .1f;
				jsonObject.collisionless = true;

				Light light = gameObjectsToExport[gameObjectIndex].GetComponent<Light>();

				// Offsets the position compared to the center of the mesh versus the FBX rotational point
				if(gameObj.transform.parent == null || hasParent == false)
				{
					jsonObject.position = gameObjectsToExport[gameObjectIndex].transform.position;
					jsonObject.position.x *= -1;

					jsonObject.rotation = ConvertToHFQuaternion(gameObj.transform.rotation);

					// Lights need to rotate 180 to be positioned right
					if(light != null)
					{
						Quaternion worldRotation = gameObj.transform.rotation * Quaternion.Euler(0, 180f, 0);
						jsonObject.rotation = ConvertToHFQuaternion(worldRotation);
					}
				}
				else
				{
					jsonObject.position = Vector3.zero;

					Transform parentTransform = gameObj.transform.parent;
					Transform childTransform = gameObj.transform;

					Vector3 totalOffset = Vector3.zero;

					// Find the center position in world for the parent and child
					Vector3 parentOffset = parentTransform.position;
					Vector3 childOffset = childTransform.position;

					Vector3 direction = (childOffset - parentOffset);
					Vector3 changeTheDirectionToParent = parentTransform.InverseTransformVector(direction);

					totalOffset = changeTheDirectionToParent;

					totalOffset.x *= -1;
					jsonObject.position += totalOffset;

					jsonObject.rotation = ConvertToHFQuaternion(gameObj.transform.localRotation);

					if(light != null)
					{
						Quaternion localRotation = gameObj.transform.localRotation * Quaternion.Euler(0, 180f, 0);;
						jsonObject.rotation = ConvertToHFQuaternion(localRotation);
					}
				}

				// now onto specific types of objects

				// There are 3 types supported, meshes, lights and zones
				// meshes are for 3D fbx objects exported
				// lights will be exported only if they are spot / point
				// everything that is not the above will be exported as a small zone

				jsonObject.type = "Zone"; // All non meshes / lights are considered zone entities
				jsonObject.shapeType = "sphere";
				jsonObject.color = new color() { red = 255, blue = 255, green = 255 };

				// Try for light export
				if (light != null)
				{
					if((light.type == LightType.Point || light.type == LightType.Spot))
					{
						// lights look in the opposite direction of HF, so we have to flip the rotation
						jsonObject.type = "Light";

						jsonObject.registrationPoint = Vector3.one / 2f;
						jsonObject.dimensions = Vector3.one * light.range;

						// Do the colour / intensity etc
						jsonObject.color = new color();
						jsonObject.color.blue = (int)(light.color.b * 255);
						jsonObject.color.red = (int)(light.color.r * 255);
						jsonObject.color.green = (int)(light.color.g * 255);
						jsonObject.intensity = light.intensity * 10;
						jsonObject.falloffRadius = 1;

						if(light.type == LightType.Spot)
						{
							jsonObject.isSpotlight = true;
							float spotAngle = light.spotAngle;
							if(spotAngle > 90)
								spotAngle = 90;

							jsonObject.cutoff = spotAngle;
							jsonObject.exponent = 10; // NOTE: This has no duplicate in Unity, set as close to Unity as I could
						}
					}
					else if(light.type == LightType.Directional || light.type == LightType.Area)
					{
						// TODO - Export lights which are area or directional as zones
//						Debug.LogError("Trying to export a directional light, which hasn't been implemented yet. This object will be exported as an empty zone");
						continue;
					}
				}

				// Find the mesh to use
				MeshFilter meshFilter = gameObj.GetComponent<MeshFilter>();
				SkinnedMeshRenderer skinnedMeshRenderer = gameObj.GetComponent<SkinnedMeshRenderer>();

				Mesh objectMesh = null;

				if(meshFilter != null)
					objectMesh = meshFilter.sharedMesh;
				else if(skinnedMeshRenderer != null)
				{
					objectMesh = new Mesh();
					skinnedMeshRenderer.BakeMesh(objectMesh);
				}
					

				// In some cases, a light and a mesh may be in the same object.
				// If this happens, we have to duplicate the object and create it anew
				// NOTE: The light will be detached from the hierachy tree in this case
				if(light != null && objectMesh != null)
				{
					// Save the old guid
					string oldGuid = jsonObject.id;

					// create a new guid
					jsonObject.id = CreateNewGUID();

					// Write this new object
					string serialized = JsonUtility.ToJson(jsonObject, true);
					jsonOutput.Append(serialized);
					jsonOutput.Append(",");

					// Reassign the guid we had before.
					jsonObject.id = oldGuid;
				}

				if(objectMesh != null)
				{
					jsonObject.collisionless = false;

					jsonObject.type = "Model"; // Tells the system this is a model
					jsonObject.shapeType = "compound"; // Tells the system to use the mesh for collision in HF

					// Find if we have a fbx file that is being reference,
					// If we do, extract that file, copy and reference this
					string assetPath = "";
					if(meshFilter != null)
						assetPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
					string assetFileName = GetFileName(assetPath);
					string folderAssetsPath = Application.dataPath;
					folderAssetsPath = folderAssetsPath.Remove(folderAssetsPath.Length - 6, 6);
					string fullAssetPath = folderAssetsPath + assetPath;

					string extension = GetFileExtension(assetPath).ToLower();
					ExportType exportType = ExportType.Procedural;

					if(extension == "fbx")
						exportType = ExportType.Fbx;
					else if(extension == "obj")
						exportType = ExportType.Obj;
					else if(extension == "blend")
						exportType = ExportType.Procedural;
					
					// -- HACK -- Always forcing to procedural so materials export correctly, this may change if we include FBX and OBJ copying
					exportType = ExportType.Procedural;

					// Skinned meshes need to be baked into whatever pose they are in, so we have a different path for that
					if(skinnedMeshRenderer != null)
						exportType = ExportType.SkinnedMesh;

					// HACK - planes and quads do not export correctly, so instead we are exporting them as HiFi simple objects
					if(assetFileName == "unity default resources" && (objectMesh.name == "Plane" || objectMesh.name == "Quad"))
						exportType = ExportType.PlaneOrQuad;

					string relativeAssetFileName = "";

					// Now either copy or create a new FBX file for export
					switch(exportType)
					{
					case ExportType.Fbx:
//						System.IO.File.Copy(fullAssetPath, fbxExportPath + assetFileName, true);
//						relativeAssetFileName = "FBXObjects/" + assetFileName;
						break;

					case ExportType.Obj: // No longer copying or exporting OBJ or FBX files so no longer needed
//						System.IO.File.Copy(fullAssetPath, objExportPath + assetFileName, true);
//						// TODO - maybe copy the material file here too
//						relativeAssetFileName = "ObjObjects/" + assetFileName;
						Debug.LogError("Export type is either FBX or OBJ, this shouldn't be happening");
						break;

					case ExportType.Procedural:
						// Check to see if an asset file name was found
						// Also sets up the file name if it has none
						if(assetFileName == "")
							assetFileName = "ProceduralModel_" + Random.Range(0, 1000000);
						else if(assetFileName == "unity default resources")
							assetFileName = "UnityPrimitive";
		
						// Remove whatever ending this file has (like .blend) and add fbx for export
						assetFileName = RemoveFileExtension(assetFileName);
						assetFileName += ".fbx";
					
						MeshRenderer meshRenderer = gameObj.GetComponent<MeshRenderer>();

						// Gets the unique mesh ID based on this object so we don't duplicate prefabs
						string meshUniqueId = GetUniqueMeshID(objectMesh, meshRenderer);

						if(meshReference.ContainsKey(meshUniqueId) == false)
						{
							// Checks to see if the file name already exists (in the case of duplicates)
							// and tacks on a unique number
							assetFileName = MakeUniqueAssetFileName(assetFileName, fbxExportPath);

							bool success = HighFidelityFBXExporter.ExportGameObjToFBX(gameObj, fbxExportPath + assetFileName, true, true);
							if(success == false)
								Debug.Log("Failed to export item " + assetFileName);

							meshReference.Add(meshUniqueId, assetFileName);
						}

						relativeAssetFileName = "FBXObjects/" + WWW.EscapeURL(meshReference[meshUniqueId]).Replace("+","%20");
						break;

					case ExportType.SkinnedMesh:
						if(assetFileName == "")
							assetFileName = "SkinnedMesh" + Random.Range(0, 1000000);

						// Remove whatever ending this file has (like .blend) and add fbx for export
						assetFileName = RemoveFileExtension(assetFileName);
						assetFileName += ".fbx";

						// Gets the unique mesh ID based on this object so we don't duplicate prefabs
						string skinnedMeshUniqueId = objectMesh.GetInstanceID().ToString();

						if(meshReference.ContainsKey(skinnedMeshUniqueId) == false)
						{
							// Checks to see if the file name already exists (in the case of duplicates)
							// and tacks on a unique number
							assetFileName = MakeUniqueAssetFileName(assetFileName, fbxExportPath);

							bool skinnedSuccess = HighFidelityFBXExporter.ExportGameObjToFBX(gameObj, fbxExportPath + assetFileName, true, true);
							if(skinnedSuccess == false)
								Debug.Log("Failed to export item " + assetFileName);

							meshReference.Add(skinnedMeshUniqueId, assetFileName);
						}

						relativeAssetFileName = "FBXObjects/" + WWW.EscapeURL(meshReference[skinnedMeshUniqueId]).Replace("+","%20");
						break;

					case ExportType.PlaneOrQuad:
						jsonObject.type = "Box";
						jsonObject.shapeType = "box";
						relativeAssetFileName = "";
						break;
					}

					// Finally add the text reference
					jsonObject.modelURL = @"file:///" + rootPath + relativeAssetFileName;

					// NOTE NOTE - THIS IS WHERE WE REFERENCE THE URL CORRECTLY
					if(URLPopupWindow.LocalOnly == false)
						jsonObject.modelURL = URLPopupWindow.URLFolder + relativeAssetFileName;
					
					// Meshes need to be offset correctly, so we have to redo the calcuations
					jsonObject.registrationPoint.x = .5f;
					jsonObject.registrationPoint.y = .5f;
					jsonObject.registrationPoint.z = .5f;

					// Find the rotation in local exporting space
					Vector3 dimensions = objectMesh.bounds.size;

					jsonObject.dimensions.x = dimensions.x * gameObj.transform.lossyScale.x;
					jsonObject.dimensions.y = dimensions.y * gameObj.transform.lossyScale.y;
					jsonObject.dimensions.z = dimensions.z * gameObj.transform.lossyScale.z;

					// Offsets the position compared to the center of the mesh versus the FBX rotational point
					if(gameObj.transform.parent == null || hasParent == false)
					{
						// If there is no parent, then we just offset by the world position
						// If a subobject of a tree is selected, the parent is the highest in the tree
						jsonObject.position = gameObjectsToExport[gameObjectIndex].transform.position;
						jsonObject.position.x *= -1;

						Vector3 positionOffset = FindOffset(objectMesh, gameObj);
						jsonObject.position += positionOffset;

						jsonObject.rotation = ConvertToHFQuaternion(gameObj.transform.rotation);
					}
					else
					{
						// All the calcuations to figure out where the child mesh should be set compared to the parent object

						jsonObject.position = Vector3.zero;

						Transform parentTransform = gameObj.transform.parent;
						MeshFilter parentMeshFilter = gameObj.transform.parent.gameObject.GetComponent<MeshFilter>();
						Transform childTransform = gameObj.transform;

						Vector3 childMeshCenter = objectMesh.bounds.center;

						Vector3 parentMeshCenter = Vector3.zero;
						if(parentMeshFilter != null)
							parentMeshCenter = parentMeshFilter.sharedMesh.bounds.center;

						Vector3 totalOffset = Vector3.zero;

						// Find the center position in world for the parent and child
						Vector3 parentOffset = parentTransform.position + parentTransform.TransformVector(parentMeshCenter);
						Vector3 childOffset = childTransform.position + childTransform.TransformVector(childMeshCenter);

						Vector3 direction = (childOffset - parentOffset);
						Vector3 changeTheDirectionToParent = parentTransform.InverseTransformVector(direction);

						totalOffset = changeTheDirectionToParent;

						totalOffset.x *= -1;
						jsonObject.position += totalOffset;

						jsonObject.rotation = ConvertToHFQuaternion(gameObj.transform.localRotation);

						// Checks to see if one of the parents has some weird skews applied to it
						// Will throw error but still allow export
						CheckForParentSkew(gameObj, GUIDReference);
					}

					// HACK - Ensures that planes or quads have proper dimensions and no reference to a model URL
					if(exportType == ExportType.PlaneOrQuad)
					{
						jsonObject.modelURL = "";

						if(objectMesh.name == "Plane")
						{

						}
						else
						{

						}
					}
				}

				// Offsets the whole group by the center so it imports closed to the player in HiFi
				if(jsonObject.parentID == GetBlankID())
				{
					Vector3 reversedBounds = boundsForAllObjects.center;
					reversedBounds.x *= -1;
					jsonObject.position -= reversedBounds;
				}

				string jsonString = JsonUtility.ToJson(jsonObject, true);

				// If we have an object mesh, we simplify the jsonObject to remove the light information
				// This is a bit messy because it is inversed parenting, but changing it to use inheritance would cause more issues at this time
				if(objectMesh != null)
					jsonString = JsonUtility.ToJson(new HiFiModelObject(jsonObject), true);
				
				jsonOutput.Append(jsonString);
				jsonOutput.Append(",");
			}

			if(jsonOutput[jsonOutput.Length - 1] == ',')
				jsonOutput.Remove(jsonOutput.Length - 1, 1);

			EditorUtility.DisplayProgressBar("Writing File", "", 1);

			jsonOutput.Append("]}");
			System.IO.File.WriteAllText(rootPath + fileName, jsonOutput.ToString());

			EditorUtility.ClearProgressBar();
		}


		#endregion

		#region Coordinates Converters

		public static Quaternion ConvertToHFQuaternion(Quaternion rotation)
		{
			Quaternion newRotation = Quaternion.identity;

			newRotation.x = rotation.x;
			newRotation.y = rotation.y * -1;
			newRotation.z = rotation.z * -1;
			newRotation.w = rotation.w;

			return newRotation;
		}

		static Vector3 FindOffset(Mesh mesh, GameObject gameObj)
		{
			return FindOffset(mesh, gameObj, gameObj.transform);
		}

		static Vector3 FindOffset(Mesh mesh, GameObject gameObj, Transform transformBy)
		{
			if(transformBy == null)
				transformBy = gameObj.transform;

			Vector3 offset = mesh.bounds.center;
			offset = transformBy.TransformVector(offset);
			offset.x *= -1;
			return offset;
		}

		#endregion


		#region GUID Methods

		private static string GetBlankID()
		{
			return "{00000000-0000-0000-0000-000000000000}";
		}

		/// <summary> Creates a new GUID in the HF format </summary>
		public static string CreateNewGUID()
		{
			string guidstring = "{" + GUID.Generate().ToString() + "}";
			guidstring = guidstring.Insert(21, "-");
			guidstring = guidstring.Insert(17, "-");
			guidstring = guidstring.Insert(13, "-");
			guidstring = guidstring.Insert(9, "-");
			return guidstring;
		}

		private static string FindProperParentGUID(GameObject gameObj, Dictionary<GameObject, string> guidDict)
		{
			Transform parentTransform = gameObj.transform.parent;

			if(parentTransform == null)
				return GetBlankID();

			if(guidDict.ContainsKey(parentTransform.gameObject))
				return guidDict[parentTransform.gameObject];

			return GetBlankID();
		}



		private static GameObject FindValidParent(GameObject gameObj, Dictionary<GameObject, string> guidRef)
		{
			Transform parentTransform = gameObj.transform.parent;

			if(parentTransform == null)
				return null;

			if(guidRef.ContainsKey(parentTransform.gameObject) == false)
				return gameObj;

			// TODO - include skinned mesh renderer in here if we need it
			// TODO - include finding if a light is a parent
			if(parentTransform.GetComponent<MeshRenderer>() == false)
				return FindValidParent(parentTransform.gameObject, guidRef);

			return parentTransform.gameObject;
		}

		/// <summary>
		/// Creates a unique key for this mesh + material
		/// </summary>
		public static string GetUniqueMeshID(Mesh mesh, MeshRenderer meshRenderer)
		{
			if(meshRenderer == null)
				return mesh.GetInstanceID().ToString();

			string returnString = mesh.GetInstanceID().ToString();

			Material[] materials = meshRenderer.sharedMaterials;

			for(int i = 0; i < materials.Length; i++)
			{
				if(materials[i] != null)
					returnString += materials[i].GetInstanceID();
				else
				{
					returnString += "null" + i.ToString();
					Debug.LogError("A mesh " + meshRenderer.name + " has a null material, could cause issues on import.");
				}
					
			}

			return returnString;
		}

		#endregion

		#region Error Checking Methods

		/// <summary>
		/// Checks to see if the player has skewed a Unity object, which won't translate well to High Fidelity
		/// </summary>
		public static void CheckForParentSkew(GameObject gameObj, Dictionary<GameObject, string> guidRef)
		{
			Transform parentTransform = gameObj.transform;

			// First get all the parents
			List<Transform> listOfParents = new List<Transform>();

			int breaker = 0;
			while(parentTransform != null && breaker < 1000)
			{
				parentTransform = parentTransform.parent;

				if(parentTransform != null && guidRef.ContainsKey(parentTransform.gameObject))
					listOfParents.Add(parentTransform);
				else
					parentTransform = null;
				
				breaker++;
				if(breaker > 999)
					Debug.Log("hit breaker");
			}

			for(int i = 0; i < listOfParents.Count; i++)
			{
				if(listOfParents[i].localScale != Vector3.one)
				{
					Debug.LogError("GameObject " + gameObj.name + " has parent(s) with a different scale than 1, 1, 1. This will not translate to High Fidelity. " +
						"Please fix and export again. Export will have completed, but some objects in High Fidelity will not transfer well.");
				}
			}
		}

		#endregion

		#region File Methods

		private static string FindRootPath(string fullFilePath)
		{
			return fullFilePath.Remove(fullFilePath.LastIndexOf('/'), fullFilePath.Length - fullFilePath.LastIndexOf('/')) + "/";
		}

		/// <summary>
		/// Recursively ensures that a file name is unique
		/// </summary>
		private static string MakeUniqueAssetFileName(string assetNameWithExtension, string path)
		{
			if(File.Exists(path + assetNameWithExtension) == false)
				return assetNameWithExtension;

			string newAssetName = assetNameWithExtension;
			string randomNumber = Random.Range(0, 1000000).ToString();

			int periodIndex = assetNameWithExtension.LastIndexOf('.');
			if(periodIndex >= 0)
				newAssetName = newAssetName.Insert(periodIndex, randomNumber);
			else
				newAssetName = newAssetName + randomNumber;

			if(File.Exists(path + newAssetName))
				return MakeUniqueAssetFileName(assetNameWithExtension, path);

			return newAssetName;
		}

		private static string GetFileName(string pathWithFileName)
		{
			if(pathWithFileName.LastIndexOf('/') < 0)
				return "";

			return pathWithFileName.Remove(0, pathWithFileName.LastIndexOf('/') + 1);
		}

		private static string GetFileExtension(string pathWithFileName)
		{
			if(pathWithFileName.LastIndexOf('.') < 0)
				return "";
			
			return pathWithFileName.Remove(0, pathWithFileName.LastIndexOf('.') + 1);
		}

		private static string RemoveFileExtension(string fileNameWithoutPath)
		{
			int period = fileNameWithoutPath.LastIndexOf('.');
			if(period >= 0)
				return fileNameWithoutPath.Remove(period, fileNameWithoutPath.Length - period);
			return fileNameWithoutPath;
		}


		/// <summary>
		/// Cleansup files at the path plus FBXObjects
		/// </summary>
		public static void CleanUpAnyOldFiles(string jsonPath)
		{
			DeleteFileAtPath(jsonPath, "*.fbx");
			DeleteFileAtPath(jsonPath, "*.fbx.meta");
		}

		/// <summary>
		/// Deletes a specific file in the FBX folder at the path
		/// </summary>
		/// <param name="jsonPath">Json path.</param>
		/// <param name="extension">Extension must be "*.fbx" or "*.fbx.meta"</param></param>
		public static void DeleteFileAtPath(string jsonPath, string extension)
		{
			jsonPath = UtiltiesHiFi.GetFolderFromPath(jsonPath);

			// Set up the directory for the fbx files
			string fbxExportPath = jsonPath + @"FBXObjects/";

			// If we can't find the fbx export path, just return and do nothing
			if(Directory.Exists(fbxExportPath) == false)
				return;

			// If we do find the path, delete all FBX files
			string[] fileNames = Directory.GetFiles(fbxExportPath, "*.fbx");

			// Go through all the files and delete them
			for(int i = 0; i < fileNames.Length; i++)
			{
//				Debug.Log("Deleted " + fileNames[i]);
				File.Delete(fileNames[i]);
			}
		}

		#endregion

		// Reference code
		// This is an experiment into removing any blank game objects and only transfering over
		// anything that is a light or model.
		// Currently we replace empty objects with zones, which seem to have worked out well.

//		/// <summary>
//		/// DO NOT USE
//		/// </summary>
//		private static Vector3 GetPositionOffsetToValidParent(GameObject gameObj, Mesh gameObjMesh, Dictionary<GameObject, string> guidRef)
//		{
//			Vector3 newPos = Vector3.zero;
//
//			GameObject parentGamObj = FindValidParent(gameObj, guidRef);
//
//			// If the gameObject has no parent or it found itself
//			if(parentGamObj == null || parentGamObj == gameObj)
//			{	
//				newPos = gameObj.transform.position;
//				Vector3 offset = gameObjMesh.bounds.center;
//				offset = gameObj.transform.TransformVector(offset);
//				newPos.x *= -1;
//				return newPos;
//			}
//
//			// A parent has been found, now we have to offset everything by the relative positions
//			Transform parentTransform = parentGamObj.transform;
//			Transform childTransform = gameObj.transform;
//
//			MeshFilter parentMeshFilter = parentTransform.GetComponent<MeshFilter>();
//			Vector3 childMeshCenter = gameObjMesh.bounds.center;
//
//			Vector3 parentMeshCenter = Vector3.zero;
//			if(parentMeshFilter != null) // In the cases where a light is being used instead
//				parentMeshCenter = parentMeshFilter.sharedMesh.bounds.center;
//
//			Vector3 totalOffset = Vector3.zero;
//
//			// Find the center position in world for the parent and child
//			Vector3 parentOffset = parentTransform.position + parentTransform.TransformVector(parentMeshCenter);
//			Vector3 childOffset = childTransform.position + childTransform.TransformVector(childMeshCenter);
//
//			Vector3 direction = (childOffset - parentOffset);
//			Vector3 changeTheDirectionToParent = parentTransform.InverseTransformVector(direction);
//
//			totalOffset = changeTheDirectionToParent;
//			totalOffset.x *= -1;
//			newPos = totalOffset;
//
//			return newPos;
//		}

//		public static Quaternion GetRotationOffParent(GameObject gameObj, Dictionary<GameObject, string> guidRef)
//		{
//			GameObject parentGamObj = FindValidParent(gameObj, guidRef);
//
//			// If the gameObject has no parent or it found itself
//			if(parentGamObj == null || parentGamObj == gameObj)
//			{	
//				return ConvertToHFQuaternion(gameObj.transform.rotation);
//			}
//
//			Quaternion parentWorldRotation = parentGamObj.transform.rotation;
//			Quaternion childWorldRotation = gameObj.transform.rotation;
//
//			Quaternion returnAngle = childWorldRotation * Quaternion.Inverse(parentWorldRotation);
//
//			return ConvertToHFQuaternion(returnAngle);
//		}

		// OLD CODE - Probably should delete

//		private static string FindProperParentGUID(GameObject gameObj, Dictionary<GameObject, string> guidDict)
//		{
//			Transform parentTransform = gameObj.transform.parent;
//
//			if(parentTransform == null)
//				return ConvertIDToString(0);
//
//			// TODO - include skinned mesh renderer in here if we need it
//			if(parentTransform.GetComponent<MeshRenderer>() == false)
//				return FindProperParentGUID(parentTransform.gameObject, guidDict);
//
//			if(guidDict.ContainsKey(parentTransform.gameObject))
//				return guidDict[parentTransform.gameObject];
//
//			Debug.LogError("For whatever reason, the GUID reference dictionary does not contain this parent info, probably because you selected a child of a parent. Ignore this warning if this is the case");
//			return ConvertIDToString(0);
//		}

		// Below is the old reference code from when Kellan took over the project
//		// To convert parentID and ID to HiFi compatible string
//		private static string convertIDToString (int ID) 
//		{
//			return ID.ToString("{00000000-0000-0000-0000-000000000000}");
//		}
//
//		// Reach every child in Depth First manner and unparent it
//		private static void UnparentChildRecursive(GameObject obj) {
//			if (null == obj) {
//				return;
//			} else {
//				var transform = obj.GetComponentsInChildren<Transform>();
//				var parent = obj.transform;
//				foreach (Transform child in transform) {
//					if (child == parent || null == child || child.GetComponent<Camera>()) {
//						continue;
//					}
//					if (!childParentMapping.ContainsKey(child.gameObject)) {
//						var parentObj = child.parent ? child.parent.gameObject : null;
//						childParentMapping.Add(child.gameObject, parentObj);
//					}
//					child.parent = null;
//					UnparentChildRecursive(child.gameObject);
//				}
//			}
//		}
//
//		private static void ExportCurrentGameObjectOLD(bool copyMaterials, bool copyTextures) {
//			List<GameObject> currentGameObjects = new List<GameObject>();
//			List<GameObject> separatedGameObjects = new List<GameObject>();
//			GameObject currentGameObject;
//
//			// Export the entire scene if no object selected
//			if (Selection.activeGameObject == null) {
//				if (EditorUtility.DisplayDialog("Export Scene", "No Game Object Selected. Do you want to export the entire scene?", "OK", "Cancel")) {
//					var gameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
//					foreach (var obj in gameObjects) {
//						// skip if gameobject is a camera
//						if (obj.GetComponent<Camera>()) {
//							continue;
//						} else if (obj.activeInHierarchy) {
//							currentGameObjects.Add(obj);
//						}
//					}
//				} else {
//					return;
//				}
//			} else {
//				foreach (var obj in Selection.objects) {
//					currentGameObject = obj as GameObject;
//					if (currentGameObject == null) {
//						EditorUtility.DisplayDialog("Warning", "Item selected is not a GameObject", "Okay");
//						return;
//					} else if (currentGameObject.GetComponent<Camera>()) {
//						// skip if gameobject is a camera
//						continue;
//					} else {
//						currentGameObjects.Add(currentGameObject);
//					}
//				}
//			}
//
//			// Separate out each child game object to export independently
//			foreach (var obj in currentGameObjects) {
//				UnparentChildRecursive(obj);
//				if (!childParentMapping.ContainsKey(obj)) {
//					var parentObj = obj.transform.parent ? obj.transform.parent.gameObject : null;
//					childParentMapping.Add(obj, parentObj);
//				}
//			}
//
//			foreach (var key in childParentMapping.Keys) {
//				separatedGameObjects.Add(key);
//			}
//
//			// Export separated game objects
//			string path = ExportGameObject(separatedGameObjects, copyMaterials, copyTextures);
//			if (path == null) {
//				return;
//			}
//
//			// Export game object information as a json
//			ExportGameObjectAsJson(separatedGameObjects, path);
//
//			// Re-Parent all object
//			foreach(var key in childParentMapping.Keys) {
//				var parent = childParentMapping[key] ? childParentMapping[key].transform : null;
//				key.transform.parent = parent;
//			}
//
//			childParentMapping.Clear();
//
//			EditorUtility.DisplayDialog("Success", "Success " + separatedGameObjects.Count + " game objects exported", "Okay");
//		}
//
//		/// <summary>
//		/// Exports ANY Game Object given to it. Will provide a dialog and return the path of the newly exported file
//		/// </summary>
//		/// <returns>The path of the newly exported FBX file</returns>
//		/// <param name="gameObj">Game object to be exported</param>
//		/// <param name="copyMaterials">If set to <c>true</c> copy materials.</param>
//		/// <param name="copyTextures">If set to <c>true</c> copy textures.</param>
//		/// <param name="oldPath">Old path.</param>
//		public static string ExportGameObject(List<GameObject> gameObjects, bool copyMaterials, bool copyTextures, string oldPath = null) {
//			foreach (var gameObj in gameObjects) {
//				if (gameObj == null) {
//					EditorUtility.DisplayDialog("Object is null", "Please select any GameObject to Export to FBX", "Okay");
//					return null;
//				}
//			}
//
//			// Get folder path
//			string newPath = GetNewPath(oldPath);
//			if (newPath == null) {
//				return null;
//			}
//
//			foreach (var gameObject in gameObjects) {
//				var fileName = newPath + "/" + gameObject.name + ".fbx";
//				if (fileName != null && fileName.Length != 0) {
//					bool isSuccess = FBXExporter.ExportGameObjToFBX(gameObject, fileName, copyMaterials, copyTextures);
//					if (!isSuccess) {
//						EditorUtility.DisplayDialog("Warning", "The extension probably wasn't an FBX file, could not export.", "Okay");
//					}
//				}
//			}
//			return newPath;
//		}
//
//		/// <summary>
//		/// Creates save dialog window depending on old path or right to the /Assets folder no old path is given
//		/// </summary>
//		/// <returns>The new path.</returns>
//		/// <param name="gameObject">Item to be exported</param>
//		/// <param name="oldPath">The old path that this object was original at.</param>
//		private static string GetNewPath(string oldPath = null) {
//			// NOTE: This must return a path with the starting "Assets/" or else textures won't copy right
//			string newPath = null;
//
//			if (oldPath == null) {
//				newPath = EditorUtility.SaveFolderPanel("Select Folder to Export FBX", "/Assets", "");
//				if (!newPath.Contains("/Assets")) {
//					EditorUtility.DisplayDialog("Warning", "Must save file in the project's assets folder", "Okay");
//					return null;
//				}
//			} else {
//				if (oldPath.StartsWith("/Assets")) {
//					oldPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/Assets"), 7) + oldPath;
//					oldPath = oldPath.Remove(oldPath.LastIndexOf('/'), oldPath.Length - oldPath.LastIndexOf('/'));
//				}
//				newPath = EditorUtility.SaveFolderPanel("Select Folder to Export FBX", oldPath, "");
//			}
//
//			int assetsIndex = newPath.IndexOf("Assets");
//
//			if (assetsIndex < 0)
//				return null;
//
//			if (assetsIndex > 0)
//				newPath = newPath.Remove(0, assetsIndex);
//
//			return newPath;
//		}
//
//		public static void ExportGameObjectAsJson(List<GameObject> gameObjects, string path) {
//			Vector3 nullVector = new Vector3(0, 0, 0);
//			Vector3 unitVector = new Vector3(1, 1, 1);
//			Dictionary<GameObject, int> objectToIDMapping = new Dictionary<GameObject, int>();
//			int objectID = 2;
//			foreach(var key in childParentMapping.Keys) {
//				if (!objectToIDMapping.ContainsKey(key)) {
//					objectToIDMapping.Add(key, objectID++);
//				}
//			}
//			string filePath = EditorUtility.SaveFilePanelInProject("Select JSON Filename", "gameObjects.json", "json", "Export GameObjects to a JSON file");
//
//			StringBuilder jsonOutput = new StringBuilder("{\"Entities\":[");
//
//			for (int i = 0; i < gameObjects.Count; i++) {
//				SerializeJSON jsonObject = new SerializeJSON();
//
//				// Setting position
//				jsonObject.position = gameObjects[i].transform.position;
//				jsonObject.position.x *= -1;
//
//				// Setting registration Point
//				if (gameObjects[i].GetComponent<MeshFilter>()) {
//					Mesh mesh = gameObjects[i].GetComponent<MeshFilter>().mesh;
//					Vector3 minBound = mesh.bounds.min;
//					Vector3 boundSize = mesh.bounds.size;
//					jsonObject.registrationPoint.x = (boundSize.x == 0) ? 0 : (minBound.x * -1) / boundSize.x;
//					jsonObject.registrationPoint.y = (boundSize.y == 0) ? 0 : (minBound.y * -1) / boundSize.y;
//					jsonObject.registrationPoint.z = (boundSize.z == 0) ? 0 : (minBound.z * -1) / boundSize.z;
//				}
//
//				// Setting Dimensions
//				Bounds bounds = new Bounds();
//
//				if (gameObjects[i].GetComponent<MeshFilter>()) {
//					bounds = gameObjects[i].GetComponent<MeshFilter>().mesh.bounds;
//				}
//
//				jsonObject.dimensions = bounds.size;
//				jsonObject.dimensions = Vector3.Scale(jsonObject.dimensions, gameObjects[i].transform.localScale);
//
//				if (jsonObject.dimensions == nullVector) {
//					jsonObject.dimensions = unitVector;
//				}
//
//				// Setting type of model
//				if (gameObjects[i].GetComponent<Light>()) {
//					jsonObject.type = "Light";
//				} else {
//					jsonObject.type = "Model";
//					jsonObject.shapeType = "compound";
//				}
//
//				// Setting Object ID and Parent ID
//				jsonObject.id = convertIDToString(objectToIDMapping[gameObjects[i]]);
//
//				var parent = childParentMapping[gameObjects[i]];
//				if (parent && objectToIDMapping.ContainsKey(parent)) {
//					jsonObject.parentID = convertIDToString(objectToIDMapping[parent]);
//				} else {
//					jsonObject.parentID = convertIDToString(0);
//				}
//
//				// Setting model URL
//				string directory = Application.dataPath.Replace("Assets", "");
//				jsonObject.modelURL = "file:///" + directory + path + "/" + gameObjects[i].name + ".fbx";
//
//				// Writing JSON to file
//				string jsonString = JsonUtility.ToJson(jsonObject);
//				jsonOutput.Append(jsonString);
//				if (i != gameObjects.Count - 1) {
//					jsonOutput.Append(",");
//				}
//			}
//			jsonOutput.Append("]}");
//			System.IO.File.WriteAllText(filePath, jsonOutput.ToString());
//		}


		// OLD - delete before release
		//		public static void ExportGameObjectAsJsonToHFOLD(List<GameObject> gameObjectsToExport, string rootPath, string fileName, bool copyMaterials = false, bool copyTextures = false) 
		//		{
		//			Dictionary<Mesh, string> meshReference = new Dictionary<Mesh, string>();
		//
		//			// Set up the directory for the fbx files
		//			string fbxExportPath = rootPath + @"FBXObjects/";
		//			if(Directory.Exists(fbxExportPath) == false)
		//				Directory.CreateDirectory(fbxExportPath);
		//
		//			// Destroy all the FBX files in the current directory
		//
		//			// No longer copying over OBJ files, so no need for a folder for this
		//			//			// Set up the directory for the obj objects
		//			//			string objExportPath = rootPath + @"ObjObjects/";
		//			//			if(Directory.Exists(objExportPath) == false)
		//			//				Directory.CreateDirectory(objExportPath);
		//
		//			StringBuilder jsonOutput = new StringBuilder("{\"Entities\":[");
		//			for (int i = 0; i < gameObjectsToExport.Count; i++)
		//			{
		//				// Exports all the lights from the system
		//				if (gameObjectsToExport[i].GetComponent<Light>())
		//				{
		//					Light light = gameObjectsToExport[i].GetComponent<Light>();
		//					if(light.type == LightType.Point || light.type == LightType.Spot)
		//					{
		//						GameObject gameObj = gameObjectsToExport[i];
		//						HFLightJsonObject jsonObject = new HFLightJsonObject();
		//						jsonObject.type = "Light";
		//
		//						jsonObject.parentID = FindProperParentGUID(gameObj, GUIDReference);
		//						jsonObject.id = GUIDReference[gameObj];
		//						bool hasParent = jsonObject.parentID != ConvertIDToString(0);
		//
		//						jsonObject.registrationPoint = Vector3.one / 2f;
		//						jsonObject.dimensions = Vector3.one * light.range;
		//
		//						Quaternion unityQuaternion = Quaternion.identity;
		//
		//						if(hasParent == false)
		//						{
		//							unityQuaternion = ConvertToHFQuaternion(gameObj.transform.rotation);
		//							jsonObject.position = gameObjectsToExport[i].transform.position;
		//							jsonObject.position.x *= -1;
		//						}
		//						else
		//						{
		//							unityQuaternion = ConvertToHFQuaternion(gameObj.transform.localRotation);
		//							jsonObject.position = gameObjectsToExport[i].transform.localPosition;
		//							jsonObject.position.x *= -1;
		//						}	
		//
		//						jsonObject.rotation = unityQuaternion;
		//
		//						// Do the colour / intensity etc
		//						jsonObject.color = new color();
		//
		//						jsonObject.color.blue = (int)(light.color.b * 255);
		//						jsonObject.color.red = (int)(light.color.r * 255);
		//						jsonObject.color.green = (int)(light.color.g * 255);
		//						jsonObject.intensity = light.intensity * 10;
		//						jsonObject.falloffRadius = 1;
		//
		//						if(light.type == LightType.Spot)
		//						{
		//							jsonObject.isSpotlight = true;
		//							float spotAngle = light.spotAngle;
		//							if(spotAngle > 90)
		//								spotAngle = 90;
		//
		//							jsonObject.cutoff = spotAngle;
		//							jsonObject.exponent = 10; // NOTE: This has no duplicate in Unity, set as close to Unity as I could
		//						}
		//
		//						// WRITE TO JSON FILE
		//						string jsonString = JsonUtility.ToJson(jsonObject, true);
		//						jsonOutput.Append(jsonString);
		//						jsonOutput.Append(",");
		//					}
		//					// Can't export directional / area lights
		//				} 
		//				// Exports any other objects which should have a mesh filter
		//				else if(gameObjectsToExport[i].GetComponent<MeshFilter>())
		//				{
		//					// Do this in the order that the file is listed out in
		//					HFJsonObject jsonObject = new HFJsonObject();
		//					GameObject gameObj = gameObjectsToExport[i];
		//
		//					// Set the type of the obejct to be exported.
		//
		//					jsonObject.type = "Model"; // Tells the system this is a model
		//					jsonObject.shapeType = "compound"; // Tells the system to use the mesh for collision in HF
		//
		//					// Assign the IDs for the object
		//					jsonObject.parentID = FindProperParentGUID(gameObj, GUIDReference);
		//					jsonObject.id = GUIDReference[gameObj];
		//					bool hasParent = jsonObject.parentID != ConvertIDToString(0);
		//
		//					// Find if we have a fbx file that is being reference,
		//					// If we do, extract that file, copy and reference this
		//					MeshFilter filter = gameObj.GetComponent<MeshFilter>();
		//					string assetPath = AssetDatabase.GetAssetPath(filter.sharedMesh);
		//					string assetFileName = GetFileName(assetPath);
		//					string folderAssetsPath = Application.dataPath;
		//					folderAssetsPath = folderAssetsPath.Remove(folderAssetsPath.Length - 6, 6);
		//					string fullAssetPath = folderAssetsPath + assetPath;
		//
		//					string extension = GetFileExtension(assetPath).ToLower();
		//					ExportType exportType = ExportType.Procedural;
		//
		//					if(extension == "fbx")
		//						exportType = ExportType.Fbx;
		//					else if(extension == "obj")
		//						exportType = ExportType.Obj;
		//					else if(extension == "blend")
		//						exportType = ExportType.Procedural;
		//					else
		//						Debug.LogError("Export Type has not been set because the exention was \"" + extension + "\"");
		//
		//					// HACK - Always forcing to procedural so materials export correctly
		//					exportType = ExportType.Procedural;
		//
		//					string relativeAssetFileName = "";
		//
		//					// Now either copy or create a new FBX file for export
		//					// TODO - If two files have the same name and extension, they will export incorrectly
		//					switch(exportType)
		//					{
		//					case ExportType.Fbx:
		//						System.IO.File.Copy(fullAssetPath, fbxExportPath + assetFileName, true);
		//						relativeAssetFileName = "FBXObjects/" + assetFileName;
		//						break;
		//					case ExportType.Obj: // No longer copying or exporting OBJ or FBX files so no longer needed
		//						//						System.IO.File.Copy(fullAssetPath, objExportPath + assetFileName, true);
		//						//						// TODO - maybe copy the material file here too
		//						//						relativeAssetFileName = "ObjObjects/" + assetFileName;
		//						break;
		//					case ExportType.Procedural:
		//						// Check to see if an asset file name was found
		//						if(assetFileName == "")
		//						{
		//							assetFileName = "ProceduralModel_" + Random.Range(0, 1000000);
		//						}
		//						else if(assetFileName == "unity default resources")
		//						{
		//							assetFileName = "UnityPrimitive";
		//						}
		//						else
		//						{
		//							// Remove whatever ending this file has (like .blend) and add fbx for export
		//							// TODO - add catch in case there is no extension on a file
		//							assetFileName = RemoveFileExtension(assetFileName);
		//							assetFileName += ".fbx";
		//						}
		//
		//						Mesh meshie = gameObj.GetComponent<MeshFilter>().sharedMesh;
		//
		//						if(meshReference.ContainsKey(meshie) == false)
		//						{
		//							// Checks to see if the file name already exists (in the case of duplicates)
		//							// and tacks on a unique number
		//							assetFileName = MakeUniqueAssetFileName(assetFileName, fbxExportPath);
		//
		//							bool success = HighFidelityFBXExporter.ExportGameObjToFBX(gameObj, fbxExportPath + assetFileName, true, true);
		//							if(success == false)
		//								Debug.Log("Failed to export item " + assetFileName);
		//
		//							meshReference.Add(meshie, assetFileName);
		//						}
		//
		//						relativeAssetFileName = "FBXObjects/" + meshReference[meshie];
		//
		//						break;
		//					}
		//
		//					// Finally add the text reference
		//					jsonObject.modelURL = @"file:///" + rootPath + relativeAssetFileName;
		//
		//					jsonObject.position = gameObjectsToExport[i].transform.localPosition;
		//					jsonObject.position.x *= -1;
		//
		//					if (gameObjectsToExport[i].GetComponent<MeshFilter>()) // TODO - remove this mesh filter restriction
		//					{
		//						Mesh mesh = gameObjectsToExport[i].GetComponent<MeshFilter>().sharedMesh;
		//
		//						// We now have to offset the position by the bounding box center
		//						// First rotate the bounding box vector into the local gameObjects transform
		//
		//						jsonObject.registrationPoint.x = .5f;
		//						jsonObject.registrationPoint.y = .5f;
		//						jsonObject.registrationPoint.z = .5f;
		//
		//						// Find the rotation in local exporting space
		//						Vector3 dimensions = mesh.bounds.size;
		//
		//						//						jsonObject.dimensions.x = dimensions.x * gameObj.transform.localScale.x;
		//						//						jsonObject.dimensions.y = dimensions.y * gameObj.transform.localScale.y;
		//						//						jsonObject.dimensions.z = dimensions.z * gameObj.transform.localScale.z;
		//
		//						jsonObject.dimensions.x = dimensions.x * gameObj.transform.lossyScale.x;
		//						jsonObject.dimensions.y = dimensions.y * gameObj.transform.lossyScale.y;
		//						jsonObject.dimensions.z = dimensions.z * gameObj.transform.lossyScale.z;
		//
		//						//						jsonObject.position = GetPositionOffsetToValidParent(gameObj, mesh, GUIDReference);
		//
		//						// TODO - rotate through all parents to make sure everything is rotated well as we go up a hierarchy
		//
		//						// Offsets the position compared to the center of the mesh versus the FBX rotational point
		//						if(gameObj.transform.parent == null || hasParent == false)
		//						{
		//							jsonObject.position = gameObjectsToExport[i].transform.position;
		//							jsonObject.position.x *= -1;
		//
		//							Vector3 positionOffset = FindOffset(mesh, gameObj);
		//							jsonObject.position += positionOffset;
		//
		//							jsonObject.rotation = ConvertToHFQuaternion(gameObj.transform.rotation);
		//						}
		//						else
		//						{
		//							jsonObject.position = Vector3.zero;
		//
		//							Transform parentTransform = gameObj.transform.parent;
		//							MeshFilter parentMeshFilter = gameObj.transform.parent.gameObject.GetComponent<MeshFilter>();
		//							Transform childTransform = gameObj.transform;
		//
		//							Vector3 childMeshCenter = mesh.bounds.center;
		//
		//							Vector3 parentMeshCenter = Vector3.zero;
		//							if(parentMeshFilter != null)
		//								parentMeshCenter = parentMeshFilter.sharedMesh.bounds.center;
		//
		//							Vector3 totalOffset = Vector3.zero;
		//
		//							// Find the center position in world for the parent and child
		//							Vector3 parentOffset = parentTransform.position + parentTransform.TransformVector(parentMeshCenter);
		//							Vector3 childOffset = childTransform.position + childTransform.TransformVector(childMeshCenter);
		//
		//							Vector3 direction = (childOffset - parentOffset);
		//							Vector3 changeTheDirectionToParent = parentTransform.InverseTransformVector(direction);
		//
		//							totalOffset = changeTheDirectionToParent;
		//
		//							totalOffset.x *= -1;
		//							jsonObject.position += totalOffset;
		//
		//							jsonObject.rotation = ConvertToHFQuaternion(gameObj.transform.localRotation);
		//
		//							//							jsonObject.rotation = GetRotationOffParent(gameObj, GUIDReference);
		//
		//						}
		//					}
		//
		//					// Writing JSON to file
		//					string jsonString = JsonUtility.ToJson(jsonObject, true);
		//					jsonOutput.Append(jsonString);
		//					jsonOutput.Append(",");
		//				}
		//			}
		//
		//			if(jsonOutput[jsonOutput.Length - 1] == ',')
		//				jsonOutput.Remove(jsonOutput.Length - 1, 1);
		//
		//			jsonOutput.Append("]}");
		//			System.IO.File.WriteAllText(rootPath + fileName, jsonOutput.ToString());
		//		}
	}
}