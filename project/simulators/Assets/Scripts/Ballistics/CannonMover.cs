using UnityEngine;

public class CannonMover : MonoBehaviour
{

    [SerializeField] private float _yawSensity = 180f;
    [SerializeField] private float _pitchSensity = 180f;
    [SerializeField] private float _rollSensity = 180f; // Added for Z rotation
    [SerializeField] private float _maxPitchDrag = 89f;

    [SerializeField, Range(0.0f, 1.0f)] private float _rotationDamping;

    private float _yawDeg;
    private float _pitchDeg;
    private float _rollDeg; // Added for Z rotation
    private Quaternion _targetRotation;
    public float rotationSpeed = 100f; // Rotation speed in degrees per second

    private void Update()
    {
        float dx = Input.GetAxis("Vertical");
        float dy = Input.GetAxis("Horizontal");

        // Q/E for Z axis rotation
        float dz = 0f;
        if (Input.GetKey(KeyCode.Q))
            dz -= 1f;
        if (Input.GetKey(KeyCode.E))
            dz += 1f;

        _yawDeg += dy * _yawSensity * Time.deltaTime;
        _pitchDeg += dx * _pitchSensity * Time.deltaTime;
        _pitchDeg = Mathf.Clamp(_pitchDeg, -_maxPitchDrag, _maxPitchDrag);

        _rollDeg += dz * _rollSensity * Time.deltaTime;

        Quaternion yawRot = Quaternion.AngleAxis(_yawDeg, Vector3.up);
        Vector3 rightAxis = yawRot * Vector3.right;
        Quaternion pitchRot = Quaternion.AngleAxis(_pitchDeg, rightAxis);

        Vector3 forwardAxis = (pitchRot * yawRot) * Vector3.forward;
        Quaternion rollRot = Quaternion.AngleAxis(_rollDeg, forwardAxis);

        _targetRotation = rollRot * pitchRot * yawRot;

        float t = 1 - Mathf.Pow(1 - Mathf.Clamp01(_rotationDamping), Time.deltaTime * 60);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, t);
    }
}