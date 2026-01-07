using UnityEngine;

public class KartWheelAnimation : MonoBehaviour
{
    [Header("Visual Wheels")]
    [SerializeField] private Transform frontLeft;
    [SerializeField] private Transform frontRight;
    [SerializeField] private Transform rearLeft;
    [SerializeField] private Transform rearRight;

    [Header("Settings")]
    [SerializeField] private float wheelRadius = 0.35f;
    [SerializeField] private float visualSpeedMultiplier = 1.0f; 

    // Добавляем выбор оси
    public enum Axis { X_Axis, Y_Axis, Z_Axis }
    [Header("Axis Correction")]
    [Tooltip("Вокруг какой оси должно крутиться колесо? Попробуйте разные варианты, если крутится не туда.")]
    [SerializeField] private Axis spinAxis = Axis.X_Axis;

    [Tooltip("Если колеса крутятся назад, поставьте галочку.")]
    [SerializeField] private bool invertDirection = false;

    private Rigidbody _rb;
    private float _currentSpinAngle = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        // 1. Считаем скорость
        float forwardSpeed = 0f;
        if (_rb != null)
        {
            // Берем локальную скорость по Z
            Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity); 
            forwardSpeed = localVelocity.z;
        }

        // 2. Считаем угол
        float dir = invertDirection ? -1f : 1f;
        float rotationStep = (forwardSpeed / wheelRadius) * Time.deltaTime * Mathf.Rad2Deg * dir;
        _currentSpinAngle += rotationStep * visualSpeedMultiplier;
        _currentSpinAngle %= 360f;

        // 3. Применяем
        ApplyWheelRotation(frontLeft);
        ApplyWheelRotation(frontRight);
        ApplyWheelRotation(rearLeft);
        ApplyWheelRotation(rearRight);
    }

    private void ApplyWheelRotation(Transform wheel)
    {
        if (wheel == null) return;

        // Выбираем вектор оси на основе настройки в инспекторе
        Vector3 axisVector = Vector3.right; // По умолчанию X
        switch (spinAxis)
        {
            case Axis.X_Axis: axisVector = Vector3.right;   break;
            case Axis.Y_Axis: axisVector = Vector3.up;      break;
            case Axis.Z_Axis: axisVector = Vector3.forward; break;
        }

        // Создаем вращение "качения" вокруг выбранной оси
        Quaternion spinRotation = Quaternion.AngleAxis(_currentSpinAngle, axisVector);

        // ВАЖНО: Применяем вращение ПОВЕРХ текущего (которое задает руль)
        // wheel.localRotation содержит поворот руля (от KartController).
        // Умножая на spinRotation, мы добавляем вращение "вокруг своей оси".
        wheel.localRotation = wheel.localRotation * spinRotation;
    }
}