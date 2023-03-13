using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Manager : MonoBehaviour
{
    public GameObject[] render;
    public GameObject[] render2;

    public GameObject Spawn_point;
    public GameObject Robot_pref;

    public GameObject Manager_point;
    public Manager Manager_point_scripst;
    public GameObject Point_lidar;

    public List<Global_points> Global_point = new List<Global_points>();

    public List<GameObject> Robots_list = new List<GameObject>();

    public int Robot_count;
    public List<Vector3> Lidar_vect = new List<Vector3>(); // ¬се точки установленные лидаром координты

    public class Global_points
    {
        public int num = 0;
        public float x = 0;
        public float z = 0;
        public int last_point_num = 0;
        public float angle = 0;
        public int visited = 0;
        public bool hub = false;
        public int last_point_num_2 = -1;
        public Global_points(int num = 0, float x = 0, float z = 0, int last_point_num = 0, float angle = 0, int visited = 0, bool hub = false, int last_point_num_2 = -1)
        {
            this.num = num;
            this.x = x;
            this.z = z;
            this.last_point_num = last_point_num;
            this.angle = angle;
            this.visited = visited;
            this.hub = hub;
            this.last_point_num_2 = last_point_num_2;
        }

    }
    

    public void Start()
    {
        /*
        for(int i = 0; i< Robot_count; i++)
        {
            Robots_list.Add(Instantiate(Robot_pref, Spawn_point.transform.position, Quaternion.identity));
            Robots_list[i].GetComponent<Robot>().man = Manager_point_scripst;
            Robots_list[i].GetComponent<Robot>().manager_gameobj = Manager_point;
            Robots_list[i].GetComponent<Robot>().lidar_point_Parant_gameobj = Point_lidar;
        }
        */
    }

    public void Spawn_robot()
    {
        Robots_list.Add(Instantiate(Robot_pref, Spawn_point.transform.position, Quaternion.identity));
        Robots_list[Robots_list.Count - 1].GetComponent<Robot>().man = Manager_point_scripst;
        Robots_list[Robots_list.Count - 1].GetComponent<Robot>().manager_gameobj = Manager_point;
        Robots_list[Robots_list.Count - 1].GetComponent<Robot>().lidar_point_Parant_gameobj = Point_lidar;
    }

    [ContextMenu("Game/Line Render")]
    public void Render_line()
    {
        render = GameObject.FindGameObjectsWithTag("Point");
        render2 = GameObject.FindGameObjectsWithTag("Point_global");

        if (render != null)
        {
            for (int i = 0; i < render.Length; i++)
            {
                render[i].GetComponent<Point>().Line();
            }
        }
        if (render2 != null)
        {
            for (int i = 0; i < render2.Length; i++)
            {
                render2[i].GetComponent<Point>().Line();
            }
        }
    }


}
