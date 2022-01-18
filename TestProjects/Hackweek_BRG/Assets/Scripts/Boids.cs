using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

public struct BoidIndividual {
    public Vector3 position;
    public Vector3 acceleration;
    public Vector3 velocity;
    public float maxForce;
    public float maxSpeed;
    public float cohesion;
    public float repel;
    public float align;
    public float goal;
};

public class Boids : MonoBehaviour
{
    public int boidCount = 100;
    public GameObject boidsPrefab;
    public float maxForce = 4.0f;
    public float maxSpeed = 8.0f;

    public float cohesion = 1.0f;
    public float repel = 1.0f;
    public float align = 1.0f;
    public float goal = 1.0f;

    private NativeArray<BoidIndividual> m_boidsBufferA;
    private NativeArray<BoidIndividual> m_boidsBufferB;
    private List<GameObject> m_boidsGO;

    // Start is called before the first frame update
    void Start()
    {
        m_boidsGO = new List<GameObject>();
        m_boidsBufferA = new NativeArray<BoidIndividual>(boidCount, Allocator.Persistent);
        m_boidsBufferB = new NativeArray<BoidIndividual>(boidCount, Allocator.Persistent);
        int rowCount = 0;
        for (int i = 0; i < boidCount; i++) {
            Vector3 pos = new Vector3(i % 10, rowCount, i % 10);
            m_boidsBufferA[i] = new BoidIndividual() {
                  position = pos
                , acceleration = Vector3.zero
                , velocity = Vector3.zero
                , maxForce = maxForce
                , maxSpeed = maxSpeed
                , cohesion = cohesion
                , repel = repel
                , align = align
                , goal = goal
            };
            m_boidsBufferB[i] = new BoidIndividual() {
                  position = pos
                , acceleration = Vector3.zero
                , velocity = Vector3.zero
                , maxForce = maxForce
                , maxSpeed = maxSpeed
                , cohesion = cohesion
                , repel = repel
                , align = align
                , goal = goal
            };
            var go = Instantiate(boidsPrefab, pos, Quaternion.identity);
            go.transform.position = m_boidsBufferA[i].position;
            m_boidsGO.Add(go);
            rowCount += ((i+1) % 10 == 0) ? 1 : 0;
        }
    }

    // Update is called once per frame
    void Update()
    {

        Vector3 mouseGoal = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);
        mouseGoal = Camera.main.ScreenToWorldPoint(mouseGoal);
        //mouseGoal = mouseGoal + (Camera.main.transform.forward * 10.0f);

        BoidMoveJob move_job = new BoidMoveJob();
        move_job.Input = m_boidsBufferA;
        move_job.Output = m_boidsBufferB;
        move_job.deltaTime = Time.deltaTime;
        move_job.boidCount = boidCount;
        move_job.goal = mouseGoal;

        JobHandle handle = move_job.Schedule(boidCount, 1);
        handle.Complete();

        for (int i = 0; i < boidCount; i++) {
            m_boidsGO[i].transform.position = m_boidsBufferB[i].position;
        }

        NativeArray<BoidIndividual> temp = m_boidsBufferA;
        m_boidsBufferA = m_boidsBufferB;
        m_boidsBufferB = temp;
    }
}

public struct BoidMoveJob : IJobParallelFor
{
    public float deltaTime;
    public int boidCount;
    public Vector3 goal;

    [ReadOnly]
    public NativeArray<BoidIndividual> Input;

    [WriteOnly]
    public NativeArray<BoidIndividual> Output;

    public Vector3 cohesion(int index, BoidIndividual individual) 
    {
        Vector3 attract = Vector3.zero;

        for (int i = 0; i < boidCount; i++) {
            if (i == index) {continue;}

            attract += Input[i].position;
        }
        attract /= (boidCount - 1);
        attract = attract - individual.position;
        //attract /= 5.0f;

        attract = Vector3.Normalize(attract);
        attract *= individual.maxSpeed;
        attract -= individual.velocity;
        if (attract.magnitude > individual.maxForce) {
            attract = Vector3.Normalize(attract) * individual.maxForce;
        }

        return attract;
    }

    public Vector3 repel_boid(int index, BoidIndividual individual) 
    {
        Vector3 repel = Vector3.zero;

        for (int i = 0; i < boidCount; i++) {
            if (i == index) {continue;}

            Vector3 other_pos = Input[i].position;

            Vector3 r = other_pos - individual.position;
            if (r.magnitude < 0.5f) {
                //repel -= (r *30);
                repel -= r;
            }
        }
        repel /= (boidCount - 1);

        repel = Vector3.Normalize(repel);
        repel *= individual.maxSpeed;
        repel -= individual.velocity;
        if (repel.magnitude > individual.maxForce) {
            repel = Vector3.Normalize(repel) * individual.maxForce;
        }

        return repel;
    }

    public Vector3 align_boid(int index, BoidIndividual individual) 
    {
        Vector3 match_vel = Vector3.zero;
        for (int i = 0; i < boidCount; i++)
        {
            if (i == index) {continue;}

            match_vel += Input[i].velocity;
        }
        match_vel /= (boidCount - 1);

        match_vel = Vector3.Normalize(match_vel);
        match_vel *= individual.maxSpeed;
        match_vel -= individual.velocity;
        if (match_vel.magnitude > individual.maxForce) {
            match_vel = Vector3.Normalize(match_vel) * individual.maxForce;
        }
        //match_vel /= 10.0f;
        return match_vel;
    }

    public void Execute(int index)
    {
        Debug.Log(index);
        BoidIndividual individual = Input[index];

        Vector3 attract = Vector3.zero;
        Vector3 repel = Vector3.zero;
        Vector3 align = Vector3.zero;
        Vector3 match_goal = Vector3.zero;

        attract = cohesion(index, individual);
        repel = repel_boid(index, individual);
        align = align_boid(index, individual);

        //match_goal = (goal - individual.position) / 2.0f;
        match_goal = Vector3.Normalize(goal - individual.position);
        match_goal *= individual.maxSpeed;
        match_goal -= individual.velocity;
        if (match_goal.magnitude > individual.maxForce) {
            match_goal = Vector3.Normalize(match_goal) * individual.maxForce;
        }

        attract *= individual.cohesion;
        repel *= individual.repel;
        align *= individual.align;
        match_goal *= individual.goal;

        individual.acceleration += attract + repel + align + match_goal;

        individual.velocity += individual.acceleration * deltaTime;

        if (individual.velocity.magnitude > individual.maxSpeed)
        {
            individual.velocity = Vector3.Normalize(individual.velocity) * individual.maxSpeed;
        }

        individual.position += (individual.velocity * deltaTime);
        Output[index] = individual;
    }
}
