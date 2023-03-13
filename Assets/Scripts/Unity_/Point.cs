using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Point : MonoBehaviour
{
    public LineRenderer[] lr = new LineRenderer[4];


    public bool first = false;
    public bool finish = false;
    public GameObject[] point_next = new GameObject[4];
    public Material mat;

#if UNITY_EDITOR
    [ContextMenu("Line Render")]
    public void Line()
    {

        for (int i = 0; i<4; i++)
        {
            lr[i].positionCount = 0;
            if (point_next[i] != null)
            {
                lr[i].positionCount = 2;
                lr[i].SetPosition(0, transform.position);
                lr[i].SetPosition(1, point_next[i].transform.position);

                //lr[i].GetComponent<Renderer>().sharedMaterial.color = new Color(0f, 0f, 1f, 1f);
                lr[i].startWidth = 0.075f;
                lr[i].endWidth = 0.075f;
            }
        }
    }

#endif



}
