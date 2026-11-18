using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vazio
{
    /// <summary>
    /// Controlador do Bibliotecario para o mundo Vazio (plataforma 2D).
    /// Andar (A/D), pulo de altura fixa (Espaco), dash invencivel (Shift)
    /// e agachar mantendo S (hitbox reduzida pela metade).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movimento lateral (A / D)")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float groundAcceleration = 80f;
        [SerializeField, Min(0f)] private float airAcceleration = 45f;
        [SerializeField, Min(0f)] private float groundDeceleration = 95f;
        [SerializeField, Min(0f)] private float airDeceleration = 22f;

        [Header("Pulo de altura fixa (Espaco)")]
        [SerializeField, Min(0.1f)] private float jumpHeight = 5.1f;
        [SerializeField, Min(1f)] private float maxFallSpeed = 28f;
        [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.1f;
        [SerializeField, Range(0f, 0.3f)] private float jumpBufferTime = 0.12f;

        [Header("Dash / Teleporte (Shift)")]
        [SerializeField, Min(0.5f)] private float dashDistance = 5f;
        [SerializeField, Min(0.03f)] private float dashDuration = 0.17f;
        [SerializeField, Min(0f)] private float dashManaCost = 20f;
        [SerializeField, Min(0f)] private float dashInvincibilityGrace = 0.07f;

        [Header("Agachar (segurar S)")]
        [SerializeField, Range(0.2f, 0.9f)] private float crouchHeightMultiplier = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float crouchSpeedMultiplier = 0.45f;

        [Header("Deteccao de cenario")]
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Apresentacao")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;

        /// <summary>Invocado quando o jogador comeca/termina o dash.</summary>
        public event Action<bool> DashStateChanged;
        /// <summary>Invocado quando o jogador comeca/termina de agachar.</summary>
        public event Action<bool> CrouchStateChanged;
        /// <summary>Invocado a cada pulo efetivado.</summary>
        public event Action Jumped;

        private Rigidbody2D _body;
        private BoxCollider2D _collider;

        private Vector2 _standSize;
        private Vector2 _standOffset;
        private float _standVisualScaleY;
        private Vector3 _standVisualLocalPosition;

        private float _moveInput;
        private float _facing = 1f;
        private bool _isCrouching;
        private bool _isDashing;

        private float _dashTimer;
        private float _dashDirection = 1f;
        private float _invincibleUntil;

        private PlayerStats _stats;
        private readonly List<Collider2D> _dashIgnored = new List<Collider2D>();

        private float _lastGroundedTime = -99f;
        private float _jumpRequestedTime = -99f;
        private bool _wasGrounded;

        public bool IsGrounded { get; private set; }
        public bool IsCrouching => _isCrouching;
        public bool IsDashing => _isDashing;
        /// <summary>Verdadeiro enquanto o dash (ou a carencia pos-dash) protege o jogador.</summary>
        public bool IsInvincible => _isDashing || Time.time < _invincibleUntil;
        public float Facing => _facing;

        /// <summary>Custo de mana do dash (0 = gratis).</summary>
        public float DashManaCost => dashManaCost;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();

            _body.freezeRotation = true;
            _body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // O corpo nunca pode dormir: o controlador escreve a velocidade
            // todo FixedUpdate e um Rigidbody2D adormecido ignora essas
            // escritas (o personagem ficaria parado).
            _body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            _standSize = _collider.size;
            _standOffset = _collider.offset;
            _stats = GetComponent<PlayerStats>();

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (visualRoot != null)
            {
                _standVisualScaleY = visualRoot.localScale.y;
                _standVisualLocalPosition = visualRoot.localPosition;
            }
        }

        private void Update()
        {
            ReadInput();

            // Agachar e' estritamente "segurar": a leitura acontece todo frame e
            // ao soltar a tecla o personagem levanta no mesmo frame, restaurando
            // o collider original (nao existe toggle nem atraso).
            UpdateCrouch();
            UpdateFacing();
            TryJump();
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (visualAnimator == null)
            {
                return;
            }

            if (!IsGrounded)
            {
                visualAnimator.PlayLocomotion(PlayerVisualAnimator.Clip.Jump);
                return;
            }

            float speed = Mathf.Abs(_body.linearVelocity.x);
            if (speed > 0.2f)
            {
                visualAnimator.PlayLocomotion(_isCrouching
                    ? PlayerVisualAnimator.Clip.Walk
                    : PlayerVisualAnimator.Clip.Run);
            }
            else
            {
                visualAnimator.PlayLocomotion(PlayerVisualAnimator.Clip.Idle);
            }
        }

        private void FixedUpdate()
        {
            // Garantia extra: se a tecla foi solta, o collider volta ao normal
            // mesmo que o Update tenha sido pulado em algum frame.
            if (!CrouchHeld && _isCrouching && CanStandUp())
            {
                SetCrouching(false);
            }

            UpdateGrounded();

            if (_isDashing)
            {
                RunDash();
                return;
            }

            ApplyHorizontalMovement();
            ClampFallSpeed();
            ConsumeCoyoteJump();
        }

        // ---------------------------------------------------------------- input
        private void ReadInput()
        {
            float horizontal = 0f;
            bool jumpPressed = false;
            bool jumpHeld = false;
            bool dashPressed = false;
            bool crouchHeld = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
                jumpPressed = kb.spaceKey.wasPressedThisFrame;
                jumpHeld = kb.spaceKey.isPressed;
                dashPressed = kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
                crouchHeld = kb.sKey.isPressed || kb.downArrowKey.isPressed;
            }
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
            jumpHeld = Input.GetKey(KeyCode.Space);
            dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            crouchHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#endif
            // keep the compilers quiet about unused flags in some configurations
            _ = jumpHeld;

            _moveInput = Mathf.Clamp(horizontal, -1f, 1f);

            if (jumpPressed)
            {
                _jumpRequestedTime = Time.time;
            }

            if (dashPressed)
            {
                TryStartDash();
            }

            CrouchHeld = crouchHeld;
        }

        /// <summary>Verdadeiro apenas enquanto S (ou seta para baixo) esta pressionado.</summary>
        public bool CrouchHeld { get; private set; }

        // --------------------------------------------------------------- pulo
        private void TryJump()
        {
            if (Time.time - _jumpRequestedTime > jumpBufferTime)
            {
                return;
            }

            bool canUseCoyote = Time.time - _lastGroundedTime <= coyoteTime;
            if (!IsGrounded && !canUseCoyote)
            {
                return;
            }

            if (_isCrouching && !CanStandUp())
            {
                return;
            }

            if (_isCrouching)
            {
                SetCrouching(false);
            }

            float gravity = Mathf.Abs(Physics2D.gravity.y * _body.gravityScale);
            if (gravity <= 0.01f)
            {
                gravity = 9.81f;
            }

            // Altura fixa: a velocidade inicial vem apenas da altura desejada,
            // nunca do tempo em que o botao permanece pressionado.
            float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            _body.WakeUp();
            _body.linearVelocity = new Vector2(_body.linearVelocity.x, jumpVelocity);

            _jumpRequestedTime = -99f;
            _lastGroundedTime = -99f;
            IsGrounded = false;
            Jumped?.Invoke();
        }

        private void ConsumeCoyoteJump()
        {
            // O pulo ja e aplicado em TryJump(); nada a fazer aqui alem de
            // manter a janela de coyote coerente com o estado fisico atual.
            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
            }
        }

        private void UpdateGrounded()
        {
            bool grounded = ProbeGround();

            if (grounded && !_wasGrounded)
            {
                _lastGroundedTime = Time.time;
            }

            _wasGrounded = grounded;
            IsGrounded = grounded;

            if (grounded)
            {
                _lastGroundedTime = Time.time;
            }
        }

        private bool ProbeGround()
        {
            Bounds bounds = _collider.bounds;
            float y = bounds.min.y + 0.02f;
            const float distance = 0.12f;

            return ProbeDown(new Vector2(bounds.min.x + 0.06f, y), distance)
                || ProbeDown(new Vector2(bounds.center.x, y), distance)
                || ProbeDown(new Vector2(bounds.max.x - 0.06f, y), distance);
        }

        private bool ProbeDown(Vector2 origin, float distance)
        {
            // RaycastAll para ignorar o proprio colisor do jogador, cuja base
            // fica acima da origem do raio.
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, distance, groundLayers);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider != _collider &&
                    hit.collider.attachedRigidbody != _body)
                {
                    return true;
                }
            }

            return false;
        }

        // -------------------------------------------------------------- movimento
        private void ApplyHorizontalMovement()
        {
            Vector2 velocity = _body.linearVelocity;
            float baseSpeed = _isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
            float speed = baseSpeed * _speedMultiplier;
            float targetX = _moveInput * speed;

            float rate;
            if (Mathf.Abs(targetX) > 0.01f)
            {
                rate = IsGrounded ? groundAcceleration : airAcceleration;
            }
            else
            {
                rate = IsGrounded ? groundDeceleration : airDeceleration;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetX, rate * Time.fixedDeltaTime);

            if (Mathf.Abs(targetX) > 0.01f || Mathf.Abs(velocity.x) > 0.01f)
            {
                _body.WakeUp();
            }

            _body.linearVelocity = velocity;
        }

        private void ClampFallSpeed()
        {
            Vector2 velocity = _body.linearVelocity;
            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
                _body.linearVelocity = velocity;
            }
        }

        private void UpdateFacing()
        {
            if (_moveInput > 0.01f)
            {
                _facing = 1f;
            }
            else if (_moveInput < -0.01f)
            {
                _facing = -1f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = _facing < 0f;
            }
        }

        // ---------------------------------------------------------------- dash
        private void TryStartDash()
        {
            if (_isDashing)
            {
                return;
            }

            // O dash custa mana e nao tem recarga: a mana e' o unico limite.
            if (dashManaCost > 0f)
            {
                if (_stats == null)
                {
                    _stats = GetComponent<PlayerStats>();
                }

                if (_stats != null && !_stats.TrySpendMana(dashManaCost))
                {
                    return;
                }
            }

            _isDashing = true;
            _dashTimer = dashDuration;
            _dashDirection = _facing;
            _invincibleUntil = Time.time + dashDuration + dashInvincibilityGrace;

            IgnoreEnemiesDuringDash(true);

            _body.WakeUp();
            _body.gravityScale = 0f;
            _body.linearVelocity = new Vector2(_dashDirection * (dashDistance / dashDuration), 0f);

            DashStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Durante o dash o jogador atravessa corpos e disparos inimigos: as
        /// colisoes com eles sao ignoradas ate o fim do dash.
        /// </summary>
        private void IgnoreEnemiesDuringDash(bool ignore)
        {
            if (_collider == null)
            {
                return;
            }

            if (!ignore)
            {
                foreach (Collider2D other in _dashIgnored)
                {
                    if (other != null)
                    {
                        Physics2D.IgnoreCollision(_collider, other, false);
                    }
                }

                _dashIgnored.Clear();
                return;
            }

            _dashIgnored.Clear();

            foreach (ArchivistBoss boss in ArchivistBoss.Active)
            {
                if (boss == null)
                {
                    continue;
                }

                foreach (Collider2D other in boss.GetComponentsInChildren<Collider2D>())
                {
                    IgnoreOne(other);
                }
            }

            foreach (ArchivistBook book in FindObjectsByType<ArchivistBook>())
            {
                IgnoreOne(book != null ? book.GetComponent<Collider2D>() : null);
            }

            foreach (BossLaser laser in FindObjectsByType<BossLaser>())
            {
                IgnoreOne(laser != null ? laser.GetComponent<Collider2D>() : null);
            }

            void IgnoreOne(Collider2D other)
            {
                if (other == null || other == _collider || other.attachedRigidbody == _body)
                {
                    return;
                }

                Physics2D.IgnoreCollision(_collider, other, true);
                _dashIgnored.Add(other);
            }
        }

        private void RunDash()
        {
            _dashTimer -= Time.fixedDeltaTime;
            _body.linearVelocity = new Vector2(_dashDirection * (dashDistance / dashDuration), 0f);

            if (_dashTimer > 0f)
            {
                return;
            }

            EndDash();
        }

        private void EndDash()
        {
            _isDashing = false;
            IgnoreEnemiesDuringDash(false);
            _body.gravityScale = _standGravityScale > 0f ? _standGravityScale : 3f;

            // Sai do dash com a velocidade de caminhada, sem "estilingue".
            _body.linearVelocity = new Vector2(_dashDirection * moveSpeed, _body.linearVelocity.y);
            DashStateChanged?.Invoke(false);
        }

        private float _standGravityScale = 3f;
        private float _speedMultiplier = 1f;
        [SerializeField] private PlayerVisualAnimator visualAnimator;

        // -------------------------------------------------------------- agachar
        private void UpdateCrouch()
        {
            if (CrouchHeld)
            {
                if (!_isCrouching)
                {
                    SetCrouching(true);
                }

                return;
            }

            // Tecla solta: levanta imediatamente e devolve o collider ao tamanho
            // original. So nao levanta se houver um teto logo acima.
            if (_isCrouching && CanStandUp())
            {
                SetCrouching(false);
            }
        }

        private void SetCrouching(bool crouch)
        {
            if (_isCrouching == crouch)
            {
                return;
            }

            _isCrouching = crouch;

            // Agachado: hitbox pela metade. Levantando: volta exatamente ao
            // tamanho original capturado no Awake. Em ambos os casos a base do
            // colisor permanece apoiada no chao.
            float height = crouch ? _standSize.y * crouchHeightMultiplier : _standSize.y;
            _collider.size = new Vector2(_standSize.x, height);
            _collider.offset = new Vector2(_standOffset.x, _standOffset.y - (_standSize.y - height) * 0.5f);

            if (visualRoot != null)
            {
                Vector3 scale = visualRoot.localScale;
                scale.y = crouch ? _standVisualScaleY * crouchHeightMultiplier : _standVisualScaleY;
                visualRoot.localScale = scale;

                Vector3 localPosition = _standVisualLocalPosition;
                localPosition.y -= (_standSize.y - height) * 0.5f;
                visualRoot.localPosition = localPosition;
            }

            CrouchStateChanged?.Invoke(crouch);
        }

        private bool CanStandUp()
        {
            float standHeight = _standSize.y;
            Vector2 center = (Vector2)transform.position + _standOffset + Vector2.up * (standHeight * 0.5f);
            Vector2 size = new Vector2(_standSize.x * 0.9f, standHeight * 0.9f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, groundLayers);
            foreach (Collider2D hit in hits)
            {
                if (hit != null && hit != _collider && hit.attachedRigidbody != _body && !hit.isTrigger)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Multiplicador externo de velocidade (lentidao de mana/poca).</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
        }

        /// <summary>Empurrao ao levar dano.</summary>
        public void ApplyKnockback(Vector2 impulse)
        {
            if (_body == null)
            {
                return;
            }

            _body.WakeUp();
            _body.linearVelocity = new Vector2(impulse.x, Mathf.Max(impulse.y, _body.linearVelocity.y));
        }

        /// <summary>Teleporte instantaneo (usado no renascer e no portal).</summary>
        public void TeleportTo(Vector3 position)
        {
            transform.position = position;

            if (_body != null)
            {
                _body.position = position;
                _body.linearVelocity = Vector2.zero;
                _body.WakeUp();
            }
        }

        /// <summary>Liga o animador de sprites do jogador. Chamado pelo setup da cena.</summary>
        public void ConfigureVisual(PlayerVisualAnimator animator)
        {
            visualAnimator = animator;
        }

        /// <summary>Liga o visual (sprite) ao controlador. Chamado pelo setup da cena.</summary>
        public void ConfigurePresentation(Transform root, SpriteRenderer renderer)
        {
            visualRoot = root;
            spriteRenderer = renderer;

            if (visualRoot != null)
            {
                _standVisualScaleY = visualRoot.localScale.y;
                _standVisualLocalPosition = visualRoot.localPosition;
            }
        }

        /// <summary>
        /// Define explicitamente a escala de gravidade usada ao terminar o dash.
        /// Chamado pelo setup da cena.
        /// </summary>
        public void ConfigureGravity(float gravityScale)
        {
            _standGravityScale = gravityScale;
            if (!_isDashing && _body != null)
            {
                _body.gravityScale = gravityScale;
            }
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Bounds bounds = box.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
