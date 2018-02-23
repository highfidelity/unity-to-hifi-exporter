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

		// Light stuff
		public bool isSpotlight;
		public float intensity = 1.0f;
		public float exponent;
		public float cutoff;
		public color color;
		public float falloffRadius;
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
}
