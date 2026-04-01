using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VoronoiLib;
using VoronoiLib.Structures;

public class Puzzle
{
	public int				gridSize	= 0;
	public List<Point2D>	points		= new List<Point2D>();
	public List<Edge2D>		edges		= new List<Edge2D>();
	public List<Polygon>	polygons	= new List<Polygon>();

	// Raw point/edge data, created/used when generating the puzzle, should only be used for debugging purposes, the variables above are the actual point/edge data for the puzzle
	public List<Point2D>		gridPoints		= new List<Point2D>();
	public List<FortuneSite>	fortuneSites	= new List<FortuneSite>();
	public LinkedList<VEdge>	vEdges			= new LinkedList<VEdge>();

	public void AddEdge(Edge2D edge)
	{
		AddPoint(edge.point1);
		AddPoint(edge.point2);

		edges.Add(edge);
	}

	public Point2D GetPoint(float x, float y)
	{
		for (int i = 0; i < points.Count; i++)
		{
			Point2D point = points[i];

			if (Mathf.Approximately(x, point.x)&& Mathf.Approximately(y, point.y))
			{
				return point;
			}
		}

		return null;
	}

	private void AddPoint(Point2D point)
	{
		for (int i = 0; i < points.Count; i++)
		{
			if (point.Equals(points[i]))
			{
				return;
			}
		}

		points.Add(point);
	}

	public void Clear()
	{
		points.Clear();
		edges.Clear();
		polygons.Clear();
		gridPoints.Clear();
		fortuneSites.Clear();
		vEdges.Clear();
	}
}

public class Polygon
{
	public List<Point2D>	points	= new List<Point2D>();
	public List<Edge2D>		edges	= new List<Edge2D>();

	public void AddEdge(Edge2D edge)
	{
		AddPoint(edge.point1);
		AddPoint(edge.point2);

		edges.Add(edge);
	}

	public void GetEdges(Point2D point, out Edge2D edge1, out Edge2D edge2)
	{
		edge1 = null;
		edge2 = null;

		for (int i = 0; i < edges.Count; i++)
		{
			Edge2D edge = edges[i];

			if (edge.HasPoint(point))
			{
				if (edge1 == null)
				{
					edge1 = edge;
				}
				else
				{
					edge2 = edge;

					return;
				}
			}
		}
	}

	private void AddPoint(Point2D point)
	{
		for (int i = 0; i < points.Count; i++)
		{
			if (point.Equals(points[i]))
			{
				return;
			}
		}

		points.Add(point);
	}
}

public class Edge2D
{
	public Point2D	point1;
	public Point2D	point2;
	public bool		isBorderEdge;

	public Edge2D(Point2D point1, Point2D point2, bool isBorderEdge = false)
	{
		this.point1			= point1;
		this.point2			= point2;
		this.isBorderEdge	= isBorderEdge;
	}

	public bool HasPoint(Point2D point)
	{
		return point.Equals(point1) || point.Equals(point2);
	}

	public float Length()
	{
		return Vector2.Distance(new Vector2(point1.x, point1.y), new Vector2(point2.x, point2.y));
	}

	public override bool Equals(object obj)
	{
		return obj.GetType() == typeof(Edge2D) && (
			((obj as Edge2D).point1.Equals(point1) && (obj as Edge2D).point2.Equals(point2)) ||
			((obj as Edge2D).point1.Equals(point2) && (obj as Edge2D).point2.Equals(point1)));
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}

public class Point2D
{
	public float x;
	public float y;

	public Point2D(float x, float y)
	{
		this.x = x;
		this.y = y;
	}

	public override string ToString()
	{
		return string.Format("[{0},{1}]", x, y);
	}

	public override bool Equals(object obj)
	{
		return obj.GetType() == typeof(Point2D) && Mathf.Approximately((obj as Point2D).x, x) && Mathf.Approximately((obj as Point2D).y, y);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}

public static class PuzzleCreator
{
	#region Public Methods

