using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [SerializeField] private BoidBehaviourParameters parameters;
    [SerializeField] private bool drawDebugs;
    [SerializeField] private Color xz, yz, trespass;
    public Vector3 headingDirection;

    private Vector3 avgCenter;

    private List<Neighbour> neighbours;
    private Environment env;
    private float delta;

    private const float epsilon = 0.001f;
    private const float TAU = Mathf.PI * 2;

    void Start()
    {
        neighbours = new();

        env = Environment.instance;
        transform.LookAt(transform.position + headingDirection * 50f);
    }

    // Update is called once per frame
    void Update()
    {
        if (CheckOutOfBounds(transform.position))
        {
            headingDirection = (env.transform.position - transform.position).normalized;
        }
        else
        {
            PopulateNeighbourList();

            headingDirection = AvoidBoids(headingDirection);
            headingDirection = AlignToGroup(headingDirection);
            headingDirection = HeadTowardsCenter(headingDirection);

        }
      

        if(drawDebugs)
        {
            print(neighbours.Count);
        }

        headingDirection.Normalize();

        if (Input.GetKeyDown(KeyCode.K))
        {
            headingDirection = RandomizeDirection();
        }

        delta = Vector3.Angle(headingDirection, transform.forward);

        if (delta > 0)
        {
            float t = Mathf.LerpAngle(delta, 0.0f, parameters.turningSpeed*Time.deltaTime);
            transform.forward = Vector3.Slerp(transform.forward, headingDirection, 1.0f - (t / delta));
        }
        transform.Translate(parameters.speed * Time.deltaTime * transform.forward, Space.World);
    }

    private void PopulateNeighbourList()
    {
        neighbours.Clear();
        foreach (Boid boid in env.boidList)
        {
            Vector3 thisToNeighbour = boid.transform.position - transform.position;
            float sqrMag = thisToNeighbour.sqrMagnitude; //using sqrMag here to hack through faster through the list
            if (sqrMag < parameters.visionRadius)
            {
                float magnitude = Mathf.Sqrt(sqrMag);
                float dotVal = Vector3.Dot(thisToNeighbour / magnitude, transform.forward); //normalizing it with the sqrmag we calculated
                                                                                            //earlier, if the neighbour is in max Vision dist
                if (dotVal > parameters.visionRange)
                {
                    neighbours.Add(new Neighbour(boid.transform, thisToNeighbour, magnitude));
                }
            }
        }
    }

    private Vector3 AvoidBoids(Vector3 initDirection)
    {
        foreach (Neighbour neighbour in neighbours)
        {
            if (neighbour.distance > parameters.trespassRadius) continue;

            initDirection += parameters.avoidanceStrength / neighbour.distance * -neighbour.toNeighbor;
        }
        return initDirection;
    }


    private Vector3 AlignToGroup(Vector3 initDirection)
    {
        foreach (Neighbour neighbour in neighbours)
        {
            initDirection += neighbour.boid.forward * parameters.alignmentStrength / neighbour.distance;
        }
        return initDirection;
    }

    private Vector3 HeadTowardsCenter(Vector3 initDirection)
    {
        if (neighbours.Count == 0) return initDirection;

        avgCenter = Vector3.zero;
        foreach (Neighbour neighbour in neighbours)
        {
            avgCenter += neighbour.boid.transform.position / neighbours.Count;
        }

        initDirection += (avgCenter - transform.position) * parameters.cohesionStrength;
        return initDirection;
    }

    private bool CheckOutOfBounds(Vector3 pos)
    {
        if (Mathf.Abs(pos.x) >= env.extents.x)
        {
            return true;
        }
        if (Mathf.Abs(pos.y) >= env.extents.y)
        { 
            return true;
        }
        if (Mathf.Abs(pos.z) >= env.extents.z)
        {
            return true;
        }
        return false;
    }

    private Vector3 RandomizeDirection()
    {
        float phi = Random.Range(0, Mathf.PI);
        float theta = Random.Range(0, TAU);

        float sinPhi = Mathf.Sin(phi);
        float cosPhi = Mathf.Cos(phi);
        float cosTheta = Mathf.Cos(theta);
        float sinTheta = Mathf.Sin(theta);

        float x = sinPhi * cosTheta;
        float y = sinPhi * sinTheta;
        float z = cosPhi;

        return new(x, y, z);
    }

    private void OnDrawGizmos()
    {
        Handles.color = Color.green;

        Handles.DrawAAPolyLine(transform.position, transform.position + headingDirection);
        Handles.color = Color.white;
        Handles.DrawAAPolyLine(transform.position, transform.position + transform.forward);
        if (drawDebugs)
        {

            float theta = Mathf.Acos(parameters.visionRange);
            float cos = Mathf.Cos(theta + Mathf.PI * 0.5f);
            float sin = Mathf.Sin(theta + Mathf.PI * 0.5f);
            Vector3 initDir = new(cos, 0, sin);
            initDir = transform.TransformDirection(initDir);
            Handles.color = xz;
            Handles.DrawSolidArc(transform.position, transform.up, initDir, 2f * theta * Mathf.Rad2Deg, parameters.visionRadius * parameters.visionRadius);
            Handles.color = trespass;
            Handles.DrawSolidArc(transform.position, transform.up, initDir, 2f * theta * Mathf.Rad2Deg, parameters.trespassRadius);
            Handles.color = yz;
            cos = Mathf.Cos(theta);
            sin = Mathf.Sin(theta);
            Vector3 yzDir = new(0, sin, cos);
            yzDir = transform.TransformDirection(yzDir);
            Handles.DrawSolidArc(transform.position, transform.right, yzDir, 2f * theta * Mathf.Rad2Deg, parameters.visionRadius * parameters.visionRadius);
            Handles.color = trespass;
            Handles.DrawSolidArc(transform.position, transform.right, yzDir, 2f * theta * Mathf.Rad2Deg, parameters.trespassRadius);
        }

        if (neighbours == null || neighbours.Count == 0) return;

        Handles.color = Color.red;
        if (drawDebugs && Application.isPlaying)
        {
            foreach (var neighbour in neighbours)
            {
                Handles.DrawAAPolyLine(transform.position, neighbour.boid.transform.position);
            }

        }

    }
}
[SerializeField]
public class Neighbour
{
    public Transform boid;
    public Vector3 toNeighbor;
    public float distance;

    public Neighbour(Transform boid, Vector3 toNeighbor, float distance)
    {
        this.boid = boid;
        this.toNeighbor = toNeighbor;
        this.distance = distance;
    }
}
