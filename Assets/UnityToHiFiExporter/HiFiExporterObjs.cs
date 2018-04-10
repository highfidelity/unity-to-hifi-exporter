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

namespace HiFiExporter
{
	/// <summary>
	/// File system that can be imported by HiFi
	/// </summary>
	[System.Serializable]
	public class HiFiJsonObject
	{
		// Model stuff
		public string type;
		public string modelURL;
		public Vector3 position;
		public Vector3 registrationPoint;
		public Vector3 dimensions;
		public Quaternion rotation;
		public string id;
		public string parentID;
		public string shapeType;
		public bool collisionless = false;
		// NOTE: If you add anything to the mode, please add it to the HiFiModelObject below as well as in its constructor

		// Light stuff
		public bool isSpotlight;
		public float intensity = 1.0f;
		public float exponent;
		public float cutoff;
		public color color;
		public float falloffRadius;
	}

	/// <summary>
	/// Used to simplify the HiFiJsonObject so that it doesn't have extra light information
	/// </summary>
	public class HiFiModelObject
	{
		public HiFiModelObject(HiFiJsonObject obj)
		{
			type = obj.type;
			modelURL = obj.modelURL;
			position = obj.position;
			registrationPoint = obj.registrationPoint;
			dimensions = obj.dimensions;
			rotation = obj.rotation;
			id = obj.id;
			parentID = obj.parentID;
			shapeType = obj.shapeType;
			collisionless = obj.collisionless;
		}

		public string type;
		public string modelURL;
		public Vector3 position;
		public Vector3 registrationPoint;
		public Vector3 dimensions;
		public Quaternion rotation;
		public string id;
		public string parentID;
		public string shapeType;
		public bool collisionless = false;
	}

	/// <summary>
	/// Color class used in hifi (hifi format)
	/// </summary>
	[System.Serializable]
	public class color
	{
		public int red;
		public int green;
		public int blue;
	}

	public class MeshInfo
	{
		public string AssetFileName;
//		public List<Material> Materials;
	}
}
