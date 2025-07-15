using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob.PolygonPuzzle
{
	public class PolygonData
	{
		#region Member Variables
		
		public Rect					gridBounds;
		public List<TriangleData>	triangles;
		public List<Vector2>		vertices;
		
		#endregion // Member Variables
	}
}
