//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Burst;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;


//public class Boid_Jobs : MonoBehaviour
//{
//    [SerializeField] private BoidBehaviourParameters parameters;
//    [SerializeField] private bool drawDebugs;
//    [SerializeField] private Color xz, yz, trespass;
//    public Vector3 headingDirection;

//    private Vector3 avgCenter;

//    private NativeList<JobHandle> jobHandleList;
//    private Environment env;
//    private float delta;

//    private const float epsilon = 0.001f;
//    private const float TAU = Mathf.PI * 2;

//    void Start()
//    {
//        jobHandleList = new(Allocator.Temp);

//        env = Environment.instance;
//        transform.LookAt(transform.position + headingDirection * 50f);
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (CheckOutOfBounds(transform.position))
//        {
//            headingDirection = (env.transform.position - transform.position).normalized;
//        }
//        else
//        {
//            CreateJobList();
//        }

//        delta = Vector3.Angle(headingDirection, transform.forward);

//        if (delta > 0)
//        {
//            float t = Mathf.LerpAngle(delta, 0.0f, parameters.turningSpeed * Time.deltaTime);
//            transform.forward = Vector3.Slerp(transform.forward, headingDirection, 1.0f - (t / delta));
//        }
//        transform.Translate(parameters.speed * Time.deltaTime * transform.forward, Space.World);
//    }

//    private void CreateJobList()
//    {
//        foreach (Boid boid in env.boidList)
//        {
//            jobHandleList.Add(CreateJob(boid));
//        }
//        JobHandle.CompleteAll(jobHandleList.AsArray());
//        jobHandleList.Dispose();
//    }

//    private void PopulateNeighbourList()
//    {
//        foreach (Boid boid in env.boidList)
//        {
//            jobHandleList.Add(CreateJob(boid));
//            float3 thisToNeighbour = boid.transform.position - transform.position;
//            float sqrMag = math.dot(thisToNeighbour, thisToNeighbour); //using sqrMag here to hack through faster through the list
//            if (sqrMag < parameters.visionRadius)
//            {
//                float magnitude = Mathf.Sqrt(sqrMag);
//                float dotVal = math.dot(thisToNeighbour / magnitude, transform.forward); //normalizing it with the sqrmag we calculated
//                                                                                         //earlier, if the neighbour is in max Vision dist
//                if (dotVal > parameters.visionRange)
//                {
//                }
//            }
//        }
//    }

//    private Vector3 AvoidBoids(Vector3 initDirection)
//    {
//        foreach (Neighbour neighbour in jobHandleList)
//        {
//            if (neighbour.distance > parameters.trespassRadius) continue;

//            initDirection += parameters.avoidanceStrength / neighbour.distance * -neighbour.toNeighbor;
//        }
//        return initDirection;
//    }


//    private Vector3 AlignToGroup(Vector3 initDirection)
//    {
//        foreach (Neighbour neighbour in jobHandleList)
//        {
//            initDirection += neighbour.boid.headingDirection * parameters.alignmentStrength / neighbour.distance;
//        }
//        return initDirection;
//    }

//    private Vector3 HeadTowardsCenter(Vector3 initDirection)
//    {
//        if (jobHandleList.Length == 0) return initDirection;

//        avgCenter = Vector3.zero;
//        foreach (Neighbour neighbour in jobHandleList)
//        {
//            avgCenter += neighbour.boid.transform.position / jobHandleList.Length;
//        }

//        initDirection += (avgCenter - transform.position) * parameters.cohesionStrength;
//        return initDirection;
//    }

//    private bool CheckOutOfBounds(Vector3 pos)
//    {
//        if (Mathf.Abs(pos.x) >= env.extents.x)
//        {
//            return true;
//        }
//        if (Mathf.Abs(pos.y) >= env.extents.y)
//        {
//            return true;
//        }
//        if (Mathf.Abs(pos.z) >= env.extents.z)
//        {
//            return true;
//        }
//        return false;
//    }

//    private JobHandle CreateJob(Boid boidFromList)
//    {
//        Neighbour_Jobs neighbourComputation = new(boidFromList);
//        return neighbourComputation.Schedule();
//    }

//    private void OnDrawGizmos()
//    {
//        Handles.color = Color.green;

//        Handles.DrawAAPolyLine(transform.position, transform.position + headingDirection);
//        Handles.color = Color.white;
//        Handles.DrawAAPolyLine(transform.position, transform.position + transform.forward);
//        if (drawDebugs)
//        {

//            float theta = Mathf.Acos(parameters.visionRange);
//            float cos = Mathf.Cos(theta + Mathf.PI * 0.5f);
//            float sin = Mathf.Sin(theta + Mathf.PI * 0.5f);
//            Vector3 initDir = new(cos, 0, sin);
//            initDir = transform.TransformDirection(initDir);
//            Handles.color = xz;
//            Handles.DrawSolidArc(transform.position, transform.up, initDir, 2f * theta * Mathf.Rad2Deg, parameters.visionRadius * parameters.visionRadius);
//            Handles.color = trespass;
//            Handles.DrawSolidArc(transform.position, transform.up, initDir, 2f * theta * Mathf.Rad2Deg, parameters.trespassRadius);
//            Handles.color = yz;
//            cos = Mathf.Cos(theta);
//            sin = Mathf.Sin(theta);
//            Vector3 yzDir = new(0, sin, cos);
//            yzDir = transform.TransformDirection(yzDir);
//            Handles.DrawSolidArc(transform.position, transform.right, yzDir, 2f * theta * Mathf.Rad2Deg, parameters.visionRadius * parameters.visionRadius);
//            Handles.color = trespass;
//            Handles.DrawSolidArc(transform.position, transform.right, yzDir, 2f * theta * Mathf.Rad2Deg, parameters.trespassRadius);
//        }
//    }
//}

//[SerializeField, BurstCompile]
//public struct Neighbour_Jobs : IJob
//{
//    public Boid boid;
//    public NativeArray<float3> resultantDirection;
//    public Neighbour_Jobs(Boid boid, NativeArray<float3> resultantDirection)
//    {
//        this.boid = boid;
//        this.resultantDirection = resultantDirection;
//    }

//    public void Execute()
//    {

//    }
//}
