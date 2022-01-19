using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct BoidIndividual {
    public Vector3 position;
    public Vector3 acceleration;
    public Vector3 velocity;
    public int flock;
    public bool alive;
};

public class Boids : MonoBehaviour
{
    public int boidCount = 100;
    public GameObject flockAPrefab;
    public GameObject flockBPrefab;
    public GameObject transmutePrefab;
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
                , flock = i < (boidCount / 2) ? 0 : 1
                , alive = true
            };
            m_boidsBufferB[i] = m_boidsBufferA[i];
            GameObject flockFab = m_boidsBufferA[i].flock == 0 ? flockAPrefab : flockBPrefab;
            GameObject go = Instantiate(flockFab, pos, Quaternion.identity);
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
                    if (m_boidsGO[i].activeSelf) {
                        m_boidsGO[i].transform.position = m_boidsBufferB[i].position;
                        if ( ! m_boidsBufferB[i].alive)
                        {
                            m_boidsGO[i].SetActive(false);
                            int otherBoid = i - (boidCount / 2);
                            Vector3 avgPos = m_boidsGO[i].transform.position - m_boidsGO[otherBoid].transform.position;
                            float dist = avgPos.magnitude;
                            avgPos = Vector3.Normalize(avgPos) * (dist / 2.0f);
                            avgPos = m_boidsGO[otherBoid].transform.position + avgPos;
                            DestroyImmediate(m_boidsGO[otherBoid]);
                            m_boidsGO[otherBoid] = Instantiate(transmutePrefab, avgPos, Quaternion.identity);
                        }
                    }
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

    public int flockStart;
    public int flockEnd;
    public int flockCount;

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
        if (cohesionForce != Vector3.zero)
        {
            cohesionForce /= (flockCount - 1);
            cohesionForce = cohesionForce - individual.position;
            cohesionForce = limitForce(cohesionForce, individual);
        }
        return cohesionForce;
    }

    public Vector3 repel(Vector3 repelForce, BoidIndividual individual)
    {
        if (repelForce != Vector3.zero)
        {
            repelForce /= (flockCount - 1);
            repelForce = limitForce(repelForce, individual);
        }
        return repelForce;
    }

    public Vector3 align(Vector3 alignForce, BoidIndividual individual)
    {
        if (alignForce != Vector3.zero)
        {
            alignForce /= (flockCount - 1);
            alignForce = limitForce(alignForce, individual);
        }
        return alignForce;
    }

    public Vector3 seekGoal(BoidIndividual individual)
    {
        Vector3 seekGoalForce = goalPosition - individual.position;
        if (seekGoalForce != Vector3.zero)
        {
            seekGoalForce = limitForce(seekGoalForce, individual);
        }
        return seekGoalForce;
    }

    public Vector3 repelFlock(Vector3 repelFlockForce, BoidIndividual individual)
    {
        if (repelFlockForce != Vector3.zero)
        {
            repelFlockForce /= (flockCount);
            repelFlockForce = limitForce(repelFlockForce, individual);
        }
        return repelFlockForce;
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
        flockCount = boidCount / 2;
        flockStart = individual.flock * flockCount;
        flockEnd = flockStart + flockCount;

        goalPosition = (individual.flock == 0) ? goalPosition : new Vector3(10,5,0);

        Vector3 cohesionForce = Vector3.zero;
        Vector3 repelForce = Vector3.zero;
        Vector3 alignForce = Vector3.zero;
        Vector3 seekGoalForce = Vector3.zero;
        Vector3 repelFlockForce = Vector3.zero;

        for (int i = flockStart; i < flockEnd; i++)
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

        int otherFlockStart = individual.flock == 0 ? flockCount : 0;
        int otherFlockEnd = individual.flock == 0 ? boidCount : flockCount;
        int proximityCount = 0;
        for (int i = otherFlockStart; i < otherFlockEnd; i++)
        {
            Vector3 otherPos = Input[i].position;
            Vector3 r = otherPos - individual.position;
            if (r.magnitude < 1.00f)
            {
                if (individual.flock == 1) {
                    //flock b attempts evasion flock a
                    repelFlockForce -= r;
                    if (r.magnitude < 0.50f)
                    {
                        proximityCount++;
                    }
                } 
                else
                {
                    //flock a attracted towards flock b
                    repelFlockForce += r;
                }
            }
        }
        if (proximityCount >= 3) {

            individual.alive = false;
        }

        cohesionForce = cohere(cohesionForce, individual);
        repelForce = repel(repelForce, individual);
        alignForce = align(alignForce, individual);
        seekGoalForce = seekGoal(individual);
        repelFlockForce = repelFlock(repelFlockForce, individual);

        cohesionForce *= cohesionAmount;
        repelForce *= repelAmount;
        alignForce *= alignAmount;
        seekGoalForce *= seekGoalAmount;
        if (individual.flock == 1)
        {
            repelFlockForce *= 14.0f * repelAmount;
        }
        else
        {
            if ((goalPosition - individual.position).magnitude > 3)
            {
                repelFlockForce *= 0.0f * repelAmount;
            }
            else
            {
                repelFlockForce *= 28.0f * repelAmount;
            }
        }

        individual.acceleration += cohesionForce + repelForce + alignForce + seekGoalForce + repelFlockForce;

        individual.velocity += individual.acceleration * deltaTime;
        individual.velocity = limitVelocity(individual.velocity);

        individual.position += (individual.velocity * deltaTime);

        Output[index] = individual;
    }
}
