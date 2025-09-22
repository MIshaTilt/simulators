using TMPro;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _targetPrefab;
    [SerializeField] private float _spawnInterval = 1f;

    public int counter = 0;
    public TextMeshProUGUI counterText;

    private BoxCollider _boxCollider;
    private float _timer;

    void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _timer = 0f;
        SpawnTarget();
    }



    public void SpawnTarget()
    {
        if (_boxCollider == null || _targetPrefab == null) return;

        Vector3 center = _boxCollider.center + transform.position;
        Vector3 size = _boxCollider.size;

        // Генерируем случайную точку внутри коллайдера
        Vector3 randomPoint = new Vector3(
            Random.Range(center.x - size.x / 2, center.x + size.x / 2),
            Random.Range(center.y - size.y / 2, center.y + size.y / 2),
            Random.Range(center.z - size.z / 2, center.z + size.z / 2)
        );

        Instantiate(_targetPrefab, randomPoint, Quaternion.identity);
    }
}
