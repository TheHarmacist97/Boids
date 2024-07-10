using System.Collections.Generic;
using UnityEngine;
public class BoidsWithoutUpdate : MonoBehaviour
{
    [SerializeField] private BoidBehaviourParameters boidParameters;
    [SerializeField] private GameObject boidModel;
    [SerializeField] private int count;
    [SerializeField] private Vector3 bounds;
    public Vector3 target;
    public float targetBias;

    private List<Transform> boids;
    private Vector3 extents;
    // Start is called before the first frame update
    void Awake()
    {
        boids = new();
        extents = bounds * 0.5f + transform.position;
        DeployBoids();
    }

    private void DeployBoids()
    {
        GameObject tempBoid;
        for (int i = 0; i < count; i++)
        {
            tempBoid = Instantiate(boidModel, transform);
            tempBoid.transform.localPosition = GetRandomPositionWithinBounds();
            tempBoid.transform.forward = Random.onUnitSphere;
            boids.Add(tempBoid.transform);
        }
    }

    private Vector3 GetRandomPositionWithinBounds()
    {
        float x = Random.Range(-extents.x, extents.x);
        float y = Random.Range(-extents.y, extents.y);
        float z = Random.Range(-extents.z, extents.z);

        return new Vector3(x, y, z);
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < boids.Count; i++)
        {
            Vector3 position = boids[i].position;

            if (OutOfBounds(boids[i].localPosition))
            {
                Vector3 resetVector = -boids[i].forward;
                Quaternion resetLookRot = Quaternion.LookRotation(resetVector, Vector3.up);
                boids[i].SetLocalPositionAndRotation(boidParameters.speed * Time.deltaTime * resetVector + position, resetLookRot);
                continue;
            }

            Vector3 finalDir;
            Vector3 separation = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 avgCenter = Vector3.zero;
            int perceivedSize = 0;


            for (int j = 0; j < boids.Count; j++)
            {
                if (i == j) continue;

                Vector3 neighborPos = boids[j].localPosition;
                Vector3 neighborForward = boids[j].forward;
                Vector3 toNeighbor = neighborPos - position;
                float distToNeighbor = toNeighbor.magnitude;

                if (distToNeighbor > boidParameters.visionRadius) continue;

                separation += -(boidParameters.trespassRadius / distToNeighbor) * boidParameters.avoidanceStrength * toNeighbor;
                alignment += (boidParameters.alignmentStrength / distToNeighbor) * neighborForward;
                perceivedSize++;
                avgCenter += neighborPos;
            }

            if (perceivedSize > 0)
            {
                avgCenter /= perceivedSize;
                cohesion = (avgCenter - position) * boidParameters.cohesionStrength;
                if (i == 1)
                {
                    Debug.Log(avgCenter + " " + cohesion);
                }
            }

            Vector3 toTarget = target - position;
            float distToTarget = toTarget.magnitude;

            finalDir = (targetBias * distToTarget * toTarget + cohesion + separation + alignment).normalized;
            Quaternion lookRot = Quaternion.LookRotation(finalDir, Vector3.up);
            Vector3 translationDelta = Time.deltaTime * boidParameters.speed * finalDir;

            boids[i].SetLocalPositionAndRotation(translationDelta + position, lookRot);
        }
    }

    private bool OutOfBounds(Vector3 pos)
    {
        if (Mathf.Abs(pos.x) >= extents.x)
        {
            return true;
        }
        if (Mathf.Abs(pos.y) >= extents.y)
        {
            return true;
        }
        if (Mathf.Abs(pos.z) >= extents.z)
        {
            return true;
        }
        return false;
    }
}
