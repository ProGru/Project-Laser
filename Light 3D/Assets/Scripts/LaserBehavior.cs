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
    [Header("Surface Instantination")]
    [Tooltip("If selected in the place where ray connect with surface the object will be instantinated")]
    public bool enableSurfaceInstantination = false;
    [Tooltip("Prefab for Surface instantination")]
    public GameObject surfacePrefab;
    [Tooltip("If selected scale wil change for each object instantinated")]
    public bool useScalingByObjectCount;
    [Tooltip("Change of object scale per object")]
    public float scaleFactor = 1;
    [Tooltip("If selected set rotation to surface normal")]
    public bool useNormalRotation;
    [Tooltip("If selected set color to surface material color")]
    public bool useColorChanging;

    List<LineRenderer> lineRendererList = new List<LineRenderer>();
    [SerializeField]
    List<GameObject> hittenObjects = new List<GameObject>();
    List<GameObject> surfaceObjects = new List<GameObject>();
    int createdLasers = 0;
    int lasersInUse = 0;
    int laserReflectionUsage = 0;
    int surfacePrefabInUse = 0;
    LineRenderer laser;
    LineRenderer prefabLineRenderer;
    Color surfacePrefabColor;
    const int first = 0;

    void Start()
    {
        prefabLineRenderer = linePrefab.GetComponent<LineRenderer>();
        GameObject gmb = Instantiate(linePrefab);
        laser = gmb.GetComponent<LineRenderer>();
        surfacePrefabColor = surfacePrefab.GetComponent<MeshRenderer>().sharedMaterials[0].color;
        if (isStatic)
        {
            StartLaser();
        }
    }

    private void FixedUpdate()
    {
        if (!isStatic)
        {
            CleanLaser();
            StartLaser();
        }
    }

    /// <summary>
    /// Clean lineRendererList (set position count to 0 for each LineRenderer, clear color),
    /// and prepare variables for reuse, clear hittenObject.
    /// </summary>
    private void CleanLaser()
    {
        lasersInUse = 0;
        laserReflectionUsage = 0;
        foreach (LineRenderer line in lineRendererList)
        {
            line.positionCount = 0;

            line.materials[first] = prefabLineRenderer.sharedMaterials[first];
            line.startColor = prefabLineRenderer.startColor;
            line.endColor = prefabLineRenderer.endColor;
        }
        hittenObjects.Clear();

        surfacePrefabInUse = 0;
        foreach (GameObject obj in surfaceObjects)
        {
            obj.transform.position = new Vector3();
            obj.transform.localScale = Vector3.zero;
            obj.transform.rotation = surfacePrefab.transform.localRotation;
        }
    }

    /// <summary>
    /// Start laser behavior and set two first point of laser in start point in given direction.
    /// </summary>
    private void StartLaser()
    {
        laser.SetPosition(0, startPoint.transform.position);
        laser.SetPosition(1, startPoint.transform.TransformDirection(directionPoint.transform.localPosition));
        FollowLaserReflectionByDistance(laser, startPoint.transform.position, startPoint.transform.TransformDirection(directionPoint.transform.localPosition), lenght, 2);
    }

    /// <summary>
    /// Using given lineRenderer generate bounces of laser from startingPoint in direction limited by distance
    /// and laserReflectionLimit
    /// </summary>
    /// <param name="line">lineRenderer passed for generate bouces/reflectios</param>
    /// <param name="startingPoint">starting point for lineRenderer</param>
    /// <param name="direction">direction in whith line will be going</param>
    /// <param name="distance">distace limit for all bounces of line(ray)</param>
    /// <param name="laserCount">curent LineRenderer position point usage</param>
    void FollowLaserReflectionByDistance(LineRenderer line, Vector3 startingPoint, Vector3 direction, float distance, int laserCount)
    {
        if (laserReflectionUsage <= reflectionLimit)
        {
            RaycastHit hit;
            line.positionCount = laserCount;
            Vector3 laserEndPoint;
            // Check if laser intersect with object using Raycast and change end point of laser using hit point or start point direction and distance
            if (Physics.Raycast(startingPoint, direction, out hit, distance))
            {
                laserEndPoint = hit.point;
                AddObjectToHittenObject(hit.transform.gameObject);
                CreateLaserRay(hit, direction, laserCount + 1, line);
                SurfacePrefabInstantination(hit);

            }
            else
            {
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
            ColorManager(usedLaser);

            RaycastHit hit;
            Vector3 laserEndPoint;
            // Check if laser intersect with object using Raycast and change end point of laser using hit point or create new laser in reflected direction
            if (Physics.Raycast(startingPoint, direction, out hit, distance))
            {
                laserEndPoint = hit.point;
                AddObjectToHittenObject(hit.transform.gameObject);
                CreateLaserRay(hit, direction, 3, usedLaser);
                SurfacePrefabInstantination(hit);
                
            }
            else
            {
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
    void CreateLaserRay(RaycastHit hit, Vector3 direction, int laserOrder, LineRenderer usedLaser)
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
                FollowLaserReflectionByDistance(usedLaser, hit.point, reflectVec, newDistance, laserOrder);
            }
        }
    }

    /// <summary>
    /// Change color of givenLaser on hit object material color
    /// </summary>
    /// <param name="obj">GameObject hiten with laser</param>
    /// <param name="usedLaser">LineRenderer of whith the color will be changed</param>
    void ChangeLaserColorByHitObject(GameObject obj, LineRenderer usedLaser)
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
    void MixLaserColorByHitObject(GameObject obj, LineRenderer usedLaser)
    {
        Material objectMaterial = obj.GetComponent<Renderer>().materials[first];
        usedLaser.startColor = Color.Lerp(objectMaterial.color, usedLaser.startColor,0.5f);
        usedLaser.endColor = Color.Lerp(objectMaterial.color, usedLaser.endColor, 0.5f);
    }

    /// <summary>
    /// Change material of givenLaser on hit object material 
    /// </summary>
    /// <param name="obj">GameObject hiten with laser</param>
    /// <param name="usedLaser">LineRenderer of whith the material will be changed</param>
    void ChangeLaserMaterialByHitObject(GameObject obj, LineRenderer usedLaser)
    {
        Material objectMaterial = obj.GetComponent<Renderer>().materials[first];
        usedLaser.material = objectMaterial;
    }

    /// <summary>
    /// Add item to hitenObject list
    /// </summary>
    /// <param name="item">GameObject object to add to list</param>
    void AddObjectToHittenObject(GameObject item)
    {
        hittenObjects.Add(item);
    }

    /// <summary>
    /// Check for mixed opction, and if there is reflectionTag on object chose metod for mixing colors in lineRenderer
    /// </summary>
    /// <param name="usedLaser">LineRenderer used for creating ray from object the color is colected</param>
    private void ColorManager(LineRenderer usedLaser)
    {
        if (hittenObjects.Count > 0)
        {
            if (hittenObjects[hittenObjects.Count - 1].transform.tag == reflectionTag)
            {
                if (mixed)
                {
                    MixLaserColorByHitObject(hittenObjects[hittenObjects.Count - 1], usedLaser);
                }
                else
                {
                    ChangeLaserColorByHitObject(hittenObjects[hittenObjects.Count - 1], usedLaser);
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

    /// <summary>
    /// Instantinate surfacePrefab in given point, if there is non used instance of prefab is using this one else create new.
    /// If use scalingByObjectCount on- set scale depends of item count.
    /// Set rotation and material color acording to selected obtions
    /// </summary>
    ///<param name="hit">RaycastHit from laser ray to hiten object</param>
    private void SurfacePrefabInstantination(RaycastHit hit)
    {
        if (enableSurfaceInstantination)
        {
            if (surfaceObjects.Count <= surfacePrefabInUse)
            {
                GameObject gmb = Instantiate(surfacePrefab);
                surfaceObjects.Add(gmb);
            }
            GameObject usedLaser = surfaceObjects[surfacePrefabInUse];
            surfacePrefabInUse++;
            usedLaser.transform.position = hit.point;

            //scaling option
            if (useScalingByObjectCount)
            {
                usedLaser.transform.localScale = ScaleFactor.ScaleByObjectCout(surfacePrefabInUse, scaleFactor, surfacePrefab.transform.localScale);
            }
            else
            {
                usedLaser.transform.localScale = ScaleFactor.DefaultScale(surfacePrefab.transform.localScale);
            }
            //color changing option
            if (useColorChanging)
            {
                usedLaser.GetComponent<MeshRenderer>().materials[first].color = GetHitenObjectColor(hit);
            }
            else
            {
                usedLaser.GetComponent<MeshRenderer>().materials[first].color = surfacePrefabColor;
            }
            //normal rotation option
            if (useNormalRotation)
            {
                usedLaser.transform.localRotation = Quaternion.FromToRotation(transform.up, hit.normal);
            }

        }
        
    }

    /// <summary>
    /// Return Color of hitten GameObject
    /// </summary>
    /// <param name="hit">Hitten object by laser Ray</param>
    /// <returns></returns>
    private Color GetHitenObjectColor(RaycastHit hit)
    {
        return hit.transform.GetComponent<MeshRenderer>().materials[first].color;
    }

}

public class ScaleFactor
{
    public static Vector3 ScaleByObjectCout(int count, float factor=1, Vector3? defaultScale = null)
    {
        Vector3 defScale;
        if (defaultScale == null)
        {
            defScale = new Vector3(1, 1, 1);
        }
        else
        {
            defScale = (Vector3)defaultScale;
        }
        float scale = count * factor;
        return new Vector3(defScale.x * scale, defScale.y * scale, defScale.z * scale);

    }

    public static Vector3 DefaultScale(Vector3? defaultScale = null)
    {
        Vector3 scale;
        if (defaultScale == null)
        {
            scale = new Vector3(1, 1, 1);
        }
        else
        {
            scale = (Vector3)defaultScale;
        }
        return scale;
    }

}



