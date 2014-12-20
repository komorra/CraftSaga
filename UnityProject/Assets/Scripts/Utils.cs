using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSEngine
{
    public static class Utils
    {
        public static void AddOrReplace<K, V>(this Dictionary<K, V> dict, K key, V val)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = val;
            }
            else
            {
                dict.Add(key, val);
            }
        }

        public static void CoordVoxelToChunk(int vx, int vy, int vz, out int cx, out int cy, out int cz, int ChunkSize = 16)
        {
            cx = vx / ChunkSize - (vx < 0 ? 1 : 0);
            cy = vy / ChunkSize - (vy < 0 ? 1 : 0);
            cz = vz / ChunkSize - (vz < 0 ? 1 : 0);
        }

        public static void CoordChunkToVoxel(int cx, int cy, int cz, out int vx, out int vy, out int vz, int ChunkSize = 16)
        {
            vx = cx * ChunkSize + (cx < 0 ? 1 : 0);
            vy = cy * ChunkSize + (cy < 0 ? 1 : 0);
            vz = cz * ChunkSize + (cz < 0 ? 1 : 0);
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
