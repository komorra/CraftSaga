using System;
using LibNoise.Unity;
using UnityEngine;
using System.Collections;

public class GenTree : IGenerable
{
    public bool ConditionMet(int x, int y, int z, ModuleBase noise)
    {
        if (y < 4) return false;
        if (z%4 != 0) return false;
        if (x%4 != 0) return false;
        return noise.GetValue(x/10.0, y/10.0, z/10.0) > 0.5;
    }

    public void Generate(PlaceVoxelDelegate placeVoxel, int x, int y, int z)
    {
        int barkH = y%4 + 6;
        int rad = Mathf.RoundToInt((x + z)%3 + 5);
        for (int la = 0; la < barkH; la++)
        {
            placeVoxel(x, y + la, z, 2, false);
        }
        for(int la=-rad;la<=rad;la++)
        {
            for (int lb = -rad; lb <= rad; lb++)
            {
                for (int lc = -rad; lc <= rad; lc++)
                {
                    if (new Vector3(la, lb, lc).magnitude < rad)
                    {
                        placeVoxel(x + la, y + lb + barkH, z + lc, 2048 + y%3, false);
                    }
                }
            }
        }
    }
}
