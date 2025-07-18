using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dotmob.PolygonPuzzle
{
	public class PolygonSprite : Image
	{
		#region Member Variables
		
		private float		scale;
		private PolygonData	polygonData;
		private float		boardSize;
		private Vector2		scaledSpriteSize;
		
		#endregion // Member Variables

		#region Public Methods

		public void Setup(PolygonData polygonData, float scale, float boardSize)
		{
			this.scale			= scale;
			this.polygonData	= polygonData;
			this.boardSize		= boardSize;

			rectTransform.sizeDelta = polygonData.gridBounds.size * scale;

			// Get how much we need to scale the sprite so that it envelops the board
			if (sprite != null)
			{
				float boardScale = sprite.rect.width < sprite.rect.height ? boardSize / sprite.rect.width : boardSize / sprite.rect.height;

				// Get the sprites width/height if placed on the board
				scaledSpriteSize = new Vector2(sprite.rect.width * boardScale, sprite.rect.height * boardScale);
			}
			else
			{
				// If no sprite is assigned, use a default size
				scaledSpriteSize = new Vector2(boardSize, boardSize);
			}

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
			float x = trianglePoint.x + polygonData.gridBounds.xMin * scale + (scaledSpriteSize.x - boardSize) / 2f;
			float y = trianglePoint.y + polygonData.gridBounds.yMin * scale + (scaledSpriteSize.y - boardSize) / 2f;

			// Avoid division by zero
			float uvX = scaledSpriteSize.x > 0 ? x / scaledSpriteSize.x : 0f;
			float uvY = scaledSpriteSize.y > 0 ? y / scaledSpriteSize.y : 0f;

			return new Vector2(uvX, uvY);
		}

		#endregion // Protected Methods
	}
} 