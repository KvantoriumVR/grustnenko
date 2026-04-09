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

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();               // Берём NavMeshAgent
        _anim = GetComponentInChildren<Animator>();          // Берём аниматор у детей
    }

    void Start()
    {
        _agent.speed = _walkSpeed;                           // Начинаем с медленной скорости
        _timer = _scanInterval;                              // Запускаем таймер до первого прослушивания

        UpdateActivePlayer();                                // Определяем, какой игрок активен
        SetWanderPoint();                                    // Выбираем первую случайную точку
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

        bool canSeePlayer = SeeTarget();                     // Видим ли игрока?

        UpdateState(canSeePlayer);                           // Обновляем состояние
        SmoothRotation();                                    // Плавно поворачиваемся
        Act();                                               // Выполняем действие (идём к цели)
        UpdateAnimation();                                   // Обновляем анимацию
        TryAttack();                                         // Пробуем атаковать
        CheckStuck();                                        // Проверяем, не застряли ли
    }

    // Проверяет, видит ли моб игрока
    bool SeeTarget()
    {
        Vector3 dir = _target.position - transform.position;
        float dist = dir.magnitude;

        if (dist > _viewDistance) return false;              // Слишком далеко
        if (Vector3.Angle(transform.forward, dir) > _viewAngle * 0.5f) return false; // Не в угле обзора

        // Луч из глаз моба (на высоте 0.8м) до игрока
        if (Physics.Raycast(transform.position + Vector3.up * 0.8f, dir.normalized, dist, _obstacleMask))
            return false;                                    // Стена загораживает

        return true;                                         // Вижу!
    }

    // Управляет состояниями: бродит -> слушает -> бежит
    void UpdateState(bool canSeePlayer)
    {
        if (canSeePlayer)
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

                // Раз в 10 кадров проверяем, не рядом ли игрок (слух)
                if (Time.frameCount % 10 == 0)
                {
                    if (Vector3.Distance(transform.position, _target.position) <= _scanRadius)
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
                // Потеряли игрока из виду - возвращаемся к брождению
                _state = MobState.Wander;
                _agent.speed = _walkSpeed;
                _agent.isStopped = false;
                _timer = _scanInterval;
                SetWanderPoint();
                break;
        }
    }

    // Плавно поворачивает моба в сторону движения
    void SmoothRotation()
    {
        if (_agent.velocity.sqrMagnitude < 0.1f) return;     // Стоим - не поворачиваемся

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
        if (Vector3.Distance(transform.position, _target.position) > _attackRange) return; // Далеко
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
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < 1.2f) return;                      // Проверяем раз в 1.2 секунды
        _stuckTimer = 0;

        // Если почти не двигаемся, но должны
        if (Vector3.Distance(transform.position, _lastPosition) < 0.15f && _agent.hasPath && _agent.remainingDistance > 0.8f)
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