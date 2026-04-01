using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob.PolygonPuzzle
{
	public class TriangleData
	{
		public Vector2 p1;
		public Vector2 p2;
		public Vector2 p3;

		public Rect Bounds { get; private set; }

		public TriangleData(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			this.p1 = p1;
			this.p2 = p2;
			this.p3 = p3;

			float minX = Mathf.Min(p1.x, p2.x, p3.x);
			float maxX = Mathf.Max(p1.x, p2.x, p3.x);
			float minY = Mathf.Min(p1.y, p2.y, p3.y);
			float maxY = Mathf.Max(p1.y, p2.y, p3.y);

			Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
		}

		public override string ToString()
		{
			return string.Format("a:{0} - b:{1} - c:{2}", p1, p2, p3);
		}
	}
}
