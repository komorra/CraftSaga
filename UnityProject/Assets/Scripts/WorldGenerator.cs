using System;
using System.Collections;
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

    private Billow billow;
    private RiggedMultifractal fractal;

    public void Initialize()
    {
        billow = new Billow();
        billow.Seed = Seed;
        fractal = new RiggedMultifractal();
        fractal.Seed = Seed;
        UnityEngine.Random.seed = Seed;
        billow.Frequency = UnityEngine.Random.value + 0.5;
        fractal.Frequency = UnityEngine.Random.value + 0.5;
    }

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

    public static bool IsSolid(int type)
    {
        return type < 64 || type >= 128;
    }

    public static bool IsLiquid(int type)
    {
        return type >= 64 && type < 128;
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
            vc.LiquidShader = WorldManager.Active.LiquidShader;

            vc.X = cx;
            vc.Y = cy;
            vc.Z = cz;
            vc.Register();
            go.isStatic = true;
            go.transform.position = new Vector3(vx, vy, vz);
        }
        bool changed = vc.Voxels.AddOrReplace(Utils.VoxelCoordToLong(x - vx, y - vy, z - vz), type);
        if(changed)
        { 
            vc.ProcessingNeeded = true;
        }
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

    public void GenerateVoxelsForCoord(int cx, int cz)
    {
        var dirth = GetDirtH(cx, cz);
        var rockh = GetRockH(cx, cz);
        int level = Math.Max(dirth, rockh);
        for (int h = level-1; h < 0; h++)
        {
            PlaceVoxel(cx, h, cz, 64);
        }
        if (rockh > dirth)
        {
            for (int h = -16; h < rockh; h++)
            {
                PlaceVoxel(cx, h, cz, 4);
            }
        }
        else
        {
            for (int h = -16; h < dirth; h++)
            {
                PlaceVoxel(cx, h, cz, h == dirth ? 1 : 3);
            }
        }                       
    }

    public int GetRockH(int x, int z)
    {
        double val = (fractal.GetValue(x/480.0, 0, z/480.0) * 96.0 - 88.0);
        if (val < 0) val = -Mathf.Pow((float)- val, 0.5f);
        return (int) val;
    }

    public int GetDirtH(int x, int z)
    {
        double val = (billow.GetValue(x / 250.0, 0, z / 250.0) * 60f + 30);
        if (val < 0) val = -Mathf.Pow((float)-val, 0.5f);
        return (int)val;
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

