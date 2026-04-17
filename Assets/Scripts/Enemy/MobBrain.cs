using UnityEngine;
using UnityEngine.AI;

// Мозги моба
public enum MobState { Wander, Listen, Chase } // Состояния: бродит, слушает, бежит

public class MobBrain : MonoBehaviour
{
    [Header("Зрение")]
    [SerializeField] private float _viewAngle = 120f;        // Угол обзора
    [SerializeField] private float _viewDistance = 25f;      // Дальность взгляда
    [SerializeField] private LayerMask _obstacleMask;        // Что блокирует взгляд (стены)

    [Header("Скорости")]
    [SerializeField] private float _walkSpeed = 1.5f;        // Скорость ходьбы
    [SerializeField] private float _runSpeed = 3.5f;         // Скорость бега

    [Header("Патрулирование")]
    [SerializeField] private float _wanderRadius = 8f;       // Радиус случайной точки для брождения
    [SerializeField] private float _scanInterval = 15f;      // Как часто останавливается послушать
    [SerializeField] private float _listenTime = 2.5f;       // Сколько секунд слушает
    [SerializeField] private float _scanRadius = 20f;        // Дальность слуха

    [Header("Атака")]
    [SerializeField] private float _attackRange = 1.5f;      // Дальность удара
    [SerializeField] private int _attackDamage = 1;          // Урон за удар
    [SerializeField] private float _attackCooldown = 1f;     // Задержка между ударами

    [Header("Поворот")]
    [SerializeField] private float _rotationSpeed = 5f;      // Скорость поворота лицом к движению

    [Header("Ссылки")]
    [SerializeField] private NavMeshAgent _agent;            // Навигационный агент (для движения)

    [Header("Цели (перетащить из сцены)")]
    [SerializeField] private Transform _fpsPlayer;           // FPS игрок
    [SerializeField] private Transform _vrPlayer;            // VR игрок

    private MobState _state = MobState.Wander;               // Текущее состояние
    private float _timer;                                     // Таймер для смены состояний
    private float _lastAttackTime;                           // Время последней атаки
    private Animator _anim;                                  // Аниматор моба
    private Transform _target;                               // Активная цель (кто сейчас в игре)

    private float _stuckTimer;                               // Таймер для проверки застревания
    private Vector3 _lastPosition;                           // Прошлая позиция (для проверки застревания)

    // --- ОПТИМИЗАЦИЯ: кэширование для избежания аллокаций ---
    private Vector3 _raycastOrigin = Vector3.zero;           // Кэш для луча (переиспользуем)
    private Vector3 _dirToTarget = Vector3.zero;             // Кэш направления к цели
    private float _sqrViewDistance;                          // Квадрат дистанции взгляда (быстрее чем Vector3.Distance)
    private float _sqrScanRadius;                            // Квадрат дистанции слуха
    private float _sqrAttackRange;                           // Квадрат дистанции атаки
    private float _halfViewAngle;                            // Половина угла обзора (кэшируем)

    // --- ОПТИМИЗАЦИЯ: интервалы обновления ---
    private int _frameCounter;                               // Счётчик кадров
    private const int SIGHT_CHECK_INTERVAL = 3;              // Проверяем зрение раз в 3 кадра
    private const int STUCK_CHECK_INTERVAL = 60;             // Проверяем застревание ~раз в секунду (60 FPS)
    private const int ANIMATION_UPDATE_INTERVAL = 2;         // Обновляем анимацию раз в 2 кадра
    private const int TARGET_UPDATE_INTERVAL = 30;           // Проверяем активного игрока раз в 30 кадров

    // --- ФИКС: кэшируем результат последней проверки зрения ---
    private bool _cachedCanSeePlayer;                        // Запомненный результат проверки зрения
    private int _lastSightCheckFrame = -999;                 // Когда последний раз проверяли зрение

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();               // Берём NavMeshAgent
        _anim = GetComponentInChildren<Animator>();          // Берём аниматор у детей

