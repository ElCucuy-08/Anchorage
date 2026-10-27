using UnityEngine;

public class FlashlightToggle : MonoBehaviour
{
    // —сылка на объект света (Spotlight) или сам фонарик
    [SerializeField] private GameObject flashlight;

    void Update()
    {
        // 1 Ч это индекс правой кнопки мыши (ѕ ћ)
        if (Input.GetMouseButtonDown(1))
        {
            if (flashlight != null)
            {
                // ћен€ет состо€ние на противоположное: включает, если выключен, и наоборот
                flashlight.SetActive(!flashlight.activeSelf);
            }
        }
    }
}
