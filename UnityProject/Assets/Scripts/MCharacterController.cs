using UnityEngine;
using System.Collections;

public class MCharacterController : MonoBehaviour
{
    public const float Gravity = -9.8f;
    public float CharacterHeight = 2f;

    public Vector3 Velocity = Vector3.zero;
    public bool OnTheGround = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	    if (!OnTheGround)
	    {
	        Velocity += Vector3.up*Gravity*Time.deltaTime;
	    }
	    int x = Mathf.RoundToInt(transform.position.x);
	    int y = Mathf.RoundToInt(transform.position.y - CharacterHeight);
	    int z = Mathf.RoundToInt(transform.position.z);

	    if (WorldManager.Active.Generator.GetVoxel(x, y - 1, z) != null)
	    {
	        OnTheGround = true;
	        Velocity = new Vector3(Velocity.x, Mathf.Max(0, Velocity.y), Velocity.z);
            
            var velY = Velocity.y;
            Velocity *= 0.92f;
            Velocity.y = velY;
	    }
	    else
	    {
	        OnTheGround = false;
	    }

	    

	    transform.position += Velocity*Time.deltaTime;
	}
}
