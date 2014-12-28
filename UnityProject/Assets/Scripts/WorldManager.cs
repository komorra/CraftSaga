using CSEngine;
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
    public Texture2D BreakTexture;

    public WorldGenerator Generator = new WorldGenerator();

	// Use this for initialization
	void Start ()
	{

	    Active = this;
        Generator.Initialize();

	    
	    //Generator.GenerateStandard();
	    //Generator.GenerateSimple();
	    //Generator.GenerateTest();
	    InvokeRepeating("Generate", 0.1f, 3);
        //Generate();
	}

    private void SyncGenerate(IProcessable c, string tag, object d)
    {
        var crd = d as int[];
        int vx, vy, vz;
        Utils.CoordChunkToVoxel(crd[0], 0, crd[1], out vx, out vy, out vz);
        for (int la = 0; la < 16; la++)
        {
            for (int lb = 0; lb < 16; lb++)
            {
                Generator.GenerateVoxelsForCoord(vx + la, vz + lb);
            }
        }
    }

    void Generate()
    {
        int x = Mathf.RoundToInt(Camera.main.transform.position.x);
        int z = Mathf.RoundToInt(Camera.main.transform.position.z);
        int cx, cy, cz;
        Utils.CoordVoxelToChunk(x, 0, z, out cx, out cy, out cz);

        int range = Mathf.FloorToInt(GlobalSettings.Active.MaxVisibilityRadius/16f);
        for (int la = -range; la <= range; la++)
        {
            for (int lb = -range; lb <= range; lb++)
            {
                int tcx = cx + la;
                int tcz = cz + lb;

                if (Vector3.Distance(new Vector3(tcx, 0, tcz), Camera.main.transform.position.GetFlatCoord()/16f) >
                    GlobalSettings.Active.MaxVisibilityRadius) continue;

                var flatKey = Utils.VoxelCoordToLong(tcx, 0, tcz);
                if (!VoxelContainer.FlatContainers.ContainsKey(flatKey))
                {
                    if (!Threader.Active.Enqueue(new Threader.Item()
                    {
                        Tag = string.Format("{0}:{1}", tcx, tcz),
                        SkipAsync = true,
                        Data = new int[] {tcx, tcz},
                        PostActionSync = SyncGenerate,
                        PriorityData = new Vector3(tcx, 0, tcz),
                        PriorityThreshold = -GlobalSettings.Active.MaxVisibilityRadius - 1000,
                        PriorityResolver = (d) =>
                        {
                            return -Vector3.Distance((Vector3) d, Camera.main.transform.position.GetFlatCoord()/16f) - 1000;
                        },
                    }))
                    {
                        return;
                    }
                }
            }
        }
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
