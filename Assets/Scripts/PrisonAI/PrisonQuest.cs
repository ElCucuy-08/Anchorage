using UnityEngine;

public class PrisonQuest
{
    bool playerInside = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    void Update()
    {
        if (!playerInside)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            GiveQuest();
        }
    }

    void GiveQuest()
    {

    }
}
