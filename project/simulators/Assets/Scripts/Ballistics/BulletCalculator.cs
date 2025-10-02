using UnityEngine;
using UnityEngine.InputSystem;

namespace forces
{
    [RequireComponent(typeof(TraectoryRenderer))]
    public class BallisticCalcuator: MonoBehaviour
    {
        [SerializeField] private Transform _launchPoint;
        [SerializeField] private float _muzzleVelocity;
        [SerializeField, Range(0,85)] private float _muzzleAngle = 20;
        [Space]
        [SerializeField] private QuadraticDrag _shootRound;
        [SerializeField] private float _mass = 1f;
        [SerializeField] private float _radius = 0.1f;
        [SerializeField] private float _dragCoefficient = 0.67f;
        [SerializeField] private float _airDensity = 1.25f;
        [SerializeField] private Vector3 _wind = Vector3.zero;
        [SerializeField] private bool air;

        // Диапазоны для случайных параметров
        [Header("Random Shot Parameters")]
        [SerializeField] private float _massMin = 0.5f;
        [SerializeField] private float _massMax = 2f;
        [SerializeField] private float _radiusMin = 0.05f;
        [SerializeField] private float _radiusMax = 0.2f;

        private TraectoryRenderer _traectoryRenderer;
        private Vector3 v0;


        // Текущие случайные параметры
        private float _currentMass;
        private float _currentRadius;

        private void Start()
        {
            _traectoryRenderer = GetComponent<TraectoryRenderer>();
            GenerateRandomParams();

        }

        private void Update()
        {
            _traectoryRenderer.DrawWithAirEuler(_currentMass, _currentRadius, _launchPoint.position, v0);

            v0 = CalculateVelocityVector(_muzzleAngle);
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Fire();
                GenerateRandomParams();
            }
        }

        private void GenerateRandomParams()
        {
            _currentMass = Random.Range(_massMin, _massMax);
            _currentRadius = Random.Range(_radiusMin, _radiusMax);
        }
        
        private void Fire()
        {
            if (_shootRound == null) return;
            GameObject newShootRound = Instantiate(_shootRound.gameObject, _launchPoint.position, Quaternion.identity);
            QuadraticDrag quadraticDrag = newShootRound.GetComponent<QuadraticDrag>();



            quadraticDrag.SetPhysicalParams(_currentMass, _currentRadius, _dragCoefficient, _airDensity, _wind, v0);
        }

        private Vector3 CalculateVelocityVector(float angle)
        {
            float vx = _muzzleVelocity * Mathf.Cos(angle * Mathf.Deg2Rad);
            float vy = _muzzleVelocity * Mathf.Sin(angle * Mathf.Deg2Rad);

            return _launchPoint.forward * vx + _launchPoint.up * vy;
        }
    }
}
