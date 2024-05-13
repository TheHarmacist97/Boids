using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Boid Bpublicehaviour", menuName = "Boid Behaviour")]
public class BoidBehaviourParameters : ScriptableObject
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
