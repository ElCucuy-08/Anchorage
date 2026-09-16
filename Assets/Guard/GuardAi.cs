using UnityEngine;
using UnityEngine.AI;

public class GuardAi : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float dayRadius = 10f;
    public float nightRadius = 8f;
    public float nightAngle = 45f;
    public bool isNight = false;
    public bool isCircle = false;

    NavMeshAgent agent;
    Transform player;
    int currentPoint = 0;
    bool chasing = false;
    Vector3 lastKnownPos;
    bool forward = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent.destination = patrolPoints[currentPoint].position;
    }

    void Update()
    {
        if (chasing)
        {
            transform.LookAt(player);
        }
        else
        {
            Patrol();
        }

        DetectPlayer();
        Chase();
    }

    void Patrol()
    {
        if (agent.remainingDistance < 0.3f)
        {
            if (isCircle)
            {
                currentPoint = (currentPoint + 1) % patrolPoints.Length;
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

            agent.destination = patrolPoints[currentPoint].position;
        }
    }

    void DetectPlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);

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
            Vector3 dir = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);

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
        Vector3 dir = player.position - start;

        if (Physics.Raycast(start, dir, out hit))
        {
            return hit.collider.CompareTag("Player");
        }

        return false;
    }

    void Chase()
    {
        if (!chasing) return;

        agent.destination = lastKnownPos;

        if (agent.remainingDistance < 0.2f)
        {
            chasing = false;
            agent.destination = patrolPoints[currentPoint].position;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dayRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, nightRadius);

        Gizmos.color = Color.yellow;

        Vector3 forwardDir = transform.forward * nightRadius;

        Vector3 left = Quaternion.Euler(0, -nightAngle, 0) * forwardDir;
        Gizmos.DrawLine(transform.position, transform.position + left);

        Vector3 right = Quaternion.Euler(0, nightAngle, 0) * forwardDir;
        Gizmos.DrawLine(transform.position, transform.position + right);
    }
}
