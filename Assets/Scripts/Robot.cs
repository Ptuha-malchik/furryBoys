using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using TMPro;
using UnityEngine;


public class Robot : MonoBehaviour
{
    //////////////////////////////////////////////////////////
    //                  Необходимо для Unity                //
    //////////////////////////////////////////////////////////

    public GameObject Sphere;
    public GameObject Lidar;                // Лидар
    public GameObject Point_Lidar;          // Префаб токи от лидара
    public GameObject Point_point_goto;     // Префаб токи к которой следуем
    public GameObject Global_point_pref;    // Префаб глобальной точки
    public Material material_green;         // Материал свободной точки
    public Material material_orange;        // Материал точки к которой движется робот
    public Manager man;                     // Менеджер точек
    public GameObject manager_gameobj;
    public GameObject lidar_point_Parant_gameobj; // Куда класть точки
    public TextMeshPro Text;

    private GameObject set_point;
    private List<GameObject> Local_point_list = new List<GameObject>();
    private GameObject Newpoint_set;
    



    //////////////////////////////////////////////////////////
    //              Непосредственно знает робот             //
    //////////////////////////////////////////////////////////

    // Настройки
    private int number = -1;

    public float Global_points_dist;        // Расстояние между глобальными точками кластера

    public int Speed_Lidar;                 // Cкорость лидара
    public float Lidar_dist;                // Дистанция лидара
    public float Speed_Robot;               // Cкорость робота
    
    public float Dist_to_set_global_point;  // Дистанция установки глобальных (от одной до другой вне кластера)
    public float Randon_distance;           // Максимальная дистанция случайной установки точки по x z
    public float Dist_to_search_angle;      // Дистанция поиска угла
    public float Global_point_search_dist;  // Дистанция поиска глобальнйо точки
    public float Robot_size;                // Размер ротота
    public float Point_min_distance;        // Минимальная дистанция между точками лидара

    // Нужно для работы
    private int counter_task = 0;
    private int start_lidar_scan = 100;     // Сколько проустить, перед началом работы (для лидара)
    private bool Mode_move = false;         // Режим перемещения false - новый маршрут true - по старому
    private bool first_iter = true;         // Первая итерация?
    private bool Stuck_robot = false;       // Робот застрял? 
    private bool finish_work = false;       // Конец работы алгортма
   // public List<Vector3> Lidar_vect = new List<Vector3>();         // Все точки установленные лидаром координты
    private List<Vector3> Point_local_road = new List<Vector3>();   // Все точки через кеоторые робот проедет или уже проехал (локально RRT)
    private List<int> Path_to_global_point = new List<int>();
    private int Last_global_pos_num = 0;
    private int Count_rotation = 0;         // Счетчик для пропуска движения в кадры поворота робота
    private float general_angle_to_walk = 0;
    private int count_reve = 0; // Счетсик позиции на возвран при застревании
    private bool zas_1 = false;     // Застрял робот? 1 проход по возврату
    private bool global_point_connect = false; // Подсоеденино к глобальной точке? Для режима возврата


    //////////////////////////////////////////////////////////
    //                      Начало кода                     //
    //////////////////////////////////////////////////////////


    void Start()
    {
        if (number == -1)
        {
            number = man.Robots_list.Count - 1;

            for (int i = 0; i < man.Global_point.Count; i++)
            {
                if (Dist_to_point_xz(new Vector3(man.Global_point[i].x, 0, man.Global_point[i].z), transform.position) < 0.05f)
                {
                    Last_global_pos_num = i;
                    search_global_point();
                    move_to_global_point_to_orange();
                    Mode_move = true;
                }
            }

            if (!Mode_move)
            {
                Last_global_pos_num = 0;
            }
            Sphere.transform.localScale = new Vector3(Robot_size * 268f, Robot_size * 268f, Robot_size * 268f);
            
            Newpoint_set = Instantiate(Point_point_goto, transform.position, Quaternion.identity) as GameObject;
            Newpoint_set.GetComponent<Point>().first = true;
            Local_point_list.Add(Newpoint_set);

            //Debug.LogWarning(Path_to_global_point.Count);
            Debug.Log("Start");
        }
    }

