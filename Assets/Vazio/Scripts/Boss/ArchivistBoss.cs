using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// O Arquivista — boss final do Vazio.
    ///
    /// Maquina de estados rigida: nenhum ataque se sobrepoe. Apenas no estado
    /// Idle os cronometros correm; Laser tem prioridade sobre Livros quando os
    /// dois vencem juntos (Livros esperam o Laser terminar). Um jogador colado
    /// no boss cancela as acoes terrestres e dispara o Melee.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class ArchivistBoss : MonoBehaviour, IDamageable
    {
        public enum State { Idle, Melee, Books, Laser, PhaseChange, Dead }
        private enum BooksPhase { Jump, Float, Descend }

        public static readonly List<ArchivistBoss> Active = new List<ArchivistBoss>();

        [Header("Vida")]
        [SerializeField, Min(1f)] private float maxHealth = 200f;
        [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoThreshold = 0.5f;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

        [Header("Alvo")]
        [SerializeField] private Transform player;
        [SerializeField] private PlayerStats playerStats;

        [Header("Prefabs")]
        [SerializeField] private GameObject bookPrefab;
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private GameObject puddlePrefab;
        [SerializeField] private GameObject acidPrefab;

        [Header("Cajado")]
        [SerializeField] private Transform staffTip;

        [Header("Arena")]
        [SerializeField] private float groundY = 0f;
        [SerializeField] private float arenaMinX = 81f;
        [SerializeField] private float arenaMaxX = 105f;

        [Header("Cronometros")]
        [SerializeField, Min(2f)] private float laserInterval = 10f;
        [SerializeField, Min(2f)] private float booksMinInterval = 5f;
        [SerializeField, Min(2f)] private float booksMaxInterval = 8f;
        [SerializeField, Min(2f)] private float puddleInterval = 8f;

        [Header("Flutuacao")]
        [SerializeField, Min(1f)] private float jumpVelocity = 11f;
        [SerializeField, Min(0.5f)] private float floatHeight = 2.6f;
        [SerializeField, Min(0.1f)] private float floatSpeed = 2.2f;
        [SerializeField, Min(0.1f)] private float gravityScale = 3.2f;

        [Header("Melee")]
        [SerializeField, Min(0.5f)] private float meleeRange = 2.5f;
        [SerializeField, Min(0f)] private float meleeDamage = 3f;
        [SerializeField, Min(0f)] private float meleeWindup = 0.28f;
        [SerializeField, Min(0f)] private float meleeActive = 0.12f;
        [SerializeField, Min(0f)] private float meleeRecover = 0.35f;
        [SerializeField, Min(0f)] private float meleeCooldown = 1.4f;

        [Header("Laser")]
        [SerializeField, Min(0f)] private float laserCharge = 0.6f;
        [SerializeField, Min(0.1f)] private float laserActive = 0.9f;
        [SerializeField, Min(0f)] private float laserRecover = 0.35f;
        [SerializeField, Min(0f)] private float laserDamage = 3f;
        [SerializeField, Min(0f)] private float laserHeight = 1.25f;

        [Header("Livros")]
        [SerializeField, Range(1, 12)] private int booksPerVolley = 5;
        [SerializeField, Min(0.05f)] private float bookSpawnInterval = 0.28f;
        [SerializeField, Min(0f)] private float booksMinAirTime = 1.4f;
        [SerializeField, Min(1f)] private float booksTimeout = 12f;
        [SerializeField, Min(0f)] private float bookDamage = 2f;

        [Header("Pocas (fase 2)")]
        [SerializeField, Min(0.5f)] private float puddleSpread = 1.7f;
        [SerializeField, Range(1, 5)] private int puddlesPerVolley = 3;
        [SerializeField, Min(0.15f)] private float acidFlightTime = 0.65f;

        [Header("Apresentacao")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BossVisualAnimator visual;
        [SerializeField] private Sprite[] bookSprites;
        [SerializeField] private string displayName = "O ARQUIVISTA";

        [Header("Caminhada")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.5f;

        [Header("Fila de ataques")]
        [SerializeField, Min(0f)] private float attackQueueDelay = 2f;

        private Rigidbody2D _body;
        private BoxCollider2D _collider;

        private State _state = State.Idle;
        private BooksPhase _booksPhase;
        private float _phaseTimer;
        private float _laserTimer;
        private float _booksTimer;
        private float _puddleTimer;

        private float _laserPhaseTimer;
        private int _laserPhaseStep;
        private float _meleePhaseTimer;
        private int _meleePhaseStep;
        private float _nextMeleeTime;
        private State _queuedAttack = State.Idle;
        private float _queuedAt = -1f;
        private float _spawnTimer;
        private float _airTime;
        private float _floatTargetY;
        private int _booksSpawned;
        private int _booksAlive;

        private bool _invulnerable;
        private bool _dead;
        private float _health;
        private bool _flashOn;
        private float _hitUntil;
        private Color _baseColor = Color.white;

        public static event Action<ArchivistBoss> AnyBossSpawned;

        public event Action<float, float> HealthChanged;
        public event Action<bool> PhaseChanged;
        public event Action<State> StateChanged;
        public event Action Died;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) : 0f;
        public bool IsAlive => !_dead;
        public bool IsPhaseTwo { get; private set; }
        public State CurrentState => _state;
        /// <summary>Ataque esperando na fila (State.Idle = nenhum).</summary>
        public State QueuedAttack => _queuedAttack;
        public string DisplayName => displayName;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();

            _body.freezeRotation = true;
            _body.gravityScale = gravityScale;
            _body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _baseColor = spriteRenderer.color;
            }

            _health = maxHealth;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }

            // Musica do chefe: o Arquivista so e' ativado quando o jogador
            // atravessa o portal.
            GameAudio.Instance?.PlayBossMusic();

            AnyBossSpawned?.Invoke(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Start()
        {
            if (player == null)
            {
                PlayerController controller = FindAnyObjectByType<PlayerController>();
                if (controller != null)
                {
                    player = controller.transform;
                    if (playerStats == null)
                    {
                        playerStats = controller.GetComponent<PlayerStats>();
                    }
                }
            }

            _laserTimer = laserInterval;
            _booksTimer = UnityEngine.Random.Range(booksMinInterval, booksMaxInterval);
            _puddleTimer = puddleInterval;

            // O boss atravessa as plataformas: ele sempre desce ate o chao da
            // arena, mesmo que a flutuacao termine em cima de uma delas.
            IgnorePlatformCollisions();

            HealthChanged?.Invoke(_health, maxHealth);
        }

        private void Update()
        {
            if (_dead || player == null)
            {
                return;
            }

            // Os cronometros correm SEMPRE, mesmo durante melee/livros; se so
            // corressem no Idle, o chefe que passa o tempo todo em melee nunca
            // chegava aos ataques especiais.
            TickTimers();

            switch (_state)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.Melee:
                    TickMelee();
                    break;
                case State.Books:
                    TickBooks();
                    break;
                case State.Laser:
                    TickLaser();
                    break;
                case State.PhaseChange:
                    TickPhaseChange();
                    break;
            }

            FacePlayer();
            UpdateVisual();
        }

        // ------------------------------------------------------------------ idle
        private void TickTimers()
        {
            _laserTimer -= Time.deltaTime;
            _booksTimer -= Time.deltaTime;

            if (IsPhaseTwo)
            {
                _puddleTimer -= Time.deltaTime;
            }
        }

        private void TickIdle()
        {
            if (!IsPhaseTwo && _health <= maxHealth * phaseTwoThreshold)
            {
                BeginPhaseTwo();
                return;
            }

            // Fila: o ataque mais leve espera o intervalo configurado depois do
            // termino do ataque anterior.
            if (_queuedAttack != State.Idle && Time.time >= _queuedAt)
            {
                State next = _queuedAttack;
                _queuedAttack = State.Idle;
                _queuedAt = -1f;

                if (next == State.Books)
                {
                    BeginBooks();
                    return;
                }
            }

            bool laserDue = _laserTimer <= 0f;
            bool booksDue = _booksTimer <= 0f && _queuedAttack == State.Idle;

            // Prioridade: laser (mais pesado) > livros > melee. Se os dois
            // especiais estao prontos, os livros entram na fila.
            if (laserDue)
            {
                if (_booksTimer <= 0f)
                {
                    _queuedAttack = State.Books;
                }

                BeginLaser();
                return;
            }

            if (booksDue)
            {
                BeginBooks();
                return;
            }

            // Melee so quando nenhum ataque especial esta pronto.
            if (CanMelee())
            {
                BeginMelee();
                return;
            }

            if (IsPhaseTwo && _puddleTimer <= 0f)
            {
                SpawnPuddles();
                _puddleTimer = puddleInterval;
            }

            // Sem golpe em andamento: caminha na direcao do jogador.
            WalkTowardsPlayer();
        }

        /// <summary>Chamado quando um ataque termina; reata o atraso da fila.</summary>
        private void NotifyAttackFinished()
        {
            if (_queuedAttack != State.Idle)
            {
                _queuedAt = Time.time + attackQueueDelay;
            }
        }

        private void WalkTowardsPlayer()
        {
            if (player == null || _body == null)
            {
                return;
            }

            float delta = player.position.x - transform.position.x;

            if (Mathf.Abs(delta) <= meleeRange * 0.9f)
            {
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                return;
            }

            float direction = Mathf.Sign(delta);
            float nextX = transform.position.x + direction * walkSpeed * Time.deltaTime;

            if (nextX <= arenaMinX + 1.2f || nextX >= arenaMaxX - 1.2f)
            {
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                return;
            }

            _body.linearVelocity = new Vector2(direction * walkSpeed, _body.linearVelocity.y);
        }

        private void UpdateVisual()
        {
            if (visual == null)
            {
                return;
            }

            if (_dead)
            {
                visual.Play(BossVisualAnimator.Clip.Death);
                return;
            }

            if (Time.time < _hitUntil)
            {
                visual.Play(BossVisualAnimator.Clip.Hit, true);
                return;
            }

            switch (_state)
            {
                case State.Melee:
                    visual.Play(BossVisualAnimator.Clip.Attack2);
                    break;

                case State.Laser:
                    visual.Play(BossVisualAnimator.Clip.Attack1);
                    break;

                case State.Books:
                    // Subindo: Jump; flutuando: Idle (parado no ar); descendo: Fall.
                    BossVisualAnimator.Clip booksClip = _booksPhase == BooksPhase.Jump
                        ? BossVisualAnimator.Clip.Jump
                        : _booksPhase == BooksPhase.Descend
                            ? BossVisualAnimator.Clip.Fall
                            : BossVisualAnimator.Clip.Idle;
                    visual.Play(booksClip);
                    break;

                case State.PhaseChange:
                    visual.Play(BossVisualAnimator.Clip.Idle);
                    break;

                default:
                    bool moving = Mathf.Abs(_body.linearVelocity.x) > 0.15f;
                    visual.Play(moving ? BossVisualAnimator.Clip.Run : BossVisualAnimator.Clip.Idle);
                    break;
            }
        }

        private bool CanMelee()
        {
            if (player == null || !IsGrounded() || Time.time < _nextMeleeTime)
            {
                return false;
            }

            return Mathf.Abs(player.position.x - transform.position.x) <= meleeRange
                && Mathf.Abs(player.position.y - transform.position.y) <= 3.5f;
        }

        private void FacePlayer()
        {
            if (spriteRenderer == null || player == null)
            {
                return;
            }

            float delta = player.position.x - transform.position.x;
            if (Mathf.Abs(delta) > 0.05f)
            {
                spriteRenderer.flipX = delta < 0f;
            }
        }

        // ----------------------------------------------------------------- melee
        private void BeginMelee()
        {
            SetState(State.Melee);
            GameAudio.Instance?.BossStaff();
            _meleePhaseStep = 0;
            _meleePhaseTimer = meleeWindup;
            StartCoroutine(MeleeTelegraphRoutine());
        }

        private IEnumerator MeleeTelegraphRoutine()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            Color original = spriteRenderer.color;
            spriteRenderer.color = new Color(1f, 0.6f, 0.9f, 1f);
            yield return new WaitForSeconds(Mathf.Max(0.05f, meleeWindup * 0.7f));
            spriteRenderer.color = original;
        }

        private void TickMelee()
        {
            _meleePhaseTimer -= Time.deltaTime;
            if (_meleePhaseTimer > 0f)
            {
                return;
            }

            switch (_meleePhaseStep)
            {
                case 0:
                    DealMeleeDamage();
                    _meleePhaseStep = 1;
                    _meleePhaseTimer = meleeActive;
                    break;

                case 1:
                    _meleePhaseStep = 2;
                    _meleePhaseTimer = meleeRecover;
                    break;

                default:
                    _nextMeleeTime = Time.time + meleeCooldown;
                    SetState(State.Idle);
                    break;
            }
        }

        private void DealMeleeDamage()
        {
            if (playerStats == null && player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }

            if (playerStats == null)
            {
                return;
            }

            Vector2 delta = (Vector2)playerStats.transform.position - (Vector2)transform.position;

            if (Mathf.Abs(delta.x) <= meleeRange + 0.8f && Mathf.Abs(delta.y) <= 3.2f)
            {
                playerStats.TakeDamage(meleeDamage, transform.position);
            }
        }

        private float GetFacing()
        {
            if (spriteRenderer != null)
            {
                return spriteRenderer.flipX ? -1f : 1f;
            }

            return player != null && player.position.x < transform.position.x ? -1f : 1f;
        }

        // ----------------------------------------------------------------- laser
        private void BeginLaser()
        {
            SetState(State.Laser);
            GameAudio.Instance?.BossStaff();
            _laserTimer = laserInterval;
            _laserPhaseStep = 0;
            _laserPhaseTimer = laserCharge;
            StartCoroutine(LaserChargeRoutine());
        }

        private IEnumerator LaserChargeRoutine()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            Color original = spriteRenderer.color;
            float t = 0f;
            float duration = Mathf.Max(0.05f, laserCharge);

            while (t < duration)
            {
                t += Time.deltaTime;
                spriteRenderer.color = Color.Lerp(original, new Color(0.85f, 0.4f, 1f, 1f), Mathf.PingPong(t * 6f, 1f));
                yield return null;
            }

            spriteRenderer.color = original;
        }

        private void TickLaser()
        {
            _laserPhaseTimer -= Time.deltaTime;
            if (_laserPhaseTimer > 0f)
            {
                return;
            }

            switch (_laserPhaseStep)
            {
                case 0:
                    SpawnLaser();
                    _laserPhaseStep = 1;
                    _laserPhaseTimer = laserActive;
                    break;

                case 1:
                    _laserPhaseStep = 2;
                    _laserPhaseTimer = laserRecover;
                    break;

                default:
                    SetState(State.Idle);
                    NotifyAttackFinished();
                    break;
            }
        }

        private void SpawnLaser()
        {
            if (laserPrefab == null)
            {
                return;
            }

            float width = Mathf.Max(4f, arenaMaxX - arenaMinX + 2f);
            Vector3 position = new Vector3((arenaMinX + arenaMaxX) * 0.5f, groundY + laserHeight, 0f);

            GameObject beam = Instantiate(laserPrefab, position, Quaternion.identity);
            BossLaser laser = beam.GetComponent<BossLaser>();
            if (laser != null)
            {
                laser.Configure(width, laserHeight * 0.55f, laserDamage, laserActive);
            }
        }

        // ----------------------------------------------------------------- livros
        private void BeginBooks()
        {
            SetState(State.Books);
            GameAudio.Instance?.BossBooks();
            _booksTimer = UnityEngine.Random.Range(booksMinInterval, booksMaxInterval);

            _booksPhase = BooksPhase.Jump;
            _booksSpawned = 0;
            _booksAlive = 0;
            _airTime = 0f;
            _spawnTimer = 0.25f;

            _body.gravityScale = gravityScale;
            _body.linearVelocity = new Vector2(0f, jumpVelocity);
        }

        private void TickBooks()
        {
            switch (_booksPhase)
            {
                case BooksPhase.Jump:
                    if (_body.linearVelocity.y <= 0.5f)
                    {
                        // Pausa a fisica e flutua no ar.
                        GameAudio.Instance?.BossFlight();
                        _booksPhase = BooksPhase.Float;
                        _body.gravityScale = 0f;
                        _body.linearVelocity = Vector2.zero;
                        _floatTargetY = transform.position.y + floatHeight;
                    }

                    break;

                case BooksPhase.Float:
                    if (transform.position.y < _floatTargetY)
                    {
                        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);
                    }

                    _airTime += Time.deltaTime;
                    _spawnTimer -= Time.deltaTime;

                    if (_booksSpawned < booksPerVolley && _spawnTimer <= 0f)
                    {
                        SpawnBook();
                        _spawnTimer = bookSpawnInterval;
                    }

                    bool allSpawned = _booksSpawned >= booksPerVolley;
                    bool cleared = _booksAlive <= 0;
                    if ((allSpawned && cleared && _airTime >= booksMinAirTime) || _airTime >= booksTimeout)
                    {
                        // So retorna ao chao quando os livros desapareceram.
                        _booksPhase = BooksPhase.Descend;
                        _body.gravityScale = gravityScale;
                    }

                    break;

                case BooksPhase.Descend:
                    _airTime += Time.deltaTime;

                    if ((IsGrounded() && _body.linearVelocity.y <= 0.1f) || _airTime >= booksTimeout + 3f)
                    {
                        SetState(State.Idle);
                        NotifyAttackFinished();
                    }

                    break;
            }
        }

        private void SpawnBook()
        {
            if (bookPrefab == null)
            {
                return;
            }

            Vector3 origin = staffTip != null ? staffTip.position : transform.position + Vector3.up;
            Vector2 toPlayer = player != null
                ? ((Vector2)player.position + Vector2.up * 0.9f - (Vector2)origin).normalized
                : Vector2.left;

            float spread = UnityEngine.Random.Range(-18f, 18f);
            Vector2 direction = Quaternion.Euler(0f, 0f, spread) * toPlayer;

            GameObject bookGo = Instantiate(bookPrefab, origin, Quaternion.identity);
            ArchivistBook book = bookGo.GetComponent<ArchivistBook>();
            if (book == null)
            {
                return;
            }

            book.Launch(direction, player, GetRandomBookSprite(), bookDamage);
            book.Destroyed += OnBookDestroyed;

            _booksSpawned++;
            _booksAlive++;
        }

        private Sprite GetRandomBookSprite()
        {
            if (bookSprites == null || bookSprites.Length == 0)
            {
                return null;
            }

            return bookSprites[UnityEngine.Random.Range(0, bookSprites.Length)];
        }

        private void OnBookDestroyed(ArchivistBook book)
        {
            if (book != null)
            {
                book.Destroyed -= OnBookDestroyed;
            }

            _booksAlive = Mathf.Max(0, _booksAlive - 1);
        }

        // ------------------------------------------------------------------ fase 2
        private void BeginPhaseTwo()
        {
            IsPhaseTwo = true;
            PhaseChanged?.Invoke(true);
            _puddleTimer = puddleInterval;
            _laserTimer = laserInterval;
            _booksTimer = UnityEngine.Random.Range(booksMinInterval, booksMaxInterval);

            SetState(State.PhaseChange);
            _phaseTimer = 1f;
            _invulnerable = true;
            StartCoroutine(PhaseFlashRoutine());
        }

        private IEnumerator PhaseFlashRoutine()
        {
            if (spriteRenderer == null)
            {
                yield break;
            }

            Color original = spriteRenderer.color;
            for (int i = 0; i < 6; i++)
            {
                spriteRenderer.color = new Color(0.85f, 0.3f, 1f, 1f);
                yield return new WaitForSeconds(0.07f);
                spriteRenderer.color = original;
                yield return new WaitForSeconds(0.07f);
            }
        }

        private void TickPhaseChange()
        {
            _phaseTimer -= Time.deltaTime;
            if (_phaseTimer <= 0f)
            {
                _invulnerable = false;
                SetState(State.Idle);
            }
        }

        /// <summary>
        /// Fase 2: o Arquivista arremessa projeteis de acido (Acid VFX 01) que,
        /// ao cair, formam as pocas no chao (Acid VFX 02).
        /// </summary>
        private void SpawnPuddles()
        {
            if (player == null)
            {
                return;
            }

            float centerX = Mathf.Clamp(player.position.x, arenaMinX + 1f, arenaMaxX - 1f);
            Vector3 origin = staffTip != null ? staffTip.position : transform.position + Vector3.up;

            for (int i = 0; i < puddlesPerVolley; i++)
            {
                float offset = (i - (puddlesPerVolley - 1) * 0.5f) * puddleSpread;
                float x = Mathf.Clamp(centerX + offset, arenaMinX + 0.8f, arenaMaxX - 0.8f);
                Vector2 target = new Vector2(x, groundY + 0.1f);

                if (acidPrefab != null)
                {
                    GameObject projectile = Instantiate(acidPrefab, origin, Quaternion.identity);
                    AcidProjectile acid = projectile.GetComponent<AcidProjectile>();
                    if (acid != null)
                    {
                        acid.LaunchTo(target, acidFlightTime + UnityEngine.Random.Range(-0.08f, 0.12f));
                    }
                }
                else if (puddlePrefab != null)
                {
                    // Compatibilidade: sem o prefab de acido, cria a poca direto.
                    Instantiate(puddlePrefab, new Vector3(x, groundY + 0.35f, 0f), Quaternion.identity);
                }
            }
        }

        // ------------------------------------------------------------------ dano
        public void TakeDamage(float amount, Vector2 sourcePosition)
        {
            if (_dead || _invulnerable || amount <= 0f)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            _hitUntil = Time.time + 0.18f;
            GameAudio.Instance?.BossHurt();
            HealthChanged?.Invoke(_health, maxHealth);

            StartCoroutine(HitFlashRoutine());

            if (_health <= 0f)
            {
                Die();
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            if (spriteRenderer == null || _flashOn)
            {
                yield break;
            }

            _flashOn = true;
            spriteRenderer.color = new Color(1.6f, 1.6f, 1.6f, 1f);
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = _baseColor;
            _flashOn = false;
        }

        private void Die()
        {
            _dead = true;
            GameAudio.Instance?.PlayMusic(null);
            SetState(State.Dead);

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_body != null)
            {
                _body.simulated = false;
            }

            Died?.Invoke();
            StartCoroutine(DeathFadeRoutine());
        }

        private IEnumerator DeathFadeRoutine()
        {
            if (spriteRenderer != null)
            {
                float t = 0f;
                Color color = spriteRenderer.color;
                while (t < 1.2f)
                {
                    t += Time.deltaTime;
                    color.a = Mathf.Clamp01(1f - t / 1.2f);
                    spriteRenderer.color = color;
                    yield return null;
                }
            }

            gameObject.SetActive(false);
        }

        private bool IsGrounded()
        {
            if (_collider == null)
            {
                return false;
            }

            // Tres raios (esquerda, centro, direita): o boss pode pousar apenas
            // com a borda em cima de uma plataforma e mesmo assim estar apoiado.
            Bounds bounds = _collider.bounds;
            float y = bounds.min.y + 0.05f;
            const float distance = 0.2f;

            return ProbeDown(new Vector2(bounds.min.x + 0.08f, y), distance)
                || ProbeDown(new Vector2(bounds.center.x, y), distance)
                || ProbeDown(new Vector2(bounds.max.x - 0.08f, y), distance);
        }

        private void IgnorePlatformCollisions()
        {
            if (_collider == null)
            {
                return;
            }

            PlatformEffector2D[] platforms = FindObjectsByType<PlatformEffector2D>();
            foreach (PlatformEffector2D platform in platforms)
            {
                Collider2D platformCollider = platform.GetComponent<Collider2D>();
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(_collider, platformCollider, true);
                }
            }
        }

        private bool ProbeDown(Vector2 origin, float distance)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, distance);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider != _collider && hit.collider.attachedRigidbody != _body)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetState(State next)
        {
            if (_state == next)
            {
                return;
            }

            _state = next;
            StateChanged?.Invoke(next);
        }

        /// <summary>Configuracao usada pelo construtor de cena.</summary>
        public void Configure(Transform target, PlayerStats stats, Transform staff, GameObject books,
            GameObject laser, GameObject puddle, SpriteRenderer renderer, BossVisualAnimator animator,
            Sprite[] booksPool)
        {
            player = target;
            playerStats = stats;
            staffTip = staff;
            bookPrefab = books;
            laserPrefab = laser;
            puddlePrefab = puddle;
            spriteRenderer = renderer;
            visual = animator;
            bookSprites = booksPool;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(new Vector3((arenaMinX + arenaMaxX) * 0.5f, groundY + laserHeight, 0f),
                new Vector3(arenaMaxX - arenaMinX, 0.6f, 0f));
        }
    }
}
