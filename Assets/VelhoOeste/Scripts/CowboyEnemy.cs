using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Cowboy inimigo (variantes 1 e 2). Persegue o jogador, atira e morre com
    /// a animacao Dead. Compartilha o CowboyVisualAnimator com o chefe.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CowboyEnemy : MonoBehaviour, IDamageable
    {
        [Header("Vida")]
        [SerializeField, Min(1f)] private float maxHealth = 6f;

        [Header("Movimento")]
        [SerializeField, Min(0f)] private float walkSpeed = 1.7f;
        [SerializeField, Min(0f)] private float runSpeed = 3.1f;
        [SerializeField, Min(0.5f)] private float shootRange = 9f;
        [SerializeField, Min(0.5f)] private float keepDistance = 4.5f;

        [Header("Tiro")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField, Min(0f)] private float shotDamage = 1f;
        [SerializeField, Min(0.1f)] private float shotCooldown = 1.7f;
        [SerializeField, Min(0.5f)] private float bulletSpeed = 13f;
        [SerializeField] private Transform firePoint;

        [Header("Referencias")]
        [SerializeField] private CowboyVisualAnimator visual;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private float _health;
        private float _nextShotTime;
        private bool _dead;
        private Transform _player;
        private float _groundY;

        public static readonly List<CowboyEnemy> Active = new List<CowboyEnemy>();

        /// <summary>Disparado quando este cowboy morre (a arena usa para contar).</summary>
        public event System.Action<CowboyEnemy> Died;

        public bool IsAlive => !_dead;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (visual == null) visual = GetComponent<CowboyVisualAnimator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _health = maxHealth;
            _groundY = transform.position.y;
        }

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        private void Start()
        {
            PlayerStats stats = FindAnyObjectByType<PlayerStats>();
            _player = stats != null ? stats.transform : null;
        }

        private void Update()
        {
            if (_dead || _player == null)
            {
                return;
            }

            float delta = _player.position.x - transform.position.x;
            float distance = Mathf.Abs(delta);
            float facing = Mathf.Sign(delta == 0f ? 1f : delta);

            if (visual != null)
            {
                visual.SetFacing(facing);
            }

            if (distance > shootRange)
            {
                Move(facing, runSpeed);
                if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Run);
            }
            else if (distance > keepDistance)
            {
                Move(facing, walkSpeed);
                if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Walk);
            }
            else
            {
                Move(0f, 0f);
                if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Idle);

                if (Time.time >= _nextShotTime)
                {
                    _nextShotTime = Time.time + shotCooldown;
                    StartCoroutine(ShootRoutine(facing));
                }
            }
        }

        private void Move(float direction, float speed)
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
            if (Mathf.Abs(direction) < 0.01f && Mathf.Abs(body.linearVelocity.y) < 0.05f)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private IEnumerator ShootRoutine(float facing)
        {
            if (visual != null)
            {
                visual.PlayTimed(CowboyVisualAnimator.Clip.Shot, 0.28f);
            }

            yield return new WaitForSeconds(0.12f);

            if (_dead || bulletPrefab == null || _player == null)
            {
                yield break;
            }

            GameAudio.Instance?.Shot();

            Vector3 origin = firePoint != null ? firePoint.position : transform.position + new Vector3(0.5f * facing, 0.95f, 0f);

            // Mira no centro do corpo do jogador NA HORA do disparo; a bala
            // segue reta depois (nao e' teleguiada).
            Collider2D targetCollider = _player.GetComponent<Collider2D>();
            Vector2 aim = targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)_player.position;
            Vector2 direction = (aim - (Vector2)origin).normalized;

            GameObject go = Instantiate(bulletPrefab, origin, Quaternion.identity);
            CowboyBullet bullet = go.GetComponent<CowboyBullet>();
            if (bullet != null)
            {
                bullet.Configure(bulletSpeed, shotDamage, 1f, _groundY);
                bullet.Launch(direction, null);
            }
        }

        public void TakeDamage(float amount, Vector2 sourcePosition)
        {
            if (_dead || amount <= 0f)
            {
                return;
            }

            _health -= amount;

            if (body != null)
            {
                Vector2 push = ((Vector2)transform.position - sourcePosition).normalized;
                body.linearVelocity = new Vector2(push.x * 3f, 2f);
            }

            if (_health <= 0f)
            {
                Die();
                return;
            }

            if (visual != null)
            {
                visual.PlayTimed(CowboyVisualAnimator.Clip.Hurt, 0.25f);
            }
        }

        private void Die()
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            Active.Remove(this);
            Died?.Invoke(this);

            if (bodyCollider != null) bodyCollider.enabled = false;
            if (body != null) body.linearVelocity = Vector2.zero;
            if (visual != null) visual.PlayTimed(CowboyVisualAnimator.Clip.Dead, 2.5f);

            Destroy(gameObject, 2.5f);
        }

        public void Configure(float health, GameObject bullet, float damage, float floorY, CowboyVisualAnimator animator,
            Transform muzzle, SpriteRenderer renderer)
        {
            maxHealth = health;
            bulletPrefab = bullet;
            shotDamage = damage;
            _groundY = floorY;
            visual = animator;
            firePoint = muzzle;
            spriteRenderer = renderer;
            _health = maxHealth;
        }
    }
}