	public static Puzzle CreatePuzzle(int numShapes, System.Random random)
	{
		Puzzle puzzle = new Puzzle();

		puzzle.gridSize	= numShapes + 1;

		bool done = false;

		while (!done)
		{
			try
			{
				puzzle.Clear();

				List<float[]> possibleFortuneSites = new List<float[]>();

				// Split the puzzle into a bunch of grid points that will be used as the random fortune site points
				for (int i = 1; i <= numShapes; i++)
				{
					for (int j = 1; j <= numShapes; j++)
					{
						possibleFortuneSites.Add(new float[] { i, j } );
					}
				}

				// Pick random fortune site points to use
				for (int i = 0; i < numShapes; i++)
				{
					int index = random.Next(0, possibleFortuneSites.Count);

					float[] gridPoint = possibleFortuneSites[index];

					possibleFortuneSites.RemoveAt(index);

					puzzle.fortuneSites.Add(new FortuneSite(i, gridPoint[0], gridPoint[1]));
				}

				// Get all the edges in the puzzle for all the polygons
				puzzle.vEdges = FortunesAlgorithm.Run(puzzle.fortuneSites, 0, 0, puzzle.gridSize, puzzle.gridSize);

				done = true;

				// Check that all the points are positive, for some reason sometimes points have negative x/y values and that messes up everything
				foreach (VEdge vEdge in puzzle.vEdges)
				{
					if (vEdge.Start.X < 0 || vEdge.Start.Y < 0 || vEdge.End.X < 0 || vEdge.End.Y < 0)
					{
						// Set done to false so a new random puzzle is generated
						done = false;

						Debug.LogWarning("Fail");

						break;
					}
				}
			}
			catch (System.Exception)
			{
				// The FortunesAlgorithm algo sometimes crashes, not sure why, this is an easy way of just restarting it so it will eventaully generate a puzzle that doesn't creash
			}
		}

		// Create the polygons using the generated edges
		PopulatePolygons(puzzle, puzzle.vEdges);

		// The polygons that are on the border of the puzzle will not have edges along the border so we need to add those edges
		ConnectBorderLines(puzzle);

		// Snape all the points so they are on a grid point
		SnapPointsToGrid(puzzle);

		// Remove duplicated points and edges where the 2 points are the same
		RemovePoints(puzzle);

		return puzzle;
	}

	#endregion

	#region Protected Methods

	#endregion

	#region Private Methods

	#endregion

	private static void PopulatePolygons(Puzzle puzzle, LinkedList<VEdge> vEdges)
	{
		Dictionary<int, Polygon> tempPolygons = new Dictionary<int, Polygon>();

		foreach (VEdge vEdge in vEdges)
		{
			if (!tempPolygons.ContainsKey(vEdge.Left.Id))
			{
				tempPolygons.Add(vEdge.Left.Id, new Polygon());
			}

			if (!tempPolygons.ContainsKey(vEdge.Right.Id))
			{
				tempPolygons.Add(vEdge.Right.Id, new Polygon());
			}

			Point2D point1 = puzzle.GetPoint((float)vEdge.Start.X, (float)vEdge.Start.Y);

			if (point1 == null)
			{
				point1 = new Point2D((float)vEdge.Start.X, (float)vEdge.Start.Y);
			}

			Point2D point2 = puzzle.GetPoint((float)vEdge.End.X, (float)vEdge.End.Y);

			if (point2 == null)
			{
				point2 = new Point2D((float)vEdge.End.X, (float)vEdge.End.Y);
			}

			if (!point1.Equals(point2))
			{
				Edge2D edge = new Edge2D(point1, point2);

				puzzle.AddEdge(edge);

				tempPolygons[vEdge.Left.Id].AddEdge(edge);
				tempPolygons[vEdge.Right.Id].AddEdge(edge);
			}
		}

		foreach (KeyValuePair<int, Polygon> pair in tempPolygons)
		{
			puzzle.polygons.Add(pair.Value);
		}
	}

