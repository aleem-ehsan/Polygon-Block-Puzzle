using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob.PolygonPuzzle
{
	[System.Serializable]
	public class BundleInfo
	{
		#region Inspector Variables

		public string			bundleName			= "";
		public PackListItem		packListItemPrefab	= null;
		public List<PackInfo> 	packInfos			= null;

		#endregion
	}
}
