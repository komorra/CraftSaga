using UnityEngine;
using System.Collections;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Active { get; private set; }

    public Shader VoxelShader;
    public Shader LiquidShader;
    public Texture2D TopTexture;
    public Texture2D SideTexture;
    public Texture2D BottomTexture;

    public WorldGenerator Generator = new WorldGenerator();

	// Use this for initialization
	void Start ()
	{

	    Active = this;
        Generator.Initialize();

	    
	    //Generator.GenerateStandard();
	    //Generator.GenerateSimple();
	    //Generator.GenerateTest();
	    InvokeRepeating("Generate", 0, 1);
        //Generate();
	}

    void Generate()
    {
        GenerateStandard();
    }

    public void GenerateStandard()
    {
        bool queryUp, queryDown;
        int s = 10;
        int x = Mathf.RoundToInt(Camera.main.transform.position.x);
        int z = Mathf.RoundToInt(Camera.main.transform.position.z);

        for (int la = -s; la <= s; la++)
        {
            for (int lb = -s; lb <= s; lb++)
            {
                int cx = x + la;
                int cz = z + lb;

                Generator.GenerateVoxelsForCoord(cx, cz);
            }
            //yield return new WaitForEndOfFrame();
        }
    }
}
