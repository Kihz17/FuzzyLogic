using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuzzyLogic : MonoBehaviour
{
    public Texture2D mapTexture;
    public Mesh carMesh;
    public Material greenMat;
    public Material blackMat;
    public float rayDistance;
    public float maxSpeed;
    public float minAngle;
    public float maxAngle;
    public float rotSpeed;

    private Quaternion counterRot = Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f));

    private List<GameObject> cars = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        for(int x = 0; x < mapTexture.width; x++)
        {
            for (int y = 0; y < mapTexture.height; y++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = new Vector3(x, 0.0f, y);
                BoxCollider boxCollider = go.AddComponent<BoxCollider>();

                Color pixelColor = mapTexture.GetPixel(x, y);
                if(pixelColor == Color.black)
                {
                    go.GetComponent<Renderer>().material = blackMat;

                    boxCollider.size = new Vector3(1.0f, 4.0f, 1.0f);
                }
                else if (pixelColor == Color.green)
                {
                    go.GetComponent<Renderer>().material = greenMat;

                    GameObject car = new GameObject();
                    car.AddComponent<MeshFilter>().mesh = carMesh;
                    car.AddComponent<MeshRenderer>().material = greenMat;
                    car.AddComponent<SphereCollider>().radius = 2.0f;

                    car.transform.position = new Vector3(x, 1.0f, y);
                    car.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    car.transform.localRotation = Quaternion.Euler(90.0f, Random.Range(0.0f, 360.0f), 0.0f);
                    cars.Add(car);
                }
 
            }
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        foreach(GameObject car in cars)
        {
            UpdateCar(car);
        }
    }

    private void UpdateCar(GameObject car)
    {
        Quaternion rot = car.transform.localRotation * counterRot;

        Vector3 straightRay = rot * Vector3.forward * rayDistance;
        Vector3 leftRay = rot * Quaternion.Euler(0.0f, 30.0f, 0.0f) * Vector3.forward * rayDistance;
        Vector3 farLeftRay = rot * Quaternion.Euler(0.0f, 60.0f, 0.0f) * Vector3.forward * rayDistance;
        Vector3 rightRay = rot * Quaternion.Euler(0.0f, -30.0f, 0.0f) * Vector3.forward * rayDistance;
        Vector3 farRightRay = rot * Quaternion.Euler(0.0f, -60.0f, 0.0f) * Vector3.forward * rayDistance;

        List<Vector3> rays = new List<Vector3>();
        rays.Add(straightRay);

        rays.Add(leftRay);
        rays.Add(farLeftRay);

        rays.Add(rightRay);
        rays.Add(farRightRay);

        List<Vector3> modifiedRays = new List<Vector3>();

        // Check for sensor collisions
        float newAngle = 0.0f;
        float speedMult = 1.0f;
        for (int i = 0; i < rays.Count; i++)
        {
            Vector3 ray = rays[i];

            RaycastHit hit;
            if(Physics.Raycast(car.transform.position, Vector3.Normalize(ray), out hit, rayDistance))
            {
                modifiedRays.Add(Vector3.Normalize(ray) * hit.distance);

                float angle = Vector3.Angle(rot * Vector3.forward, Vector3.Normalize(ray));
                float fuzzyAngle = Mathf.Clamp((angle - minAngle) / (maxAngle - minAngle), 0.0f, 1.0f);

                float fuzzyDistance = 1.0f - (hit.distance / rayDistance);
                speedMult = Mathf.Clamp(speedMult - fuzzyDistance, 0.1f, 1.0f);

                if (i > 0 && i <= 2)
                {
                    fuzzyAngle *= -1;
                }

                newAngle += maxAngle * fuzzyAngle * fuzzyDistance;
            }
            else
            {
                modifiedRays.Add(ray);
            }
        }

        Quaternion newRot = Quaternion.Euler(0.0f, newAngle, 0.0f);

        // Draw rays
        foreach (Vector3 ray in modifiedRays)
        {
            Debug.DrawRay(car.transform.position, ray, Color.red);
        }

        // Change rot based on rays
        newRot = newRot * car.transform.localRotation;
        car.transform.localRotation = Quaternion.Slerp(car.transform.localRotation, newRot, Time.deltaTime * rotSpeed);
        rot = car.transform.localRotation * counterRot; // Update rot

        float speed = maxSpeed * speedMult;
        car.transform.position = car.transform.position + (rot * Vector3.forward * speed * Time.deltaTime);
    }
}
