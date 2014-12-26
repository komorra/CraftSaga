using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CSEngine
{
    public static class Utils
    {
        public static bool AddOrReplace(this Dictionary<long, int> dict, long key, int val)
        {
            bool changed = false;
            if (dict.ContainsKey(key))
            {
                if (dict[key] != val) changed = true;
                if (GlobalSettings.Active.DebugSuppressIdentityVoxelChecks)
                {
                    Debug.Log(dict[key] + " " + val);
                    GlobalSettings.Active.DebugSuppressIdentityVoxelChecks = false;
                }
                dict[key] = val;
            }
            else
            {
                dict.Add(key, val);
                changed = true;                
            }            
            return changed;
        }

        public static Vector3 GetFlatCoord(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }

        public static int C2V(int c)
        {
            return c*16;
        }

        public static int V2C(int v)
        {
            return Mathf.FloorToInt(v/16f);
        }

        public static int RoundV(int v)
        {
            return C2V(V2C(v));
        }

        public static void CoordVoxelToChunk(int vx, int vy, int vz, out int cx, out int cy, out int cz, int ChunkSize = 16)
        {
            //cx = vx / ChunkSize - (vx < 0 ? 1 : 0);
            //cy = vy / ChunkSize - (vy < 0 ? 1 : 0);
            //cz = vz / ChunkSize - (vz < 0 ? 1 : 0);
            cx = V2C(vx);
            cy = V2C(vy);
            cz = V2C(vz);
        }

        public static void CoordChunkToVoxel(int cx, int cy, int cz, out int vx, out int vy, out int vz, int ChunkSize = 16)
        {
            //-1 => [-16 .. -1]
            // 0 => [  0 .. 15]
            //vx = cx*ChunkSize;// + (cx < 0 ? 1 : 0);
            //vy = cy*ChunkSize;// + (cy < 0 ? 1 : 0);
            //vz = cz*ChunkSize;// + (cz < 0 ? 1 : 0);
            vx = C2V(cx);
            vy = C2V(cy);
            vz = C2V(cz);
        }

        unsafe public static long VoxelCoordToLong(int vx, int vy, int vz)
        {
            long output = 0;
            var ptr = (short*)&output;
            ptr[0] = (short)vx;
            ptr[1] = (short)vy;
            ptr[2] = (short)vz;
            return output;
        }

        unsafe public static void LongToVoxelCoord(long crd, out int vx, out int vy, out int vz)
        {
            var ptr = (short*) &crd;
            vx = ptr[0];
            vy = ptr[1];
            vz = ptr[2];
        }
    }
}
