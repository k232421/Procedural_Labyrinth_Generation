
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;
using Random = UnityEngine.Random;


public class Procedural_Labyrinth_Generation : MonoBehaviour
{
    /// <summary>
    /// For a Prefab to generate a Room with the specifications
    /// </summary>
    /// 
    [Serializable]
    public class RoomType 
    {
        public GameObject prefab_room;
        public float sizeofscale;
        public int min_xlength;
        public int max_xlength;
        public int min_ylength;
        public int max_ylength;
        public int min_numbers;
        public int max_numbers;
    }

    private class TileRoom
    {
        public Vector3 position;
        public int xlength;
        public int ylength;

        public TileRoom( int x , int y)
        {           
            this.xlength = x;
            this.ylength = y;
        }
    }

    private class Edge
    {
        public double edgelenth;
        public Vector3 point_P, point_Q;

        public Edge(double d, Vector3 p, Vector3 q)
        {
            this.edgelenth = d;
            this.point_P = p;
            this.point_Q = q;
        }

    }

    [Serializable]
    public class PathPen
    {
        public GameObject ob;
        public int markwidth;
        public float movespeed;
    }

    /// <summary>
    /// use to gereration a room in random position
    /// </summary>   
    /// 
    [SerializeField] public float r;     //as a radius
    [SerializeField] public float[] xrange = new float[2];
    [SerializeField] public float[] yrange = new float[2];

    /// <summary>
    /// generate Room in RoomContainer and ready to run Navi()
    /// </summary>
    [SerializeField] public RoomType[] roomtypes ;
    [SerializeField] public Transform RoomContainer;
    private bool ifgenerate=false;
    private bool ifready = false;
    private bool last = false;
    List<Vector3> lasttime=new List<Vector3>();

    /// <summary>
    /// Reference for the RoomTile() and DT ...
    /// </summary>
    /// 
    public float acceptrate;
    private RuleTile ruletile;
    private Vector3 position;
    private IPoint[] Points;
    private Point p;
    private List<TileRoom> tileque=new List<TileRoom>();
    private Delaunator de;
    private List<Edge> Delaunary_edges = new List<Edge>();     //need able to sort
    private List<Edge> MST_edges = new List<Edge>();

    [SerializeField] public PathPen pp;
    private bool needpath = false;
    private int readypath = 0;
    private bool lockkey = false;
    private bool positionlock = false;


    /*
    /// <summary>
    /// Reference Input Date by Script
    /// </summary>
    public Procedural_Labyrinth_Generation()
	{
        //create statements if need
	}
    */
    private void Awake()
    {
        ruletile = AssetDatabase.LoadAssetAtPath<RuleTile>("Assets/Tile/RuleTile/Labyrinth.asset");
    }


    /// <summary>
    /// Need work after 2D physic engine
    /// </summary>
    ///
    private void FixedUpdate()
    {
        ifready = CheckifReady();
        
        if (ifready)
        {
            Navigate();
            RoomTile(tileque);           
            Delunary_Triangle(Points);
            MinimumSpanningTree(Delaunary_edges);
            MST_Union_DT(Delaunary_edges);
            //Path_Tile();
            
            ifgenerate = false;
        }
        if (needpath)
        {
            Path_Tile();
        }
    }

    //private Collider2D[] collider2Ds;
    private bool CheckifReady()
    {        
        var rms = GameObject.FindGameObjectsWithTag("Room");
        Vector3 t;
        if (last == false && ifgenerate)
        {
            //collider2Ds = new Collider2D[rms.Length];
            int i = 0;
            foreach (GameObject r in rms)
            {
                t = new Vector3(r.transform.position.x, r.transform.position.y);
                lasttime.Add(t);

                i++;
            }
            
            last = true;
            
        }
        else if(last && ifgenerate)
        {
            int i = 0;
            foreach (GameObject r in rms)
            {
                if (r.transform.position != lasttime[i])
                {                    
                    t = new Vector3(r.transform.position.x, r.transform.position.y);
                    lasttime[i] = t;
                    return false;
                }
                i++;
            }
            GC.Collect();
            return true;
        }
        return false;
    }
    


    public void Generate()
    {
        CreateRoom(roomtypes);
    }

    private void CreateRoom(RoomType[] rts)
    {
        int n;
        float x, y;
        int x_size, y_size;
        foreach (RoomType rt in rts)
        {
            //var so = rt.GetComponent("RoomType") as RoomType;
            n = Random.Range(rt.min_numbers, rt.max_numbers + 1);
            for(int i=0; i<n; i++)
            {

                x = Random.Range(xrange[0], xrange[1]);
                y = Random.Range(yrange[0], yrange[1]);

                while (Mathf.Pow(x,2) + Mathf.Pow(y,2) > Mathf.Pow(r,2))     //check if (x, y) is in cycle r
                {
                    x = Random.Range(xrange[0], xrange[1]);
                    y = Random.Range(yrange[0], yrange[1]);
                }
                position = new Vector3(x, y, 0f);

                x_size = Random.Range(rt.min_xlength, rt.max_xlength);
                y_size = Random.Range(rt.min_ylength, rt.max_ylength);

                // create a room in scenes
                var room = Instantiate(rt.prefab_room, RoomContainer);
                room.transform.position = position;
                room.transform.localScale *= rt.sizeofscale;

                tileque.Add(new TileRoom(x_size, y_size));          //*******position not link now*****
            }
        }
        ifgenerate = true;
        Debug.Log("Num of Room " + tileque.Count);
    }

