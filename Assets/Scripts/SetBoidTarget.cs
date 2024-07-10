using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetBoidTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private BoidsWithoutUpdate boidManager;

    private WaitForSeconds attackPeriod;
    private void Start()
    {
        attackPeriod = new WaitForSeconds(2.5f);
    }

    // Update is called once per frame
    void Update()
    {
        boidManager.target = target.position;
    }

    public void Attack()
    {
        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        boidManager.targetBias = 1.0f;
        yield return attackPeriod;
        boidManager.targetBias = 0.0002f;
    }
}