	/// <summary>
	/// Connects the lines along the border for each polygon
	/// </summary>
	private static void ConnectBorderLines(Puzzle puzzle)
	{
		for (int i = 0; i < puzzle.polygons.Count; i++)
		{
			Polygon polygon = puzzle.polygons[i];

			List<Point2D> unconnectedPoints;

			// If a polygon is not closed then some of its edges connect with the border of the puzzle, we need to create more edges along
			// the border of the puzzle to connect the polygons unconnected points
			if (!IsPolygonClosed(polygon, out unconnectedPoints))
			{
				// First connect any points that are on the same border side (top, bottom, left, right)
				ConnectSameLinePoints(puzzle, polygon, unconnectedPoints);

				// If there are any points left in unconnectedPoints after calling ConnectSameLinePoints then those points are on different sides
				// of the puzzle so we need to connect them with a corner of the puzzle
				if (unconnectedPoints.Count == 2)
				{
					// If there are only two unconnected points then simply connect those points
					ConnectPolygon(puzzle, polygon, unconnectedPoints[0], unconnectedPoints[1], puzzle.gridSize);
				}
				else if (unconnectedPoints.Count == 4)
				{
					// If there are 4 unconnected points then we need to get the pairs that are not part of the same line
					List<List<Point2D>> pointPairs = GetUnconnectedPointPairs(polygon, unconnectedPoints);

					for (int j = 0; j < pointPairs.Count; j++)
					{
						ConnectPolygon(puzzle, polygon, pointPairs[j][0], pointPairs[j][1], puzzle.gridSize);
					}
				}
			}
		}
	}

	/// <summary>
	/// Checks if the polygon is closed. A polygon is closed if all vertices are connected to 2 edges
	/// </summary>
	private static bool IsPolygonClosed(Polygon polygon, out List<Point2D> unconnectedPoints)
	{
		Dictionary<string, List<Point2D>> pointMap = new Dictionary<string, List<Point2D>>();

		for (int i = 0; i < polygon.edges.Count; i++)
		{
			IsPolygonClosedHelper(pointMap, polygon.edges[i].point1);
			IsPolygonClosedHelper(pointMap, polygon.edges[i].point2);
		}

		bool connected = true;

		unconnectedPoints = new List<Point2D>();

		foreach (KeyValuePair<string, List<Point2D>> pair in pointMap)
		{
			if (pair.Value.Count == 1)
			{
				connected = false;

				unconnectedPoints.Add(pair.Value[0]);
			}
		}

		return connected;
	}

	/// <summary>
	/// Helper method to IsPolygonClosed. Adds the point to the pointMap
	/// </summary>
	private static void IsPolygonClosedHelper(Dictionary<string, List<Point2D>> pointMap, Point2D point)
	{
		string pointKey = string.Format("{0}_{1}", point.x, point.y);

		if (!pointMap.ContainsKey(pointKey))
		{
			pointMap.Add(pointKey, new List<Point2D>());
		}

		pointMap[pointKey].Add(point);
	}

	/// <summary>
	/// Connects the given polygon along the border of the puzzle from point1 to point2
	/// </summary>
	private static void ConnectPolygon(Puzzle puzzle, Polygon polygon, Point2D point1, Point2D point2, float puzzleSize)
	{
		int index = puzzle.polygons.IndexOf(polygon);

		puzzle.polygons.RemoveAt(index);

		List<Point2D> connectionPath = GetPointPath(puzzle.polygons, point1, point2, puzzleSize);

		if (connectionPath == null)
		{
			connectionPath = null;

			int n = 0;

			n = 1991;

			n++;
		}

		puzzle.polygons.Insert(index, polygon);

		for (int i = 0; i < connectionPath.Count - 1; i++)
		{
			Point2D edgeStart	= connectionPath[i];
			Point2D edgeEnd		= connectionPath[i + 1];

			// Add the new edge to the polygon
			Edge2D edge = new Edge2D(edgeStart, edgeEnd, true);

			polygon.edges.Add(edge);
			puzzle.edges.Add(edge);

			// The first point in the list will always be point1 which is already added to the list of points in this polygon
			if (i != 0)
			{
				polygon.points.Add(edgeStart);
				puzzle.points.Add(edgeStart);
			}

			// The last point in the list will always be point2 which is already added to the list of points in this polygon
			if (i + 1 != connectionPath.Count - 1)
			{
				polygon.points.Add(edgeEnd);
				puzzle.points.Add(edgeEnd);
			}
		}
	}

