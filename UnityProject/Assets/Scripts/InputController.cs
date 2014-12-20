using UnityEngine;
using System.Collections;
using CharacterController = UnityEngine.CharacterController;

[RequireComponent(typeof(MCharacterController))]
public class InputController : MonoBehaviour
{
    public float Speed = 3f;
    public float JumpSpeed = 10f;

    private MCharacterController charController;

    // Use this for initialization
    void Start()
    {

        charController = GetComponent<MCharacterController>();

    }

    // Update is called once per frame
    void Update()
    {
        var velY = charController.Velocity.y;
        if (charController.OnTheGround)
        {
            if (Input.GetKey(KeyCode.W)) charController.Velocity += transform.forward*Time.deltaTime*Speed;
            if (Input.GetKey(KeyCode.S)) charController.Velocity -= transform.forward*Time.deltaTime*Speed;
            if (Input.GetKey(KeyCode.A)) charController.Velocity -= transform.right*Time.deltaTime*Speed;
            if (Input.GetKey(KeyCode.D)) charController.Velocity += transform.right*Time.deltaTime*Speed;
            charController.Velocity.y = velY;

            if (Input.GetKey(KeyCode.Space))
            {                
                charController.OnTheGround = false;
                charController.Velocity += Vector3.up*JumpSpeed;                
            }
        }
    }
}
