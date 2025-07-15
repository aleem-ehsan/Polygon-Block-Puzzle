using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	[RequireComponent(typeof(CanvasRenderer))]

	public class GridImage : Graphic
	{


		#region Member Variables
		
		private int		gridSize;
		private float	thickness;
		
		#endregion // Member Variables

		#region Properties
		
		public int		GridSize	{ get { return gridSize; } set { gridSize = value; SetAllDirty(); } }
		public float	Thickness	{ get { return gridSize; } set { thickness = value; SetAllDirty(); } }
		
		#endregion // Properties

		#region Protected Methods

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (gridSize > 1)
			{
				float	xPivotOffset	= rectTransform.pivot.x * rectTransform.rect.width;
				float	yPivotOffset	= rectTransform.pivot.y * rectTransform.rect.height;
				Vector2	pivotOffset		= new Vector2(xPivotOffset, yPivotOffset);

				float cellWidth		= rectTransform.rect.width / gridSize;
				float cellHeight	= rectTransform.rect.height / gridSize;

				float halfThickness = thickness / 2f;

				int tIndex = 0;

				for (int i = 1; i < gridSize; i++)
				{
					// Add the vertical grid line
					float xPos = i * cellWidth;

					Vector2 bl = new Vector2(xPos - halfThickness, 0);
					Vector2 br = new Vector2(xPos + halfThickness, 0);
					Vector2 tl = new Vector2(xPos - halfThickness, rectTransform.rect.height);
					Vector2 tr = new Vector2(xPos + halfThickness, rectTransform.rect.height);

					AddGridLine(vh, pivotOffset, bl, br, tl, tr, tIndex);

					tIndex += 4;

					// Add the horizontal grid line
					float yPos = i * cellHeight;

					bl = new Vector2(0, yPos - halfThickness);
					br = new Vector2(rectTransform.rect.width, yPos - halfThickness);
					tl = new Vector2(0, yPos + halfThickness);
					tr = new Vector2(rectTransform.rect.width, yPos + halfThickness);

					AddGridLine(vh, pivotOffset, bl, br, tl, tr, tIndex);

					tIndex += 4;
				}
			}
		}

		private void AddGridLine(VertexHelper vh, Vector2 pivotOffset, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, int tIndex)
		{
			AddVert(vh, bl, pivotOffset);
			AddVert(vh, br, pivotOffset);
			AddVert(vh, tl, pivotOffset);
			AddVert(vh, tr, pivotOffset);

			vh.AddTriangle(tIndex, tIndex + 1, tIndex + 2);
			vh.AddTriangle(tIndex + 1, tIndex + 3, tIndex + 2);
		}

		private void AddVert(VertexHelper vh, Vector2 vert, Vector2 pivotOffset)
		{
			vh.AddVert(vert - pivotOffset, color, Vector2.zero);
		}

		#endregion // Protected Methods
	}
}
