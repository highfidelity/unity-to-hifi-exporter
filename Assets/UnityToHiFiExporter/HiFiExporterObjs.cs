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
