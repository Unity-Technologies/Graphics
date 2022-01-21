using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public struct BoidIndividual
{
    public float3 position;
    public float3 acceleration;
    public float3 velocity;
    public int flockID;
    public bool alive;
    public float timeSinceReplicate;
    public bool canReplicate;
};

public enum Spell
{
      Transmute
    , Replicate
};

public class Boids : MonoBehaviour
{
    public Spell spell = Spell.Transmute;
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

    private int m_currentBoidCount;

    // Start is called before the first frame update
    void Start()
    {
        m_boidsGO = new List<GameObject>();
        m_boidsBufferA = new NativeArray<BoidIndividual>(boidCount, Allocator.Persistent);
        m_boidsBufferB = new NativeArray<BoidIndividual>(boidCount, Allocator.Persistent);
        int rowCount = 0;
        m_currentBoidCount = (spell == Spell.Replicate) ? boidCount / 2 : boidCount;
        for (int i = 0; i < m_currentBoidCount; i++)
        {
            Vector3 pos = new Vector3(i % 10, rowCount, i % 10);
            m_boidsBufferA[i] = new BoidIndividual()
            {
                  position = pos
                , acceleration = Vector3.zero
                , velocity = Vector3.zero
                , flockID = i < (m_currentBoidCount / 2) ? 0 : 1
                , alive = true
                , canReplicate = false
                , timeSinceReplicate = 0
            };
            m_boidsBufferB[i] = m_boidsBufferA[i];
            GameObject flockFab = m_boidsBufferA[i].flockID == 0 ? flockAPrefab : flockBPrefab;
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

                int count = m_currentBoidCount;
                for (int i = 0; i < count; i++)
                {
                    if (m_boidsGO[i].activeSelf) {
                        m_boidsGO[i].transform.position = m_boidsBufferB[i].position;
                        m_boidsGO[i].transform.rotation = Quaternion.LookRotation(math.normalize(m_boidsBufferB[i].velocity), Vector3.up);
                        switch (spell) {
                            case Spell.Transmute:
                            {
                                if ( ! m_boidsBufferB[i].alive)
                                {
                                    m_boidsGO[i].SetActive(false);
                                    int otherBoid = i - (m_currentBoidCount / 2);
                                    Vector3 avgPos = m_boidsGO[i].transform.position - m_boidsGO[otherBoid].transform.position;
                                    float dist = avgPos.magnitude;
                                    avgPos = avgPos.normalized * (dist / 2.0f);
                                    avgPos = m_boidsGO[otherBoid].transform.position + avgPos;
                                    DestroyImmediate(m_boidsGO[otherBoid]);
                                    m_boidsGO[otherBoid] = Instantiate(transmutePrefab, avgPos, Quaternion.identity);
                                }
                            }break;
                            case Spell.Replicate:
                            {
                                if (m_currentBoidCount < boidCount && m_boidsBufferB[i].canReplicate) {
                                    GameObject go = Instantiate(transmutePrefab, m_boidsBufferB[i].position, Quaternion.identity);
                                    go.transform.position = m_boidsBufferB[i].position;
                                    BoidIndividual dupe = m_boidsBufferB[i];
                                    dupe.canReplicate = false;
                                    m_boidsBufferB[i] = dupe;
                                    dupe.flockID = 2;
                                    m_boidsBufferB[m_currentBoidCount] = dupe;
                                    m_boidsGO.Add(go);
                                    m_currentBoidCount++;
                                }
                            }break;
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
            moveJob.time = Time.time;
            moveJob.deltaTime = m_accumulatedTime;
            moveJob.boidCount = m_currentBoidCount;
            moveJob.goalPosition = mouseGoal;
            moveJob.maxForce = maxForce;
            moveJob.maxSpeed = maxSpeed;
            moveJob.cohesionAmount = cohesion;
            moveJob.repelAmount = repel;
            moveJob.alignAmount = align;
            moveJob.seekGoalAmount = matchGoal;
            moveJob.spell = spell;
            m_accumulatedTime = 0.0f;

            m_handle = moveJob.Schedule(m_currentBoidCount, 16);
            m_firstRun = false;
        }

    }
}

public struct BoidMoveJob : IJobParallelFor
{
    public float time;
    public float deltaTime;
    public int boidCount;
    public float maxForce;
    public float maxSpeed;
    public float cohesionAmount;
    public float repelAmount;
    public float alignAmount;
    public float seekGoalAmount;
    public Spell spell;

    public float3 goalPosition;

    public int flockCount;
    public int otherFlockCount;

    [ReadOnly]
    public NativeArray<BoidIndividual> Input;

    [WriteOnly]
    public NativeArray<BoidIndividual> Output;

    public float3 limitForce(float3 force, BoidIndividual individual)
    {
        force = math.normalize(force);
        force *= maxSpeed;
        force -= individual.velocity;
        if (math.length(force)> maxForce)
        {
            force = math.normalize(force) * maxForce;
        }
        return force;
    }

    public float3 cohere(float3 cohesionForce, BoidIndividual individual)
    {
        if ( ! cohesionForce.Equals(float3.zero) && flockCount > 1)
        {
            cohesionForce /= (flockCount - 1);
            cohesionForce = cohesionForce - individual.position;
            cohesionForce = limitForce(cohesionForce, individual);
        }
        return cohesionForce;
    }

    public float3 repel(float3 repelForce, BoidIndividual individual)
    {
        if ( ! repelForce.Equals(float3.zero) && flockCount > 1)
        {
            repelForce /= (flockCount - 1);
            repelForce = limitForce(repelForce, individual);
        }
        return repelForce;
    }

    public float3 align(float3 alignForce, BoidIndividual individual)
    {
        if ( ! alignForce.Equals(float3.zero) && flockCount > 1)
        {
            alignForce /= (flockCount - 1);
            alignForce = limitForce(alignForce, individual);
        }
        return alignForce;
    }

    public float3 seekGoal(BoidIndividual individual)
    {
        float3 seekGoalForce = goalPosition - individual.position;
        if ( ! seekGoalForce.Equals(float3.zero) && flockCount > 1)
        {
            seekGoalForce = limitForce(seekGoalForce, individual);
        }
        return seekGoalForce;
    }

    public float3 repelFlock(float3 repelFlockForce, BoidIndividual individual)
    {
        if ( ! repelFlockForce.Equals(float3.zero) && otherFlockCount > 1)
        {
            repelFlockForce /= (otherFlockCount);
            repelFlockForce = limitForce(repelFlockForce, individual);
        }
        return repelFlockForce;
    }

    public float3 limitVelocity(float3 velocity)
    {
        if (math.length(velocity) > maxSpeed)
        {
            velocity = math.normalize(velocity) * maxSpeed;
        }

        return velocity;
    }

    public void Execute(int index)
    {
        BoidIndividual individual = Input[index];
        individual.canReplicate = false;

        flockCount = 0;
        otherFlockCount = 0;

        switch (spell)
        {
            case Spell.Transmute:
            {
                goalPosition = (individual.flockID == 0) ? goalPosition : new float3(10,5,0);
            }break;
            case Spell.Replicate:
            {
                goalPosition = (individual.flockID == 0) ? goalPosition : new float3(10,5,0);
                goalPosition = (individual.flockID == 2) ? new float3(15,10,0) : goalPosition;
            }break;
        }

        float3 cohesionForce = float3.zero;
        float3 repelForce = float3.zero;
        float3 alignForce = float3.zero;
        float3 seekGoalForce = float3.zero;
        float3 repelFlockForce = float3.zero;

        int proximityCount = 0;
        for (int i = 0; i < boidCount; i++)
        {
            if (i == index) {continue;}

            if (Input[i].flockID == individual.flockID) {
                cohesionForce += Input[i].position;

                float3 otherPos = Input[i].position;
                float3 r = otherPos - individual.position;
                if (math.length(r) < 0.5f)
                {
                    repelForce -= r;
                }

                alignForce += Input[i].velocity;
                flockCount++;
            } else {
                switch (spell)
                {
                    case Spell.Transmute:
                    {
                        float3 otherPos = Input[i].position;
                        float3 r = otherPos - individual.position;
                        if (math.length(r) < 1.00f)
                        {
                            if (individual.flockID == 1) {
                                //flock b attempts evasion flock a
                                repelFlockForce -= r;
                                if (math.length(r) < 0.50f)
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
                        otherFlockCount++;
                    }break;
                    case Spell.Replicate:
                    {
                        if (individual.flockID != 2 && Input[i].flockID != 2) {
                            float3 otherPos = Input[i].position;
                            float3 r = otherPos - individual.position;
                            if (math.length(r) < 2.00f)
                            {
                                repelFlockForce += r;
                                if (individual.flockID == 0)
                                {
                                    if (math.length(r) < 0.50f)
                                    {
                                        proximityCount++;
                                    }
                                }
                            }
                            otherFlockCount++;
                        }
                    }break;
                }
            }
        }

        switch (spell)
        {
            case Spell.Transmute:
            {
                if (proximityCount >= 3) {
                    individual.alive = false;
                }
            }break;
            case Spell.Replicate:
            {
                if (proximityCount >= 10 && time - individual.timeSinceReplicate > 1.0f) {
                    individual.timeSinceReplicate = time;
                    individual.canReplicate = true;
                }
            }break;
        }

        cohesionForce = cohere(cohesionForce, individual);
        repelForce = repel(repelForce, individual);
        alignForce = align(alignForce, individual);
        seekGoalForce = seekGoal(individual);
        repelFlockForce = repelFlock(repelFlockForce, individual);

        cohesionForce *= cohesionAmount;
        repelForce *= repelAmount * 4.0f;
        alignForce *= alignAmount;
        seekGoalForce *= seekGoalAmount;

        switch (spell)
        {
            case Spell.Transmute:
            {
                if (individual.flockID == 1)
                {
                    repelFlockForce *= 14.0f * repelAmount;
                }
                else
                {
                    if (math.length((goalPosition - individual.position)) > 3)
                    {
                        repelFlockForce *= 0.0f * repelAmount;
                    }
                    else
                    {
                        repelFlockForce *= 28.0f * repelAmount;
                    }
                }
            };break;
            case Spell.Replicate:
            {
                if (math.length((goalPosition - individual.position)) > 3)
                {
                    repelFlockForce *= 0.0f * repelAmount;
                }
                else
                {
                    repelFlockForce *= 7.0f * repelAmount;
                }
            };break;
        }

        individual.acceleration += cohesionForce + repelForce + alignForce + seekGoalForce + repelFlockForce;

        individual.velocity += individual.acceleration * deltaTime;
        individual.velocity = limitVelocity(individual.velocity);

        individual.position += (individual.velocity * deltaTime);

        Output[index] = individual;
    }
}