	/// <summary>
	/// Gets the corner points required to connect point1 and point2 assuming those points are on the border of the puzzle
	/// </summary>
	private static List<Point2D> GetPointPath(List<Polygon> polygons, Point2D point1, Point2D point2, float puzzleSize)
	{
		// Try and get the path in the clockwise direction
		List<Point2D> path = GetPathAlongBorder(polygons, point1, point2, puzzleSize, true);

		if (path == null)
		{
			// If we could not get the path in the clockwise direction try and get it in the counter-clocksize direction
			path = GetPathAlongBorder(polygons, point1, point2, puzzleSize, false);
		}

		return path;
	}

	/// <summary>
	/// Gets the list of points required to take to get from point1 to point2 along the border of the puzzle in get given direction
	/// </summary>
	private static List<Point2D> GetPathAlongBorder(List<Polygon> polygons, Point2D point1, Point2D point2, float puzzleSize, bool clockwise)
	{
		List<Point2D> path = new List<Point2D>();

		Point2D nextStart = point1;

		path.Add(nextStart);

		// Try getting a path from point1 to point2 along the border of the puzzle in the clockwise direction
		while (true)
		{
			// Get the next corner to use
			Point2D nextCorner = clockwise ? GetNextCornerClockwise(nextStart, puzzleSize) : GetNextCornerCounterClockwise(nextStart, puzzleSize);

			// Check if point2 is on the line between nextStart and nextCorner, if so then the path is completed
			if (IsPointOnLine(point2, nextStart, nextCorner) || nextCorner.Equals(point2))
			{
				// Check if there are any other points between nextStart and point2, is so then we cannot connect point1 and point2 clockwise
				if (IsPointOnLine(polygons, nextStart, point2))
				{
					return null;
				}
				else
				{
					path.Add(point2);

					return path;
				}
			}
			// Check if there is any other point between nextStart and nextCorner
			else if (IsPointOnLine(polygons, nextStart, nextCorner) || AnyPolygonContainPoint(polygons, nextCorner))
			{
				return null;
			}

			path.Add(nextCorner);

			nextStart = nextCorner;
		}
	}

	private static Point2D GetNextCornerClockwise(Point2D point, float puzzleSize)
	{
		// Top-Right
		if (Mathf.Approximately(point.y, puzzleSize) && point.x < puzzleSize)
		{
			return new Point2D(puzzleSize, puzzleSize);
		}
		// Bottom-Right
		else if (Mathf.Approximately(point.x, puzzleSize) && point.y > 0)
		{
			return new Point2D(puzzleSize, 0);
		}
		// Bottom-Left
		else if (Mathf.Approximately(point.y, 0) && point.x > 0)
		{
			return new Point2D(0, 0);
		}
		// Top-Left
		else if (Mathf.Approximately(point.x, 0) && point.y < puzzleSize)
		{
			return new Point2D(0, puzzleSize);
		}

		return null;
	}

	private static Point2D GetNextCornerCounterClockwise(Point2D point, float puzzleSize)
	{
		// Top-Left
		if (Mathf.Approximately(point.y, puzzleSize) && point.x > 0)
		{
			return new Point2D(0, puzzleSize);
		}
		// Bottom-Left
		else if (Mathf.Approximately(point.x, 0) && point.y > 0)
		{
			return new Point2D(0, 0);
		}
		// Bottom-Right
		else if (Mathf.Approximately(point.y, 0) && point.x < puzzleSize)
		{
			return new Point2D(puzzleSize, 0);
		}
		// Top-Right
		else if (Mathf.Approximately(point.x, puzzleSize) && point.y < puzzleSize)
		{
			return new Point2D(puzzleSize, puzzleSize);
		}

		return null;
	}

