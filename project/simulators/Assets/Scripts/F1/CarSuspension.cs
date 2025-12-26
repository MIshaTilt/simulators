using UnityEngine;

public class CarSuspension : MonoBehaviour
{
    [Header("Suspension Points")]
    [SerializeField] private Transform fl; // Front Left
    [SerializeField] private Transform fr; // Front Right
    [SerializeField] private Transform rl; // Rear Left
    [SerializeField] private Transform rr; // Rear Right

    [Header("Visual Wheels (Meshes)")]
    [SerializeField] private Transform visualFL; 
    [SerializeField] private Transform visualFR;
    [SerializeField] private Transform visualRL;
    [SerializeField] private Transform visualRR;

    [Header("Suspension Settings")]
    [SerializeField] private float restLength = 0.4f;      // Длина покоя
    [SerializeField] private float springTravel = 0.2f;    // Ход подвески (+/- от покоя)
    [SerializeField] private float springStiffness = 30000f; // Жесткость пружины
    [SerializeField] private float damperStiffness = 4000f;  // Жесткость демпфера
    [SerializeField] private float wheelRadius = 0.35f;    // Радиус колеса

    [Header("Anti-Roll Bar")]
    [SerializeField] private float frontAntiRollStiffness = 8000f;
    [SerializeField] private float rearAntiRollStiffness = 6000f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private Rigidbody rb;

    // Состояния сжатия для вычисления скорости (демпфирования) и ARB
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
    public float CompressionRL => lastRLcompression;
    public float CompressionRR => lastRRcompression;

