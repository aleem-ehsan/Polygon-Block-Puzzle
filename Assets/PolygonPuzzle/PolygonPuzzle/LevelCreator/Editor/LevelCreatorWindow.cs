using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace dotmob.PolygonPuzzle
{
	public class LevelCreatorWindow : EditorWindow
	{
		#region Member Variables
		
		private int		minNumShapes;
		private int		maxNumShapes;
		private int		numLevels;
		private string	filename;
		private Object	outputFolder;
		private bool	overwriteLevels;
		private bool	displayFolderError;
		
		private int					seed;
		private Texture2D			lineTexture;
		private PuzzleCreatorWorker	puzzleCreatorWorker;

		#endregion // Member Variables

		#region Properties

		private string OutputFolderAssetPath
		{
			get { return EditorPrefs.GetString("PolygonPuzzle.LevelCreatorWindow.OutputFolderAssetPath", null); }
			set { EditorPrefs.SetString("PolygonPuzzle.LevelCreatorWindow.OutputFolderAssetPath", value); }
		}
		
		private Texture2D LineTexture
		{
			get
			{
				if (lineTexture == null)
				{
					lineTexture = new Texture2D(1, 1);
					lineTexture.SetPixel(0, 0, new Color(37f/255f, 37f/255f, 37f/255f));
					lineTexture.Apply();
				}

				return lineTexture;
			}
		}
		
		#endregion // Properties

		#region Draw Methods

		[MenuItem("Dotmob/Level Editor")]
		public static void OpenWindow()
		{
			EditorWindow.GetWindow<LevelCreatorWindow>("Level Editor");
		}

		private void OnEnable()
		{
			if (!string.IsNullOrEmpty(OutputFolderAssetPath))
			{
				// Set the reference to the folder
				outputFolder = AssetDatabase.LoadAssetAtPath<Object>(OutputFolderAssetPath);

				// Check if the folder still exists
				if (outputFolder == null)
				{
					OutputFolderAssetPath = null;
				}
			}
		}

		private void OnDisable()
		{
			DestroyImmediate(LineTexture);
		}

		private void Update()
		{
			if (puzzleCreatorWorker != null)
			{
				if (puzzleCreatorWorker.Stopped)
				{
					AssetDatabase.Refresh();

					string title, message;

					if (string.IsNullOrEmpty(puzzleCreatorWorker.error))
					{
						title	= "Generating Levels";
						message	= "Successfully generated " + numLevels + " level files and placed them in the following folder: " + GetOutputFolderFullPath();;
					}
					else
					{
						PrintErrorMessage(puzzleCreatorWorker.error);

						title	= "Generating Levels";
						message	= "An unexpected error occured while generating the levels, check the Unity Console for more details";
					}

					EditorUtility.DisplayDialog(title, message, "Okay");

					EditorUtility.ClearProgressBar();

					puzzleCreatorWorker = null;
				}
				else
				{
					string	title		= "Generating Levels";
					string	info		= "Creating " + numLevels + " randomly generated levels...";
					float	progress	= puzzleCreatorWorker.Progress;

					bool cancelled = EditorUtility.DisplayCancelableProgressBar(title, info, progress);

					if (cancelled)
					{
						puzzleCreatorWorker.Stop();
						puzzleCreatorWorker = null;

						EditorUtility.ClearProgressBar();
					}
				}
			}
		}
		
		private void OnGUI()
		{
			EditorGUILayout.Space();

			BeginBox("Level Editor © Dotmobstudio");

			EditorGUILayout.Space();

			minNumShapes	= Mathf.Max(2, EditorGUILayout.IntField("Min Number Of Shapes", minNumShapes));
			maxNumShapes	= Mathf.Max(minNumShapes, EditorGUILayout.IntField("Max Number Of Shapes", maxNumShapes));
			numLevels		= Mathf.Max(1, EditorGUILayout.IntField("Number Of Levels", numLevels));

			EditorGUILayout.Space();

			filename		= EditorGUILayout.TextField("Filename", filename);
			overwriteLevels	= EditorGUILayout.Toggle("Overwrite Existing Files", overwriteLevels);

			Object newOutputFolder = EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(Object), false);

			if (outputFolder != newOutputFolder)
			{
				outputFolder 			= newOutputFolder;
				OutputFolderAssetPath	= (outputFolder == null) ? null : AssetDatabase.GetAssetPath(outputFolder);
				displayFolderError		= false;

				if (!string.IsNullOrEmpty(OutputFolderAssetPath) && !System.IO.Directory.Exists(GetOutputFolderFullPath())) 
				{
					OutputFolderAssetPath	= null;
					displayFolderError		= true;
				}
			}

			if (displayFolderError)
			{
				EditorGUILayout.HelpBox("Output Folder must be a folder from your project window.", MessageType.Error);
				GUI.enabled = false;
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Generate Level Files"))
			{
				GenerateLevelFiles();
			}

			GUI.enabled = true;

			EndBox();

			EditorGUILayout.Space();
		}

		private string GetOutputFolderFullPath()
		{
			if (string.IsNullOrEmpty(OutputFolderAssetPath))
			{
				return Application.dataPath;
			}

			return Application.dataPath + OutputFolderAssetPath.Remove(0, "Assets".Length);
		}

		/// <summary>
		/// Begins a new box, must call EndBox
		/// </summary>
		private void BeginBox(string boxTitle = "")
		{
			GUIStyle style		= new GUIStyle("HelpBox");
			style.padding.left	= 0;
			style.padding.right	= 0;

			GUILayout.BeginVertical(style);

			if (!string.IsNullOrEmpty(boxTitle))
			{
				DrawBoldLabel(boxTitle);

				DrawLine();
			}
		}

		/// <summary>
		/// Ends the box.
		/// </summary>
		private void EndBox()
		{
			GUILayout.EndVertical();
		}

		/// <summary>
		/// Draws a bold label
		/// </summary>
		private void DrawBoldLabel(string text)
		{
			EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
		}

		/// <summary>
		/// Draws a simple 1 pixel height line
		/// </summary>
		private void DrawLine()
		{
			GUIStyle lineStyle			= new GUIStyle();
			lineStyle.normal.background	= LineTexture;

			GUILayout.BeginVertical(lineStyle);
			GUILayout.Space(1);
			GUILayout.EndVertical();
		}

		#endregion // Draw Methods

		#region Private Methods
		
		private void GenerateLevelFiles()
		{
			seed = Random.Range(0, int.MaxValue);

			puzzleCreatorWorker = new PuzzleCreatorWorker(
				numLevels,
				minNumShapes,
				maxNumShapes,
				string.IsNullOrEmpty(filename) ? "level" : filename,
				GetOutputFolderFullPath(),
				overwriteLevels,
				new System.Random(seed));

			puzzleCreatorWorker.StartWorker();
		}

		private void PrintErrorMessage(string error)
		{
			string errorMessage = "An unexpected error occurred while generating levels. Please send this log message to dotmobstudio@gmail.com\n\n";

			errorMessage += "=== Values ===\n";
			errorMessage += "minShapes: " + minNumShapes + "\n";
			errorMessage += "maxShapes: " + minNumShapes + "\n";
			errorMessage += "numLevels: " + numLevels + "\n";
			errorMessage += "seed: " + seed + "\n\n";

			errorMessage += "=== Error ===\n";
			errorMessage += error;

			Debug.LogError(errorMessage);
		}
		
		#endregion // Private Methods
	}
}
