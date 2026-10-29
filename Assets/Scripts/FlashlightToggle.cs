using System.Collections;
using UnityEngine;

public class FlashlightToggle : MonoBehaviour
{
    [SerializeField] private GameObject flashlight;

    private float maxWorkTime = 60f;
    private float forcedCooldownTime = 20f;
    private float manualCooldownTime = 10f;

    private float currentWorkTime = 0f;
    private bool isCooldown = false;
    private Coroutine flashlightCoroutine;

    void Start()
    {
        if (flashlight != null)
        {
            flashlight.SetActive(false);
        }
    }

    void Update()
    {
        if (isCooldown) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (flashlight != null)
            {
                bool targetState = !flashlight.activeSelf;
                flashlight.SetActive(targetState);

                if (targetState)
                {
                    flashlightCoroutine = StartCoroutine(TrackFlashlightLogic());
                }
                else
                {
                    if (flashlightCoroutine != null) StopCoroutine(flashlightCoroutine);

                    float cooldown = (currentWorkTime > 10f) ? manualCooldownTime : 0f;
                    StartCoroutine(CooldownRoutine(cooldown));
                }
            }
        }
    }

    private IEnumerator TrackFlashlightLogic()
    {
        while (currentWorkTime < maxWorkTime)
        {
            currentWorkTime += Time.deltaTime;
            yield return null;
        }

        flashlight.SetActive(false);
        StartCoroutine(CooldownRoutine(forcedCooldownTime));
    }

    private IEnumerator CooldownRoutine(float cooldownDuration)
    {
        if (cooldownDuration > 0)
        {
            isCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
        }

        currentWorkTime = 0f;
        isCooldown = false;
    }
}
