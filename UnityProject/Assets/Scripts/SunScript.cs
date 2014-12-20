using UnityEngine;
using System.Collections;

public class SunScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
	{

	    transform.position = Camera.main.transform.position - transform.forward*1000f;

	}
}
