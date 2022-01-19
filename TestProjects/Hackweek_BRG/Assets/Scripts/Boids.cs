using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct BoidIndividual {
    public Vector3 position;
    public Vector3 acceleration;
    public Vector3 velocity;
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
    public float matchGoal = 1.0f;

    private NativeArray<BoidIndividual> m_boidsBufferA;
    private NativeArray<BoidIndividual> m_boidsBufferB;
    private List<GameObject> m_boidsGO;

    private bool m_firstRun = true;
    private JobHandle m_handle;
    private float m_accumulatedTime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        m_boidsGO = new List<GameObject>();
        m_boidsBufferA = new NativeArray<BoidIndividual>(boidCount, Allocator.Persistent);
        m_boidsBufferB = new NativeArray<BoidIndividual>(boidCount, Allocator.Persistent);
        int rowCount = 0;
        for (int i = 0; i < boidCount; i++)
        {
            Vector3 pos = new Vector3(i % 10, rowCount, i % 10);
            m_boidsBufferA[i] = new BoidIndividual()
            {
                  position = pos
                , acceleration = Vector3.zero
                , velocity = Vector3.zero
            };
            m_boidsBufferB[i] = m_boidsBufferA[i];
            var go = Instantiate(boidsPrefab, pos, Quaternion.identity);
            go.transform.position = m_boidsBufferA[i].position;
            m_boidsGO.Add(go);
            rowCount += ((i+1) % 10 == 0) ? 1 : 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_accumulatedTime += Time.deltaTime;
        if (m_handle.IsCompleted || m_firstRun)
        {
            if ( ! m_firstRun)
            {
                m_handle.Complete();

                for (int i = 0; i < boidCount; i++)
                {
                    m_boidsGO[i].transform.position = m_boidsBufferB[i].position;
                }

                NativeArray<BoidIndividual> temp = m_boidsBufferA;
                m_boidsBufferA = m_boidsBufferB;
                m_boidsBufferB = temp;

            }

            Vector3 mouseGoal = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);
            mouseGoal = Camera.main.ScreenToWorldPoint(mouseGoal);

            if (m_firstRun)
            {
                m_accumulatedTime = Time.deltaTime;
            }

            BoidMoveJob moveJob = new BoidMoveJob();
            moveJob.Input = m_boidsBufferA;
            moveJob.Output = m_boidsBufferB;
            moveJob.deltaTime = m_accumulatedTime;
            moveJob.boidCount = boidCount;
            moveJob.goalPosition = mouseGoal;
            moveJob.maxForce = maxForce;
            moveJob.maxSpeed = maxSpeed;
            moveJob.cohesionAmount = cohesion;
            moveJob.repelAmount = repel;
            moveJob.alignAmount = align;
            moveJob.seekGoalAmount = matchGoal;
            m_accumulatedTime = 0.0f;

            m_handle = moveJob.Schedule(boidCount, 16);
            m_firstRun = false;
        }

    }
}

public struct BoidMoveJob : IJobParallelFor
{
    public float deltaTime;
    public int boidCount;
    public float maxForce;
    public float maxSpeed;
    public float cohesionAmount;
    public float repelAmount;
    public float alignAmount;
    public float seekGoalAmount;

    public Vector3 goalPosition;

    [ReadOnly]
    public NativeArray<BoidIndividual> Input;

    [WriteOnly]
    public NativeArray<BoidIndividual> Output;

    public Vector3 limitForce(Vector3 force, BoidIndividual individual)
    {
        force = Vector3.Normalize(force);
        force *= maxSpeed;
        force -= individual.velocity;
        if (force.magnitude > maxForce)
        {
            force = Vector3.Normalize(force) * maxForce;
        }
        return force;
    }

    public Vector3 cohere(Vector3 cohesionForce, BoidIndividual individual)
    {
        cohesionForce /= (boidCount - 1);
        cohesionForce = cohesionForce - individual.position;

        return limitForce(cohesionForce, individual);
    }

    public Vector3 repel(Vector3 repelForce, BoidIndividual individual)
    {
        repelForce /= (boidCount - 1);

        return limitForce(repelForce, individual);
    }

    public Vector3 align(Vector3 alignForce, BoidIndividual individual)
    {
        alignForce /= (boidCount - 1);

        return limitForce(alignForce, individual);
    }

    public Vector3 seekGoal(BoidIndividual individual)
    {
        Vector3 seekGoalForce = goalPosition - individual.position;
        return limitForce(seekGoalForce, individual);
    }

    public Vector3 limitVelocity(Vector3 velocity)
    {
        if (velocity.magnitude > maxSpeed)
        {
            velocity = Vector3.Normalize(velocity) * maxSpeed;
        }

        return velocity;
    }

    public void Execute(int index)
    {
        BoidIndividual individual = Input[index];

        Vector3 cohesionForce = Vector3.zero;
        Vector3 repelForce = Vector3.zero;
        Vector3 alignForce = Vector3.zero;
        Vector3 seekGoalForce = Vector3.zero;

        for (int i = 0; i < boidCount; i++)
        {
            if (i == index) {continue;}

            cohesionForce += Input[i].position;

            Vector3 otherPos = Input[i].position;
            Vector3 r = otherPos - individual.position;
            if (r.magnitude < 0.5f)
            {
                repelForce -= r;
            }

            alignForce += Input[i].velocity;
        }

        cohesionForce = cohere(cohesionForce, individual);
        repelForce = repel(repelForce, individual);
        alignForce = align(alignForce, individual);
        seekGoalForce = seekGoal(individual);

        cohesionForce *= cohesionAmount;
        repelForce *= repelAmount;
        alignForce *= alignAmount;
        seekGoalForce *= seekGoalAmount;

        individual.acceleration += cohesionForce + repelForce + alignForce + seekGoalForce;

        individual.velocity += individual.acceleration * deltaTime;
        individual.velocity = limitVelocity(individual.velocity);

        individual.position += (individual.velocity * deltaTime);

        Output[index] = individual;
    }
}