        // --- ОПТИМИЗАЦИЯ: предвычисляем квадраты дистанций ---
        _sqrViewDistance = _viewDistance * _viewDistance;
        _sqrScanRadius = _scanRadius * _scanRadius;
        _sqrAttackRange = _attackRange * _attackRange;
        _halfViewAngle = _viewAngle * 0.5f;
    }

    void Start()
    {
        _agent.speed = _walkSpeed;                           // Начинаем с медленной скорости
        _timer = _scanInterval;                              // Запускаем таймер до первого прослушивания

        UpdateActivePlayer();                                // Определяем, какой игрок активен
        SetWanderPoint();                                    // Выбираем первую случайную точку
        _lastPosition = transform.position;                  // Инициализируем прошлую позицию
    }

    // Определяет, какой игрок сейчас активен (FPS или VR)
    private void UpdateActivePlayer()
    {
        if (_fpsPlayer != null && _fpsPlayer.gameObject.activeInHierarchy)
            _target = _fpsPlayer;                            // FPS активен
        else if (_vrPlayer != null && _vrPlayer.gameObject.activeInHierarchy)
            _target = _vrPlayer;                             // VR активен
        else
            _target = null;                                  // Никого нет
    }

    void Update()
    {
        if (_target == null) return;                         // Нет цели - ничего не делаем

        // --- ОПТИМИЗАЦИЯ: обновляем активного игрока не каждый кадр ---
        _frameCounter++;
        if (_frameCounter % TARGET_UPDATE_INTERVAL == 0)
            UpdateActivePlayer();

        // --- ОПТИМИЗАЦИЯ: проверка зрения с интервалом ---
        bool canSeePlayer = _cachedCanSeePlayer;
        int sightCheckRate = (_state == MobState.Chase) ? 1 : SIGHT_CHECK_INTERVAL;

        if (_frameCounter - _lastSightCheckFrame >= sightCheckRate)
        {
            canSeePlayer = SeeTarget();
            _cachedCanSeePlayer = canSeePlayer;
            _lastSightCheckFrame = _frameCounter;
        }

        UpdateState(canSeePlayer);                           // Обновляем состояние

        // --- ОПТИМИЗАЦИЯ: поворот только если движемся ---
        if (_agent.velocity.sqrMagnitude > 0.01f)
            SmoothRotation();                                // Плавно поворачиваемся

        Act();                                               // Выполняем действие (идём к цели)

        // --- ОПТИМИЗАЦИЯ: анимация с интервалом ---
        if (_frameCounter % ANIMATION_UPDATE_INTERVAL == 0)
            UpdateAnimation();                               // Обновляем анимацию

        TryAttack();                                         // Пробуем атаковать

        // --- ОПТИМИЗАЦИЯ: застревание с интервалом ---
        if (_frameCounter % STUCK_CHECK_INTERVAL == 0)
            CheckStuck();                                    // Проверяем, не застряли ли

        // --- ФИКС: сбрасываем счётчик кадров чтобы не переполнился ---
        if (_frameCounter > 10000)
            _frameCounter = 0;
    }

    // Проверяет, видит ли моб игрока
    bool SeeTarget()
    {
        // --- ОПТИМИЗАЦИЯ: используем sqrMagnitude вместо Distance ---
        _dirToTarget.x = _target.position.x - transform.position.x;
        _dirToTarget.y = _target.position.y - transform.position.y;
        _dirToTarget.z = _target.position.z - transform.position.z;

        float sqrDist = _dirToTarget.x * _dirToTarget.x +
                        _dirToTarget.y * _dirToTarget.y +
                        _dirToTarget.z * _dirToTarget.z;

        if (sqrDist > _sqrViewDistance) return false;        // Слишком далеко

        // --- ОПТИМИЗАЦИЯ: сначала проверяем угол (дешевле чем raycast) ---
        if (Vector3.Angle(transform.forward, _dirToTarget) > _halfViewAngle)
            return false; // Не в угле обзора

        // Луч из глаз моба (на высоте 0.8м) до игрока
        // --- ОПТИМИЗАЦИЯ: используем временную переменную вместо new Vector3 ---
        _raycastOrigin.x = transform.position.x;
        _raycastOrigin.y = transform.position.y + 0.8f;
        _raycastOrigin.z = transform.position.z;

        float dist = Mathf.Sqrt(sqrDist); // Один sqrt для raycast

        // --- ФИКС: нормализуем направление безопасно ---
        Vector3 normalizedDir = _dirToTarget / dist;

        if (Physics.Raycast(_raycastOrigin, normalizedDir, dist, _obstacleMask))
            return false;                                    // Стена загораживает

        return true;                                         // Вижу!
    }

    // Управляет состояниями: бродит -> слушает -> бежит
    void UpdateState(bool canSeePlayer)
    {
        if (canSeePlayer && _state != MobState.Chase)
        {
            _state = MobState.Chase;                         // Вижу - бегу
            _agent.speed = _runSpeed;
            _agent.isStopped = false;
            return;
        }

        switch (_state)
        {
            case MobState.Wander:
                // Если дошли до точки - выбираем новую
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f)
                    SetWanderPoint();

                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _state = MobState.Listen;                // Пора остановиться и послушать
                    _agent.isStopped = true;                 // Останавливаемся
                    _timer = _listenTime;                    // Таймер на прослушивание
                }
                break;

            case MobState.Listen:
                _timer -= Time.deltaTime;

                // --- ОПТИМИЗАЦИЯ: слух с интервалом через счётчик кадров ---
                if (_frameCounter % 10 == 0)
                {
                    // --- ОПТИМИЗАЦИЯ: используем sqrMagnitude ---
                    float dx = _target.position.x - transform.position.x;
                    float dy = _target.position.y - transform.position.y;
                    float dz = _target.position.z - transform.position.z;
                    float sqrDist = dx * dx + dy * dy + dz * dz;

                    if (sqrDist <= _sqrScanRadius)
                    {
                        _state = MobState.Chase;             // Услышали - бежим
                        _agent.speed = _runSpeed;
                        _agent.isStopped = false;
                        return;
                    }
                }

                if (_timer <= 0f)
                {
                    _state = MobState.Wander;                // Послушали - идём дальше
                    _agent.isStopped = false;
                    _timer = _scanInterval;                  // Сброс таймера до следующего прослушивания
                    SetWanderPoint();
                }
                break;

            case MobState.Chase:
                // --- ФИКС: выходим из погони только когда ТОЧНО не видим ---
                if (!canSeePlayer)
                {
                    // Потеряли игрока из виду - возвращаемся к брождению
                    _state = MobState.Wander;
                    _agent.speed = _walkSpeed;
                    _agent.isStopped = false;
                    _timer = _scanInterval;
                    SetWanderPoint();
                }
                break;
        }
    }

    // Плавно поворачивает моба в сторону движения
    void SmoothRotation()
    {
        Vector3 targetDirection = _agent.velocity.normalized; // Куда идём

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    // Выполняет действие в зависимости от состояния
    void Act()
    {
        if (_state == MobState.Chase)
            _agent.SetDestination(_target.position);         // Бежим к игроку
    }

    // Пробует атаковать, если в зоне удара
    void TryAttack()
    {
        if (_state != MobState.Chase) return;                // Не в погоне - не атакуем

        // --- ОПТИМИЗАЦИЯ: используем sqrMagnitude вместо Distance ---
        float dx = _target.position.x - transform.position.x;
        float dy = _target.position.y - transform.position.y;
        float dz = _target.position.z - transform.position.z;
        float sqrDist = dx * dx + dy * dy + dz * dz;

        if (sqrDist > _sqrAttackRange) return; // Далеко
        if (Time.time - _lastAttackTime < _attackCooldown) return; // Перезарядка

        PlayerHealth playerHealth = _target.GetComponentInChildren<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(_attackDamage);          // Наносим урон
            _anim.SetTrigger("Attack");                      // Запускаем анимацию атаки
            _lastAttackTime = Time.time;                     // Запоминаем время атаки
        }
    }

    // Обновляет параметры аниматора
    void UpdateAnimation()
    {
        _anim.SetBool("isRunning", _state == MobState.Chase);    // Бег
        _anim.SetBool("isWalking", _state == MobState.Wander);   // Ходьба
        _anim.SetBool("isListening", _state == MobState.Listen); // Слушает
    }

    // Выбирает случайную точку для брождения
    void SetWanderPoint()
    {
        for (int i = 0; i < 10; i++)  // Пробуем 10 раз найти точку
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * _wanderRadius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _wanderRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);         // Идём к найденной точке
                return;
            }
        }
        _agent.SetDestination(transform.position);           // Если не нашли - стоим на месте
    }

    // Проверяет, не застрял ли моб (не меняется позиция долгое время)
    void CheckStuck()
    {
        // --- ОПТИМИЗАЦИЯ: используем sqrMagnitude ---
        float dx = transform.position.x - _lastPosition.x;
        float dy = transform.position.y - _lastPosition.y;
        float dz = transform.position.z - _lastPosition.z;
        float sqrDist = dx * dx + dy * dy + dz * dz;

        // Если почти не двигаемся, но должны
        if (sqrDist < 0.0225f && _agent.hasPath && _agent.remainingDistance > 0.8f) // 0.15^2 = 0.0225
        {
            transform.Rotate(0, Random.Range(-90f, 90f), 0);  // Поворачиваемся случайно

            if (_state == MobState.Chase)
                _agent.SetDestination(_target.position);     // Перезапускаем путь к игроку
            else
                SetWanderPoint();                            // Перезапускаем путь к точке
        }
        _lastPosition = transform.position;                  // Запоминаем позицию для следующей проверки
    }
}