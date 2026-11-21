using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Chefe do velho oeste: Cowboy 3, maior que os outros. Tem rajada de tiros
    /// e um tiro carregado (animacao Charging) que causa o triplo do dano e vem
    /// com brilho. Ao morrer, materializa o portal para o mundo do Vazio.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CowboyBoss : MonoBehaviour, IDamageable
    {
        [Header("Vida")]
        [SerializeField, Min(1f)] private float maxHealth = 48f;

        [Header("Movimento")]
        [SerializeField, Min(0f)] private float walkSpeed = 1.4f;
        [SerializeField, Min(0.5f)] private float attackRange = 12f;

        [Header("Tiro normal")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField, Min(0f)] private float shotDamage = 2f;
        [SerializeField, Min(0.5f)] private float bulletSpeed = 14f;

        [Header("Rajada")]
        [SerializeField, Range(2, 8)] private int burstCount = 5;
        [SerializeField, Min(0.05f)] private float burstInterval = 0.16f;
        [SerializeField, Min(0.5f)] private float burstCooldown = 3.4f;

        [Header("Tiro carregado")]
        [SerializeField] private GameObject chargedBulletPrefab;
        [SerializeField, Min(0.2f)] private float chargeDuration = 1.25f;
        [SerializeField, Min(1f)] private float chargedMultiplier = 3f;
        [SerializeField, Min(0.5f)] private float chargedCooldown = 4.5f;

        [Header("Portal")]
        [SerializeField] private GameObject portalPrefab;

        [Header("Referencias")]
        [SerializeField] private CowboyVisualAnimator visual;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Transform firePoint;

        public static readonly List<CowboyBoss> Active = new List<CowboyBoss>();

        /// <summary>Disparado quando o chefe morre (a arena usa para liberar).</summary>
        public event System.Action<CowboyBoss> Died;

        private float _health;
        private bool _dead;
        private bool _busy;
        private float _nextBurst;
        private float _nextCharged;
        private Transform _player;
        private float _groundY;

        public bool IsAlive => !_dead;
        public float Health => _health;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (visual == null) visual = GetComponent<CowboyVisualAnimator>();

            _health = maxHealth;
            _groundY = transform.position.y;
            _nextBurst = Time.time + 1.5f;
            _nextCharged = Time.time + 5f;
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

            if (_busy)
            {
                return;
            }

            if (Time.time >= _nextBurst)
            {
                _nextBurst = Time.time + burstCooldown;
                StartCoroutine(BurstRoutine(facing));
                return;
            }

            if (Time.time >= _nextCharged)
            {
                _nextCharged = Time.time + chargedCooldown;
                StartCoroutine(ChargedRoutine(facing));
                return;
            }

            if (distance > attackRange * 0.6f)
            {
                if (body != null)
                {
                    body.linearVelocity = new Vector2(facing * walkSpeed, body.linearVelocity.y);
                }

                if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Walk);
            }
            else
            {
                if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Idle);
            }
        }

        private IEnumerator BurstRoutine(float facing)
        {
            _busy = true;
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

            for (int i = 0; i < burstCount; i++)
            {
                if (_dead) yield break;

                if (visual != null) visual.PlayTimed(CowboyVisualAnimator.Clip.Shot, 0.2f);
                yield return new WaitForSeconds(0.06f);
                Fire(bulletPrefab, facing, 1f, shotDamage, bulletSpeed);
                yield return new WaitForSeconds(burstInterval);
            }

            _busy = false;
            if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Idle);
        }

        private IEnumerator ChargedRoutine(float facing)
        {
            _busy = true;
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

            if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Charging);

            float t = 0f;
            while (t < chargeDuration)
            {
                if (_dead) yield break;
                t += Time.deltaTime;
                if (visual != null) visual.SetFacing(facing);
                yield return null;
            }

            if (visual != null) visual.PlayTimed(CowboyVisualAnimator.Clip.Shot, 0.3f);

            GameObject prefab = chargedBulletPrefab != null ? chargedBulletPrefab : bulletPrefab;
            Fire(prefab, facing, chargedMultiplier, shotDamage, bulletSpeed * 1.15f);

            yield return new WaitForSeconds(0.35f);

            _busy = false;
            if (visual != null) visual.PlayLocomotion(CowboyVisualAnimator.Clip.Idle);
        }

        private void Fire(GameObject prefab, float facing, float multiplier, float damage, float speed)
        {
            if (prefab == null || _player == null)
            {
                return;
            }

            Vector3 origin = firePoint != null ? firePoint.position : transform.position + new Vector3(0.5f * facing, 0.95f, 0f);

            Collider2D targetCollider = _player.GetComponent<Collider2D>();
            Vector2 aim = targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)_player.position;
            Vector2 direction = (aim - (Vector2)origin).normalized;

            if (multiplier > 1f)
            {
                GameAudio.Instance?.RifleShot();
            }
            else
            {
                GameAudio.Instance?.Shot();
            }

            GameObject go = Instantiate(prefab, origin, Quaternion.identity);
            CowboyBullet bullet = go.GetComponent<CowboyBullet>();
            if (bullet != null)
            {
                bullet.Configure(speed, damage, multiplier, _groundY);
                bullet.Launch(direction, null);
            }
        }

        public void TakeDamage(float amount, Vector2 sourcePosition)
        {
            if (_dead || amount <= 0f)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);

            if (_health <= 0f)
            {
                Die();
                return;
            }

            if (visual != null)
            {
                visual.PlayTimed(CowboyVisualAnimator.Clip.Hurt, 0.22f);
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
            StopAllCoroutines();
            _busy = false;

            if (bodyCollider != null) bodyCollider.enabled = false;
            if (body != null) body.linearVelocity = Vector2.zero;
            if (visual != null) visual.PlayTimed(CowboyVisualAnimator.Clip.Dead, 3f);

            StartCoroutine(OpenPortalRoutine());
        }

        private IEnumerator OpenPortalRoutine()
        {
            yield return new WaitForSeconds(1.6f);

            if (portalPrefab != null)
            {
                Vector3 position = new Vector3(transform.position.x, _groundY + 0.05f, 0f);
                Instantiate(portalPrefab, position, Quaternion.identity);
            }

            yield return new WaitForSeconds(2f);
            Destroy(gameObject);
        }

        public void Configure(float health, GameObject bullet, GameObject chargedBullet, GameObject portal,
            float floorY, CowboyVisualAnimator animator, Transform muzzle)
        {
            maxHealth = health;
            bulletPrefab = bullet;
            chargedBulletPrefab = chargedBullet;
            portalPrefab = portal;
            _groundY = floorY;
            visual = animator;
            firePoint = muzzle;
            _health = maxHealth;
        }
    }
}