    public float HitDistFL { get; private set; }
    public float HitDistFR { get; private set; }
    public float HitDistRL { get; private set; }
    public float HitDistRR { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Передаем и точку подвески (fl), и визуальную модель (visualFL)
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
        float currentLenForVisual = maxDist; // По умолчанию колесо висит полностью вытянутым

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist))
        {
            hitDistance = hit.distance;
            float currentLength = hit.distance - wheelRadius;
            
            // Ограничение хода (Physics)
            currentLength = Mathf.Clamp(currentLength, restLength - springTravel, restLength + springTravel);

            // Сжатие пружины
            float compression = restLength - currentLength;
            float springForce = compression * springStiffness;
            float compressionVelocity = (compression - lastCompression) / Time.fixedDeltaTime;
            float damperForce = compressionVelocity * damperStiffness;

            totalForce = springForce + damperForce;

            Vector3 forceVector = pivot.up * totalForce;
            rb.AddForceAtPosition(forceVector, pivot.position, ForceMode.Force);

            lastCompression = compression;
            
            // Для визуализации: колесо стоит на земле
            currentLenForVisual = hit.distance;
        }
        else
        {
            lastCompression = -springTravel; 
            totalForce = 0f;
            // Для визуализации: колесо вытянуто на максимум
            currentLenForVisual = maxDist; 
        }

        // --- ОБНОВЛЕНИЕ ВИЗУАЛЬНОГО ПОЛОЖЕНИЯ ---
        if (visualWheel != null)
        {
            // Мы ставим колесо ровно туда, где коснулся луч (или в конец подвески),
            // учитывая радиус колеса, чтобы оно не проваливалось в землю.
            // Формула: Позиция крепления - (Вектор Вниз * Дистанция до центра колеса)
            
            // Дистанция до центра колеса = Дистанция луча - Радиус? 
            // Нет, hit.distance — это расстояние до земли. Центр колеса выше на wheelRadius.
            // Но мы двигаем визуальный центр.
            
            // Проще: ставим колесо на позицию: Origin + Direction * (HitDistance - Radius)
            // Но если мы в полете, HitDistance нет. Берем maxDist.
            
            float visualDist = currentLenForVisual - wheelRadius;
            
            // Защита от "проваливания" сквозь ограничители подвески при очень сильных ударах
            visualDist = Mathf.Clamp(visualDist, restLength - springTravel, restLength + springTravel);

            visualWheel.position = origin + direction * visualDist;
            
            // Примечание: Вращение (Steering) управляется из KartController через localRotation,
            // а здесь мы управляем position. Они не конфликтуют.
        }

        return totalForce;
    }


    private void ApplyAntiRollBars()
    {
        float frontDiff = lastFLcompression - lastFRcompression;
        float frontForce = frontDiff * frontAntiRollStiffness;

        if (lastFLcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(-fl.up * frontForce, fl.position, ForceMode.Force);
        if (lastFRcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(fr.up * frontForce, fr.position, ForceMode.Force);

        float rearDiff = lastRLcompression - lastRRcompression;
        float rearForce = rearDiff * rearAntiRollStiffness;

        if (lastRLcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(-rl.up * rearForce, rl.position, ForceMode.Force);
        if (lastRRcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(rr.up * rearForce, rr.position, ForceMode.Force);
    }

    // =========================================================
    //               ВИЗУАЛИЗАЦИЯ (GIZMOS)
    // =========================================================
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        DrawWheelGizmo(fl);
        DrawWheelGizmo(fr);
        DrawWheelGizmo(rl);
        DrawWheelGizmo(rr);
    }

    private void DrawWheelGizmo(Transform pivot)
    {
        if (pivot == null) return;

        Vector3 origin = pivot.position;
        Vector3 direction = -pivot.up;

        // Расчетные длины
        float minLen = restLength - springTravel; // Максимальное сжатие (верхняя точка)
        float maxLen = restLength + springTravel; // Максимальный вылет (нижняя точка)
        float rayLen = maxLen + wheelRadius;      // Полная длина луча проверки

        // 1. Рисуем ось подвески (пределы)
        Gizmos.color = Color.white; 
        Vector3 pointMin = origin + direction * minLen;
        Vector3 pointMax = origin + direction * maxLen;
        
        Gizmos.DrawLine(origin, pointMax); // Линия всего хода
        Gizmos.DrawLine(pointMin + pivot.right * 0.05f, pointMin - pivot.right * 0.05f); // Отметка макс сжатия
        Gizmos.DrawLine(pointMax + pivot.right * 0.05f, pointMax - pivot.right * 0.05f); // Отметка макс вылета

        // Точка покоя (Rest Length)
        Vector3 pointRest = origin + direction * restLength;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointRest + pivot.right * 0.1f, pointRest - pivot.right * 0.1f);

        // 2. Raycast и текущее положение
        bool isHit = Physics.Raycast(origin, direction, out RaycastHit hit, rayLen);
        
        Vector3 wheelCenterPos;
        
        if (isHit)
        {
            // ЛУЧ ПОПАЛ
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, hit.point);
            
            // Точка контакта с землей
            Gizmos.DrawWireSphere(hit.point, 0.05f);

            // Вычисляем центр колеса
            wheelCenterPos = hit.point + pivot.up * wheelRadius;
        }
        else
        {
            // ЛУЧ НЕ ПОПАЛ (колесо висит)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + direction * rayLen);
            
            // Колесо висит на максимальном вылете
            wheelCenterPos = origin + direction * maxLen;
        }

        // 3. Рисуем само колесо (WireSphere)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(wheelCenterPos, wheelRadius);

        // 4. Рисуем "Пружину" (зигзаг)
        DrawSpring(origin, wheelCenterPos);
    }

    private void DrawSpring(Vector3 start, Vector3 end)
    {
        Gizmos.color = new Color(1f, 0.5f, 0f); // Оранжевый
        int segments = 8;
        Vector3 prev = start;
        Vector3 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);
        float step = dist / segments;
        
        // Вектор смещения для зигзага (перпендикулярно оси пружины)
        // Берем локальное "право" от transform, если возможно, иначе приблизительно
        Vector3 right = Vector3.Cross(dir, Vector3.forward).normalized * 0.1f;
        if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(dir, Vector3.right).normalized * 0.1f;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i * step;
            Vector3 currentBase = start + dir * t;
            // Зигзаг: четные влево, нечетные вправо
            Vector3 offset = (i % 2 == 0) ? right : -right;
            if (i == segments) offset = Vector3.zero; // конец в центре

            Vector3 current = currentBase + offset;
            Gizmos.DrawLine(prev, current);
            prev = current;
        }
    }
}