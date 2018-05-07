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
using HiFiExporter;

namespace HiFiExporter 
{
	/// <summary>
	/// Creates a popup that is used just before exporting the fbx files, allows the player to set up a URL where the remote files will be
	/// NOTE: This will eventually be phased out once the relative links are working well
	/// </summary>
	public class URLPopupWindow : EditorWindow {

		public static bool LocalOnly = false;
		public static string URLFolder = "URL://";
		private static bool setFocus = false;

		/// <summary>
		/// Initializes the window and readies it for use
		/// </summary>
		public static void Init()
		{
			URLPopupWindow window = ScriptableObject.CreateInstance<URLPopupWindow>();
			window.position = new Rect(200, 200, 250, 186);
			window.Focus();
			setFocus = true;
			window.ShowPopup();
		}

		void OnGUI()
		{
			EditorGUILayout.LabelField("Please choose the root URL these objects will be sent to",
				EditorStyles.wordWrappedLabel);

			GUIStyle subtextStyle = EditorStyles.wordWrappedMiniLabel;

			EditorGUILayout.LabelField("If you are using objects on remote servers, you must upload them to a spot on the internet. " +
				"Please enter the relative path here. This is the path that you just saved the json file to. This should be" +
				"a place on Amazon servers or somewhere that the model fbx will live forever.", 
				subtextStyle);

			GUILayout.Space(10);

			GUI.SetNextControlName("URLField");
			URLFolder = EditorGUILayout.TextArea(URLFolder);

			if(setFocus == true)
			{
				EditorGUI.FocusTextInControl("URLField");
				setFocus = false;
			}

			GUILayout.Space(10);

			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Don't Use"))
			{
				LocalOnly = true;

				ExporterMenuHiFi.ExportCurrentGameObjects();
				this.Close();
			}	

			if (GUILayout.Button("Okay")) 
			{
				LocalOnly = false;

				if(URLFolder == "URL://")
					LocalOnly = true;

				if(URLFolder[URLFolder.Length - 1] != '/')
					URLFolder += "/";

				ExporterMenuHiFi.ExportCurrentGameObjects();
				this.Close();
			}
			EditorGUILayout.EndHorizontal();

			if(GUILayout.Button("Cancel"))
			{
				this.Close();
			}
		}
	}
}