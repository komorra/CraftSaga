using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CSEngine;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class VoxelContainer : MonoBehaviour, IProcessable
{
    public class Data
    {
        public IntPtr Vertices;
        public IntPtr Normals;
        public IntPtr UVs;
        public IntPtr Tris;
        public IntPtr Tex;

        public Data()
        {
            const int vmax = 4*6*16*16*16/2;
            const int imax = 6*6*16*16*16/2;
            const int texsize = 512;
            int v3 = Marshal.SizeOf(typeof (Vector3));
            int v2 = Marshal.SizeOf(typeof (Vector2));
            int i = sizeof (int);

            Vertices = Marshal.AllocHGlobal(vmax*v3);
            Normals = Marshal.AllocHGlobal(vmax*v3);
            UVs = Marshal.AllocHGlobal(vmax*v2);
            Tris = Marshal.AllocHGlobal(imax*i);
            Tex = Marshal.AllocHGlobal(texsize*texsize*i);
        }

        public Vector3[] GetVertices(int vcount)
        {
            var arr = new Vector3[vcount];
            IntPtr temp = Vertices;
            int siz = Marshal.SizeOf(typeof (Vector3));

            for (int la = 0; la < arr.Length; la++)
            {
                arr[la] = (Vector3)Marshal.PtrToStructure(temp, typeof (Vector3));
                temp = new IntPtr(temp.ToInt64() + siz);
            }
            return arr;
        }

        public Vector3[] GetNormals(int vcount)
        {
            var arr = new Vector3[vcount];
            IntPtr temp = Normals;
            int siz = Marshal.SizeOf(typeof(Vector3));

            for (int la = 0; la < arr.Length; la++)
            {
                arr[la] = (Vector3)Marshal.PtrToStructure(temp, typeof(Vector3));
                temp = new IntPtr(temp.ToInt64() + siz);
            }
            return arr;
        }

        public Vector2[] GetUVs(int vcount)
        {
            var arr = new Vector2[vcount];
            IntPtr temp = UVs;
            int siz = Marshal.SizeOf(typeof(Vector2));

            for (int la = 0; la < arr.Length; la++)
            {
                arr[la] = (Vector2)Marshal.PtrToStructure(temp, typeof(Vector2));
                temp = new IntPtr(temp.ToInt64() + siz);
            }
            return arr;
        }

        public int[] GetTris(int icount)
        {
            var arr = new int[icount];

            Marshal.Copy(Tris, arr, 0, icount);
                        
            return arr;
        }

        public int[] GetTex(int texw, int texh)
        {
            var arr = new int[texw*texh];
            Marshal.Copy(Tex, arr, 0, arr.Length);
            return arr;
        }
    }

    private static Data[] datas = new Data[Threader.MaxWorkingCount];
    public static Dictionary<long, VoxelContainer> Containers = new Dictionary<long, VoxelContainer>();
    public static Dictionary<long, List<VoxelContainer>> FlatContainers = new Dictionary<long, List<VoxelContainer>>();
    public Dictionary<long,int> Voxels = new Dictionary<long, int>();
    public Dictionary<long, float> Damages = new Dictionary<long, float>();

    private Texture2D breakCoords;

    public Dictionary<long, int> Solid
    {
        get {
            lock (Voxels)
            {
                return Voxels.Where(o => WorldGenerator.IsSolid(o.Value)).ToDictionary(o => o.Key, o => o.Value);
            }
        }
    }
    public Dictionary<long, int> Liquid
    {
        get {
            lock (Voxels)
            {
                return Voxels.Where(o => WorldGenerator.IsLiquid(o.Value)).ToDictionary(o => o.Key, o => o.Value);
            }
        }
    }
    public Shader Shader;
    public Shader LiquidShader;
    public Texture2D TopTexture;
    public Texture2D SideTexture;
    public Texture2D BottomTexture;
    public Texture2D BreakTexture;
    public bool ProcessingNeeded = true;
    public bool BreaksProcessingNeeded = false;

    public Color[] aocol { get; set; }

    public int X;
    public int Y;
    public int Z;

    public int VX
    {
        get
        {
            int vx, vy, vz;
            Utils.CoordChunkToVoxel(X, Y, Z, out vx, out vy, out vz);
            return vx;
        }
    }

    public int VY
    {
        get
        {
            int vx, vy, vz;
            Utils.CoordChunkToVoxel(X, Y, Z, out vx, out vy, out vz);
            return vy;
        }
    }

    public int VZ
    {
        get
        {
            int vx, vy, vz;
            Utils.CoordChunkToVoxel(X, Y, Z, out vx, out vy, out vz);
            return vz;
        }
    }

    public int TimesProcessed { get; private set; }

    private Texture3D AOTexture;

    static VoxelContainer()
    {
        for (int la = 0; la < datas.Length; la++)
        {
            datas[la] = new Data();
        }
    }

	// Use this for initialization
	void Start ()
	{
	    breakCoords = new Texture2D(16, 1, TextureFormat.ARGB32, false);
        breakCoords.filterMode = FilterMode.Point;
	    breakCoords.wrapMode = TextureWrapMode.Clamp;	    
	    breakCoords.SetPixels(Enumerable.Repeat(new Color(0, 0, 0, 0), 16).ToArray());
	    breakCoords.Apply();

	    InvokeRepeating("StateCheck", 0.1f, 0.1f);
	    InvokeRepeating("BreaksCheck", 0.1f, 0.15f);
	}

    void OnDrawGizmos()
    {
        if (IsProcessing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(VX + 8, VY + 8, VZ + 8), Vector3.one * 16);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(VX + 8, VY + 8, VZ + 8), Vector3.one * 16);
    }    

    private void BreaksCheck()
    {
        if (BreaksProcessingNeeded)
        {
            BreaksProcessingNeeded = false;
            int index = 0;
            foreach (var kv in Damages.Take(16))
            {
                int x, y, z;
                Utils.LongToVoxelCoord(kv.Key, out x, out y, out z);
                Color32 c = new Color32();
                c.r = (byte) x;
                c.g = (byte) y;
                c.b = (byte) z;
                c.a = (byte) (kv.Value*255f);
                breakCoords.SetPixel(index, 0, (Color) c);
                index++;
            }
            for (int la = 0; la < 16; la++)
            {
                breakCoords.SetPixel(index, 0, new Color(0, 0, 0, 0));
            }
            breakCoords.Apply();
            renderer.material.SetTexture("_BreakCoords", breakCoords);
        }
    }

    //void OnTriggerEnter(Collider other)
    //{
    //    var bcs = GetComponents<BoxCollider>();
    //    foreach (var bc in bcs)
    //    {
    //        bc.enabled = true;
    //    }
    //    Debug.Log("ENTER");
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    var bcs = GetComponents<BoxCollider>();
    //    foreach (var bc in bcs)
    //    {
    //        if (!bc.isTrigger)
    //        {
    //            bc.enabled = false;
    //        }
    //    }
    //}

    public void Register()
    {
        Containers.Add(Utils.VoxelCoordToLong(X, Y, Z), this);
        long flatkey = Utils.VoxelCoordToLong(X, 0, Z);
        if (!FlatContainers.ContainsKey(flatkey))
        {
            FlatContainers.Add(flatkey, new List<VoxelContainer>());
        }
        if (!FlatContainers[flatkey].Contains(this))
        {
            FlatContainers[flatkey].Add(this);
        }
    }

    public void Unregister()
    {
        Containers.Remove(Utils.VoxelCoordToLong(X, Y, Z));
        long flatkey = Utils.VoxelCoordToLong(X, 0, Z);
        FlatContainers[flatkey].Remove(this);
        if (FlatContainers[flatkey].Count == 0)
        {
            FlatContainers.Remove(flatkey);
        }
    }

    void StateCheck()
    {
        if (ProcessingNeeded)
        {
            ProcessingNeeded = false;
            Process();
        }
        //Debug.Log(Vector3.Distance(new Vector3(VX, VY, VZ), Camera.main.transform.position));	    	    
        Vector3 cam = Camera.main.transform.position;
        if (Vector2.Distance(new Vector2(VX, VZ), new Vector2(cam.x, cam.z)) > GlobalSettings.Active.MaxVisibilityRadius * 2.0)
        {
            Unregister();
            Destroy(gameObject);
        }
    }    

    private object AsyncProcess(IProcessable c, string tag, int thread)
    {
        var container = c as VoxelContainer;
        Dictionary<long, int> Voxels = null;
        if (tag == "Terrain")
        {
            Voxels = container.Solid;
        }
        else if (tag == "Liquid")
        {
            Voxels = container.Liquid;
        }

        if (Voxels.Count == 0) return null;
        
        int vcount = 0;
        int icount = 0;
        int texw = 0;
        int texh = 0;        

        var keys = Voxels.Keys.ToArray();
        var vals = Voxels.Values.ToArray();
        var data = datas[thread];

        Stopwatch watch = new Stopwatch();
        watch.Start();
        Mesher.MeshVoxels(Voxels.Count, keys, vals,
            data.Vertices, data.Normals, data.UVs, data.Tris, ref vcount, ref icount, data.Tex, ref texw, ref texh, tag=="Liquid");
        watch.Stop();
        UnityEngine.Debug.Log(watch.Elapsed);        

        if (tag == "Terrain")
        {
            var cs = WorldGenerator.ChunkSize;
            container.aocol = new Color[cs*cs*cs];
            for (int la = 0; la < cs*cs*cs; la++)
            {
                int x = la%cs;
                int y = (la/cs)%cs;
                int z = (la/(cs*cs));

                float val = container.CalcAO(x, y, z);
                //float val = 1;
                //val = z/15f;
                //float val = (x == 0 && y == 0 && z == 0) || (x == 1 && y==1 && z==1) ? 1 : 0;
                container.aocol[la] = new Color(val, val, val);
            }
        }

        return new object[] { data.GetVertices(vcount), data.GetNormals(vcount), data.GetUVs(vcount), data.GetTris(icount), texw, texh, data.GetTex(texw, texh) };
    }

    private void SyncProcess(IProcessable c, string tag, object d)
    {
        if (d == null) return;

        var container = c as VoxelContainer;
        if (container == null) return;

        var data = d as object[];
        if (data == null) return;

        if (tag == "Liquid")
        {
            foreach (Transform t in container.transform)
            {
                Destroy(t.gameObject);
            }

            GameObject liGo = new GameObject("Liquid");
            liGo.isStatic = true;
            liGo.transform.parent = container.transform;
            liGo.transform.localPosition = Vector3.zero;

            var vertList = data[0] as Vector3[];
            var nrmList = data[1] as Vector3[];
            var uvList = data[2] as Vector2[];
            var indList = data[3] as int[];
            var texW = (int)data[4];
            var texH = (int)data[5];
            var itex = data[6] as int[];

            var mesh = new Mesh();
            mesh.vertices = vertList;
            mesh.normals = nrmList;
            mesh.uv = uvList;
            mesh.triangles = indList;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            var mf = liGo.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            //Debug.Log(texW + " " + texH);
            var tex = new Texture2D(texW, texH);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            var tdata = new Color32[texW * texH];
            var handle = GCHandle.Alloc(tdata, GCHandleType.Pinned);
            Marshal.Copy(itex, 0, handle.AddrOfPinnedObject(), itex.Length);
            handle.Free();
            ////Debug.Log(String.Format("{0}:{1}:{2}:{3}", tdata[0].r, tdata[0].g, tdata[0].b, tdata[0].a));
            tex.SetPixels32(tdata);
            tex.Apply();

            liGo.AddComponent<MeshRenderer>();

            //container.renderer.material = new Material(container.Shader);
            liGo.renderer.material = new Material(LiquidShader);
            if (liGo.renderer.material.mainTexture != null)
            {
                Destroy(liGo.renderer.material.mainTexture);
            }
            liGo.renderer.material.mainTexture = tex;
            liGo.renderer.material.SetTexture("_TopSkin", container.TopTexture);
            liGo.renderer.material.SetTexture("_SideSkin", container.SideTexture);
            liGo.renderer.material.SetTexture("_BottomSkin", container.BottomTexture);
            liGo.renderer.material.SetTexture("_AO", container.AOTexture);            
        }
        else if (tag == "Terrain")
        {
            var vertList = data[0] as Vector3[];
            var nrmList = data[1] as Vector3[];
            var uvList = data[2] as Vector2[];
            var indList = data[3] as int[];
            var texW = (int) data[4];
            var texH = (int) data[5];
            var itex = data[6] as int[];

            var mesh = new Mesh();
            mesh.vertices = vertList;
            mesh.normals = nrmList;
            mesh.uv = uvList;
            mesh.triangles = indList;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            var mf = container.GetComponent<MeshFilter>();
            mf.mesh = mesh;

            //Debug.Log(texW + " " + texH);
            var tex = new Texture2D(texW, texH);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            var tdata = new Color32[texW*texH];
            var handle = GCHandle.Alloc(tdata, GCHandleType.Pinned);
            Marshal.Copy(itex, 0, handle.AddrOfPinnedObject(), itex.Length);
            handle.Free();
            ////Debug.Log(String.Format("{0}:{1}:{2}:{3}", tdata[0].r, tdata[0].g, tdata[0].b, tdata[0].a));
            tex.SetPixels32(tdata);
            tex.Apply();

            if (container.AOTexture != null)
            {
                Destroy(container.AOTexture);
            }
            var cs = WorldGenerator.ChunkSize;
            container.AOTexture = new Texture3D(cs, cs, cs, TextureFormat.ARGB32, false);
            container.AOTexture.filterMode = FilterMode.Trilinear;
            container.AOTexture.wrapMode = TextureWrapMode.Clamp;
            container.AOTexture.SetPixels(aocol);
            container.AOTexture.Apply();

            //container.renderer.material = new Material(container.Shader);
            container.renderer.material = new Material(Shader);
            if (container.renderer.material.mainTexture != null)
            {
                Destroy(container.renderer.material.mainTexture);
            }
            container.renderer.material.mainTexture = tex;
            container.renderer.material.SetTexture("_TopSkin", container.TopTexture);
            container.renderer.material.SetTexture("_SideSkin", container.SideTexture);
            container.renderer.material.SetTexture("_BottomSkin", container.BottomTexture);
            container.renderer.material.SetTexture("_AO", container.AOTexture);
            container.renderer.material.SetVector("_ChunkPos", new Vector4(VX, VY, VZ));
            container.renderer.material.SetTexture("_Break", container.BreakTexture);
            container.renderer.material.SetTexture("_BreakCoords", container.breakCoords);
        }
    }

    public void Process()
    {
        TimesProcessed++;

        if (Solid.Any())
        {
            Threader.Active.Enqueue(new Threader.Item()
            {
                ActionASync = AsyncProcess,
                PostActionSync = SyncProcess,
                Context = this,
                PriorityData = new Vector3(VX, 0, VZ),
                PriorityThreshold = -GlobalSettings.Active.MaxVisibilityRadius,
                PriorityResolver = (d) =>
                {
                    return -Vector3.Distance((Vector3) d, Camera.main.transform.position.GetFlatCoord());
                },
                Tag = "Terrain"
            });
        }

        if (Liquid.Any())
        {
            Threader.Active.Enqueue(new Threader.Item()
            {
                ActionASync = AsyncProcess,
                PostActionSync = SyncProcess,
                Context = this,
                PriorityData = new Vector3(VX, 0, VZ),
                PriorityThreshold = -GlobalSettings.Active.MaxVisibilityRadius,
                PriorityResolver = (d) =>
                {
                    return -Vector3.Distance((Vector3) d, Camera.main.transform.position.GetFlatCoord());
                },
                Tag = "Liquid"
            });
        }
    }

    private bool Exist(int x, int y, int z)
    {
        if (x < 0) x = 0;
        if (x > 15) x = 15;
        if (y < 0) y = 0;
        if (y > 15) y = 15;
        if (z < 0) z = 0;
        if (z > 15) z = 15;

        long key = Utils.VoxelCoordToLong(x, y, z);
        return Voxels.ContainsKey(key);
    }

    private float CalcAO(int x, int y, int z)
    {
        float ao = 1;
        int ex = 0;
        for (int la = -1; la <= 1; la++)
        {
            for (int lb = -1; lb <= 1; lb++)
            {
                for (int lc = -1; lc <= 1; lc++)
                {
                    if (Voxels.ContainsKey(Utils.VoxelCoordToLong(x + la, y + lb, z + lc)))
                    {
                        ex++;
                    }
                }
            }
        }
        if (ex == 0) return 1;
        //if (ex == 27) return 0;
        //for (int la = 0; la < 10; la++)
        //{
        //    var dir = UnityEngine.Random.onUnitSphere;
        //    if (dir.y < 0) continue;
        //    for (int lb = 0; lb < 6; lb++)
        //    {
        //        var pos = dir*(2f + lb*0.7f);
        //        var ox = Mathf.RoundToInt(pos.x);
        //        var oy = Mathf.RoundToInt(pos.y);
        //        var oz = Mathf.RoundToInt(pos.z);
        //        long key = Utils.VoxelCoordToLong(Mathf.RoundToInt(x + ox), Mathf.RoundToInt(y + oy), Mathf.RoundToInt(z + oz));
        //        if (Voxels.ContainsKey(key))
        //        {
        //            ao -= 0.1f/(1f+lb);
        //            break;
        //        }
        //    }
        //}
        int s = 3;
        float total = 0;
        float sum = 0;
        for (int la = -s; la <= s; la++)
        {
            for (int lb = 0; lb <= s; lb++)
            {
                for (int lc = -s; lc <= s; lc++)
                {
                    if (la == 0 && lb == 0 && lc == 0) continue;
                    if ((la + lb + lc)%2 == 0) continue;

                    total++;

                    var vx = Mathf.RoundToInt(x + la + X * WorldGenerator.ChunkSize + (X < 0 ? 1 : 0));
                    var vy = Mathf.RoundToInt(y + lb + Y * WorldGenerator.ChunkSize + (Y < 0 ? 1 : 0));
                    var vz = Mathf.RoundToInt(z + lc + Z * WorldGenerator.ChunkSize + (Z < 0 ? 1 : 0));
                    var vox = WorldManager.Active.Generator.GetVoxel(vx, vy, vz);
                    if(vox!=null)
                    {
                        if (WorldGenerator.IsSolid(vox.Value))
                        {
                            sum++;
                        }
                        if (WorldGenerator.IsLiquid(vox.Value))
                        {
                            sum += 0.05f;
                        }
                    }
                }
            }
        }
        ao -= sum/total;
        return ao;
    }

    public bool IsProcessing { get; set; }
}
