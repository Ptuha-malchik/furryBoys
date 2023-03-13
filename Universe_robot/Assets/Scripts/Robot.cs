using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    public GameObject[] Points;
    public Manager man;

    private int counter = 0;
    private int AllPoints = 0;

    private GameObject point_Now;
    void Start()
    {
        man.Render_line();
        Points = man.render;
        AllPoints = Points.Length;

        for (int i = 0; i < AllPoints; i++)
        {
            if (Points[i].GetComponent<Point>().first == true)
            {
                point_Now = Points[i];
                break;
            }
        }
    }


    void FixedUpdate()
    {
        Debug.Log(point_Now.GetComponent<Point>().point_next);
        if (point_Now.GetComponent<Point>().point_next[0] != null)
        {
            GameObject[] Next = point_Now.GetComponent<Point>().point_next;
            move(Next[0]);
        }

        
    }

    private void move(GameObject PositionNext)
    {
        transform.position = Vector3.MoveTowards(transform.position, PositionNext.transform.position, 0.2f);
        if (transform.position == PositionNext.transform.position)
        {
            point_Now = PositionNext;
        }
    }

    private void Choosing_a_path(GameObject PositionNext)
    {
        

    }


    private void lidar()
    {

    }
}
