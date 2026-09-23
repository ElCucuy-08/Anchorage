using UnityEngine;
using UnityEngine.AI;

public class Prison : MonoBehaviour
{
    public Transform[] patrolPoints;
    NavMeshAgent agent;

    int currentPoint = 0;
    public bool isCircle = false;
    bool forward = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.destination = patrolPoints[currentPoint].position;
    }

    public void Update()
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
}