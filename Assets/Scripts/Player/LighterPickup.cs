using UnityEngine;

/// <summary>
/// Зажигалка: при касании игроком даёт +10 здоровья и исчезает.
/// Достаточно повесить компонент на объект зажигалки — коллайдер станет триггером сам.
/// </summary>
public class LighterPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private bool destroyOnPickup = true;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;
        else
            Debug.LogWarning("[LighterPickup] На объекте нет коллайдера — подобрать зажигалку не получится.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerDeath playerDeath = other.GetComponentInParent<PlayerDeath>();

        if (playerDeath == null || playerDeath.IsDead)
            return;

        playerDeath.Heal(healAmount);

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            enabled = false;
    }
}