	private void Navigate()
    {
        GameObject[] father = GameObject.FindGameObjectsWithTag("Room");

        Points = new IPoint[father.Length];
        int index = 0;
        
        foreach (GameObject room in father)
        {
            //setup points then Delaunary_triangle can work            
            p.X = room.transform.position.x;
            p.Y = room.transform.position.y;
            Points[index] = p;

            tileque[index].position = room.transform.position;  //setup point position for tile a room
            index++;
        }    
    }

	private void RoomTile(List<TileRoom> tileRoom)
    {
        Tilemap map = GameObject.Find("Labyrinth").GetComponent<Tilemap>();
        Vector3Int location,temp;

        for(int i=0; i<tileRoom.Count; i++)
        {
            location= map.WorldToCell(tileRoom[i].position);
            temp = location;
            for(int x = -tileRoom[i].xlength; x < tileRoom[i].xlength; x++)
            {
                temp.x = location.x + x; 
                for(int y= -tileRoom[i].ylength; y<tileRoom[i].ylength; y++)
                {
                    temp.y = location.y + y;
                    map.SetTile(temp, ruletile);
                }
            }
        }
        var delete = GameObject.FindGameObjectsWithTag("Room");
        foreach (GameObject i in delete)
        {
            Destroy(i);
        }
        delete = null;
        GC.Collect();
    }

    private void Delunary_Triangle(IPoint[] ips)
    {
        de = new Delaunator(ips);
        double dist;
        de.ForEachTriangleEdge(edge => {
            dist = Vector3.Distance(edge.P.ToVector3(), edge.Q.ToVector3());
            Delaunary_edges.Add(new Edge(dist, edge.P.ToVector3(), edge.Q.ToVector3()));
        });
    }

    private void MinimumSpanningTree(List<Edge> edges)
    {
        lasttime = new List<Vector3>(); // a set to check if a visit node is already in the tree
        MST_edges = new List<Edge>();
        var sortededges = edges.OrderBy(edge => edge.edgelenth);

        foreach(Edge edge in sortededges)
        {
            if(!lasttime.Exists(node => node == edge.point_P))
            {
                lasttime.Add(edge.point_P);
                lasttime.Add(edge.point_Q);
                MST_edges.Add(edge);
            }
            if(!lasttime.Exists(node => node == edge.point_Q))
            {
                lasttime.Add(edge.point_Q);
                MST_edges.Add(edge);
            }
        }
        Debug.Log("Num of MST edges " + MST_edges.Count);
    }

    private void MST_Union_DT(List<Edge> DTedges)
    {
        var sortededges = DTedges.OrderBy(edge => edge.edgelenth);
        int addedges = Mathf.RoundToInt(acceptrate * (Delaunary_edges.Count - MST_edges.Count));
        for(int i=0 ; i < addedges; i++)
        {
            foreach (Edge e in DTedges)
            {
                if(!MST_edges.Exists(MSTe => MSTe == e))
                {
                    MST_edges.Add(e);
                    i++;
                    break;
                }
            }
        }
        needpath = true;
        Debug.Log("Num of DTMST edges is " + MST_edges.Count);
    }

    private void Path_Tile()
    {
        Rigidbody2D rigidbody = pp.ob.GetComponent<Rigidbody2D>();
        Tilemap map = GameObject.Find("Labyrinth").GetComponent<Tilemap>();
        if(!lockkey)
        {
            pp.ob.transform.position = MST_edges[readypath].point_P; //initial postion
            lockkey = true;
        }        

        if(readypath < MST_edges.Count)
        {
            if (map.WorldToCell(pp.ob.transform.position) != map.WorldToCell(MST_edges[readypath].point_Q))
            {
                if (!positionlock)
                {
                    //setup start point P and the move speed
                    rigidbody.velocity = new Vector2(pp.movespeed * (MST_edges[readypath].point_Q.x - pp.ob.transform.position.x), pp.movespeed * (MST_edges[readypath].point_Q.y - pp.ob.transform.position.y));
                    positionlock = true;
                }
                for(int x = map.WorldToCell(pp.ob.transform.position).x - pp.markwidth; x < map.WorldToCell(pp.ob.transform.position).x + pp.markwidth ; x++)
                {
                    for(int y = map.WorldToCell(pp.ob.transform.position).y - pp.markwidth; y < map.WorldToCell(pp.ob.transform.position).y + pp.markwidth; y++)
                    {
                        map.SetTile(new Vector3Int(x, y, 0), ruletile);
                    }
                }
            }
            else
            {
                GC.Collect();
                readypath++;
                lockkey = false;
                positionlock = false;
            }
        }
        if(readypath == MST_edges.Count)
        {
            Debug.Log("Finish");
            needpath = false;
            Destroy(pp.ob);
        }

        
    }
}
