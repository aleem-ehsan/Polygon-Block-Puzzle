using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob.PolygonPuzzle
{
	public class PuzzleCreatorWorker : Worker
	{
		#region Member Variables
		
		private int				numLevels;
		private int				minShapes;
		private int				maxShapes;
		private string			filenamePrefix;
		private string			outFolderPath;
		private bool			overwrite;
		private System.Random	random;

		private int numLevelsGenerated;
		
		#endregion // Member Variables

		#region Public Methods
		
		public PuzzleCreatorWorker(int numLevels, int minShapes, int maxShapes, string filenamePrefix, string outFolderPath, bool overwrite, System.Random random)
		{
			this.numLevels		= numLevels;
			this.minShapes		= minShapes;
			this.maxShapes		= maxShapes;
			this.filenamePrefix	= filenamePrefix;
			this.outFolderPath	= outFolderPath;
			this.overwrite		= overwrite;
			this.random			= random;
		}
		
		#endregion // Public Methods

		#region Protected Methods

		protected override void Begin()
		{
			numLevelsGenerated = 0;
		}

		protected override void DoWork()
		{
			// Pick a random number of shapes to use
			int numShapes = random.Next(minShapes, maxShapes + 1);

			// Generate the puzzle
			Puzzle puzzle = PuzzleCreator.CreatePuzzle(numShapes, random);

			// Split up each polygon into triangles that we will use to build the shapes in the game
			List<List<Point2D>> polygonTriangles = GetTriangles(puzzle.polygons);

			// Create the json string that makes up the level files contents
			string contents = CreateLevelFileContents(puzzle, polygonTriangles);
			string filename = GetFilename();
			string filePath = outFolderPath + "/" + filename + ".json";

			System.IO.File.WriteAllText(filePath, contents);

			numLevelsGenerated++;

			Progress = ((float)numLevelsGenerated / (float)numLevels);

			if (numLevelsGenerated >= numLevels)
			{
				Stop();
			}
		}
		
		#endregion // Protected Methods

		#region Private Methods

		private string GetFilename()
		{
			if (overwrite)
			{
				if (numLevelsGenerated == 0)
				{
					return filenamePrefix;
				}
				else
				{
					return filenamePrefix + "_" + numLevelsGenerated;
				}
			}
			else
			{
				return GetUniqueFilename();
			}
		}

		private string GetUniqueFilename()
		{
			for (int i = numLevelsGenerated;; i++)
			{
				string filename;

				if (i == 0)
				{
					filename = filenamePrefix;
				}
				else
				{
					filename = filenamePrefix + "_" + i;
				}

				if (!System.IO.File.Exists(string.Format("{0}/{1}.json", outFolderPath, filename)))
				{
					return filename;
				}
			}
		}

		/// <summary>
		/// Sets a list of points that make up the triangles for each polygon
		/// </summary>
		private List<List<Point2D>> GetTriangles(List<Polygon> polygons)
		{
			List<List<Point2D>> polygonTriangles = new List<List<Point2D>>();

			for (int i = 0; i < polygons.Count; i++)
			{
				Polygon polygon = polygons[i];

				SetPointsClockwise(polygon);

				polygonTriangles.Add(GetTriangles(polygon));
			}

			return polygonTriangles;
		}

		/// <summary>
		/// Sets the points and edges lists so that the points are in a clockwise direction
		/// </summary>
		private void SetPointsClockwise(Polygon polygon)
		{
			// First sort the edges of the polygon so that they are all adjacent
			for (int i = 0; i < polygon.edges.Count - 1; i++)
			{
				Edge2D edge1 = polygon.edges[i];

				int index = -1;

				for (int j = i + 1; j < polygon.edges.Count; j++)
				{
					Edge2D edge2 = polygon.edges[j];

					if (edge1.point2.Equals(edge2.point1))
					{
						index = j;
						break;
					}
					else if (edge1.point2.Equals(edge2.point2))
					{
						Point2D temp = edge2.point2;

						edge2.point2 = edge2.point1;
						edge2.point1 = temp;

						index = j;
						break;
					}
				}

				Edge2D edgeSwap1 = polygon.edges[i + 1];
				Edge2D edgeSwap2 = polygon.edges[index];

				polygon.edges[i + 1] = edgeSwap2;
				polygon.edges[index] = edgeSwap1;
			}

			// We are now going to re-build the points list
			polygon.points.Clear();

			for (int i = 0; i < polygon.edges.Count; i++)
			{
				polygon.points.Add(polygon.edges[i].point1);
			}

			// Check if the list of points are clockwise, if not reverse the list
			if (!IsClockwise(polygon.points))
			{
				polygon.points.Reverse();
			}
		}

		/// <summary>
		/// Returns true if the given points are arragned clockwise
		/// </summary>
		private bool IsClockwise(List<Point2D> points)
		{
			float sum = 0;

			for (int i = 0; i < points.Count; i++)
			{
				int j = (i + 1) % points.Count;

				Point2D p1 = points[i];
				Point2D p2 = points[j];

				sum += (p2.x - p1.x) * (p2.y + p1.y);
			}

			return sum > 0;
		}

		/// <summary>
		/// Splits the polygon into triangles
		/// </summary>
		private List<Point2D> GetTriangles(Polygon polygon)
		{
			List<Point2D>	trianglePoints	= new List<Point2D>();
			List<Point2D>	points			= new List<Point2D>(polygon.points);

			int i = 0;

			while (points.Count > 0)
			{
				Point2D pointToTry	= points[i];
				Point2D rPoint		= points[(i + 1) % points.Count];
				Point2D lPoint		= points[(i == 0) ? points.Count - 1 : i - 1];

				List<Point2D> triangle = new List<Point2D>() { pointToTry, rPoint, lPoint };

				if (points.Count == 3)
				{
					trianglePoints.AddRange(triangle);
					break;
				}

				// Check if the triangle is inside the polygon and the triangle does not contain any other point in the polygon
				if (IsClockwise(triangle) && !ContainsAnyOtherPoint(triangle, points))
				{
					// Add the triangles points
					trianglePoints.AddRange(triangle);

					// Remove the point from the polygon
					points.RemoveAt(i);

					// Restart at the beginning
					i = 0;
				}
				else
				{
					// The triangle is outside the polygon, try the next point
					i++;
				}
			}

			return trianglePoints;
		}

		private bool ContainsAnyOtherPoint(List<Point2D> triangle, List<Point2D> polygonPoints)
		{
			for (int i = 0; i < polygonPoints.Count; i++)
			{
				Point2D point = polygonPoints[i];

				if (triangle.Contains(point))
				{
					continue;
				}

				if (IsPointInTriangle(point, triangle[0], triangle[1], triangle[2]))
				{
					return true;
				}
			}

			return false;
		}

		private bool IsPointInTriangle(Point2D point, Point2D triPoint1, Point2D triPoint2, Point2D triPoint3)
		{
			float d1 = Sign(point, triPoint1, triPoint2);
			float d2 = Sign(point, triPoint2, triPoint3);
			float d3 = Sign(point, triPoint3, triPoint1);

			bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			bool pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(neg && pos);
		}
		
		private float Sign(Point2D p1, Point2D p2, Point2D p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		private string CreateLevelFileContents(Puzzle puzzle, List<List<Point2D>> polygonTriangles)
		{
			Dictionary<string, object> json = new Dictionary<string, object>();

			json["timestamp"]	= (ulong)System.Math.Round(Utilities.SystemTimeInMilliseconds);
			json["grid_size"]	= puzzle.gridSize;

			List<object> polygonJson = new List<object>();

			for (int i = 0; i < polygonTriangles.Count; i++)
			{
				polygonJson.Add(GetPolygonData(polygonTriangles[i]));
			}

			json["polygons"] = polygonJson;

			return Utilities.ConvertToJsonString(json);
		}

		private Dictionary<string, object> GetPolygonData(List<Point2D> polygonTriangles)
		{
			float		minX			= int.MaxValue;
			float		maxX			= int.MinValue;
			float		minY			= int.MaxValue;
			float		maxY			= int.MinValue;
			List<float>	tiranglePoints	= new List<float>();

			for (int i = 0; i < polygonTriangles.Count; i++)
			{
				float x = polygonTriangles[i].x;
				float y = polygonTriangles[i].y;

				minX = Mathf.Min(minX, x);
				maxX = Mathf.Max(maxX, x);
				minY = Mathf.Min(minY, y);
				maxY = Mathf.Max(maxY, y);
			}

			for (int i = 0; i < polygonTriangles.Count; i++)
			{
				float x = polygonTriangles[i].x - minX;
				float y = polygonTriangles[i].y - minY;

				tiranglePoints.Add(x);
				tiranglePoints.Add(y);
			}

			Dictionary<string, object> json = new Dictionary<string, object>();

			json["bounds"]			= new List<float>() { minX, minY, maxX - minX, maxY - minY };
			json["triangle_points"]	= tiranglePoints;

			return json;
		}

		#endregion // Private Methods

		#region Debug Methods

		private string PrintEdges(List<Edge2D> edges)
		{
			string prnt = "";

			for (int i = 0; i < edges.Count; i++)
			{
				prnt += string.Format("p1: {0}, p2: {1}\n", edges[i].point1, edges[i].point2);
			}

			return prnt;
		}

		private string PrintPoints(List<Point2D> points)
		{
			string prnt = "";

			for (int i = 0; i < points.Count; i++)
			{
				prnt += points[i].ToString() + "\n";
			}

			return prnt;
		}

		#endregion // Debug Methods
	}
}
