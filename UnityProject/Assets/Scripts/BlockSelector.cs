using UnityEngine;
using System.Collections;

public class BlockSelector : MonoBehaviour
{

    public Vector3 PlacementCoord;
    public Vector3 RemovalCoord;
    public bool Touched = false;

    private static Material lineMaterial;

    private static void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            lineMaterial = new Material("Shader \"Lines/Colored Blended\" {" +
                                        "SubShader { Pass { " +
                                        "    Blend SrcAlpha OneMinusSrcAlpha " +
                                        "    Cull Off Fog { Mode Off } " +
                                        "    BindChannels {" +
                                        "      Bind \"vertex\", vertex Bind \"color\", color }" +
                                        "} } }");
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    // Use this for initialization
	void Start () {
	
        CreateLineMaterial();
	}
	
	// Update is called once per frame
	void Update ()
	{
	    var dir = transform.forward;
	    var cur = transform.position;
	    Touched = false;
	    for (int la = 0; la < 9; la++)
	    {
	        int x = Mathf.FloorToInt(cur.x);
	        int y = Mathf.FloorToInt(cur.y);
	        int z = Mathf.FloorToInt(cur.z);
	        if (WorldManager.Active.Generator.GetVoxel(x, y, z) == null)
	        {
	            PlacementCoord = new Vector3(x, y, z);
	        }
	        else
	        {
	            RemovalCoord = new Vector3(x, y, z);
	            Touched = true;
	            break;
	        }
	        cur += dir*0.5f;
	    }
	}

    void OnPostRender()
    {
        if (!Touched) return;

        CreateLineMaterial();
        // set the current material
        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(new Color(1, 1, 1, 0.2f));
        float x1 = RemovalCoord.x;
        float y1 = RemovalCoord.y;
        float z1 = RemovalCoord.z;
        float x2 = RemovalCoord.x+1;
        float y2 = RemovalCoord.y+1;
        float z2 = RemovalCoord.z+1;
        GL.Vertex3(x1, y1, z1);
        GL.Vertex3(x2, y1, z1);
        GL.Vertex3(x1, y2, z1);
        GL.Vertex3(x2, y2, z1);
        GL.Vertex3(x1, y1, z2);
        GL.Vertex3(x2, y1, z2);
        GL.Vertex3(x1, y2, z2);
        GL.Vertex3(x2, y2, z2);

        GL.Vertex3(x1, y1, z1);
        GL.Vertex3(x1, y2, z1);
        GL.Vertex3(x2, y1, z1);
        GL.Vertex3(x2, y2, z1);
        GL.Vertex3(x1, y1, z2);
        GL.Vertex3(x1, y2, z2);
        GL.Vertex3(x2, y1, z2);
        GL.Vertex3(x2, y2, z2);

        GL.Vertex3(x1, y1, z1);
        GL.Vertex3(x1, y1, z2);
        GL.Vertex3(x2, y1, z1);
        GL.Vertex3(x2, y1, z2);
        GL.Vertex3(x1, y2, z1);
        GL.Vertex3(x1, y2, z2);
        GL.Vertex3(x2, y2, z1);
        GL.Vertex3(x2, y2, z2);
        
        //GL.Color(new Color(1, 1, 1, 0.5f));
        //GL.Vertex3(0, 0, 0);
        //GL.Vertex3(1, 0, 0);
        //GL.Vertex3(0, 1, 0);
        //GL.Vertex3(1, 1, 0);
        //GL.Color(new Color(0, 0, 0, 0.5f));
        //GL.Vertex3(0, 0, 0);
        //GL.Vertex3(0, 1, 0);
        //GL.Vertex3(1, 0, 0);
        //GL.Vertex3(1, 1, 0);
        GL.End();
    }
}
