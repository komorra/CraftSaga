using UnityEngine;
using System.Collections;

public class GlobalSettings : MonoBehaviour {

    public static GlobalSettings Active { get; private set; }


    public bool DebugSuppressIdentityVoxelChecks = false;

	// Use this for initialization
	void Start ()
	{

	    Active = this;

	}
}
