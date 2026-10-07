using UnityEngine;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour
{
    [Header("Точки патруля")]
    public Transform pointsParent;
    [Header("Обзор днем")]
    public float dayRadius = 10f;
    [Header("Обзор ночью")]
    public float nightRadius = 8f;
    public float nightAngle = 45f;
    [Header("Настройки")]
    public bool isNight = false;
    public bool isCircle = true;
    private Transform[] patrolPoints;
    private NavMeshAgent agent;
    private Transform player;
    private int currentPoint = 0;
    private bool chasing = false;
    private bool forward = true;
    private Vector3 lastKnownPos;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj =
        GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;

        if (pointsParent != null)
        {
            patrolPoints = new Transform[pointsParent.childCount];

            for (int i = 0; i < pointsParent.childCount; i++)
            {
                patrolPoints[i] = pointsParent.GetChild(i);
            }
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (!chasing)
        {
            Patrol();
        }
        else
        {
            if (player != null)
                transform.LookAt(player);
        }

        if (player != null)
        {
            DetectPlayer();
        }

        Chase();
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending &&
        agent.remainingDistance <= agent.stoppingDistance)
        {
            if (isCircle)
            {
                currentPoint =
                (currentPoint + 1) % patrolPoints.Length;
            }
            else
            {
                if (forward)
                {
                    currentPoint++;

                    if (currentPoint >= patrolPoints.Length - 1)
                        forward = false;
                }
                else
                {
                    currentPoint--;

                    if (currentPoint <= 0)
                        forward = true;
                }
            }

            agent.SetDestination(
            patrolPoints[currentPoint].position);
        }
    }

    void DetectPlayer()
    {
        float dist =
        Vector3.Distance(transform.position,
        player.position);

        if (!isNight)
        {
            if (dist < dayRadius && HasLineOfSight())
            {
                chasing = true;
                lastKnownPos = player.position;
            }

            return;
        }

        if (dist < nightRadius)
        {
            Vector3 dir =
            (player.position - transform.position).normalized;

            float angle =
            Vector3.Angle(transform.forward, dir);

            if (angle < nightAngle && HasLineOfSight())
            {
                chasing = true;
                lastKnownPos = player.position;
            }
        }
    }

    bool HasLineOfSight()
    {
        RaycastHit hit;

        Vector3 start = transform.position + Vector3.up * 1.7f;

        Vector3 dir = (player.position - start).normalized;

        float distance =
        Vector3.Distance(start, player.position);

        if (Physics.Raycast(start, dir, out hit, distance))
        {
            return hit.collider.CompareTag("Player");
        }

        return false;
    }

    void Chase()
    {
        if (!chasing)
            return;

        lastKnownPos = player.position;

        agent.SetDestination(lastKnownPos);

        if (!HasLineOfSight())
        {
            if (!agent.pathPending &&
            agent.remainingDistance <=
            agent.stoppingDistance)
            {
                chasing = false;

                if (patrolPoints.Length > 0)
                {
                    agent.SetDestination(
                    patrolPoints[currentPoint].position);
                }
            }
        }
    }
}