using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

namespace Interact
{
	[CreateAssetMenu(menuName = "Interact/InteractRuleset")]
	public class InteractRuleset : ScriptableObject
	{
		public List<InteractRules> interactions = new List<InteractRules>();

		public void Serialize(string path)
		{
			var rulePath = NodeManager.Path(path, name);
			for (int i = 0; i < interactions.Count; i++)
			{
				var pathI = NodeManager.Path(rulePath, i);
				interactions[i].Serialize(pathI);
			}
		}
	}
}