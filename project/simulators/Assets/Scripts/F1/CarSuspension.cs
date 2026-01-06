using UnityEngine;

public class CarSuspension : MonoBehaviour
{
    [Header("Suspension Points (Raycast Origins)")]
    [SerializeField] private Transform fl; 
    [SerializeField] private Transform fr; 
    [SerializeField] private Transform rl; 
    [SerializeField] private Transform rr; 

    [Header("Visual Wheels (Meshes)")]
    [SerializeField] private Transform visualFL; 
    [SerializeField] private Transform visualFR;
    [SerializeField] private Transform visualRL;
    [SerializeField] private Transform visualRR;

    [Header("Suspension Settings")]
    [SerializeField] private float restLength = 0.4f;      
    [SerializeField] private float springTravel = 0.2f;    
    [SerializeField] private float springStiffness = 30000f; 
    [SerializeField] private float damperStiffness = 4000f;  
    [SerializeField] private float wheelRadius = 0.35f;    

    [Header("Anti-Roll Bar")]
    [SerializeField] private float frontAntiRollStiffness = 8000f;
    [SerializeField] private float rearAntiRollStiffness = 6000f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private Rigidbody rb;
    private Transform _ghostRoot; // "Призрачный" родитель для колес
    
    // Состояния сжатия
    private float lastFLcompression;
    private float lastFRcompression;
    private float lastRLcompression;
    private float lastRRcompression;

    // Телеметрия
    public float ForceFL { get; private set; }
    public float ForceFR { get; private set; }
    public float ForceRL { get; private set; }
    public float ForceRR { get; private set; }
    
    public float CompressionFL => lastFLcompression;
    public float CompressionFR => lastFRcompression;
    public float HitDistFL { get; private set; }
    public float HitDistFR { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // --- FIX FOR SKEWED WHEELS ---
        // 1. Создаем чистый контейнер в корне сцены
        GameObject ghostObj = new GameObject(name + "_VisualWheels_Container");
        _ghostRoot = ghostObj.transform;
        
        // 2. Сразу выравниваем его с машиной
        _ghostRoot.position = transform.position;
        _ghostRoot.rotation = transform.rotation;
        _ghostRoot.localScale = Vector3.one; // ВАЖНО: Масштаб всегда 1,1,1

        // 3. Переносим визуальные колеса в этот контейнер
        // Теперь они дети "правильного" объекта, а не сплющенной машины
        if (visualFL) visualFL.SetParent(_ghostRoot, true);
        if (visualFR) visualFR.SetParent(_ghostRoot, true);
        if (visualRL) visualRL.SetParent(_ghostRoot, true);
        if (visualRR) visualRR.SetParent(_ghostRoot, true);
    }

    private void Update()
    {
        // Синхронизируем контейнер с машиной каждый кадр.
        // Колеса будут вращаться вместе с корпусом, но не будут сплющиваться.
        if (_ghostRoot != null)
        {
            _ghostRoot.position = transform.position;
            _ghostRoot.rotation = transform.rotation;
        }
    }

    private void OnDestroy()
    {
        // Убираем мусор, если машина уничтожена
        if (_ghostRoot) Destroy(_ghostRoot.gameObject);
    }

    private void FixedUpdate()
    {
        ForceFL = SimulateWheel(fl, visualFL, ref lastFLcompression, out float distFL);
        ForceFR = SimulateWheel(fr, visualFR, ref lastFRcompression, out float distFR);
        ForceRL = SimulateWheel(rl, visualRL, ref lastRLcompression, out float distRL);
        ForceRR = SimulateWheel(rr, visualRR, ref lastRRcompression, out float distRR);

        HitDistFL = distFL; HitDistFR = distFR;

        ApplyAntiRollBars();
    }

    private float SimulateWheel(Transform pivot, Transform visualWheel, ref float lastCompression, out float hitDistance)
    {
        Vector3 origin = pivot.position;
        Vector3 direction = -pivot.up;
        float maxDist = restLength + springTravel + wheelRadius;
        
        hitDistance = maxDist; 
        float totalForce = 0f;
        float currentLenForVisual = maxDist;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist))
        {
            hitDistance = hit.distance;
            float currentLength = hit.distance - wheelRadius;
            
            currentLength = Mathf.Clamp(currentLength, restLength - springTravel, restLength + springTravel);

            float compression = restLength - currentLength;
            float springForce = compression * springStiffness;
            float compressionVelocity = (compression - lastCompression) / Time.fixedDeltaTime;
            float damperForce = compressionVelocity * damperStiffness;

            totalForce = springForce + damperForce;

            Vector3 forceVector = pivot.up * totalForce;
            rb.AddForceAtPosition(forceVector, pivot.position, ForceMode.Force);

            lastCompression = compression;
            currentLenForVisual = hit.distance;
        }
        else
        {
            lastCompression = -springTravel; 
            totalForce = 0f;
            currentLenForVisual = maxDist; 
        }

        if (visualWheel != null)
        {
            float visualDist = currentLenForVisual - wheelRadius;
            visualDist = Mathf.Clamp(visualDist, restLength - springTravel, restLength + springTravel);

            // Используем pivot (который всё еще на машине), чтобы найти точку в мире
            // А visualWheel (который теперь в контейнере) перемещаем в эту мировую точку.
            visualWheel.position = origin + direction * visualDist;
            
            // Вращение (Steering) по-прежнему работает из KartController, так как он меняет localRotation.
            // А так как родитель теперь _ghostRoot (1,1,1), localRotation не вызывает искажений.
        }

        return totalForce;
    }

    private void ApplyAntiRollBars()
    {
        float frontDiff = lastFLcompression - lastFRcompression;
        float frontForce = frontDiff * frontAntiRollStiffness;
        if (lastFLcompression > -springTravel + 0.001f) rb.AddForceAtPosition(-fl.up * frontForce, fl.position, ForceMode.Force);
        if (lastFRcompression > -springTravel + 0.001f) rb.AddForceAtPosition(fr.up * frontForce, fr.position, ForceMode.Force);

        float rearDiff = lastRLcompression - lastRRcompression;
        float rearForce = rearDiff * rearAntiRollStiffness;
        if (lastRLcompression > -springTravel + 0.001f) rb.AddForceAtPosition(-rl.up * rearForce, rl.position, ForceMode.Force);
        if (lastRRcompression > -springTravel + 0.001f) rb.AddForceAtPosition(rr.up * rearForce, rr.position, ForceMode.Force);
    }
    
    private void OnDrawGizmos()
    {
         if (!showGizmos) return;
         DrawWheelGizmo(fl); DrawWheelGizmo(fr); DrawWheelGizmo(rl); DrawWheelGizmo(rr);
    }
    
    private void DrawWheelGizmo(Transform pivot) 
    {
        if(pivot == null) return;
        Vector3 origin = pivot.position;
        Vector3 direction = -pivot.up;
        float maxDist = restLength + springTravel + wheelRadius;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist)) {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.05f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hit.point + pivot.up * wheelRadius, wheelRadius);
        } else {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + direction * maxDist);
        }
    }
}