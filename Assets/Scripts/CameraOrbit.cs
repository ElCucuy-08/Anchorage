using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("Параметры вращения")]
    [SerializeField] private Transform targetObject;
    [SerializeField] private float orbitSpeed = 20f; // Градусы в секунду
    [SerializeField] private float orbitDistance = 5f; // Дальность от объекта
    [SerializeField] private float orbitHeight = 2f; // Высота орбиты

    [Header("Смещение")]
    [SerializeField] private Vector3 lookAtOffset = Vector3.zero;

    private float currentAngle = 0f;

    private void Update()
    {
        if (targetObject == null)
            return;

        // Увеличиваем угол на основе скорости
        currentAngle += orbitSpeed * Time.deltaTime;

        // Вычисляем позицию камеры на орбите
        float x = Mathf.Cos(Mathf.Deg2Rad * currentAngle) * orbitDistance;
        float z = Mathf.Sin(Mathf.Deg2Rad * currentAngle) * orbitDistance;

        Vector3 newPosition = targetObject.position + new Vector3(x, orbitHeight, z);
        transform.position = newPosition;

        // Смотрим на цель
        Vector3 lookAtPosition = targetObject.position + lookAtOffset;
        transform.LookAt(lookAtPosition);
    }
}