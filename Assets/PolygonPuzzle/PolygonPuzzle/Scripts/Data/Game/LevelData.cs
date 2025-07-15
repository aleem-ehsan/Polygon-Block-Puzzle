using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob.PolygonPuzzle
{
	public class LevelData
	{
		#region Member Variables

		private TextAsset	levelFile;
		private string		levelFileText;
		private bool		isLevelFileParsed;

		// Values parsed from level file
		private string	timestamp;
		private int		gridSize;

		private List<PolygonData> polygonDatas;

		#endregion

		#region Properties

		public string	Id			{ get; private set; }
		public string	PackId		{ get; private set; }
		public int		LevelIndex	{ get; private set; }

		public string				Timestamp		{ get { if (!isLevelFileParsed) ParseLevelFile(); return timestamp; } }
		public int					GridSize		{ get { if (!isLevelFileParsed) ParseLevelFile(); return gridSize; } }
		public List<PolygonData>	PolygonDatas	{ get { if (!isLevelFileParsed) ParseLevelFile(); return polygonDatas; } }

		private string LevelFileText
		{
			get
			{
				if (string.IsNullOrEmpty(levelFileText) && levelFile != null)
				{
					levelFileText	= levelFile.text;
					levelFile		= null;
				}

				return levelFileText;
			}
		}

		#endregion

		#region Constructor

		public LevelData(TextAsset levelFile, string packId, int levelIndex)
		{
			this.levelFile	= levelFile;
			PackId			= packId;
			LevelIndex		= levelIndex;
			Id				= string.Format("{0}_{1}", packId, levelIndex);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Parse the json in the level file
		/// </summary>
		private void ParseLevelFile()
		{
			if (isLevelFileParsed) return;

			string levelFileContents = LevelFileText;

			JSONNode json = JSON.Parse(levelFileContents);

			if (json != null)
			{
				timestamp	= json["timestamp"].Value;
				gridSize	= json["grid_size"].AsInt;

				polygonDatas = new List<PolygonData>();

				foreach (JSONNode polygonJson in json["polygons"].AsArray)
				{
					PolygonData polygonData = new PolygonData();

					ParsePolygonBounds(polygonData, polygonJson["bounds"].AsArray);
					ParsePolygonTriangles(polygonData, polygonJson["triangle_points"].AsArray);

					polygonDatas.Add(polygonData);
				}
			}

			isLevelFileParsed = true;
		}

		private void ParsePolygonBounds(PolygonData polygonData, JSONArray boundsJson)
		{
			Vector2 pos		= new Vector2(boundsJson[0].AsFloat, boundsJson[1].AsFloat);
			Vector2 size	= new Vector2(boundsJson[2].AsFloat, boundsJson[3].AsFloat);

			polygonData.gridBounds = new Rect(pos, size);
		}

		private void ParsePolygonTriangles(PolygonData polygonData, JSONArray trianglePointsJson)
		{
			List<TriangleData>	triangleDatas	= new List<TriangleData>();
			List<Vector2>		vertices		= new List<Vector2>();

			for (int i = 0; i < trianglePointsJson.Count; i += 6)
			{
				Vector2 p1 = new Vector2(trianglePointsJson[i], trianglePointsJson[i + 1]);
				Vector2 p2 = new Vector2(trianglePointsJson[i + 2], trianglePointsJson[i + 3]);
				Vector2 p3 = new Vector2(trianglePointsJson[i + 4], trianglePointsJson[i + 5]);

				TriangleData triangleData = new TriangleData(p1, p2, p3);

				triangleDatas.Add(triangleData);

				if (!vertices.Contains(triangleData.p1)) vertices.Add(triangleData.p1);
				if (!vertices.Contains(triangleData.p2)) vertices.Add(triangleData.p2);
				if (!vertices.Contains(triangleData.p3)) vertices.Add(triangleData.p3);
			}

			polygonData.triangles	= triangleDatas;
			polygonData.vertices	= vertices;
		}

		#endregion
	}
}
