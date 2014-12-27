using System;
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
        IntPtr vertices,
        IntPtr normals,
        IntPtr uvs,
        IntPtr tris,
        ref int vertexCount,
        ref int indexCount,
        IntPtr texture,
        ref int texW,
        ref int texH,
        [MarshalAs(UnmanagedType.I1)] bool liquid
        );
}
