using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
public class JobsTest : MonoBehaviour
{
    [SerializeField] bool useCollated;
    [SerializeField] int length;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Update()
    {
        uint seed = 9421;
        Random rng = new(seed);

        float startTime = Time.realtimeSinceStartup;
        if (useCollated)
        {
            NativeArray<float> result = new(1, Allocator.TempJob);
            result[0] = 0;
            NativeArray<float2> vectorArray = new(length, Allocator.TempJob);
            for (int i = 0; i < length; i++)
            {
                vectorArray[i] = rng.NextFloat2();
            }
            SimpleCollatedJob sJob = new()
            {
                a = vectorArray,
                result = result
            };
            JobHandle jHandle = sJob.Schedule();
            jHandle.Complete();
            Debug.Log("Collated time " + ((Time.realtimeSinceStartup - startTime) * 1000f) + " ms " + sJob.result[0]);

            vectorArray.Dispose();
            result.Dispose();
        }
        else
        {
            NativeArray<NativeArray<float>> nestedResultArray = new(length, Allocator.Temp);
            NativeArray<JobHandle> jobList = new(length, Allocator.Temp);
            for (int i = 0; i < length; i++)
            {
                nestedResultArray[i] = new NativeArray<float>(1, Allocator.TempJob);
                jobList[i] = new SimpleDistributedJob(rng.NextFloat2(), nestedResultArray[i]).Schedule();
            }
            JobHandle.CompleteAll(jobList);
            float endResult = 0.0f;
            //NativeArray<float> copy;
            for (int i = 0; i < length; i++)
            {
                endResult += nestedResultArray[i][0];
                nestedResultArray[i].Dispose();
            }
            Debug.Log("Distributed Time: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + " ms " + endResult);
            nestedResultArray.Dispose();
        }
    }
}

[BurstCompile]
public struct SimpleDistributedJob : IJob
{
    [ReadOnly]
    public float2 a;
    public NativeArray<float> result;

    public SimpleDistributedJob(float2 a, NativeArray<float> result)
    {
        this.a = a;
        this.result = result;
    }

    public void Execute()
    {
        result[0] += a.x + a.y;
    }
}
[BurstCompile]
public struct SimpleCollatedJob : IJob
{
    [ReadOnly]
    public NativeArray<float2> a;
    public NativeArray<float> result;
    public void Execute()
    {
        result[0] = 0f;
        foreach (float2 item in a)
        {
            result[0] += item.x + item.y;
        }
    }
}
