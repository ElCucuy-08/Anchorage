using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Простое преследование игрока. Скрипт уже привязан к заключённым в сцене map_indor,
/// поэтому достаточно вернуть файл с прежним GUID.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFollow : MonoBehaviour
{
    public float chaseRange = 15f;
    public float stoppingDistance = 1.5f;
    public NavMeshAgent agent;
    public Transform playerTarget;

    private PlayerDeath playerDeath;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTarget = player.transform;
        }

        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
        }
    }

    private void Update()
    {
        if (agent == null || playerTarget == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= chaseRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
        }
        else if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Страховка: если агент остановился чуть дальше нужной точки,
        // смерть всё равно сработает, когда враг вплотную.
        if (distance <= stoppingDistance + 0.6f)
        {
            if (playerDeath == null)
                playerDeath = playerTarget.GetComponentInParent<PlayerDeath>();

            if (playerDeath != null && !playerDeath.IsDead)
                playerDeath.TouchEnemy("Вас поймали");
        }
    }
}
