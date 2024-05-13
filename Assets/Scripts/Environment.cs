using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    [SerializeField] private Boid boidPrefab;
    [SerializeField, Range(1, 1000)] private int entityCount;
    public List<Boid> boidList;
    public Vector3 bounds;
    public static Environment instance;
    private int lastEntityCount;
    private Boid tempBoid;

    [HideInInspector]
    public Vector3 extents;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(instance.gameObject);
        }
        instance = this;

        extents = bounds*0.5f + transform.position;
        DeployBoids(entityCount);
        lastEntityCount = entityCount;
    }

    private void Update()
    {
        UpdateBoidList();
    }


    private void UpdateBoidList()
    {
        if (lastEntityCount == entityCount) return;
        int difference = entityCount - lastEntityCount;
        if (difference > 0)
        {
            DeployBoids(difference);
        }
        else
        {
            DestroyBoids(-difference);
        }
        lastEntityCount = boidList.Count;
    }

    private void DeployBoids(int additions)
    {
        for (int i = 0; i < additions; i++)
        {
            tempBoid = Instantiate(boidPrefab, transform);
            tempBoid.transform.position = GetRandomPositionWithinBounds();
            tempBoid.headingDirection = Random.onUnitSphere;
            boidList.Add(tempBoid);
        }
    }

    private Vector3 GetRandomPositionWithinBounds()
    {
        float x = Random.Range(-extents.x, extents.x);
        float y = Random.Range(-extents.y, extents.y);
        float z = Random.Range(-extents.z, extents.z);
        Debug.Log(x+" , "+y+", "+z);

        return new Vector3(x, y, z);
    }

    private void DestroyBoids(int deletions)
    {
        for (int i = entityCount; i > entityCount - deletions; i--)
        {
            tempBoid = boidList[i];
            boidList.RemoveAt(i);
            Destroy(tempBoid.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(transform.position, bounds);
    }
}
