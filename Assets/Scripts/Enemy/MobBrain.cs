using UnityEngine;
using UnityEngine.AI;

public enum MobState { Wander, Listen, Chase }

public class MobBrain : MonoBehaviour
{
    [Header("Зрение")]
    [SerializeField] private float _viewAngle = 120f;
    [SerializeField] private float _viewDistance = 25f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Скорости")]
    [SerializeField] private float _walkSpeed = 1.5f;
    [SerializeField] private float _runSpeed = 3.5f;

    [Header("Патрулирование")]
    [SerializeField] private float _wanderRadius = 8f;
    [SerializeField] private float _scanInterval = 15f;      // Как часто останавливается послушать
    [SerializeField] private float _listenTime = 2.5f;       // Сколько стоит и слушает
    [SerializeField] private float _scanRadius = 20f;

    [Header("Атака")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private int _attackDamage = 1;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("Поворот")]
    [SerializeField] private float _rotationSpeed = 5f;      // Скорость плавного поворота (увеличь, если нужно быстрее)

    [Header("Ссылки")]
    [SerializeField] private Transform _target;
    [SerializeField] private NavMeshAgent _agent;

    private MobState _state = MobState.Wander;
    private float _timer;
    private float _lastAttackTime;
    private Animator _anim;

    private float _stuckTimer;
    private Vector3 _lastPosition;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        _agent.speed = _walkSpeed;
        _timer = _scanInterval;
        SetWanderPoint();
    }

    void Update()
    {
        bool canSeePlayer = SeeTarget();

        UpdateState(canSeePlayer);
        SmoothRotation();      // ← Новый вызов: плавный поворот лицом к движению
        Act();
        UpdateAnimation();
        TryAttack();
        CheckStuck();
    }

    // ====================== ЗРЕНИЕ ======================
    bool SeeTarget()
    {
        if (_target == null) return false;

        Vector3 dir = _target.position - transform.position;
        float dist = dir.magnitude;

        if (dist > _viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > _viewAngle * 0.5f) return false;
        if (Physics.Raycast(transform.position + Vector3.up * 0.8f, dir.normalized, dist, _obstacleMask))
            return false;

        return true;
    }

    // ====================== СОСТОЯНИЯ ======================
    void UpdateState(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            _state = MobState.Chase;
            _agent.speed = _runSpeed;
            _agent.isStopped = false;
            return;
        }

        switch (_state)
        {
            case MobState.Wander:
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f)
                    SetWanderPoint();

                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _state = MobState.Listen;
                    _agent.isStopped = true;
                    _timer = _listenTime;
                }
                break;

            case MobState.Listen:
                _timer -= Time.deltaTime;

                if (Time.frameCount % 10 == 0)
                {
                    if (Vector3.Distance(transform.position, _target.position) <= _scanRadius)
                    {
                        _state = MobState.Chase;
                        _agent.speed = _runSpeed;
                        _agent.isStopped = false;
                        return;
                    }
                }

                if (_timer <= 0f)
                {
                    _state = MobState.Wander;
                    _agent.isStopped = false;
                    _timer = _scanInterval;
                    SetWanderPoint();
                }
                break;

            case MobState.Chase:
                // Потеряли игрока → сразу в патруль
                _state = MobState.Wander;
                _agent.speed = _walkSpeed;
                _agent.isStopped = false;
                _timer = _scanInterval;
                SetWanderPoint();
                break;
        }
    }

    // ====================== ПЛАВНЫЙ ПОВОРОТ ЛИЦОМ К ДВИЖЕНИЮ ======================
    void SmoothRotation()
    {
        if (_agent.velocity.sqrMagnitude < 0.1f) return;   // если стоим — не поворачиваем

        // Берём направление, куда реально движется агент
        Vector3 targetDirection = _agent.velocity.normalized;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    void Act()
    {
        if (_state == MobState.Chase)
            _agent.SetDestination(_target.position);
    }

    void TryAttack()
    {
        if (_state != MobState.Chase) return;
        if (Vector3.Distance(transform.position, _target.position) > _attackRange) return;
        if (Time.time - _lastAttackTime < _attackCooldown) return;

        _target.GetComponent<PlayerHealth>()?.TakeDamage(_attackDamage);
        _anim.SetTrigger("Attack");
        _lastAttackTime = Time.time;
    }

    void UpdateAnimation()
    {
        _anim.SetBool("isRunning", _state == MobState.Chase);
        _anim.SetBool("isWalking", _state == MobState.Wander);
        _anim.SetBool("isListening", _state == MobState.Listen);
    }

    void SetWanderPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * _wanderRadius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _wanderRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                return;
            }
        }
        _agent.SetDestination(transform.position);
    }

    void CheckStuck()
    {
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < 1.2f) return;
        _stuckTimer = 0;

        if (Vector3.Distance(transform.position, _lastPosition) < 0.15f && _agent.hasPath && _agent.remainingDistance > 0.8f)
        {
            transform.Rotate(0, Random.Range(-90f, 90f), 0);

            if (_state == MobState.Chase)
                _agent.SetDestination(_target.position);
            else
                SetWanderPoint();
        }
        _lastPosition = transform.position;
    }
}