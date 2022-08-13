using Interact;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractUtils : MonoBehaviour, ITFuncStr
{
    public MonoBehaviour Obj { get => this; }
	public bool log =false;
    public void Func(List<string> args, List<object> oargs)
    {
        if (args[0] == "Wait")
        {
            InteractCoroutine.Run(this, Wait(float.Parse(args[1]), args.Count == 3 ? args[2] : ""));
        }
    }

    IEnumerator Wait(float time, string tag)
    {
		if(log)
			Logs.L($"start: {tag}");
        yield return new WaitForSeconds(time);
        //Logs.L($"end: {tag}");
    }
}
