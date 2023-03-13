using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Manager : MonoBehaviour
{
    public GameObject[] render;


    [ContextMenu("Game/Line Render")]
    
    public void Render_line()
    {
        render = GameObject.FindGameObjectsWithTag("Point");
        if (render != null)
        {
            for (int i = 0; i < render.Length; i++)
            {
                render[i].GetComponent<Point>().Line();
            }
        } 
    }


}
