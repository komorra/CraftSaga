using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public static class Mesher
{
    [DllImport("Mesher")]
    public static extern void MeshVoxels(
        int voxelCount,
        long[] coords,
        int[] types,
        Vector3[] vertices,
        Vector3[] normals,
        Vector2[] uvs,
        int[] tris,
        ref int vertexCount,
        ref int indexCount,
        int[] texture,
        ref int texW,
        ref int texH,
        [MarshalAs(UnmanagedType.I1)] bool liquid
        );
}
