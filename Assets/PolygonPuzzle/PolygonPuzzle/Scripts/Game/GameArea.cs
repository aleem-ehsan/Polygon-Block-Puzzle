using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace dotmob.PolygonPuzzle
{
	[ExecuteInEditMode]
	public class GameArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		#region Classes

		private class PolygonObject
		{
			public int				index					= 0;
			public PolygonSprite		polygonSprite			= null;
			public RectTransform	polygonContainerMarker	= null;
			public bool				isOnBoard				= false;
			public Vector2			gridPosition			= Vector2.zero;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private PolygonSprite 		polygonSpritePrefab		= null;
		[SerializeField] private RectTransform		boardContainer			= null;
		[SerializeField] private GridLayoutGroup	polygonContainer		= null;
		[SerializeField] private float				spaceBetweenContainers	= 25f;
		[Space]
		[SerializeField] private bool				showGridLines 		= true;
		[SerializeField] private Color				gridLineColor 		= Color.white;
		[SerializeField] private float				gridLineThickness 	= 4;
		[Space]
		[SerializeField] private int				polygonsInRow 		= 5;
		[SerializeField] private bool				polygonsFitCell		= true;
		[SerializeField] private float				polygonGhostAlpha	= 0.5f;
		[SerializeField] private List<Color> 		polygonColors 		= null;
		[SerializeField] private List<Sprite> 		polygonSprites 		= null;
		[Space]
		[SerializeField] private float				hintMinAlpha		= 0.5f;
		[SerializeField] private float				hintMaxAlpha		= 0.5f;
		[SerializeField] private AnimationCurve		hintAnimCurve		= null;
		[SerializeField] private float				hintAnimDuration	= 1f;	

		#endregion

		#region Member Variables

		private ObjectPool				polygonSpritePool;
		private RectTransform 			placedPolygonsContainer;
		private RectTransform 			polygonHintsContainer;
		private GridImage				gridImage;

		private LevelData				activeLevelData;
		private float					gridCellSize;
		private List<PolygonObject>		polygonObjects;
		private List<PolygonObject>		hintPolygonObjects;
		private float					polygonContainerScale;
		private float					polygonScale;

		private bool					isPointerActive;
		private int						activePointerId;
		private PolygonObject			activePolygonObject;
		private PolygonObject			activePolygonObjectGhost;

		#endregion

		#region Properties

		private RectTransform RectT					{ get { return transform as RectTransform; } }
		private RectTransform PolygonContainerRectT	{ get { return polygonContainer.transform as RectTransform; } }

		#endregion

		#region Unity Methods

		#endregion

		#region Public Variables

		public void Initialize()
		{
			polygonObjects		= new List<PolygonObject>();
			hintPolygonObjects	= new List<PolygonObject>();

			polygonSpritePool = new ObjectPool(polygonSpritePrefab.gameObject, 2, ObjectPool.CreatePoolContainer(transform, "polygon_sprite_pool"));

			// Create the two contains that will hold the polygons that are placed on the board
			placedPolygonsContainer	= CreateBoardContainer("placed_polygons_container");
			polygonHintsContainer	= CreateBoardContainer("polygon_hints_container");

			// Create a GridImage on the placedPolygonsContainer if we are to show grid lines
			if (showGridLines)
			{
				gridImage			= placedPolygonsContainer.gameObject.AddComponent<GridImage>();
				gridImage.color		= gridLineColor;
				gridImage.Thickness	= gridLineThickness;
			}

			GameEventManager.Instance.RegisterEventHandler(GameEventManager.ActiveLevelCompletedEventId, OnActiveLevelCompleted);
		}

		public void SetupLevel(LevelData levelData, LevelSaveData levelSaveData)
		{
			activeLevelData = levelData;

			SizeAndPositionContainers();

			// Clear the UI from the previous game
			Clear();

			polygonScale = boardContainer.rect.width / levelData.GridSize;

			if (showGridLines)
			{
				gridImage.GridSize = activeLevelData.GridSize;
			}

			SetupPolygonContainer(levelData.PolygonDatas.Count);

			CreatePolygonImages(levelData);

			// For each polygon object, if it is placed in the save data, move it to the board at it's placed position
			for (int i = 0; i < polygonObjects.Count; i++)
			{
				// Check if it's been placed
				if (levelSaveData.placedPositions.ContainsKey(i))
				{
					// Get the placed position
					Vector3 gridPosition = levelSaveData.placedPositions[i];

					MovePolygonToBoard(polygonObjects[i], gridPosition, false);
				}

				// Check if a hint was used for it, if so then display a hint
				if (levelSaveData.hintsDisplayed.Contains(i))
				{
					DisplayHint(i);
				}
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			// If there is already an active pointer or the active level data is now null then ignore this event
			if (isPointerActive || activeLevelData == null)
			{
				return;
			}

			isPointerActive = true;
			activePointerId = eventData.pointerId;

			Vector2 mouseScreenPosition = eventData.position;

			// Check if the mouse is in the board container, if so we need to check if the player clicked on a polygon on the board and start dragging it
			if (RectTransformUtility.RectangleContainsScreenPoint(boardContainer, mouseScreenPosition))
			{
				TryStartDraggingPolygonOnBoard(mouseScreenPosition);
			}
			// Check if the mouse is in the polygon container, if so we need to check if the player clicked on a polygon and start dragging it
			else if (RectTransformUtility.RectangleContainsScreenPoint(PolygonContainerRectT, mouseScreenPosition))
			{
				TryStartDraggingPolygonInContainer(mouseScreenPosition);
			}

			// If activePolygonObject is not null then a polygon was selected
			if (activePolygonObject != null)
			{
				// Create a PolygonObject that will be displayed on the grid as a preview/ghost polygon when dragging
				activePolygonObjectGhost = CreatePolygonObject(activeLevelData, activePolygonObject.index);
				activePolygonObjectGhost.polygonSprite.gameObject.SetActive(false);

				SetPolygonAlpha(activePolygonObjectGhost.polygonSprite, polygonGhostAlpha);

				 SoundManager.Instance.Play("shape-selected");
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			// If the event is not for the active down pointer then ignore this event
			if (!isPointerActive || eventData.pointerId != activePointerId || activePolygonObject == null)
			{
				return;
			}

			UpdateActivePolygonObjectPosition(eventData.position);

			Vector2 gridPosition;

			if (TryGetValidGridPosition(out gridPosition))
			{
				activePolygonObjectGhost.polygonSprite.gameObject.SetActive(true);

				MovePolygonToBoard(activePolygonObjectGhost, gridPosition, false);
			}
			else
			{
				activePolygonObjectGhost.polygonSprite.gameObject.SetActive(false);
			}
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			// If the event is not for the active down pointer then ignore this event
			if (!isPointerActive || eventData.pointerId != activePointerId)
			{
				return;
			}

			if (activePolygonObject != null)
			{
				Vector2 gridPosition;

				// Try and place the active polygon on the grid at it's current location over the grid
				if (TryGetValidGridPosition(out gridPosition))
				{
					MovePolygonToBoard(activePolygonObject, gridPosition, false);

					GameManager.Instance.PolygonPlacedOnBoard(activePolygonObject.index, gridPosition);
				}
				else
				{
					// Polygon cannot be placed on the board at it's current location, move it back to the polygon container
					MoveToPolygonMarker(activePolygonObject);

					GameManager.Instance.PolygonRemovedFromBoard(activePolygonObject.index);
				}

				// Return the placement polygon sprite to the pool
				ObjectPool.ReturnObjectToPool(activePolygonObjectGhost.polygonSprite.gameObject);

				SoundManager.Instance.Play("shape-placed");
			}

			isPointerActive		= false;
			activePolygonObject	= null;
		}

		/// <summary>
		/// Displays the polygon as a hint on the grid
		/// </summary>
		public void DisplayHint(int polygonIndex)
		{
			// Create a polygon object that will be used to display the hint
			PolygonObject polygonObject	= CreatePolygonObject(activeLevelData, polygonIndex);

			hintPolygonObjects.Add(polygonObject);

			// Start the animation that will fade in/out the hint
			Color fromColor	= polygonObject.polygonSprite.color;
			Color toColor	= polygonObject.polygonSprite.color;

			fromColor.a	= hintMinAlpha;
			toColor.a	= hintMaxAlpha;

			UIAnimation animation = UIAnimation.Color(polygonObject.polygonSprite, fromColor, toColor, hintAnimDuration);

			animation.style				= UIAnimation.Style.Custom;
			animation.animationCurve	= hintAnimCurve;
			animation.loopType			= UIAnimation.LoopType.Reverse;
			animation.startOnFirstFrame	= true;

			animation.Play();

			// Move the hint shape to the grid container
			PolygonData polygonData = activeLevelData.PolygonDatas[polygonIndex];

			MovePolygonToBoard(polygonObject, polygonData.gridBounds.position, true);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the size of the polygon container based on the size the boardContainer
		/// </summary>
		private void SizeAndPositionContainers()
		{
			// Make sure the anchors and pivots are set to middle
			boardContainer.anchorMin		= new Vector2(0.5f, 0.5f);
			boardContainer.anchorMax		= new Vector2(0.5f, 0.5f);
			boardContainer.pivot			= new Vector2(0.5f, 0.5f);
			PolygonContainerRectT.anchorMin = new Vector2(0.5f, 0.5f);
			PolygonContainerRectT.anchorMax = new Vector2(0.5f, 0.5f);
			PolygonContainerRectT.pivot		= new Vector2(0.5f, 0.5f);

			// Set the size of the containers
			float boardContainerSize		= RectT.rect.width;
			float polygonContainerHeight	= RectT.rect.height - spaceBetweenContainers - boardContainerSize;

			boardContainer.sizeDelta		= new Vector2(boardContainerSize, boardContainerSize);
			PolygonContainerRectT.sizeDelta	= new Vector2(RectT.rect.width, polygonContainerHeight);

			// Set the position of the containers
			float boardContainerY = RectT.rect.height / 2f - boardContainerSize / 2f;
			float polygonContainerY = -RectT.rect.height / 2f + polygonContainerHeight / 2f;

			boardContainer.anchoredPosition			= new Vector2(0, boardContainerY);
			PolygonContainerRectT.anchoredPosition	= new Vector2(0, polygonContainerY);
		}

		/// <summary>
		/// Creates a GameObject container
		/// </summary>
		private RectTransform CreateBoardContainer(string name)
		{
			GameObject		container		= new GameObject(name);
			RectTransform	containerRectT	= container.AddComponent<RectTransform>();

			containerRectT.SetParent(boardContainer, false);

			// Set anchors to expand to fill
			containerRectT.anchorMin = Vector2.zero;
			containerRectT.anchorMax = Vector2.one;
			containerRectT.offsetMin = Vector2.zero;
			containerRectT.offsetMax = Vector2.zero;

			return containerRectT;
		}

		/// <summary>
		/// Invoked when the GameManager determines the active level has been completed
		/// </summary>
		private void OnActiveLevelCompleted(string eventId, object[] data)
		{
			// Set the active level data to null so mouse events will be ignored until the next level starts
			activeLevelData = null;
		}

		/// <summary>
		/// Removes all polygons from the game
		/// </summary>
		public void Clear()
		{
			polygonSpritePool.ReturnAllObjectsToPool();

			// Destroy all the polygon markers in the polygon container
			for (int i = 0; i < polygonObjects.Count; i++)
			{
				PolygonObject polygonObject = polygonObjects[i];

				Destroy(polygonObject.polygonContainerMarker.gameObject);
			}

			// Stop the UIAnimations on the hint polygons
			for (int i = 0; i < hintPolygonObjects.Count; i++)
			{
				UIAnimation.DestroyAllAnimations(hintPolygonObjects[i].polygonSprite.gameObject);
			}

			polygonObjects.Clear();
			hintPolygonObjects.Clear();
		}

		/// <summary>
		/// Sets the number of columns/rows in the polygonContainer grid
		/// </summary>
		private void SetupPolygonContainer(int numPolygons)
		{
			int		cols		= polygonsInRow;
			int		rows		= 1;
			bool	growRowNext = true;

			// Increase the number of rows/cols in the grid until we have enough spots to place each polygon
			while (numPolygons > rows * cols)
			{
				if (growRowNext)
				{
					rows++;
				}
				else
				{
					cols++;
				}

				growRowNext = !growRowNext;
			}

			// Set the column constraint so when we add items to the GridLayoutGroup they will auto position
			polygonContainer.constraint			= GridLayoutGroup.Constraint.FixedColumnCount;
			polygonContainer.constraintCount	= cols;

			Vector2 containerSize = PolygonContainerRectT.rect.size;

			// Set the size of a cell in the grid to the max size it can be given the number of columns/rows
			float cellWidth		= (containerSize.x - polygonContainer.spacing.x * (cols - 1) - polygonContainer.padding.left - polygonContainer.padding.right) / cols;
			float cellHeight	= (containerSize.y - polygonContainer.spacing.y * (rows - 1) - polygonContainer.padding.top - polygonContainer.padding.bottom) / rows;

			polygonContainer.cellSize = new Vector2(cellWidth, cellHeight);
		}

		/// <summary>
		/// Creates the tiles for each of the polygons
		/// </summary>
		private void CreatePolygonImages(LevelData levelData)
		{
			polygonContainerScale = float.MaxValue;

			// Create all the polygons objects
			for (int i = 0; i < levelData.PolygonDatas.Count; i++)
			{
				PolygonData		polygonData		= levelData.PolygonDatas[i];
				PolygonObject	polygonObject	= CreatePolygonObject(levelData, i);

				polygonObjects.Add(polygonObject);

				// Create a placement marker object for the polygon in the polygonContainer
				polygonObject.polygonContainerMarker = new GameObject("polygon_marker").AddComponent<RectTransform>();
				polygonObject.polygonContainerMarker.SetParent(polygonContainer.transform, false);

				float polygonWidth	= polygonObject.polygonSprite.rectTransform.rect.width;
				float polygonHeight	= polygonObject.polygonSprite.rectTransform.rect.width;

				float widthDiff		= Mathf.Abs(polygonContainer.cellSize.x - polygonWidth);
				float heightDiff	= Mathf.Abs(polygonContainer.cellSize.y - polygonHeight);
				float scale			= (widthDiff > heightDiff) ? polygonContainer.cellSize.x / polygonWidth : polygonContainer.cellSize.y / polygonHeight;

				if (polygonsFitCell)
				{
					polygonObject.polygonContainerMarker.localScale = new Vector3(scale, scale, 1f);
				}
				else
				{
					polygonContainerScale = Mathf.Min(polygonContainerScale, scale);
				}
			}

			// Now move all polygons to their polygonContainer marker
			for (int i = 0; i < polygonObjects.Count; i++)
			{
				MoveToPolygonMarker(polygonObjects[i]);
			}
		}

		private PolygonObject CreatePolygonObject(LevelData levelData, int polygonIndex)
		{
			PolygonData		polygonData		= levelData.PolygonDatas[polygonIndex];
			PolygonSprite	polygonSprite	= polygonSpritePool.GetObject<PolygonSprite>();

			SetPolygonVisuals(polygonSprite, polygonIndex + levelData.LevelIndex);

			polygonSprite.Setup(polygonData, polygonScale, boardContainer.rect.width);

			PolygonObject polygonObject = new PolygonObject();

			polygonObject.index			= polygonIndex;
			polygonObject.polygonSprite	= polygonSprite;

			return polygonObject;
		}

		/// <summary>
		/// Sets the color and sprite of the PolygonSprite
		/// </summary>
		private void SetPolygonVisuals(PolygonSprite polygonSprite, int index)
		{
			// Set color with null check
			if (polygonColors != null && polygonColors.Count > 0)
			{
				polygonSprite.color = polygonColors[index % polygonColors.Count];
			}
			else
			{
				polygonSprite.color = Color.white; // Default color
			}

			// Set sprite with null check
			if (polygonSprites != null && polygonSprites.Count > 0)
			{
				polygonSprite.sprite = polygonSprites[index % polygonSprites.Count];
			}
			else
			{
				// If no sprites are assigned, create a default white sprite or use a fallback
				polygonSprite.sprite = null;
				Debug.LogWarning("No sprites assigned to polygonSprites list in GameArea. Please assign sprites in the inspector.");
			}

			SetPolygonAlpha(polygonSprite, 1f);
		}

		/// <summary>
		/// Sets the alpha of the PolygonSprite color
		/// </summary>
		private void SetPolygonAlpha(PolygonSprite polygonSprite, float alpha)
		{
			Color color = polygonSprite.color;

			color.a = alpha;

			polygonSprite.color = color;
		}

		/// <summary>
		/// Moves the polygon back to the polygon container
		/// </summary>
		private void MoveToPolygonMarker(PolygonObject polygonObject, bool animate = false)
		{
			polygonObject.polygonSprite.rectTransform.SetParent(polygonObject.polygonContainerMarker, false);

			polygonObject.polygonSprite.rectTransform.anchoredPosition	= Vector2.zero;
			polygonObject.polygonSprite.rectTransform.localScale			= polygonsFitCell ? Vector3.one : new Vector3(polygonContainerScale, polygonContainerScale, 1f);
		}

		private bool TryStartDraggingPolygonInContainer(Vector2 screenPosition)
		{
			// Get the screenPosition relative to the polygonContainer
			Vector2 polygonContainerPosition;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(PolygonContainerRectT, screenPosition, null, out polygonContainerPosition);

			// Set the position relative to the top/left corner of the polygonContainer
			polygonContainerPosition.x += PolygonContainerRectT.rect.width / 2f;
			polygonContainerPosition.y -= PolygonContainerRectT.rect.height / 2f;

			float			minDistance				= float.MaxValue;
			PolygonObject	closestPolygonObject	= null;

			// Get the closest polygon marker to polygonContainerPosition
			for (int i = 0; i < polygonObjects.Count; i++)
			{
				PolygonObject polygonObject = polygonObjects[i];

				float distance = Vector2.Distance(polygonObject.polygonContainerMarker.anchoredPosition, polygonContainerPosition);

				if (distance < minDistance)
				{
					minDistance				= distance;
					closestPolygonObject	= polygonObject;
				}
			}

			// Check if the closest polygon object is not already on the board
			if (closestPolygonObject != null && !closestPolygonObject.isOnBoard)
			{
				SetPolygonObjectAsActive(closestPolygonObject, screenPosition);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Sets the given polygon object as the active object being dragged by the mouse
		/// </summary>
		private void SetPolygonObjectAsActive(PolygonObject polygonObject, Vector2 initialScreenPosition)
		{
			activePolygonObject = polygonObject;

			activePolygonObject.polygonSprite.transform.SetParent(transform);

			activePolygonObject.polygonSprite.transform.localScale = Vector3.one;

			UpdateActivePolygonObjectPosition(initialScreenPosition);
		}

		/// <summary>
		/// Sets the activePolygonObjects position to the given screen position inside GameAreas RectTransform
		/// </summary>
		private void UpdateActivePolygonObjectPosition(Vector2 screenPosition)
		{
			Vector2 gameAreaPosition;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(RectT, screenPosition, null, out gameAreaPosition);

			Vector2 polygonPosition = gameAreaPosition;

			polygonPosition.y += (activePolygonObject.polygonSprite.rectTransform.rect.height * activePolygonObject.polygonSprite.rectTransform.localScale.y) / 2f;

			activePolygonObject.polygonSprite.rectTransform.anchoredPosition = polygonPosition;
		}

		/// <summary>
		/// Checks if the active polygon object can be placed on the grid at it's current location and returns the valid position if it can
		/// </summary>
		private bool TryGetValidGridPosition(out Vector2 gridPosition)
		{
			RectTransform activePolygonSpriteRect = activePolygonObject.polygonSprite.rectTransform;

			// Get the position of the active polygons in the board container
			Vector2 boardPosition = Utilities.SwitchToRectTransform(activePolygonSpriteRect, boardContainer);

			// Check that the polygon object (middle point) is on the board
			if (boardContainer.rect.Contains(boardPosition))
			{
				Vector2 polygonSize = activePolygonSpriteRect.rect.size;

				// Adjust the polygons board position so it is at the bottom/left corner of the polygon
				boardPosition -= polygonSize / 2f;

				// Not adjust the polygons board position so it's between [0, board container width/height]
				boardPosition += boardContainer.rect.size / 2f;

				// Clamp the x/y position so it is on the board
				boardPosition.x = Mathf.Clamp(boardPosition.x, 0, boardContainer.rect.width - polygonSize.x);
				boardPosition.y = Mathf.Clamp(boardPosition.y, 0, boardContainer.rect.height - polygonSize.y);

				// Get the grid x/y position from the polygons board position
				gridPosition.x = Mathf.Round(boardPosition.x / polygonScale);
				gridPosition.y = Mathf.Round(boardPosition.y / polygonScale);

				// Check that it can be placed at this position by checking if it overlaps any other polygon
				if (CanPlaceActivePolygonOnGridAt(gridPosition))
				{
					return true;
				}
			}

			gridPosition.x = 0;
			gridPosition.y = 0;

			return false;
		}

		/// <summary>
		/// Checks if we can place the active polygon object at the given position on the board
		/// </summary>
		private bool CanPlaceActivePolygonOnGridAt(Vector2 gridPostion)
		{
			for (int i = 0; i < polygonObjects.Count; i++)
			{
				if (i == activePolygonObject.index)
				{
					continue;
				}

				PolygonObject polygonObject = polygonObjects[i];

				// Check if the polygon is on the board and if it overlaps with the active polygon
				if (polygonObject.isOnBoard && CheckPolygonsOverlap(activePolygonObject, polygonObject, gridPostion))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns true if the two polygons overlap on the board
		/// </summary>
		private bool CheckPolygonsOverlap(PolygonObject polygonObject1, PolygonObject polygonObject2, Vector2 pObj1GridPos)
		{
			PolygonData polygonData1 = activeLevelData.PolygonDatas[polygonObject1.index];
			PolygonData polygonData2 = activeLevelData.PolygonDatas[polygonObject2.index];
			Vector2		pObj2GridPos = polygonObject2.gridPosition;

			// First check if the square bounding box for the two polygons overlap, if not then we know that the polygons are not overlapping
			Vector2 p1Min = pObj1GridPos;
			Vector2 p1Max = pObj1GridPos + polygonData1.gridBounds.size;
			Vector2 p2Min = pObj2GridPos;
			Vector2 p2Max = pObj2GridPos + polygonData2.gridBounds.size;

			if (!RectanglesOverlap(p1Min, p1Max, p2Min, p2Max))
			{
				return false;
			}

			// Check if any of the triangles in polygon1 overlap any triangle in polygon2
			for (int i = 0; i < polygonData1.triangles.Count; i++)
			{
				TriangleData triangle1 = polygonData1.triangles[i];

				for (int j = 0; j < polygonData2.triangles.Count; j++)
				{
					TriangleData triangle2 = polygonData2.triangles[j];

					if (TrianglesOverlap(triangle1, triangle2, pObj1GridPos, pObj2GridPos))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if 2 rectangles overlap
		/// </summary>
		private bool RectanglesOverlap(Vector2 sq1Min, Vector2 sq1Max, Vector2 sq2Min, Vector2 sq2Max)
		{
			return !((sq1Min.x >= sq2Max.x) || (sq1Max.x <= sq2Min.x) || (sq1Min.y >= sq2Max.y) || (sq1Max.y <= sq2Min.y));
		}

		private bool TrianglesOverlap(TriangleData triangle1, TriangleData triangle2, Vector2 tri1Offset, Vector2 tri2Offset)
		{
			Vector2 t1Min = new Vector2(triangle1.Bounds.xMin, triangle1.Bounds.yMin) + tri1Offset;
			Vector2 t1Max = new Vector2(triangle1.Bounds.xMax, triangle1.Bounds.yMax) + tri1Offset;
			Vector2 t2Min = new Vector2(triangle2.Bounds.xMin, triangle2.Bounds.yMin) + tri2Offset;
			Vector2 t2Max = new Vector2(triangle2.Bounds.xMax, triangle2.Bounds.yMax) + tri2Offset;

			// Fist check if the two bounding boxes for the triangles overlap
			if (!RectanglesOverlap(t1Min, t1Max, t2Min, t2Max))
			{
				return false;
			}

			// Get the positions of the vertices for both triangles
			Vector2 t1a = triangle1.p1 + tri1Offset;
			Vector2 t1b = triangle1.p2 + tri1Offset;
			Vector2 t1c = triangle1.p3 + tri1Offset;
			Vector2 t2a = triangle2.p1 + tri2Offset;
			Vector2 t2b = triangle2.p2 + tri2Offset;
			Vector2 t2c = triangle2.p3 + tri2Offset;

			return
				// Check if any line segment in triangle1 intersects any line segment in triangle2
				LinesIntersect(t1a, t1b, t2a, t2b) || LinesIntersect(t1a, t1b, t2a, t2c) || LinesIntersect(t1a, t1b, t2b, t2c) ||
				LinesIntersect(t1a, t1c, t2a, t2b) || LinesIntersect(t1a, t1c, t2a, t2c) || LinesIntersect(t1a, t1c, t2b, t2c) ||
				LinesIntersect(t1b, t1c, t2a, t2b) || LinesIntersect(t1b, t1c, t2a, t2c) || LinesIntersect(t1b, t1c, t2b, t2c);
		}

		/// <summary>
		/// Returns true if the two lines intersect each other
		/// </summary>
		private bool LinesIntersect(Vector2 l1a, Vector2 l1b, Vector2 l2a, Vector2 l2b)
		{
			float a = ((l2b.x - l2a.x) * (l1a.y - l2a.y) - (l2b.y - l2a.y) * (l1a.x - l2a.x)) / ((l2b.y - l2a.y) * (l1b.x - l1a.x) - (l2b.x - l2a.x) * (l1b.y - l1a.y));
			float b = ((l1b.x - l1a.x) * (l1a.y - l2a.y) - (l1b.y - l1a.y) * (l1a.x - l2a.x)) / ((l2b.y - l2a.y) * (l1b.x - l1a.x) - (l2b.x - l2a.x) * (l1b.y - l1a.y));

			return a > 0 && a < 1 && b > 0 && b < 1;
		}

		/// <summary>
		/// Returns true if the point in in the triangle made up of the 3 triPoints
		/// </summary>
		private bool IsPointInTriangle(Vector2 point, Vector2 triPoint1, Vector2 triPoint2, Vector2 triPoint3)
		{
			if (point == triPoint1 || point == triPoint2 || point == triPoint3)
			{
				return false;
			}

			float d1 = Sign(point, triPoint1, triPoint2);
			float d2 = Sign(point, triPoint2, triPoint3);
			float d3 = Sign(point, triPoint3, triPoint1);

			bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			bool pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(neg && pos);
		}

		/// <summary>
		/// Helper method for IsPointInTriangle
		/// </summary>
		private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		/// <summary>
		/// Places the given polygon on the board at the given position
		/// </summary>
		private void MovePolygonToBoard(PolygonObject polygonObject, Vector2 gridPosition, bool isHint)
		{
			RectTransform polygonSpriteRect	= polygonObject.polygonSprite.rectTransform;
			RectTransform container			= isHint ? polygonHintsContainer : placedPolygonsContainer;

			polygonSpriteRect.SetParent(container);

			polygonSpriteRect.localScale			= Vector3.one;
			polygonSpriteRect.anchoredPosition	= gridPosition * polygonScale + polygonSpriteRect.rect.size / 2f - boardContainer.rect.size / 2f;

			polygonObject.isOnBoard		= true;
			polygonObject.gridPosition	= gridPosition;
		}

		/// <summary>
		/// Attempts to start dragging a polygon from the board at the given screen position
		/// </summary>
		private bool TryStartDraggingPolygonOnBoard(Vector2 screenPosition)
		{
			// Get the screenPosition relative to the gridContainer
			Vector2 boardPosition;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(boardContainer, screenPosition, null, out boardPosition);

			// Get the polygon that was selected
			PolygonObject polygonObject = GetSelectedPolygonOnBoard((boardPosition + boardContainer.rect.size / 2f) / polygonScale);

			if (polygonObject != null)
			{
				polygonObject.isOnBoard = false;

				SetPolygonObjectAsActive(polygonObject, screenPosition);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the GridTile in gridTiles that is closest to the given position relative to gridContainer
		/// </summary>
		private PolygonObject GetSelectedPolygonOnBoard(Vector2 gridPosition)
		{
			for (int i = 0; i < polygonObjects.Count; i++)
			{
				PolygonObject polygonObject = polygonObjects[i];

				if (polygonObject.isOnBoard)
				{
					PolygonData polygonData = activeLevelData.PolygonDatas[polygonObject.index];

					for (int j = 0; j < polygonData.triangles.Count; j++)
					{
						TriangleData triangle = polygonData.triangles[j];

						Vector2 trianglePoint1 = triangle.p1 + polygonObject.gridPosition;
						Vector2 trianglePoint2 = triangle.p2 + polygonObject.gridPosition;
						Vector2 trianglePoint3 = triangle.p3 + polygonObject.gridPosition;

						if (IsPointInTriangle(gridPosition, trianglePoint1, trianglePoint2, trianglePoint3))
						{
							return polygonObject;
						}
					}
				}
			}

			return null;
		}

		#endregion
	}
}
