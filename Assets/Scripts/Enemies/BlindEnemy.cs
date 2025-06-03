using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class BlindEnemy : Enemy
{
    private readonly int MAXHEALTH = 2;
    private enum EnemyStates { PATROL, ATTACK, KNOCKED }
    
    [Header("States")]
    [SerializeField] private EnemyStates _CurrentState;
    [SerializeField] private float _StateTime;
    
    [Header("Player")]
    [SerializeField] private GameObject _Player;
    
    [Header("Layers")]
    [SerializeField] private LayerMask _LayerPlayer;
    [SerializeField] private LayerMask _LayerObjectsAndPlayer;
    [SerializeField] private LayerMask _LayerDoor;
    
    [Header("Detection spheres")]
    [SerializeField] private GameObject _DetectionSphere;
    [SerializeField] private GameObject _DetectionDoors;
    [SerializeField] private GameObject _DetectionPlayer;
    
    [Header("Waypoints")]
    [SerializeField] private List<GameObject> _WaypointsFirstPart;
    [SerializeField] private List<GameObject> _WaypointsSecondPart;
    
    [Header("Door to change rooms")]
    [SerializeField] private Door _DoorLocked;

    [Header("Audio")]
    [SerializeField] private AudioClip _blindAudioClip;
    [SerializeField] private AudioClip _blindAttackAudioClip;
    
    
    private AudioSource _BlindAudioSource;
    private NavMeshAgent _NavMeshAgent;
    private Rigidbody _Rigidbody;
    private Collider _Collider;
    private Animator _Animator;
    private Vector3 _SoundPos;
    private float _RangeSearchSound;
    private bool _OpeningDoor;
    private bool _Patrolling;
    private bool _Search;
    
    public bool Jumping { get; private set; }
    public bool AttackAvaliable;

    private Coroutine _PatrolCoroutine;
    private Coroutine _ChangeStateToPatrol;
    private Coroutine _AttackCoroutine;
    
    public event Action OnJump;
    
    private int _Hp;
    public override int hp => _Hp;

    private int _DownTime;
    public override int downTime => _DownTime;
    
    private void Awake()
    {
        _Collider = GetComponent<Collider>();
        _Rigidbody = GetComponent<Rigidbody>();
        _BlindAudioSource = GetComponent<AudioSource>();
        _Animator = GetComponent<Animator>();
        _NavMeshAgent = GetComponent<NavMeshAgent>();
        _SoundPos = Vector3.zero;
        _RangeSearchSound = 55;
        AttackAvaliable = true;
        _OpeningDoor = false;
        _Patrolling = false;
        _Search = false;
        Jumping = false;
        _Hp = MAXHEALTH;

        _PatrolCoroutine = null;
        _AttackCoroutine = null;
        _ChangeStateToPatrol = null;
        
        _DetectionSphere.GetComponent<DetectionSphere>().OnEnter += OnEnter;
        _DetectionDoors.GetComponent<DetectionDoorSphere>().OnDetectDoor += OpenDoors;
        _DetectionPlayer.GetComponent<DetectionBox>().OnEnter += PlayerAhead;
    }

    private void OnDestroy()
    {
        _DetectionSphere.GetComponent<DetectionSphere>().OnEnter -= OnEnter;
        _DetectionDoors.GetComponent<DetectionDoorSphere>().OnDetectDoor -= OpenDoors;
        _DetectionPlayer.GetComponent<DetectionBox>().OnEnter -= PlayerAhead;
    }

    private void Start()
    {
        InitState(EnemyStates.PATROL);
        _BlindAudioSource.Play();
    }

    #region FSM

    private void ChangeState(EnemyStates newState)
    {
        Debug.Log($"---------------------- Sortint de {_CurrentState} a {newState} ------------------------");
        ExitState(_CurrentState);

        Debug.Log($"---------------------- Entrant a {newState} ------------------------");
        InitState(newState);
    }

    private void InitState(EnemyStates newState)
    {
        _CurrentState = newState;

        switch (_CurrentState)
        {
            case EnemyStates.PATROL:
                _BlindAudioSource.resource = _blindAudioClip;
                _BlindAudioSource.Play();
                if (_PatrolCoroutine == null)
                    _PatrolCoroutine = StartCoroutine(Patrol());
                break;
            case EnemyStates.ATTACK:
                _Animator.enabled = false;
                _NavMeshAgent.enabled = false;
                _Rigidbody.isKinematic = false;
                _Collider.enabled = false;
                break;
            case EnemyStates.KNOCKED:
                _Collider.enabled = false;
                _Animator.enabled = false;
                _NavMeshAgent.SetDestination(transform.position);
                StartCoroutine(WakeUp(10));
                break;
        }
    }

    private void ExitState(EnemyStates exitState)
    {
        switch (exitState)
        {
            case EnemyStates.PATROL:
                _Patrolling = false;
                if (_PatrolCoroutine != null)
                {
                    StopCoroutine(_PatrolCoroutine);
                    _PatrolCoroutine = null;
                }
                if (_ChangeStateToPatrol != null)
                {
                    StopCoroutine(_ChangeStateToPatrol);
                    _ChangeStateToPatrol = null;
                }
                break;
            case EnemyStates.ATTACK:
                _RangeSearchSound = 25;
                if(!_NavMeshAgent.enabled)
                    _NavMeshAgent.enabled = true;
        
                _Rigidbody.isKinematic = true;
                _Animator.enabled = true;
                _Collider.enabled = true;
                break;
            case EnemyStates.KNOCKED:
                _Collider.enabled = true;
                _Animator.enabled = false;
                _Hp = MAXHEALTH;
                break;
        }
    }

    #endregion 

    // Funcio per moure l'enemic pel mapa
    IEnumerator Patrol()
    {
        Vector3 point = Vector3.zero;
        while (true)
        {
            if (!_Patrolling)
            {
                if (!_Search)
                {
                    while (true)
                    {
                        //Aquest monstre pot anar entre dues parts del mapa, però una d'aquestes
                        //està tancada fins a superar el puzle dels jeroglifics,
                        //per això la comprovació de la porta.
                        if (_DoorLocked.isLocked)
                        {
                            point = _WaypointsFirstPart[Random.Range(0, _WaypointsFirstPart.Count)].transform.position;
                        }
                        else
                        {
                            int random = Random.Range(0, 3);

                            switch (random)
                            {
                                case 0:
                                    point = _WaypointsFirstPart[Random.Range(0, _WaypointsFirstPart.Count)].transform.position;
                                    break;
                                case 1:
                                case 2:
                                    point = _WaypointsSecondPart[Random.Range(0, _WaypointsSecondPart.Count)].transform.position;
                                    break;
                            }
                        }
                        if (point != _NavMeshAgent.destination)
                            break;
                    }
                    _NavMeshAgent.SetDestination(point);
                }
                else
                {
                    //Aquesta comprovació és per evitar que si ha sentit un so
                    //molt fort elimini el SetDestination() cap al punt
                    if(_RangeSearchSound > 0)
                    {
                        RandomPoint(_SoundPos, _RangeSearchSound, out Vector3 coord);
                        point = coord;
                        _NavMeshAgent.SetDestination(point);
                    }
                }
                _Animator.enabled = true;
                _Patrolling = true;
            }

            if (!_NavMeshAgent.pathPending && _NavMeshAgent.remainingDistance <= _NavMeshAgent.stoppingDistance)
            {
                //Si l'enemic està investigant el so (el booleà _Search) i no s'ha activat la corutina
                //específica l'activem. Aquesta corutina és la que farà canviar al monstre d'estat perquè
                //torni a patrullar pels seus waypoints.
                if (_Search && _CurrentState != EnemyStates.ATTACK && _ChangeStateToPatrol == null)
                {
                    Debug.Log("Activo la corutina WakeUp");
                    _ChangeStateToPatrol = StartCoroutine(WakeUp(2));
                }
                _Animator.enabled = false;
                _Patrolling = false;
                yield return new WaitForSeconds(1);
            }
            else
                yield return new WaitForSeconds(0.5f);
        }
    }

    //Aquesta funcio busca un als navmesh punt aleatori donat un punt i el radi de la circumferencia.
    private void RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 50; i++)
        {
            //Agafa un punt aleatori dins de l'esfera amb el radi que passem per parametre
            Vector3 randomPoint = new Vector3(center.x, center.y, center.z) + Random.insideUnitSphere * range;
            Vector3 point = new Vector3(randomPoint.x, randomPoint.y, randomPoint.z);

            //Filtre per seleccionar les superficies que utilitza l'agent
            NavMeshQueryFilter filter = new()
            {
                agentTypeID = _NavMeshAgent.agentTypeID,
                areaMask = _NavMeshAgent.areaMask
            };

            //Comprovem que el punt que hem agafat esta dins del NavMesh
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 1.0f, filter))
            {
                result = hit.position;
            }
        }
        Debug.Log("No he trobat un punt! (Enemic cec)");
        result = center;
    }

    //Funcio que criden tant els objectes llençables com el jugador
    //per notificar als enemics que han fet un so, i que aquests actuin acorde
    public override void ListenSound(Vector3 pos, int lvlSound)
    {
        bool wall = false;
        _SoundPos = pos;
        //Fem un raycast des de la posicio de l'enemic fins a l'origen del so i recollim tots els objectes que hem tocat en el cami
        RaycastHit[] hits = Physics.RaycastAll(transform.position, _SoundPos - transform.position, Vector3.Distance(_SoundPos, transform.position));

        //Per cada objecte que hem tocat mirem si són objectes que atenuen el so
        //Si ho són reduïm el valor del so rebut per paràmetre pel valor que ens dona l'objecte.
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.TryGetComponent<IAttenuable>(out IAttenuable a))
            {
                lvlSound = a.AttenuateSound(lvlSound);
                wall = true;
            }
        }

        //Distància del cec fins al punt del so
        float dist = Vector3.Distance(transform.position, _SoundPos);
        Debug.Log($"Distància entre cec i punt de so: {dist}");

        //Si la distància entre l'origen del so i el monstre és menor a 10 i el monstre té visió directa amb el jugador,
        //el monstre passarà al valor més agressiu de resposta al so.
        if (dist < 10 && Physics.Raycast(transform.position, (_Player.transform.position - transform.position), out RaycastHit info, dist, _LayerObjectsAndPlayer))
        {
            if (info.collider.TryGetComponent<Player>(out _))
            {
                lvlSound = 8;
            }
            else
            {
                lvlSound = 3;
            }
        }

        Debug.Log($"Nivell so: {lvlSound}");

        //Llista de resposta al so per part del monstre. Com més fort és el nivell de so
        //més a prop d'aquest va, fins que si és molt alt el monstre atacarà.
        if (lvlSound > 0 && _CurrentState == EnemyStates.PATROL)
        {
            if (lvlSound > 0 && lvlSound <= 2)
            {
                _RangeSearchSound = 1.5f;
                _Search = true;
            }
            else if (lvlSound > 2 && lvlSound <= 3)
            {
                _RangeSearchSound = 1;
                _Search = true;
            }
            else if (lvlSound > 3 && lvlSound <= 5)
            {
                _RangeSearchSound = 0.5f;
                _Search = true;
            }
            else if (lvlSound > 5)
            {
                //Si la distància calculada anteriorment està entre 1.5 i 8, no té cap paret davant i pot saltar,
                //saltarà cap a la font de so, independentment de si aquesta és produïda pel jugador o per un objecte.
                if (dist <= 8 && !wall && !Jumping)
                {
                    Debug.Log("Faig salt!");
                    Jumping = true;
                    ChangeState(EnemyStates.ATTACK);
                    Vector3 start = transform.position;
                    Vector3 end = _SoundPos;
                    
                    _Rigidbody.AddForce((end - start) * 140);

                    //No necessiten comprovació ja que només entrarà aquí quan pugui tornar a saltar,
                    //que es recupera quan acaba la corutina de RecoverJump
                    _BlindAudioSource.PlayOneShot(_blindAttackAudioClip);
                    StartCoroutine(RecoverJump());
                    StartCoroutine(DelayRecover());
                }
                //Si la distància és massa gran només anirà fins al punt d'orígen del so.
                else if (Vector3.Distance(_SoundPos, transform.position) > 8)
                {
                    _RangeSearchSound = 0;
                    _Search = true;
                    _NavMeshAgent.SetDestination(_SoundPos);
                    Debug.Log($"Soc l'enemic cec i vaig al punt {_NavMeshAgent.destination}");
                }
            }
            
            if(lvlSound <= 5)
                ChangeState(EnemyStates.PATROL);
        }
    }

    public override void TakeHealth()
    {
        if (_CurrentState != EnemyStates.KNOCKED)
        {
            _Hp--;
            if (_Hp == 0)
                ChangeState(EnemyStates.KNOCKED);
        }
    }

    IEnumerator WakeUp(int time)
    {
        yield return new WaitForSeconds(time);
        if(_CurrentState == EnemyStates.KNOCKED)
            _Hp = MAXHEALTH;
        _Search = false;
        ChangeState(EnemyStates.PATROL);
    }

    private void OpenDoors(Door door)
    {
        if (!door.isLocked && !door.isOpen && !_OpeningDoor)
        {
            door.onDoorOpen += Resume;
            _NavMeshAgent.isStopped = true;
            door.Open(transform.position);
            _OpeningDoor = true;
        }
        else if (door.isLocked)
        {
            _NavMeshAgent.SetDestination(transform.position);
        }
    }

    private void Resume(Door door)
    {
        _NavMeshAgent.isStopped = false;
        StartCoroutine(CloseDoorAutomatic(door));
        door.onDoorOpen -= Resume;
        _OpeningDoor = false;
    }

    IEnumerator CloseDoorAutomatic(Door door)
    {
        yield return new WaitForSeconds(1);
        door.Close();
    }

    public void ActivatePatrol()
    {
        ChangeState(EnemyStates.PATROL);
    }
    
    IEnumerator DelayRecover()
    {
        yield return new WaitForSeconds(0.3f);
        OnJump?.Invoke();
    }
    
    IEnumerator RecoverJump()
    {
        yield return new WaitForSeconds(3f);
        AttackAvaliable = true;
        Jumping = false;
    }

    private void OnEnter()
    {
        if(_CurrentState != EnemyStates.KNOCKED)
        {
            _Search = false;
            if (_ChangeStateToPatrol != null)
            {
                StopCoroutine(_ChangeStateToPatrol);
                _ChangeStateToPatrol = null;
            }

            if (!_NavMeshAgent.enabled)
                _NavMeshAgent.enabled = true;
        }
    }

    private void PlayerAhead(Player player)
    {
        if(!Jumping)
            ListenSound(player.transform.position, 20);
    }
}