    void FixedUpdate()
    {
        Text.text = counter_task.ToString();
        //Generate_graph();
        if (finish_work) // Если глобальные точки все заняты
        {
            // Поиск свободной глобальной точки
            search_global_point();
        }
        else
        {

            if (Stuck_robot) // Если робот застрял
            {
                move_to_last_global_point();
            }
            else
            {
                //Debug.LogWarning(Path_to_global_point.Count);
                //Path_to_global_point.Add(0);
                if (Path_to_global_point.Count > 0) // Если есть траектория движения по глобальным точкам
                {
                    move_to_global_point_to_orange(); // Движение к глобальной токе (незанятой)
                }
                else
                {
                    //Debug.Log(Last_global_pos_num);
                    //searc_global_point_connect();

                    if (Mode_move) // Если доехали до глобальной точки - подготовка 
                    {
                        counter_task++;
                        Newpoint_set = Instantiate(Point_point_goto, transform.position, Quaternion.identity) as GameObject;
                        Newpoint_set.GetComponent<Point>().first = true;
                        Local_point_list.Add(Newpoint_set);

                        RRT_SPAWN_POINTS();
                        man.Render_line();
                        Mode_move = false;
                    }


                    if (start_lidar_scan > 0) // пропуск начала работы для сканирования территории
                    {
                        lidar();
                        start_lidar_scan--;
                    }

                    else
                    {
                        lidar();
                        if (!Mode_move)
                        {
                            if (Newpoint_set == null)
                            {
                                Newpoint_set = Instantiate(Point_point_goto, transform.position, Quaternion.identity) as GameObject;
                                Newpoint_set.GetComponent<Point>().first = true;
                                Local_point_list.Add(Newpoint_set);
                            }
                            move(Newpoint_set);

                        }
                    }
                    searc_global_point_connect();
                }
            }
        }
    }

    // Поиск ближайших глобальных точек для возожности подсоеденится
    public bool searc_global_point_connect()
    {
        float min_dist = 10000;
        int point_min = -1;

        for (int i = 0; i < man.Global_point.Count; i++)
        {
            float Dist = 2;

            if (Last_global_pos_num >= 0)
            {
                Dist = Dist_to_point_xz(new Vector3(man.Global_point[i].x, 0, man.Global_point[i].z), new Vector3(man.Global_point[Last_global_pos_num].x, 0, man.Global_point[Last_global_pos_num].z));
            }

            if (Dist > 1.3f)// Если не предыдущий кластер
            {
                Dist = Dist_to_point_xz(new Vector3(man.Global_point[i].x, 0, man.Global_point[i].z), transform.position);
                if (Dist < Global_point_search_dist) // Если входит в радиус поиска
                {
                    //Debug.LogWarning(Dist);
                    //Debug.LogWarning("Connect to "+i +" x = "+ man.Global_point[i].x);
                    Vector3 vect = new Vector3(man.Global_point[i].x, 0, man.Global_point[i].z);
                    if (dist_point_to_ray(transform.position, vect, Robot_size)) // Если можно соеденить не задев препятствия
                    {
                        if (Dist < min_dist)
                        {
                            min_dist = Dist;
                            point_min = i;
                        }
                    }
                }
            }
        }
        if (point_min == -1)
        {
            return false;
        }
        else
        {
            Generate_graph_glpoint_to_glpoint(Point_local_road); // Строим маршрут
            counter_task++;

            man.Global_point[Last_global_pos_num].last_point_num_2 = point_min; // Соединяем глобальные точки
            man.Global_point[point_min].visited = 1;
            Mode_move = true; // Режим движдения по глобальным точкам

            for (int k = 0; k < Local_point_list.Count; k++)
            {
                Destroy(Local_point_list[k]);
            }
            Destroy(set_point);
            //Destroy(Newpoint_set);

            Local_point_list.Clear();
            Point_local_road.Clear();


            Newpoint_set = Instantiate(Point_point_goto, transform.position, Quaternion.identity) as GameObject;
            Newpoint_set.GetComponent<Point>().first = true;
            Local_point_list.Add(Newpoint_set);

            Last_global_pos_num = point_min;
            search_global_point();
            Generate_graph();

        }
        return true;
    }

    // Движение к выбранной точке
    public void move_to_global_point_to_orange()
    {
        int counter_mov_to_global_point = Path_to_global_point.Count - 1;
        Vector3 pos = new Vector3(man.Global_point[Path_to_global_point[counter_mov_to_global_point]].x, transform.position.y, man.Global_point[Path_to_global_point[counter_mov_to_global_point]].z);

        if (Count_rotation < 50)
        {
            Count_rotation++;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, pos, Speed_Robot);
        }

        // ----------------------

