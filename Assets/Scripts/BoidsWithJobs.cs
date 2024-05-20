using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

public class BoidsWithJobs : MonoBehaviour
{
    Stopwatch sw;
    public BehaviorParameters behaviors;
    [SerializeField] private bool drawDebugs;
    [Header("Environment")]
    [SerializeField] private float3 bounds;
    private float3 extents;

    [SerializeField, Range(50, 1500)] private int count;
    [SerializeField] private GameObject boidPrefab;

    private Transform[] transforms;
    private TransformAccessArray accessArray;
    private Random rng;
    private NativeArray<float3> boidForwards;
    private NativeArray<float3> boidPositions;
    private NativeArray<float3> prescribedDirections;
    private int innerLoopBatchCount;
    private JobHandle rulesJob;
    private JobHandle moveJob;
    private void Start()
    {
        transforms = new Transform[count];
        extents = bounds * 0.5f;
        rng = new(23940);
        sw = new();

        boidForwards = new(count, Allocator.Persistent);
        boidPositions = new(count, Allocator.Persistent);
        prescribedDirections = new(count, Allocator.Persistent);

        for (int i = 0; i < count; i++)
        {
            Transform t = Instantiate(boidPrefab, transform).transform;
            t.localPosition = GetRandomPosWithinBounds(rng);
            boidPositions[i] = t.localPosition;

            t.forward = rng.NextFloat3Direction();
            boidForwards[i] = t.forward;
            prescribedDirections[i] = t.forward;

            transforms[i] = t;
        }
        accessArray = new TransformAccessArray(transforms);
        rulesJob = new JobHandle();
        moveJob = new JobHandle();
    }

    private void OnDisable()
    {
        accessArray.Dispose();
        boidForwards.Dispose();
        boidPositions.Dispose();
        prescribedDirections.Dispose();
    }

    private void Update()
    {
        sw.Restart();
        for (int i = 0; i < count; i++)
        {
            boidPositions[i] = transforms[i].localPosition;
            boidForwards[i] = transforms[i].forward;
        }
        rulesJob = new ApplyRules()
        {
            extents = extents,
            visionRadius = behaviors.visionRadius,
            visionRange = behaviors.visionRange,

            trespassRadius = behaviors.trespassRadius,
            separationStrength = behaviors.avoidanceStrength,
            alignmentStrength = behaviors.alignmentStrength,
            cohesionStrength = behaviors.cohesionStrength,

            boidPositions = boidPositions,
            boidForwards = boidForwards,

            prescribedDirections = prescribedDirections

        }.Schedule();
        rulesJob.Complete();

        moveJob = new MoveBoids()
        {
            turningSpeed = behaviors.turningSpeed,
            speed = behaviors.speed,
            prescribedDirection = prescribedDirections,
            forward = boidForwards,
            deltaTime = Time.deltaTime

        }.Schedule(accessArray);
        moveJob.Complete();
        Debug.Log(sw.ElapsedMilliseconds);
    }

    private Vector3 GetRandomPosWithinBounds(Random rng)
    {
        float x = rng.NextFloat(-extents.x, extents.x);
        float y = rng.NextFloat(-extents.y, extents.y);
        float z = rng.NextFloat(-extents.z, extents.z);
        return new(x, y, z);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugs) return;
        Handles.color = Color.cyan;
        Handles.DrawWireCube(transform.position, bounds);
    }

}


[BurstCompile]
public struct ApplyRules : IJob
{
    public float3 extents;
    public float visionRadius;  //radius of sphere of vision
    public float visionRange; //dot value (-1 -- 1) threshold
                              //between transform forward and any arbitrary vector in the vision cone

    public float trespassRadius;
    public float separationStrength;
    public float alignmentStrength;
    public float cohesionStrength;

    [ReadOnly] public NativeArray<float3> boidPositions;
    [ReadOnly] public NativeArray<float3> boidForwards;

    [WriteOnly] public NativeArray<float3> prescribedDirections;
    private int percievedSize;
    public void Execute()
    {
        for (int i = 0; i < boidPositions.Length; i++)
        {
            if (CheckOutOfBounds(boidPositions[i]))
            {
                prescribedDirections[i] = math.normalize(-boidPositions[i]);
                continue;
            }

            float3 currentPosition = boidPositions[i];
            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;
            percievedSize = 0;

            for (int j = 0; j < boidPositions.Length; j++)
            {
                if (i == j) continue;

                float3 neighborPos = boidPositions[j];
                if (math.length(neighborPos - currentPosition) > visionRadius)
                    continue;

                separation += GetSeparationVector(currentPosition, neighborPos, trespassRadius);
                alignment += boidForwards[j];
                cohesion += neighborPos;

                percievedSize++;
            }

            if (percievedSize == 0)
            {
                //prescribedDirections[i] = prescribedDirections[i];
                continue;
            }
            float avg = 1 / percievedSize;

            alignment *= avg;
            cohesion *= avg;

            prescribedDirections[i] = math.normalize(
                alignment * alignmentStrength +
                cohesion * cohesionStrength +
                separation * separationStrength);
        }
    }
    private readonly bool CheckOutOfBounds(float3 pos)
    {
        if (math.abs(pos.x) >= extents.x)
        {
            return true;
        }
        if (math.abs(pos.y) >= extents.y)
        {
            return true;
        }
        if (math.abs(pos.z) >= extents.z)
        {
            return true;
        }
        return false;
    }

    private readonly float3 GetSeparationVector(float3 pos, float3 neighbor, float maxDist)
    {
        float3 diff = pos - neighbor;
        float mag = math.length(diff);
        return (diff / mag) * math.saturate(maxDist / mag);
    }
}



[BurstCompile]
public struct MoveBoids : IJobParallelForTransform
{
    [ReadOnly] public float turningSpeed;
    [ReadOnly] public float speed;
    [ReadOnly] public NativeArray<float3> prescribedDirection;
    [ReadOnly] public NativeArray<float3> forward;
    [ReadOnly] public float deltaTime;
    private readonly float GetStableAngle(float3 a, float3 b)
    {
        return 2 * math.atan2(math.length(a - b), math.length(a + b));
    }

    public void Execute(int index, TransformAccess transform)
    {
        float delta = GetStableAngle(forward[index], prescribedDirection[index]);
        float3 finalDir = forward[index];
        if (delta > 0)
        {
            float t = Mathf.Lerp(delta, 0.0f, turningSpeed * deltaTime);
            t = 1.0f - (t / delta);
            finalDir = math.lerp(forward[index], prescribedDirection[index], t);
        }

        Vector3 change = finalDir * speed * deltaTime;
        transform.SetLocalPositionAndRotation(change + transform.localPosition, quaternion.LookRotation(finalDir, Vector3.up));
    }
}
[System.Serializable]
public struct BehaviorParameters
{
    [Header("Vision")]
    [Range(-1f, 1f)] public float visionRange;
    [Tooltip("in square terms"), Range(1f, 3f)] public float visionRadius;

    [Header("Separation")]
    [Tooltip("in unit terms"), Range(0.2f, 3f)] public float trespassRadius;
    [Range(0, 2f)] public float avoidanceStrength;

    [Header("Alignment")]
    [Range(0, 2f)] public float alignmentStrength;

    [Header("Cohesion")]
    [Range(0, 2f)] public float cohesionStrength;

    public float speed;
    public float turningSpeed;
}
