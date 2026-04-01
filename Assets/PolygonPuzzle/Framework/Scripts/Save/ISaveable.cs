using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob
{
	public interface ISaveable
	{
		string SaveId { get; }
		Dictionary<string, object> Save();
	}
}