	/// <summary>
	/// Checks if there is any points on the line created by connecting linePoint1 and linePoint2
	/// </summary>
	private static bool IsPointOnLine(List<Polygon> polygons, Point2D linePoint1, Point2D linePoint2)
	{
		for (int i = 0; i < polygons.Count; i++)
		{
			List<Point2D> points = polygons[i].points;

			for (int j = 0; j < points.Count; j++)
			{
				Point2D point = points[j];

				if (IsPointOnLine(point, linePoint1, linePoint2))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool IsPointOnLine(Point2D point, Point2D linePoint1, Point2D linePoint2)
	{
		if (Mathf.Approximately(linePoint1.x, linePoint2.x))
		{
			return Mathf.Approximately(point.x, linePoint1.x) &&
				        ((point.y > linePoint1.y && point.y < linePoint2.y) ||
				         (point.y < linePoint1.y && point.y > linePoint2.y));
		}
		else if (Mathf.Approximately(linePoint1.y, linePoint2.y))
		{
			return Mathf.Approximately(point.y, linePoint1.y) &&
				        ((point.x > linePoint1.x && point.x < linePoint2.x) ||
				         (point.x < linePoint1.x && point.x > linePoint2.x));
		}

		return false;
	}

	private static bool AnyPolygonContainPoint(List<Polygon> polygons, Point2D point)
	{
		for (int i = 0; i < polygons.Count; i++)
		{
			Polygon polygon = polygons[i];

			for (int j = 0; j < polygon.points.Count; j++)
			{
				Point2D polygonPoint = polygon.points[j];

				if (point.Equals(polygonPoint))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static void ConnectSameLinePoints(Puzzle puzzle, Polygon polygon, List<Point2D> unconnectedPoints)
	{
		for (int i = 0; i < unconnectedPoints.Count; i++)
		{
			Point2D point1 = unconnectedPoints[i];

			for (int j = i + 1; j < unconnectedPoints.Count; j++)
			{
				Point2D point2 = unconnectedPoints[j];

				bool onLeftRight = Mathf.Approximately(point1.x, point2.x) && (Mathf.Approximately(point1.x, 0f) || Mathf.Approximately(point1.x, puzzle.gridSize));
				bool onTopBottom = Mathf.Approximately(point1.y, point2.y) && (Mathf.Approximately(point1.y, 0f) || Mathf.Approximately(point1.y, puzzle.gridSize));

				if (onLeftRight || onTopBottom)
				{
					Edge2D edge = new Edge2D(point1, point2, true);

					polygon.edges.Add(edge);
					puzzle.edges.Add(edge);

					unconnectedPoints.RemoveAt(j);
					unconnectedPoints.RemoveAt(i);

					i--;

					break;
				}
			}
		}
	}

	private static List<List<Point2D>> GetUnconnectedPointPairs(Polygon polygon, List<Point2D> unconnectedPoints)
	{
		List<List<Point2D>> unconnectedPointPairs = new List<List<Point2D>>();

		while (unconnectedPoints.Count > 0)
		{
			Point2D point1 = unconnectedPoints[0];

			List<Point2D> pair = new List<Point2D>();

			pair.Add(point1);

			for (int i = 1; i < unconnectedPoints.Count; i++)
			{
				Point2D point2 = unconnectedPoints[i];

				bool isPair = true;

				for (int j = 0; j < polygon.edges.Count; j++)
				{
					Edge2D edge = polygon.edges[j];

					if ((point1.Equals(edge.point1) && point2.Equals(edge.point2)) ||
					    (point1.Equals(edge.point2) && point2.Equals(edge.point1)))
					{
						isPair = false;

						break;
					}
				}

				if (isPair)
				{
					pair.Add(point2);

					unconnectedPointPairs.Add(pair);

					unconnectedPoints.Remove(point1);
					unconnectedPoints.Remove(point2);

					break;
				}
			}
		}

		return unconnectedPointPairs;
	}

	// Gets the polygons with the given edge
	private static List<Polygon> GetPolygonsWithEdge(Edge2D edge, List<Polygon> polygons)
	{
		List<Polygon> polygonsWithEdge = new List<Polygon>();

		for (int i = 0; i < polygons.Count; i++)
		{
			Polygon polygon = polygons[i];

			for (int j = 0; j < polygon.edges.Count; j++)
			{
				Edge2D polygonEdge = polygon.edges[j];

				if (edge.Equals(polygonEdge))
				{
					polygonsWithEdge.Add(polygon);

					break;
				}
			}

			// There can only be at most 2 polygons that share an edge
			if (polygonsWithEdge.Count == 2)
			{
				break;
			}
		}

		return polygonsWithEdge;
	}

	private static void SnapPointsToGrid(Puzzle puzzle)
	{
		puzzle.gridPoints = new List<Point2D>();

		for (int x = 0; x <= puzzle.gridSize; x++)
		{
			for (int y = 0; y <= puzzle.gridSize; y++)
			{
				puzzle.gridPoints.Add(new Point2D(x, y));
			}
		}

		for (int i = 0; i < puzzle.points.Count; i++)
		{
			Point2D point = puzzle.points[i];

			SnapPointToGrid(puzzle, point, puzzle.gridPoints);
		}
	}

	private static void SnapPointToGrid(Puzzle puzzle, Point2D point, List<Point2D> gridPoints)
	{
		// Sort the grid points so the closest ones to point are at the front
		gridPoints.Sort((Point2D point1, Point2D point2) =>
		{
			float point1Dist = Vector2.Distance(new Vector2(point.x, point.y), new Vector2(point1.x, point1.y));
			float point2Dist = Vector2.Distance(new Vector2(point.x, point.y), new Vector2(point2.x, point2.y));

			if (point1Dist < point2Dist) return -1;
			if (point1Dist > point2Dist) return 1;

			return 0;
		});

		if (point.Equals(gridPoints[0]))
		{
			// Point is already on the a grid point
			return;
		}

		for (int i = 0; i < gridPoints.Count; i++)
		{
			if (TryMovePointToGridPoint(puzzle, point, gridPoints[i]))
			{
				break;
			}
		}
	}

	private static bool TryMovePointToGridPoint(Puzzle puzzle, Point2D point, Point2D gridPoint)
	{
		float pointX = point.x;
		float pointY = point.y;

		point.x = gridPoint.x;
		point.y = gridPoint.y;

		// Check if any polygons now have two neighbouring edges that are parallel, is so then we cannot move the point to this grid point
		for (int i = 0; i < puzzle.polygons.Count; i++)
		{
			Polygon polygon = puzzle.polygons[i];

			for (int j = 0; j < polygon.points.Count; j++)
			{
				Point2D p = polygon.points[j];

				Edge2D edge1;
				Edge2D edge2;

				polygon.GetEdges(p, out edge1, out edge2);

				Vector2 dir1, dir2;

				dir1 = (p.Equals(edge1.point1))
					? new Vector2(edge1.point2.x - p.x, edge1.point2.y - p.y)
					: new Vector2(edge1.point1.x - p.x, edge1.point1.y - p.y);
				
				dir2 = (p.Equals(edge2.point1))
					? new Vector2(edge2.point2.x - p.x, edge2.point2.y - p.y)
					: new Vector2(edge2.point1.x - p.x, edge2.point1.y - p.y);

				if (dir1.normalized == dir2.normalized || Vector2.Angle(dir1, dir2) <= 20 || Vector2.Angle(dir2, dir1) <= 20)
				{
					point.x = pointX;
					point.y = pointY;

					return false;
				}
			}
		}

		return true;
	}

	private static void RemovePoints(Puzzle puzzle)
	{
		RemovePoints(puzzle.points);
		RemovePoints(puzzle.edges);

		for (int i = 0; i < puzzle.polygons.Count; i++)
		{
			Polygon polygon = puzzle.polygons[i];

			for (int j = 0; j < polygon.points.Count; j++)
			{
				RemovePoints(polygon.points);
				RemovePoints(polygon.edges);
			}
		}
	}

	private static void RemovePoints(List<Point2D> points)
	{
		for (int i = 0; i < points.Count; i++)
		{
			for (int j = i + 1; j < points.Count; j++)
			{
				if (points[i].Equals(points[j]))
				{
					points.RemoveAt(j);
				}
			}
		}
	}

	private static void RemovePoints(List<Edge2D> edges)
	{
		for (int i = 0; i < edges.Count; i++)
		{
			if (edges[i].point1.Equals(edges[i].point2))
			{
				edges.RemoveAt(i);
				i--;
			}
		}
	}
}