        if (transform.position == pos)
        {
            Debug.Log("2");
            general_angle_to_walk = man.Global_point[Path_to_global_point[counter_mov_to_global_point]].angle;
            man.Global_point[Path_to_global_point[counter_mov_to_global_point]].visited = 1;
            Last_global_pos_num = Path_to_global_point[counter_mov_to_global_point];
            Count_rotation = 0;

            Path_to_global_point.RemoveAt(counter_mov_to_global_point);
            
        }
        else
        {
            Vector3 der = pos - transform.position;
            Quaternion rotation = Quaternion.LookRotation(der);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 2.5f * Time.deltaTime);
        }
    }

  
    public void move_to_last_global_point()
    {

        //int count = Local_point_list.Count-1;
        if (zas_1 == false)
        {
            count_reve = Local_point_list.Count - 1;
            zas_1 = true;
        }

        if (count_reve >= 0)
        {
            Vector3 pos = Local_point_list[count_reve].transform.position;
            if (Count_rotation < 50)
            {
                Count_rotation++;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, pos, Speed_Robot);
            }
            if (transform.position == pos)
            {
                //Destroy(Local_point_list[count]);
                //Local_point_list.RemoveAt(count);
                //Point_local_road.RemoveAt(count);
                count_reve--;

                Count_rotation = 0;
            }
            else
            {
                Vector3 der = pos - transform.position;
                Quaternion rotation = Quaternion.LookRotation(der);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 2.5f * Time.deltaTime);
            }
        }
        else
        {
            zas_1 = false;
            for (int k = 0; k < Local_point_list.Count; k++)
            {
                Destroy(Local_point_list[k]);
            }
            Destroy(set_point);

            Local_point_list.Clear();
            Point_local_road.Clear();

            Newpoint_set = Instantiate(Point_point_goto, transform.position, Quaternion.identity) as GameObject;
            Newpoint_set.GetComponent<Point>().first = true;
            Local_point_list.Add(Newpoint_set);


            RRT_SPAWN_POINTS();
            man.Render_line();
            Stuck_robot = false;



        }
    }
    //////////////////////////////////////////////////////////
    //      Функция движения к точке и выбор маршрута       //
    //////////////////////////////////////////////////////////
    private void move(GameObject PositionNext)
    {
        if (Count_rotation < 50)
        {
            Count_rotation++;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, PositionNext.transform.position, Speed_Robot);
        }

        if (transform.position == PositionNext.transform.position)
        {

            RRT_SPAWN_POINTS();
            Count_rotation = 0;
        }
        else
        {
            Vector3 der = PositionNext.transform.position - transform.position;
            Quaternion rotation = Quaternion.LookRotation(der);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 2.5f * Time.deltaTime);
        }
    }
    //////////////////////////////////////////////////////////
    // Глобальный планировщик движения по глобальным точкам //
    //////////////////////////////////////////////////////////
    public void search_global_point()
    {
        int go_to = 0; // Список возможных направлений
        float dist_min = 1000;
        bool set_point = false;
        // Поиск ближайшей по расстоянию точки для движения к ней
        for (int i = 0; i<man.Global_point.Count; i++)
        {
            float dist_t = Dist_to_point_xz(new Vector3(man.Global_point[i].x, 0, man.Global_point[i].z), transform.position);
            if ((man.Global_point[i].visited==0) && (dist_t < dist_min))
            {
                set_point = true;
                dist_min = dist_t;
                go_to = i;
            }
        }

        if (set_point)
        {
            finish_work = false;
            if (Last_global_pos_num >= 0)
            {
                Path_to_global_point = Deicstra(Last_global_pos_num, go_to);
            }
            else
            {

                Path_to_global_point = Deicstra(0, go_to);
            }
            if (Path_to_global_point != null)
            {
                man.Global_point[go_to].visited = 2;
            }
        }
        else
        {
            finish_work = true;
        }
    }



    //////////////////////////////////////////////////////////
    //      Функция лидара (имитация + обработка данных)    //
    //////////////////////////////////////////////////////////
    private void lidar()
    {
        RaycastHit hit;
        Ray ray = new Ray(Lidar.transform.position, Lidar.transform.forward);
        Vector3 Lidar_posirion = Lidar.transform.position;

        float angle = Lidar.transform.eulerAngles.y + 90f;

        Physics.Raycast(ray, out hit, Lidar_dist);
        if (hit.collider != null)
        {
            float Distance = hit.distance;
            if (angle >= 360)
            {
                angle -= 360;
            }
            if (angle <= -360)
            {
                angle += 360;
            }
            angle *= Mathf.Deg2Rad;
            // Distance

            Vector3 position = new Vector3(Lidar_posirion.x - Distance * Mathf.Cos(angle), Lidar_posirion.y, Lidar_posirion.z + Distance * Mathf.Sin(angle));
            if (Point_correct(position, Point_min_distance))
            {
                man.Lidar_vect.Add(position);
                Instantiate(Point_Lidar, position, Quaternion.identity).transform.SetParent(manager_gameobj.transform);
            }
        }
        Lidar.transform.Rotate(0, Speed_Lidar + Random.Range(-0.3f, 0.3f), 0);
    }

    //////////////////////////////////////////////////////////
    //         Установка случайных точек                    //
    //////////////////////////////////////////////////////////
    public void RRT_SPAWN_POINTS()
    {
        bool no_set_point = true;
        for (int i = 0; i<200; i++)
        {
            float angle = -general_angle_to_walk+90;
            //angle = 30;
            Vector3 New_poin_generate = new Vector3();

            if (first_iter)
            {
                New_poin_generate = transform.position;
            }
            else
            {
                float coord_z = Random.Range(0, Randon_distance);
                float coord_x = 0;
                if (coord_z > Randon_distance / 10)
                {
                    coord_x = Random.Range(-coord_z, coord_z);
                }
                else 
                {
                    coord_x = Random.Range(-Randon_distance / 10, Randon_distance / 10);
                }

                New_poin_generate = new Vector3(transform.position.x + coord_x, 0.5f, transform.position.z + coord_z);
            }

            float pos_rotation_x = (New_poin_generate.x - Lidar.transform.position.x)* Mathf.Cos(angle * Mathf.Deg2Rad) - (New_poin_generate.z - Lidar.transform.position.z) * Mathf.Sin(angle * Mathf.Deg2Rad) + Lidar.transform.position.x;
            float pos_rotation_z = (New_poin_generate.x - Lidar.transform.position.x) * Mathf.Sin(angle * Mathf.Deg2Rad) + (New_poin_generate.z - Lidar.transform.position.z) * Mathf.Cos(angle * Mathf.Deg2Rad) + Lidar.transform.position.z;


            New_poin_generate = new Vector3(pos_rotation_x, 0.5f, pos_rotation_z);

            //Instantiate(Point_point_goto, New_poin_generate, Quaternion.identity);

            //Debug.Log(angle);
            //general_angle_to_walk
           
            if (dist_point_to_ray(transform.position, New_poin_generate, Robot_size) && i > 0)
            {
                no_set_point = false;
                set_point = Instantiate(Point_point_goto, New_poin_generate, Quaternion.identity);
                int num = Local_point_list.Count;
                set_point.name = num.ToString();

                Point_local_road.Add(New_poin_generate);
                Local_point_list.Add(Newpoint_set);
                //Local_point_list.Add(set_point);

                set_point.transform.SetParent(lidar_point_Parant_gameobj.transform);
                Newpoint_set.GetComponent<Point>().point_next[0] = set_point;
                Newpoint_set = Newpoint_set.GetComponent<Point>().point_next[0];
                man.Render_line();

                //searc_global_point_connect();

                List<float> angle_to_void = Search_fork();

                //Debug.Log(angle_to_void.Count);

                //Debug.Log("2");
                if (angle_to_void.Count <= 2)
                {
                    if (angle_to_void.Count == 2)
                    {
                        if (((Mathf.Abs(angle_to_void[0]) + Mathf.Abs(angle_to_void[1]) > 160) && (Mathf.Abs(angle_to_void[0])) + Mathf.Abs(angle_to_void[1]) < 200)){
                            // Если коридор
                            //Debug.Log("коридор");
                            if (first_iter)
                            {
                                Searc_instance_global_point(angle_to_void);
                            }
                        }
                        else
                        {
                            // Если поворот
                            //Debug.Log("поворот");
                            Searc_instance_global_point(angle_to_void);
                        }
                    }
                    if (angle_to_void.Count == 1)
                    {
                        // Если тупик
                        //Debug.Log("тупик");
                        Searc_instance_global_point(angle_to_void);
                        
                        // ВСТАВИТЬ Алгоритм возврата назад
                    }
                }
                else
                {
                    // Если развилка с количеством путей >= 3
                    //Debug.Log("развилка");
                    Searc_instance_global_point(angle_to_void);
                }
                first_iter = false;

                Generate_graph();
                break;
            }

        }
        if (no_set_point)
        {
            Stuck_robot = true;
            return;
        }
        first_iter = false;
    }

    // Построение кратчайшего маршрута локальных точек к новой глобальной точки и Дэикстра
    private void Generate_graph_glpoint_to_glpoint(List<Vector3> Point)
    {
        if (Point.Count <= 1)
        {
            return;
        }
        float[,] dijkstra_array_dist = new float[Point.Count, Point.Count];
        float[] node = new float[Point.Count];
        bool[] visited = new bool[Point.Count];

        // составление весов графов
        for (int count = 0; count < Point.Count; count++)
        {
            node[count] = 1000;
            visited[count] = false;
            for (int j = 0; j < Point.Count; j++)
            {
                dijkstra_array_dist[count, j] = 1000;
            }
        }
        
        for (int count = 0; count < Point.Count; count++)
        {
            for (int j = count; j < Point.Count; j++)
            {
                if (count != j)
                {
                    if (dist_point_to_ray(Point[count], Point[j], Robot_size))
                    {
                        dijkstra_array_dist[count, j] = Dist_to_point_xz(Point[count], Point[j]);
                        dijkstra_array_dist[count, j] = (float)System.Math.Round((double)dijkstra_array_dist[count, j], 4);
                    }
                }
            }
        }


        node[0] = 0;
        visited[0] = true;
        int w = 0; // Текущая точка

        bool trigg_new_node = false; // Если есть лучший маршрут, рассчитаем относительно него
        for (int i = 0; i < Point.Count; i++)
        {
            List<int> Point_count = new List<int>();
            for (int j = 0; j < Point.Count; j++)
            {
                if (w != j)
                {
                    if (dijkstra_array_dist[w, j] != 1000)
                    {
                        if (visited[j] == false)
                        {
                            if (node[w] + dijkstra_array_dist[w, j] <= node[j])
                            {
                                node[j] = node[w] + dijkstra_array_dist[w, j];
                                node[j] = (float)System.Math.Round((double)node[j], 4);
                            }
                        }
                    }
                }
            }
            if (!trigg_new_node)
            {
                float min = 1000;
                int min_number = 0;
                visited[w] = true;

                for (int s = 0; s < node.Length; s++)
                {
                    if (visited[s] == false)
                    {
                        if (node[s] < min)
                        {
                            min = node[s];
                            min_number = s;
                        }
                    }
                }
                w = min_number;
            }
        }

        if (node[Point.Count-1] >= 1000)
        {
            Debug.Log("Маршрут не найден");
        }

        float finish_dist = node[node.Length-1];
        w = node.Length - 1;
        int c = 0;
        //Debug.Log("5");
        while ((w != 0) && c < 1000)
        {
            c++;
            for (int j = 0; j < node.Length; j++)
            {
                if (w != j)
                {
                    if (dijkstra_array_dist[w, j] < 1000)
                    {
                        float fff = node[w] - dijkstra_array_dist[w, j];
                        fff = (float)System.Math.Round((double)fff, 4);
                        float aaa = node[j];
                        aaa = (float)System.Math.Round((double)aaa, 4);
                        if (fff == aaa)
                        {

                            man.Global_point.Add(new Manager.Global_points(man.Global_point.Count, Point[w].x, Point[w].z, Last_global_pos_num, visited: 1));
                            man.Global_point[man.Global_point.Count-1].last_point_num = Last_global_pos_num;
                            Last_global_pos_num = man.Global_point.Count - 1;

                            finish_dist -= dijkstra_array_dist[w, j];
                            w = j;
                            break;
                        }
                    }
                }
            }
        }


        //Debug.Log("6");
        //path.Add(start);
        //man.Global_point.Add(new Manager.Global_points(man.Global_point.Count, Point[j].x, Point[j].z, Last_global_pos_num, visited: 1));
        //Last_global_pos_num = man.Global_point.Count - 1;

        for (int k = 0; k < Local_point_list.Count; k++)
        {
            Destroy(Local_point_list[k]);
        }
        Destroy(set_point);
        Local_point_list.Clear();
        Generate_graph();
    }

    // Дэикстра глобальная
    public List<int> Deicstra(int start, int finish)
    {
        List<int> path = new List<int>();

        int size = man.Global_point.Count;
        float[,] dijkstra_array_dist = new float[size, size];
        float[] node = new float[size];
        bool[] visited = new bool[size];

        for (int i = 0; i < man.Global_point.Count; i++)
        {
            node[i] = 1000;
            visited[i] = false;
            for (int j = 0; j < size; j++)
            {
                dijkstra_array_dist[i, j] = 1000;
            }
        }

        // составление весов графов
        for (int i = 0; i < man.Global_point.Count; i++)
        {
            int last = man.Global_point[i].last_point_num;

            Vector3 first = new Vector3(man.Global_point[i].x, 0, man.Global_point[i].z);
            Vector3 second = new Vector3(man.Global_point[last].x, 0, man.Global_point[last].z);

            float dist = Dist_to_point_xz(first, second);
            dijkstra_array_dist[i, last] = dist;
            dijkstra_array_dist[last, i] = dist;
            
            if (man.Global_point[i].last_point_num_2 != -1)
            {
                last = man.Global_point[i].last_point_num_2;
                first = new Vector3(man.Global_point[i].x, 0, man.Global_point[i].z);
                second = new Vector3(man.Global_point[last].x, 0, man.Global_point[last].z);
                dist = Dist_to_point_xz(first, second);
                dijkstra_array_dist[i, last] = dist;
                dijkstra_array_dist[last, i] = dist;
            }
        }

        // Вычисление маршрута (минимальная стоимость)

        node[start] = 0;
        visited[start] = true;
        int w = start; // Текущая точка

        bool trigg_new_node = false; // Если есть лучший маршрут, рассчитаем относительно него
        for (int i = 0; i < size; i++)
        {
            List<int> Point_count = new List<int>();
            for (int j = 0; j < size; j++)
            {
                if (w != j)
                {
                    if (dijkstra_array_dist[w, j] != 1000)
                    {
                        if (visited[j] == false)
                        {
                            if (node[w] + dijkstra_array_dist[w, j] <= node[j])
                            {
                                node[j] = node[w] + dijkstra_array_dist[w, j];
                                node[j] = (float)System.Math.Round((double)node[j], 4);
                            }
                        }
                    }
                }
            }
            if (!trigg_new_node)
            {
                float min = 1000;
                int min_number = 0;
                visited[w] = true;

                for (int s = 0; s < node.Length; s++)
                {
                    if (visited[s] == false)
                    {
                        if (node[s] < min)
                        {
                            min = node[s];
                            min_number = s;
                        }
                    }
                }
                w = min_number;
            }
        }
        if (node[finish] >= 1000)
        {
            Debug.Log("Маршрут не найден");
        }
        // Вычисление маршрута (Позиции)

        float finish_dist = node[finish];
        w = finish;
        int c = 0;

        while ((w != start) && c < 1000)
        {
            c++;
            for (int j = 0; j < node.Length; j++)
            {
                if (w != j)
                {
                    if (dijkstra_array_dist[w, j] < 1000)
                    {
                        float fff = node[w] - dijkstra_array_dist[w, j];
                        fff = (float)System.Math.Round((double)fff, 4);
                        float aaa = node[j];
                        aaa = (float)System.Math.Round((double)aaa, 4);
                        if (fff == aaa)
                        {
                            path.Add(w);
                            //man.Global_point.Add(new Manager.Global_points(man.Global_point.Count, Point[j].x, Point[j].z, Last_global_pos_num, visited: 1));
                            //Last_global_pos_num = man.Global_point.Count - 1;

                            finish_dist -= dijkstra_array_dist[w, j];
                            w = j;
                            break;
                        }
                    }
                }
            }
        }

        path.Add(start);

        for (int k = 0; k < Local_point_list.Count; k++)
        {
            Destroy(Local_point_list[k]);
        }
        Destroy(set_point);

        Local_point_list.Clear();
        Point_local_road.Clear();


        return path;
    }


    // Если в радиусе от робота нету глобальных точек - поставит ее
    public void Searc_instance_global_point(List<float> angle_to_void)
    {
        List<int> num_list = new List<int>();
        List<float> angle_to_point = new List<float>();
        List<Vector3> Position_point_with_angle = new List<Vector3>();

        int num = man.Global_point.Count;
        int var_of_count_glp = man.Global_point.Count;
        int counter = 0;

        for (int k = 0; k < angle_to_void.Count; k++)
        {
            float angle_pos = angle_to_void[k];
            //Debug.Log(angle_pos);
            Vector3 position_test = new Vector3(Lidar.transform.position.x - Global_points_dist * Mathf.Cos(angle_pos * Mathf.Deg2Rad), 2f, Lidar.transform.position.z + Global_points_dist * Mathf.Sin(angle_pos * Mathf.Deg2Rad));
            
            Position_point_with_angle.Add(position_test);
            angle_to_point.Add(angle_to_void[k]);
            num_list.Add(num_list.Count);
            counter++;

            for (int j = 0; j < var_of_count_glp; j++)
            {
                if (Dist_to_point_xz(position_test, new Vector3(man.Global_point[j].x, 0, man.Global_point[j].z)) < Dist_to_set_global_point)
                {
                    return;
                }
            }
        }


        Generate_graph_glpoint_to_glpoint(Point_local_road);

        for (int j = 0; j < num_list.Count; j++)
        {
            num_list[j] += man.Global_point.Count;
        }


        // Поиск минимального расстояния от предыдущей точки к текущей группе пока не поставленных точек (Для соединения предыдущей группы с текущей группой) 
        
        float dist_min = 1000;
        int min_num = 0;
        if (man.Global_point.Count != 0)
        {
            for (int j = 0; j < num_list.Count; j++)
            {
                //Vector3 a = new Vector3(man.Global_point[Last_global_pos_num].x, 0, man.Global_point[Last_global_pos_num].z);
                float dist = Dist_to_point_xz(new Vector3(man.Global_point[Last_global_pos_num].x, 0, man.Global_point[Last_global_pos_num].z), new Vector3(Position_point_with_angle[j].x, 0, Position_point_with_angle[j].z));
                if (dist < dist_min)
                {
                    dist_min = dist;
                    min_num = j;
                }
            }
        }
        else
        {
            min_num = -1;
        }


        int center_num = num_list.Count + man.Global_point.Count;
        int min_num_point = 0;
        for (int j = 0; j < num_list.Count; j++)
        {
            if ((j == min_num) && (!first_iter))
            {
                man.Global_point.Add(new Manager.Global_points(num_list[j], Position_point_with_angle[j].x, Position_point_with_angle[j].z, Last_global_pos_num, angle_to_point[j], 1));
                min_num_point = num_list[j];
            }
            else
            {
                man.Global_point.Add(new Manager.Global_points(num_list[j], Position_point_with_angle[j].x, Position_point_with_angle[j].z, center_num, angle_to_point[j]));
            }
        }

        // Установка центральной точки
        if (min_num == -1)
        {
            man.Global_point.Add(new Manager.Global_points(man.Global_point.Count, Lidar.transform.position.x, Lidar.transform.position.z, center_num, visited: 1, hub: true));
        }
        else
        {
            man.Global_point.Add(new Manager.Global_points(man.Global_point.Count, Lidar.transform.position.x, Lidar.transform.position.z, min_num, visited: 1, hub: true));
            man.Global_point[center_num].last_point_num = min_num_point;
        }

        Last_global_pos_num = man.Global_point.Count-1;
        if ((angle_to_void.Count > 1) && (!Mode_move)) 
        {
            Next_angle(Last_global_pos_num);
        }
        else
        {
            search_global_point();
            Mode_move = true;
        }
        counter_task++;
    }

    // Выбор генерального направления
    public void Next_angle(int Last_pos)
    {
        //Debug.Log("8");
        // проверить доступно ли направление (перебором и поиском узла, который не посещен) !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //general_angle_to_walk = angle_to_void[Rand_angle];

        List<int> num_Global_point = new List<int>();

        for (int j = 0; j < man.Global_point.Count; j++)
        {
            if (man.Global_point[j].last_point_num == Last_pos)
            {
                num_Global_point.Add(j);
            }
        }
        //Debug.Log("9");

        int Rand_angle = Random.Range(0, num_Global_point.Count-1);

        man.Global_point[num_Global_point[Rand_angle]].visited = 1;
        //Debug.Log(man.Global_point[num_Global_point[Rand_angle]].angle);
        general_angle_to_walk = man.Global_point[num_Global_point[Rand_angle]].angle;
        Last_global_pos_num = num_Global_point[Rand_angle];
        //Debug.Log("10");
    }

    // Отрисовка графа
    public void Generate_graph()
    {
        //Debug.LogWarning("Gen");
        GameObject[] del_obj = GameObject.FindGameObjectsWithTag("Point_global");
        for (int k = 0; k < del_obj.Length; k++)
        {
            Destroy(del_obj[k]);
        }

        GameObject[] global_point_s = new GameObject[man.Global_point.Count];

        for (int j = 0; j < man.Global_point.Count; j++)
        {
            //Debug.Log(Global_point[j].num);
            global_point_s[j] = Instantiate(Global_point_pref, new Vector3(man.Global_point[j].x, 2, man.Global_point[j].z), Quaternion.identity);
            if (man.Global_point[j].visited == 1)
            {
                global_point_s[j].GetComponent<MeshRenderer>().material = material_green;
                //material_green
            }
            if (man.Global_point[j].visited == 2)
            {
                global_point_s[j].GetComponent<MeshRenderer>().material = material_orange;
                //material_green
            }
        }
        for (int j = 0; j < man.Global_point.Count; j++)
        {
            global_point_s[j].GetComponent<Point>().point_next[0] = global_point_s[man.Global_point[j].last_point_num];
            if (man.Global_point[j].last_point_num_2 != -1)
            {
                global_point_s[j].GetComponent<Point>().point_next[1] = global_point_s[man.Global_point[j].last_point_num_2];
            }
        }
        man.Render_line();
        //Debug.LogWarning("exit");
    }

    // Проверка, есть ли рядом точки, которые лидар уже отметил
    public bool Point_correct(Vector3 pos_point, float dist)
    {
        for (int i=0; i < man.Lidar_vect.Count; i++)
        {
            if (Vector2.Distance(new Vector2(man.Lidar_vect[i].x, man.Lidar_vect[i].z), new Vector2(pos_point.x, pos_point.z)) < dist)
            {
                return false;
            }
        }
        return true;
    }


    // Вычисление возможно ли проехать из А в Б не задев препятствия
    public bool dist_point_to_ray(Vector3 position_start, Vector3 position_finish, float dist_min)
    {
        double x1 = (double) position_start.x;
        double y1 = (double) position_start.z;

        double x2 = (double) position_finish.x;
        double y2 = (double) position_finish.z;

        // ax+by+c 
        // y1
        // x*(y2-y1)-y*(x2-x1)-x1*y2+y1*x2 = 0

        double a = y2 - y1;
        double b = -(x2 - x1);
        double c = -x1 * y2 + y1 * x2;
        Vector2 P2_P1 = new Vector2(position_finish.x - position_start.x, position_finish.z - position_start.z);

        for (int i = 0; i < man.Lidar_vect.Count; i++)
        {
            double x = (double) man.Lidar_vect[i].x;
            double y = (double) man.Lidar_vect[i].z;
            Vector2 P2_M = new Vector2((float)(x - position_start.x), (float)(y - position_start.z));

            //float Perp = Vector2.Dot(P2_P1, P2_M) / Mathf.Pow((Mathf.Sqrt(Mathf.Pow(position_finish.x - position_start.x, 2) + Mathf.Pow(position_finish.z - position_start.z, 2))), 2);
            float Perp = Vector2.Dot(P2_P1, P2_M) / (Mathf.Pow(position_finish.x - position_start.x, 2) + Mathf.Pow(position_finish.z - position_start.z, 2));

            if (Perp>=0 && Perp<=1)
            {
                float dist = Mathf.Abs((float)(a * x + b * y + c)) / Mathf.Sqrt((float)(a * a + b * b));
                if (dist < dist_min)
                {
                    return false;
                }
            }
            else
            {
                float dist = Dist_to_point_xz(position_finish, man.Lidar_vect[i]);
                //float dist = Dist_to_point_xz(position_finish, position_start);
                if (dist < dist_min)
                {
                    return false;
                }
            }
            
        }
        return true;
    }


    public List<float> Search_fork()
    {

        List<float> angele_pints = new List<float>();
        List<float> angle_direction = new List<float>();

        // угл поиска = вычисление угла в равнобедренном треугольнике
        // с высотой равной дистанции Dist_to_search_angle
        // и основанием равным размеру робота
        // ctg (angele_searc/2) = 2*Randon_distance/Robot_size

        float angele_searc = Mathf.Atan2(Robot_size * 2, 2 * Dist_to_search_angle) * Mathf.Rad2Deg * 2;
        for (int i = 0; i < man.Lidar_vect.Count; i++)
        {
            if ((Dist_to_point_xz(man.Lidar_vect[i], Lidar.transform.position) < Dist_to_search_angle))
            {
                float x_b = -man.Lidar_vect[i].x + Lidar.transform.position.x;
                float y_b = man.Lidar_vect[i].z - Lidar.transform.position.z;
                float angle_to_point_d = Mathf.Atan2(y_b, x_b) / Mathf.PI * 180;
                angele_pints.Add(angle_to_point_d);
            }
        }

        angele_pints.Sort();
        float new_angle = 0;
        for (int i = 0; i < angele_pints.Count-1; i++)
        {
            //Debug.Log(angele_pints[i]);
            new_angle = angele_pints[i] + angele_searc;
            if (new_angle < angele_pints[i + 1])
            {
                float Angle_d_buff = Mathf.Atan2(Mathf.Sin(angele_pints[i] * Mathf.Deg2Rad) + Mathf.Sin(angele_pints[i + 1] * Mathf.Deg2Rad), Mathf.Cos(angele_pints[i] * Mathf.Deg2Rad) + Mathf.Cos(angele_pints[i + 1] * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
                angle_direction.Add(Angle_d_buff);
               
            }
        }
        new_angle = angele_pints[angele_pints.Count - 1] + angele_searc;
        if (new_angle > 180)
        {
            new_angle = new_angle - 360;
        }
        /*
        Debug.Log("1 = " + angele_pints[angele_pints.Count - 1]);
        Debug.Log("2 = " + angele_pints[0]);
        Debug.Log("3 = "+new_angle);
        Debug.Log("4 = " + angele_searc);
        Debug.Log("-----------------------");
        */
        if (new_angle > 0)
        {
            float Angle_d_buff = Mathf.Atan2(Mathf.Sin(angele_pints[0] * Mathf.Deg2Rad) + Mathf.Sin(angele_pints[angele_pints.Count - 1] * Mathf.Deg2Rad), Mathf.Cos(angele_pints[0] * Mathf.Deg2Rad) + Mathf.Cos(angele_pints[angele_pints.Count - 1] * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
            angle_direction.Add(Angle_d_buff);
        }
        else
        {
            if (new_angle < angele_pints[0])
            {
                float Angle_d_buff = Mathf.Atan2(Mathf.Sin(angele_pints[0] * Mathf.Deg2Rad) + Mathf.Sin(angele_pints[angele_pints.Count - 1] * Mathf.Deg2Rad), Mathf.Cos(angele_pints[0] * Mathf.Deg2Rad) + Mathf.Cos(angele_pints[angele_pints.Count - 1] * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
                angle_direction.Add(Angle_d_buff);
            }
        }

        return angle_direction;
    }

    public float Dist_to_point_xz(Vector3 first, Vector3 second)
    {
        return Mathf.Sqrt(Mathf.Pow(first.x - second.x, 2) + Mathf.Pow(first.z - second.z, 2));
    }

}
