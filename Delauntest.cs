using System;
using System.Linq ;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;

public class Delauntest : MonoBehaviour
{
    //for DT
    public GameObject[] obs;
    public Delaunator de;
    public IPoint[] ips;
    public Point p;

    
    public LineRenderer lr;
    public List<Vector3> points;

    //for visualize
    private Transform container;
    [SerializeField] Material lineMaterial;


    public class Edge{
        public double ED { get; set; }
        public Vector3 P { get; set; }
        public Vector3 Q { get; set; }
    }

    public void Spawn()
    {
        

    }
    public void work()
    {
        obs = GameObject.FindGameObjectsWithTag("Room");


        //Debug.Log("obs room is " + obs.Length);
        ips = new IPoint[obs.Length];

        for(int i=0; i < obs.Length; i++)
        {
            p.X= obs[i].transform.position.x;
            p.Y= obs[i].transform.position.y;

            ips[i] = p;
            

        }
        
        de = new Delaunator(ips);

        //Debug.Log("the halfedges are " + de.Halfedges.Length);
         List<Edge> eds = new List<Edge>();


        de.ForEachTriangleEdge(edge => 
        {
            eds.Add( new Edge() { P = edge.P.ToVector3() ,
                                Q = edge.Q.ToVector3() ,
                                ED = Vector3.Distance(edge.P.ToVector3(), edge.Q.ToVector3()) }  );       
        });

        var mstedges = MST(eds);

        Debug.Log(""+mstedges.Count);


        // Creat Line in visual
        int j = 0;
        foreach (Edge edge in mstedges)
        {
            Debug.Log("position: " + edge.P + "   " + edge.Q);
            CreatLine(container, $"MSTEdge - {j}", new Vector3[] { edge.P, edge.Q });
            j++;
        }

        //Destroy the navi gameobjects
        foreach(GameObject ob in obs)
        {
            Destroy(ob);
        }
        GC.Collect();



        /*
        de.ForEachTriangleEdge(edge =>
        {

               // CreateLine(TrianglesContainer, $"TriangleEdge - {edge.Index}", new Vector3[] { edge.P.ToVector3(), edge.Q.ToVector3() }, Color.green, triangleEdgeWidth, 0);
            

        });
        */
    }

    private void CreatLine(Transform container, string name ,Vector3[] points , int order = 1)
    {
        var lineGameObject = new GameObject(name);
        lineGameObject.transform.parent = container;
        var lineRenderer = lineGameObject.AddComponent<LineRenderer>();

        lineRenderer.SetPositions(points);
        lineRenderer.material = lineMaterial ?? new Material(Shader.Find("Standard"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.startWidth = .01f;
        lineRenderer.endWidth = .01f;
        lineRenderer.sortingOrder = order;
    }


    public void CreatMazePath()
    {
        // Init run work() to Creat a delaunary triangle relationship   ...
        //
        // Optional restore the GameObject transition and Tile the Room then Destroy the GameObject
        
        /* Copy the edges and vertices from DT to MST
         * Set {MST.edges} Union Set {DT.edges} //random pick 12% to 15%
         *
         * eds for DT  ;   MSTedge = MST(eds)
         * add 15% of DT edges not in the MSTedge
         */

        /* Optional
         * ForEachEdge in MST tile on the tilemap
         * 
         */
    }
    private List<Edge> MST( List<Edge> E ) //input vertex and edge from delaunary triangle graph
    {
        points = new List<Vector3>();
        List<Edge> r = new List<Edge>();

        var SortEdges = E.OrderBy(e => e.ED); // Big-oh (E log E)

        
        foreach (Edge edge in SortEdges)        // Big-oh ( 2 E )
        {

            if ( !points.Exists(p => p == edge.P) )
            {
                points.Add(edge.P);
                points.Add(edge.Q);
                r.Add(edge);
            }
            if ( !points.Exists(p => p == edge.Q))
            {
                points.Add(edge.Q);
                r.Add(edge);
            }
        }


        return r;
    }


}
