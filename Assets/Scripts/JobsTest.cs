using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
public class JobsTest : MonoBehaviour
{
    [SerializeField] bool useJobs;
    [SerializeField] int length;
    private Random rng;
    private JobHandle jobHandle;
    // Start is called before the first frame update
    void Start()
    {
        uint seed = 9421;
        rng = new(seed);
    }

    private void Update()
    {
        float startTime = Time.realtimeSinceStartup;
        if (useJobs)
        {
            NativeArray<JobHandle> jobs = new(10,Allocator.Temp);
            for (int i = 0; i<10; i++)
            {
                jobs[i] = (ScheduleToughJob());
            }
            JobHandle.CompleteAll(jobs);
            jobs.Dispose();
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                ToughSyncedOperation();
            }
        }
        Debug.Log("With"+ (useJobs?" Jobs: ": "out Jobs: ") + ((Time.realtimeSinceStartup - startTime) * 1000f) + " ms");
    }

    private void ToughSyncedOperation()
    {
        float value = 0f;
        for (int i = 0; i < length; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }

    private JobHandle ScheduleToughJob()
    {
        JobStruct job = new JobStruct()
        {
            length = length
        };
        return job.Schedule();
    }
}

[BurstCompile]
public struct JobStruct : IJob
{
    public int length;
    public void Execute()
    {
        float value = 0f;
        for (int i = 0;i < length;i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }
}