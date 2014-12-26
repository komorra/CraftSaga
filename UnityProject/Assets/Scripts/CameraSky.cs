using UnityEngine;
using System.Collections;

public class CameraSky : MonoBehaviour
{

    public Shader SkyShader;

	// Use this for initialization
	void Start ()
	{

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.renderer.material = new Material(SkyShader);
	    quad.transform.rotation = transform.rotation;
	    quad.transform.parent = transform;

	    float distance = camera.farClipPlane*0.95f;
	    quad.transform.localPosition = Vector3.forward*distance;
        var frustumHeight = 2.0f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var frustumWidth = frustumHeight * camera.aspect;
	    quad.transform.localScale = new Vector3(frustumWidth,frustumHeight,1);

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
