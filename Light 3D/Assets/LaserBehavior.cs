using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBehavior : MonoBehaviour
{
    [Header("Start Setup")]
    public Transform startPoint;
    public Transform directionPoint;
    public string reflectionTag;
    [SerializeField]
    [Tooltip("LineRenderer prefab used for replication")]
    GameObject linePrefab;
    [Header("Laser Parameters")]
    [SerializeField]
    [Tooltip("Limit lenght of the laser")]
    float lenght;
    [SerializeField]
    [Tooltip("Limit bounces/reflectios of laser")]
    int reflectionLimit = 20;
    [Tooltip("If false update laser position each fixed update, if true is called once in Start")]
    public bool isStatic = false;
    [Tooltip("If selected color from bouncing surface with reflected tag wil be mixed, else change color to object color")]
    public bool mixed = false;


    List<LineRenderer> lineRendererList = new List<LineRenderer>();
    [SerializeField]
    List<GameObject> hitenObject = new List<GameObject>();
    int createdLasers = 0;
    int lasersInUse = 0;
    int laserReflectionUsage = 0;
    LineRenderer laser;
    LineRenderer prefabLineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        prefabLineRenderer = linePrefab.GetComponent<LineRenderer>();
        GameObject gmb = Instantiate(linePrefab);
        laser = gmb.GetComponent<LineRenderer>();
        if (isStatic)
        {
            StartLaser();
        }
    }

    private void FixedUpdate()
    {
        //instantinate first starting point of first lineRenderer
        if (!isStatic)
        {
            CleanLaser();
            StartLaser();
        }
    }

    /// <summary>
    /// Clean lineRendererList (set position count to 0 for each LineRenderer, clear color), and is preparing variables for reuse, clear hitenObject.
    /// </summary>
    private void CleanLaser()
    {
        lasersInUse = 0;
        laserReflectionUsage = 0;
        foreach (LineRenderer i in lineRendererList)
        {
            i.positionCount = 0;

            i.materials[0] = prefabLineRenderer.sharedMaterials[0];
            i.startColor = prefabLineRenderer.startColor;
            i.endColor = prefabLineRenderer.endColor;
        }
        hitenObject.Clear();
    }

    /// <summary>
    /// Start laser behavior and set two first point of laser in start point and in given direction point.
    /// </summary>
    private void StartLaser()
    {
        laser.SetPosition(0, startPoint.transform.position);
        laser.SetPosition(1, startPoint.transform.TransformDirection(directionPoint.transform.localPosition));
        FallowLaserReflectionByDistance(laser, startPoint.transform.position, startPoint.transform.TransformDirection(directionPoint.transform.localPosition), lenght, 2);
    }

    /// <summary>
    /// Using given lineRenderer generate bounces of laser from startingPoint in direction limited by distance and laserReflectionLimit
    /// </summary>
    /// <param name="line">lineRenderer passed for generate bouces/reflectios</param>
    /// <param name="startingPoint">starting point for lineRenderer</param>
    /// <param name="direction">direction in whith line will be going</param>
    /// <param name="distance">distace limit for all bounces of line(ray)</param>
    /// <param name="laserCount">curent LineRenderer position point usage</param>
    void FallowLaserReflectionByDistance(LineRenderer line, Vector3 startingPoint, Vector3 direction, float distance, int laserCount)
    {
        if (laserReflectionUsage <= reflectionLimit)
        {
            RaycastHit hit;
            line.positionCount = laserCount;
            Vector3 laserEndPoint;
            // Check if laser intersect with object using Raycast and change end point of laser using hit point or start point direction and distance
            if (Physics.Raycast(startingPoint, direction, out hit, distance))
            {
                Debug.DrawRay(startingPoint, direction * hit.distance, Color.yellow);
                laserEndPoint = hit.point;
                AddObjectToHitenObject(hit.transform.gameObject);
                CreateLaserRay(hit, direction, laserCount + 1, line);
            }
            else
            {
                Debug.DrawRay(startingPoint, direction * distance, Color.white);
                laserEndPoint = direction * distance + startingPoint;
            }
            line.SetPosition(laserCount - 1, laserEndPoint);
        }
    }

    /// <summary>
    /// Create new lineRenderer using linePrefab from startingPoint in direction limited by distance and laserReflectionLimit
    /// </summary>
    /// <param name="startingPoint">starting point for new lineRenderer</param>
    /// <param name="direction">direction in whith line will be going</param>
    /// <param name="distance">distace limit for all bounces of line(ray)</param>
    void CreateLaserReflectionByDistance(Vector3 startingPoint, Vector3 direction, float distance)
    {
        if (laserReflectionUsage <= reflectionLimit)
        {

            LineRenderer usedLaser = LineRendererDeliverer();
            usedLaser.positionCount = 2;
            usedLaser.SetPosition(0, startingPoint);
            ColorMenager(usedLaser);

            RaycastHit hit;
            Vector3 laserEndPoint;
            // Check if laser intersect with object using Raycast and change end point of laser using hit point or create new laser in reflected direction
            if (Physics.Raycast(startingPoint, direction, out hit, distance))
            {
                Debug.DrawRay(startingPoint, direction * hit.distance, Color.yellow);
                laserEndPoint = hit.point;
                AddObjectToHitenObject(hit.transform.gameObject);
                CreateLaserRay(hit, direction, 3, usedLaser);
            }
            else
            {
                Debug.DrawRay(startingPoint, direction * distance, Color.white);
                laserEndPoint = direction * distance + startingPoint;
            }
            usedLaser.SetPosition(1, laserEndPoint);
        }
    }

    /// <summary>
    /// Create new part of laser, and increse laserReflectionUsage
    /// </summary>
    /// <param name="hit">RaycatHit with previous laserRay with object on scene</param>
    /// <param name="direction">direcion of previous laserRay</param>
    /// <param name="laserOrder">order of first point in laser positionCount</param>
    /// <param name="usedLaser">LineRenderer whith we are using for creating new part of laser(Ray) </param>
    private void CreateLaserRay(RaycastHit hit, Vector3 direction, int laserOrder, LineRenderer usedLaser)
    {
        Vector3 reflectVec = Vector3.Reflect(direction, hit.normal);
        float newDistance = lenght - Vector3.Distance(startPoint.transform.position, hit.point);
        if (newDistance > 0)
        {
            if (hit.transform.tag == reflectionTag)
            {
                laserReflectionUsage++;
                CreateLaserReflectionByDistance(hit.point, reflectVec, newDistance);
            }
            else
            {
                laserReflectionUsage++;
                FallowLaserReflectionByDistance(usedLaser, hit.point, reflectVec, newDistance, laserOrder);
            }
        }
    }

    /// <summary>
    /// Change color of givenLaser on hit object material color
    /// </summary>
    /// <param name="obj">GameObject hiten with laser</param>
    /// <param name="usedLaser">LineRenderer of whith the color will be changed</param>
    private void ChangeLaserColorByHitObject(GameObject obj, LineRenderer usedLaser)
    {
        Material objectMaterial = obj.GetComponent<Renderer>().materials[0];
        usedLaser.startColor = objectMaterial.color;
        usedLaser.endColor = objectMaterial.color;
    }

    /// <summary>
    /// Mix color of givenLaser on hit object material color
    /// </summary>
    /// <param name="obj">GameObject hiten with laser (taken color)</param>
    /// <param name="usedLaser">LineRenderer of whith the color will be mixed</param>
    private void MixLaserColorByHitObject(GameObject obj, LineRenderer usedLaser)
    {
        Material objectMaterial = obj.GetComponent<Renderer>().materials[0];
        usedLaser.startColor = Color.Lerp(objectMaterial.color, usedLaser.startColor,0.5f);
        usedLaser.endColor = Color.Lerp(objectMaterial.color, usedLaser.endColor, 0.5f);
    }

    /// <summary>
    /// Change material of givenLaser on hit object material 
    /// </summary>
    /// <param name="obj">GameObject hiten with laser</param>
    /// <param name="usedLaser">LineRenderer of whith the material will be changed</param>
    private void ChangeLaserMaterialByHitObject(GameObject obj, LineRenderer usedLaser)
    {
        Material objectMaterial = obj.GetComponent<Renderer>().materials[0];
        usedLaser.material = objectMaterial;
    }

    /// <summary>
    /// Add item to hitenObject list
    /// </summary>
    /// <param name="item">GameObject object to add to list</param>
    private void AddObjectToHitenObject(GameObject item)
    {
        hitenObject.Add(item);
    }

    /// <summary>
    /// Check for mixed opction, and if there is reflectionTag on object chose metod for mixing colors in lineRenderer
    /// </summary>
    /// <param name="usedLaser">LineRenderer used for creating ray from object the color is colected</param>
    private void ColorMenager(LineRenderer usedLaser)
    {
        if (hitenObject.Count > 0)
        {
            if (hitenObject[hitenObject.Count - 1].transform.tag == reflectionTag)
            {
                if (mixed)
                {
                    MixLaserColorByHitObject(hitenObject[hitenObject.Count - 1], usedLaser);
                }
                else
                {
                    ChangeLaserColorByHitObject(hitenObject[hitenObject.Count - 1], usedLaser);
                }
            }
        }
    }

    /// <summary>
    /// Create new LineRenderer or return existing one depends of lasers in use.
    /// </summary>
    /// <returns> LineRenderer used for curent iteration of laser reflection</returns>
    private LineRenderer LineRendererDeliverer()
    {
        if (lineRendererList.Count <= lasersInUse)
        {
            GameObject gmb = Instantiate(linePrefab);
            lineRendererList.Add(gmb.GetComponent<LineRenderer>());
            createdLasers++;
        }
        LineRenderer usedLaser = lineRendererList[lasersInUse];
        lasersInUse += 1;
        return usedLaser;
    }
}
