using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	public class PolygonImage : RawImage
	{
		#region Member Variables
		
		private float		scale;
		private PolygonData	polygonData;
		private float		boardSize;
		private Vector2		scaledTextureSize;
		
		#endregion // Member Variables

		#region Public Methods

		public void Setup(PolygonData polygonData, float scale, float boardSize)
		{
			this.scale			= scale;
			this.polygonData	= polygonData;
			this.boardSize		= boardSize;

			rectTransform.sizeDelta = polygonData.gridBounds.size * scale;

			// Get how much we need to scale the texture so that it envelops the board
			float boardScale = texture.width < texture.height ? boardSize / texture.width : boardSize / texture.height;

			// Get the textures width/height if placed on the board
			scaledTextureSize = new Vector2(texture.width * boardScale, texture.height * boardScale);

			SetAllDirty();
		}
		
		#endregion // Public Methods

		#region Protected Methods

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (polygonData != null)
			{
				float	xPivotOffset	= rectTransform.pivot.x * rectTransform.sizeDelta.x;
				float	yPivotOffset	= rectTransform.pivot.y * rectTransform.sizeDelta.y;
				Vector2	pivotOffset		= new Vector2(xPivotOffset, yPivotOffset);

				for (int i = 0; i < polygonData.triangles.Count; i++)
				{
					TriangleData triangleData = polygonData.triangles[i];

					AddVert(vh, triangleData.p1 * scale, pivotOffset);
					AddVert(vh, triangleData.p2 * scale, pivotOffset);
					AddVert(vh, triangleData.p3 * scale, pivotOffset);

					int tIndex = i * 3;

					vh.AddTriangle(tIndex, tIndex + 1, tIndex + 2);
				}
			}
		}

		private void AddVert(VertexHelper vh, Vector2 trianglePoint, Vector2 pivotOffset)
		{
			vh.AddVert(trianglePoint - pivotOffset, color, GetUV(trianglePoint));
		}

		private Vector2 GetUV(Vector2 trianglePoint)
		{
			float x = trianglePoint.x + polygonData.gridBounds.xMin * scale + (scaledTextureSize.x - boardSize) / 2f;
			float y = trianglePoint.y + polygonData.gridBounds.yMin * scale + (scaledTextureSize.y - boardSize) / 2f;

			float uvX = x / scaledTextureSize.x;
			float uvY = y / scaledTextureSize.y;

			return new Vector2(uvX, uvY);
		}

		#endregion // Protected Methods
	}
}
