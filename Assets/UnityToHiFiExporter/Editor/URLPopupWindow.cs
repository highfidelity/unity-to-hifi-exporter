using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HiFiExporter;

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
		window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 186);
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
		URLFolder = GUILayout.TextArea(URLFolder);

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
