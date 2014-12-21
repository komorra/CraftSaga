using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CSEngine;
using LibNoise.Unity.Generator;
using UnityEngine;


[Serializable]
public class WorldGenerator
{
    public const int ChunkSize = 16;
    public int Seed = 0;

    private Billow billow = new Billow();

    public bool IsContactVoxel(int x, int y, int z)
    {
        int ex = 0;
        if (GetVoxel(x, y, z) == null) return false;
        if ((GetVoxel(x - 1, y + 0, z + 0) != null) &&
            (GetVoxel(x + 1, y + 0, z + 0) != null) &&
            //(GetVoxel(x + 0, y - 1, z + 0) != null) &&
            (GetVoxel(x + 0, y + 1, z + 0) != null) &&
            (GetVoxel(x + 0, y + 0, z - 1) != null) &&
            (GetVoxel(x + 0, y + 0, z + 1) != null)) return false;
        return true;
    }

    public int? GetVoxel(int x, int y, int z)
    {
        int cx, cy, cz;
        Utils.CoordVoxelToChunk(x, y, z, out cx, out cy, out cz, ChunkSize);
        VoxelContainer vc = null;            
        if (!VoxelContainer.Containers.TryGetValue(Utils.VoxelCoordToLong(cx,cy,cz),out vc))
        {
            return null;
        }
        int val;
        int vx, vy, vz;
        Utils.CoordChunkToVoxel(cx, cy, cz, out vx, out vy, out vz);
        if (vc.Voxels.TryGetValue(Utils.VoxelCoordToLong(x - vx, y - vy, z - vz), out val))
        {
            return val;
        }
        return null;
    }

    public void PlaceVoxel(int x, int y,int z, int type)
    {
        //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.transform.position = new Vector3(x, y, z) + Vector3.one * 0.5f;
        //cube.renderer.material.color = Color.red;

        int cx, cy, cz;
        Utils.CoordVoxelToChunk(x, y, z, out cx, out cy, out cz, ChunkSize);
        int vx, vy, vz;
        Utils.CoordChunkToVoxel(cx, cy, cz, out vx, out vy, out vz);
        VoxelContainer vc = null;            
        if (!VoxelContainer.Containers.TryGetValue(Utils.VoxelCoordToLong(cx,cy,cz), out vc))
        {
            GameObject go = new GameObject("Voxel Container");
            go.transform.parent = WorldManager.Active.transform;
            vc = go.AddComponent<VoxelContainer>();
            vc.SideTexture = WorldManager.Active.SideTexture;
            vc.TopTexture = WorldManager.Active.TopTexture;
            vc.BottomTexture = WorldManager.Active.BottomTexture;
            vc.Shader = WorldManager.Active.VoxelShader;
            vc.X = cx;
            vc.Y = cy;
            vc.Z = cz;
            vc.Register();
            go.isStatic = true;
            go.transform.position = new Vector3(vx, vy, vz);
        }
        vc.Voxels.AddOrReplace(Utils.VoxelCoordToLong(x - vx, y - vy, z - vz), type);
        vc.ProcessingNeeded = true;
    }

    public void GenerateSimple()
    {
        for (int la = -150; la < 150; la++)
        {
            for (int lb = -150; lb < 150; lb++)
            {
                float perlin = Mathf.PerlinNoise(1000+la/30f, 8000+lb/30f);                
                int h = (int) ((perlin + 0f)*16f);
                for (int lc = 0; lc < h; lc++)
                {
                    PlaceVoxel(la, lc, lb, 1);
                }
                if (UnityEngine.Random.value < 0.01)
                {
                    for (int lc = 0; lc < 5; lc++)
                    {
                        PlaceVoxel(la, lc + h, lb, 2);
                    }
                    for (int lx = -4; lx <= 4; lx++)
                    {
                        for (int ly = -4; ly <= 4; ly++)
                        {
                            for (int lz = -4; lz <= 4; lz++)
                            {
                                Vector3 radius = new Vector3(lx, ly, lz);
                                if (radius.magnitude < 4)
                                {
                                    PlaceVoxel(la + lx, h + 6 + ly, lb + lz, 2048);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public void GenerateTest()
    {
        for (int la = 0; la < 16; la++)
        {
            for (int lb = 0; lb < 8; lb++)
            {
                for (int lc = 0; lc < 16; lc++)
                {
                    //if (la > 0 && la < 15) continue;
                    //if (lc > 0 && lc < 15) continue;
                    //if (la == 0 || lc == 0 || la == 15 || lc == 15)
                    if(new Vector3(la-8,lb-8,lc-8).magnitude > 6)
                    PlaceVoxel(la, lb, lc, 2);
                }
            }
        }
        PlaceVoxel(0, 0, 0, 1);
        PlaceVoxel(0, 1, 0, 1);
    }

    public void GenerateStandard()
    {
        bool queryUp, queryDown;
        int s = 1;
        for (int la = -s; la <= s; la++)
        {
            for (int lb = -s; lb <= s; lb++)
            {
                int curh = 2;
                do
                {
                    QueryChunk(la, curh, lb, out queryUp, out queryDown);
                    curh--;
                } while (queryDown);
            }
        }        
    }


    private void QueryChunk(int cx, int cy, int cz, out bool queryUp, out bool queryDown)
    {
        queryUp = false;
        queryDown = true;

        int vx, vy, vz;
        Utils.CoordChunkToVoxel(cx, cy, cz, out vx, out vy, out vz);

        int[] vertsum = new int[16];
        for (int la = 0; la < 16; la++)
        {
            for (int lb = 0; lb < 16; lb++)
            {
                for (int lc = 0; lc < 16; lc++)
                {
                    int x = vx + la;
                    int y = vy + lb;
                    int z = vz + lc;

                    if (billow.GetValue(x/100.0, y/100.0, z/100.0) < y/10.0)
                    {
                        PlaceVoxel(x, y, z, 1);
                        if (y == 0)
                        {
                            vertsum[lb]++;
                        }
                    }
                }
            }
        }
        for (int la = 0; la < 16; la++)
        {
            if (vertsum[la] == 256)
            {
                Debug.Log(vertsum[la]);
                queryDown = false;
            }
        }
    }
}

