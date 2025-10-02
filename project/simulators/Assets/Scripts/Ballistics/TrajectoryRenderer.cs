//using forces;
using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class TraectoryRenderer : MonoBehaviour
{
    [Header("Tajectory parametres")]
    [SerializeField] private int _pointCount = 30;
    [SerializeField] private float _lineWidth = 0.15f;
    [SerializeField] private float _timeStep = 0.1f;

    [Header("Физика воздуха")]
    [SerializeField] private float _mass = 1f;             // кг
    [SerializeField] private float _radius = 0.1f;         // м
    [SerializeField] private float _dragCoefficient = 0.47f;// сфера
    [SerializeField] private float _airDensity = 1.225f;   // кг/м^3
    [SerializeField] private Vector3 _wind = Vector3.zero; // м/с
    
    private float _area;

    private LineRenderer _lineRenderer;

    private void Awake() => InitializeLineRenderer();

    private void InitializeLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.useWorldSpace = true;

        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void DrawVacuum(Vector3 startPosition, Vector3 startVelocity)
    {
        if (_pointCount < 2) _pointCount = 2;

        _lineRenderer.positionCount = _pointCount;

        for (int i = 0; i < _pointCount; ++i)
        {
            float t = i * _timeStep;
            Vector3 newPosition = startPosition + startVelocity * t + Physics.gravity * t * t / 2;
            _lineRenderer.SetPosition(i, newPosition);
        }
    }

    // Новый метод: DrawVacuum с параметрами массы и радиуса
    public void DrawVacuum(Vector3 startPosition, Vector3 startVelocity, float mass, float radius)
    {
        _mass = mass;
        _radius = radius;
        _area = Mathf.PI * _radius * _radius;
        DrawVacuum(startPosition, startVelocity);
    }

    public void DrawWithAirEuler(float mass, float radius, Vector3 startPosition, Vector3 startVelocity)
    {
        _area = Mathf.PI * radius * radius;
        
        Vector3 p = startPosition;
        Vector3 v = startVelocity;
        _lineRenderer.positionCount = _pointCount;

        for (int i = 0; i < _pointCount; i++)
        {
            _lineRenderer.SetPosition(i, p);

            Vector3 vRel = v - _wind;
            float speed = vRel.magnitude;
            Vector3 drag = speed > 1e-6f ? (-0.5f * _airDensity * _dragCoefficient * _area * speed) * vRel : Vector3.zero;
            Vector3 a = Physics.gravity + drag / mass;

            v += a * _timeStep;
            p += v * _timeStep;
        }
    }
}
