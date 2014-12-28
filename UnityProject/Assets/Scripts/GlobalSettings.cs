using UnityEngine;
using System.Collections;

public class GlobalSettings : MonoBehaviour {

    public static GlobalSettings Active { get; private set; }


    public bool DebugSuppressIdentityVoxelChecks = false;
    public float MaxVisibilityRadius = 150;

	// Use this for initialization
	void Start ()
	{

	    Active = this;

	    Screen.lockCursor = true;
	}
}
