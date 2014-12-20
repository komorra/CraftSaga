using UnityEngine;
using System.Collections;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Active { get; private set; }

    public Shader VoxelShader;
    public Texture2D TopTexture;
    public Texture2D SideTexture;
    public Texture2D BottomTexture;

    public WorldGenerator Generator = new WorldGenerator();

	// Use this for initialization
	void Start ()
	{

	    Active = this;

        //Generator.GenerateStandard();
        //Generator.GenerateSimple();
        Generator.GenerateTest();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
