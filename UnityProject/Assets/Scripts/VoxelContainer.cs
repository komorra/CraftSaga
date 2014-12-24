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

    public static Dictionary<long, VoxelContainer> Containers = new Dictionary<long, VoxelContainer>();
    public Dictionary<long,int> Voxels = new Dictionary<long, int>();

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
    public bool ProcessingNeeded = true;

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

	// Use this for initialization
	void Start ()
	{
	    //Process();
        //rigidbody.isKinematic = true;
        //rigidbody.useGravity = false;
	}

    void OnDrawGizmos()
    {
        if (IsProcessing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(VX + 8, VY + 8, VZ + 8), Vector3.one * 16);
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
    }

    public void Unregister()
    {
        Containers.Remove(Utils.VoxelCoordToLong(X, Y, Z));
    }

    // Update is called once per frame
	void Update () {
	    if (ProcessingNeeded)
	    {
	        ProcessingNeeded = false;
	        Process();
	    }
        //Debug.Log(Vector3.Distance(new Vector3(VX, VY, VZ), Camera.main.transform.position));	    	    
	    Vector3 cam = Camera.main.transform.position;
        if(Vector2.Distance(new Vector2(VX, VZ), new Vector2(cam.x,cam.z)) > 100)
	    {
	        Unregister();
	        Destroy(gameObject);
	    }
	}

    public void Process()
    {
        TimesProcessed++;
        Func<IProcessable, object> async = new Func<IProcessable, object>((c =>
        {            
            var container = c as VoxelContainer;
            var Voxels = container.Solid;
            if (Voxels.Count == 0) return null;
           
            Vector3[] vertices = new Vector3[4*16*16*16*6];
            Vector3[] normals = new Vector3[4*16*16*16*6];
            Vector2[] uvs = new Vector2[4*16*16*16*6];
            int[] tris = new int[6*16*16*16*6];
            int vcount = 0;
            int icount = 0;
            int texw = 0;
            int texh = 0;
            int[] itex = new int[256*256];

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Mesher.MeshVoxels(Voxels.Count, Voxels.Keys.ToArray(), Voxels.Values.ToArray(),
                vertices, normals, uvs, tris, ref vcount, ref icount, itex, ref texw, ref texh, false);
            watch.Stop();
            UnityEngine.Debug.Log(watch.Elapsed);

            Array.Resize(ref vertices, vcount);
            Array.Resize(ref normals, vcount);
            Array.Resize(ref uvs, vcount);
            Array.Resize(ref tris, icount);
            Array.Resize(ref itex, texw*texh);
            
            var cs = WorldGenerator.ChunkSize;
            container.aocol = new Color[cs * cs * cs];
            for (int la = 0; la < cs * cs * cs; la++)
            {
                int x = la % cs;
                int y = (la / cs) % cs;
                int z = (la / (cs * cs));

                float val = container.CalcAO(x, y, z);
                //float val = 1;
                //val = z/15f;
                //float val = (x == 0 && y == 0 && z == 0) || (x == 1 && y==1 && z==1) ? 1 : 0;
                container.aocol[la] = new Color(val, val, val);
            }

            return new object[] { vertices, normals, uvs, tris, texw, texh, itex };            
        }));

        Action<IProcessable, object> syncAction = new Action<IProcessable, object>((c, d) =>
        {
            if (d == null) return;

            var container = c as VoxelContainer;
            if (container == null) return;

            var data = d as object[];

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
        });

        Func<IProcessable, object> asyncLiquid = new Func<IProcessable, object>((c =>
        {
            //Debug.Log("AL");
            var container = c as VoxelContainer;
            var Voxels = container.Liquid;
            if (Voxels.Count == 0) return 0;

            Vector3[] vertices = new Vector3[4 * 16 * 16 * 16 * 6];
            Vector3[] normals = new Vector3[4 * 16 * 16 * 16 * 6];
            Vector2[] uvs = new Vector2[4 * 16 * 16 * 16 * 6];
            int[] tris = new int[6 * 16 * 16 * 16 * 6];
            int vcount = 0;
            int icount = 0;
            int texw = 0;
            int texh = 0;
            int[] itex = new int[2048 * 2048];

            //Debug.Log("ALP");
            Mesher.MeshVoxels(Voxels.Count, Voxels.Keys.ToArray(), Voxels.Values.ToArray(),
                vertices, normals, uvs, tris, ref vcount, ref icount, itex, ref texw, ref texh, true);

            //Debug.Log("ALM");
            Array.Resize(ref vertices, vcount);
            Array.Resize(ref normals, vcount);
            Array.Resize(ref uvs, vcount);
            Array.Resize(ref tris, icount);
            Array.Resize(ref itex, texw * texh);

            //var cs = WorldGenerator.ChunkSize;
            //container.aocol = new Color[cs * cs * cs];
            //for (int la = 0; la < cs * cs * cs; la++)
            //{
            //    int x = la % cs;
            //    int y = (la / cs) % cs;
            //    int z = (la / (cs * cs));

            //    float val = container.CalcAO(x, y, z);
            //    //float val = 1;
            //    //val = z/15f;
            //    //float val = (x == 0 && y == 0 && z == 0) || (x == 1 && y==1 && z==1) ? 1 : 0;
            //    container.aocol[la] = new Color(val, val, val);
            //}

            return new object[] { vertices, normals, uvs, tris, texw, texh, itex };
        }));

        Action<IProcessable, object> syncActionLiquid = new Action<IProcessable, object>((c, d) =>
        {
            //Debug.Log("ALS");                      

            var container = c as VoxelContainer;
            if (container == null) return;
            var data = d as object[];
            if (data == null) return;

            foreach (Transform t in container.transform)
            {
                Destroy(t.gameObject);
            }

            GameObject liGo = new GameObject("Liquid");
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

            //if (container.AOTexture != null)
            //{
            //    Destroy(container.AOTexture);
            //}
            //var cs = WorldGenerator.ChunkSize;
            //container.AOTexture = new Texture3D(cs, cs, cs, TextureFormat.ARGB32, false);
            //container.AOTexture.filterMode = FilterMode.Trilinear;
            //container.AOTexture.wrapMode = TextureWrapMode.Clamp;
            //container.AOTexture.SetPixels(aocol);
            //container.AOTexture.Apply();

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
            liGo.renderer.material.SetTexture("_BottomSkin",container.BottomTexture);
            liGo.renderer.material.SetTexture("_AO", container.AOTexture);
        });

        Threader.Active.Enqueue(new Threader.Item()
        {
            ActionASync = async,
            PostActionSync = syncAction,
            Context = this,
            PriorityData = new Vector3(VX,VY,VZ),
            PriorityResolver = (d) =>
            {
                return 1f/(1f + Vector3.Distance((Vector3) d, Camera.main.transform.position));
            },
            Tag = "Terrain"
        });

        Threader.Active.Enqueue(new Threader.Item()
        {
            ActionASync = asyncLiquid,
            PostActionSync = syncActionLiquid,
            Context = this,
            PriorityData = new Vector3(VX, VY, VZ),
            PriorityResolver = (d) =>
            {
                return 1f / (1f + Vector3.Distance((Vector3)d, Camera.main.transform.position));
            },
            Tag = "Liquid"
        });
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
        if (ex == 27) return 0;
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
        int s = 4;
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
