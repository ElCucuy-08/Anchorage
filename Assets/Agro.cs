using UnityEngine;
using UnityEngine.AI;
public class Agro : MonoBehaviour
{
    NavMeshAgent agent;
    Transform player;
    Vector3 playerCorrectPosition = Vector3.zero;




    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    private void Update()
    {
        playerCorrectPosition = player.position;
        playerCorrectPosition.y = transform.position.y;
        transform.LookAt(playerCorrectPosition);

        if (Vector3.Distance(playerCorrectPosition, transform.position) > 1.5)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerCorrectPosition, Time.deltaTime * 8);
        }
    }
    private void FixedUpdate()
    {
       
       
    }

    
}
