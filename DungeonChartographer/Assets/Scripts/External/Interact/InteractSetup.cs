using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interact
{

	[ExecuteInEditMode]
	public class InteractSetup : MonoBehaviour
	{
		public bool all = true;
		public bool removeZOrder = true;

		public bool remove = false;

		void OnEnable()
		{
			Validate();
		}

		void LateUpdate()
		{
			if (remove)
			{
				DestroyImmediate(GetComponent<InteractStorage>());
				DestroyImmediate(GetComponent<InteractModule>());
				DestroyImmediate(GetComponent<InteractPickup>());
				DestroyImmediate(GetComponent<InteractState>());
				DestroyImmediate(this);
				remove = false;
			}
			if (removeZOrder)
			{
				if (GetComponent(typeof(IZControl)) != null)
					DestroyImmediate(GetComponent(typeof(IZControl)));
			}
		}

        internal void Validate(InteractState sample = null)
        {
			if (all)
			{
				InteractState state = null;
				InteractStorage store = null;
				InteractModule modul = null;
				InteractPickup picku = null;
				if ((state = GetComponent<InteractState>()) == null)
					state = gameObject.AddComponent<InteractState>();
				if ((store = GetComponent<InteractStorage>()) == null)
					store = gameObject.AddComponent<InteractStorage>();
				if ((modul = GetComponent<InteractModule>()) == null)
					modul = gameObject.AddComponent<InteractModule>();
				if ((picku = GetComponent<InteractPickup>()) == null)
					picku = gameObject.AddComponent<InteractPickup>();
				state.ValidateComponents();
				store.ValidateComponents(sample!= null ? sample.store : null);
				modul.ValidateComponents();
				picku.ValidateComponents();
			}
        }
    }


}

namespace Common
{
	// extra control for 23d camera
	public interface IZControl
	{

	}
	public partial class ZOrderController : IZControl
	{
	}
}