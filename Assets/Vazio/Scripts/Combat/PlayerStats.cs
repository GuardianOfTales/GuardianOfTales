using System;
using System.Collections;
using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Vida (5 coracoes = 10 metades), mana (100, recarrega em 15 s) e status do
    /// Bibliotecario: i-frames, knockback, lentidao por mana zerada e por pocas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Vida")]
        [SerializeField, Min(1f)] private float maxHealth = 10f;
        [SerializeField, Min(0f)] private float invulnerabilityAfterHit = 0.5f;
        [SerializeField, Min(0f)] private float knockbackForce = 7f;

        [Header("Mana")]
        [SerializeField, Min(1f)] private float maxMana = 100f;
        [SerializeField, Min(0.5f)] private float manaRegenSeconds = 15f;
        [SerializeField, Min(0f)] private float manaSlowMultiplier = 0.6f;

        [Header("Renascer")]
        [SerializeField] private Vector3 respawnPoint = new Vector3(84f, 1.2f, 0f);
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        [Header("Referencias")]
        [SerializeField] private PlayerController controller;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private PlayerVisualAnimator visualAnimator;

        private float _health;
        private float _mana;
        private float _invulnerableUntil;
        private float _slowUntil;
        private float _slowMultiplier = 1f;
        private bool _dead;

        public event Action<float, float> HealthChanged;
        public event Action<float, float> ManaChanged;
        public event Action Died;
        public event Action Respawned;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public float HealthNormalized => maxHealth > 0f ? _health / maxHealth : 0f;
        public float Mana => _mana;
        public float MaxMana => maxMana;
        public float ManaNormalized => maxMana > 0f ? _mana / maxMana : 0f;
        public bool IsAlive => !_dead;
        public bool IsInvincible => _dead || Time.time < _invulnerableUntil || (controller != null && controller.IsInvincible);
        public bool IsManaEmpty => _mana <= 0.01f;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }

            _health = maxHealth;
            _mana = maxMana;
        }

        private void Start()
        {
            RaiseHealth();
            RaiseMana();
        }

        private void Update()
        {
            if (_dead)
            {
                return;
            }

            RegenerateMana();
            UpdateSpeedMultiplier();
        }

        private void RegenerateMana()
        {
            if (_mana >= maxMana)
            {
                return;
            }

            float rate = maxMana / Mathf.Max(0.5f, manaRegenSeconds);
            _mana = Mathf.Min(maxMana, _mana + rate * Time.deltaTime);
            RaiseMana();
        }

        private void UpdateSpeedMultiplier()
        {
            if (controller == null)
            {
                return;
            }

            float multiplier = 1f;

            // Poca corrompida (lentidao temporaria).
            if (Time.time < _slowUntil)
            {
                multiplier = Mathf.Min(multiplier, _slowMultiplier);
            }

            // Punicacao do GDD: mana zerada deixa o jogador lento ate 10%.
            if (_mana < maxMana * 0.1f)
            {
                multiplier = Mathf.Min(multiplier, manaSlowMultiplier);
            }

            controller.SetSpeedMultiplier(multiplier);
        }

        /// <summary>Gasta mana; retorna false se nao houver saldo.</summary>
        public bool TrySpendMana(float amount)
        {
            if (_dead || amount <= 0f)
            {
                return false;
            }

            if (_mana + 0.001f < amount)
            {
                return false;
            }

            _mana -= amount;
            RaiseMana();
            return true;
        }

        public void RestoreMana(float amount)
        {
            _mana = Mathf.Clamp(_mana + amount, 0f, maxMana);
            RaiseMana();
        }

        public void Heal(float amount)
        {
            if (_dead)
            {
                return;
            }

            _health = Mathf.Clamp(_health + amount, 0f, maxHealth);
            RaiseHealth();
        }

        /// <summary>Aplica lentidao (multiplicador entre 0 e 1) por um tempo.</summary>
        public void ApplySlow(float multiplier, float duration)
        {
            if (_dead)
            {
                return;
            }

            if (Time.time >= _slowUntil || multiplier < _slowMultiplier)
            {
                _slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
            }

            _slowUntil = Mathf.Max(_slowUntil, Time.time + Mathf.Max(0.1f, duration));
        }

        public void TakeDamage(float amount, Vector2 sourcePosition)
        {
            if (_dead || amount <= 0f || IsInvincible)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            _invulnerableUntil = Time.time + invulnerabilityAfterHit;
            RaiseHealth();

            if (visualAnimator != null)
            {
                visualAnimator.PlayTimed(PlayerVisualAnimator.Clip.Hurt, 0.25f);
            }

            if (controller != null)
            {
                Vector2 direction = ((Vector2)transform.position - sourcePosition).normalized;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Vector2.up;
                }

                controller.ApplyKnockback(direction * knockbackForce);
            }

            StartCoroutine(FlashRoutine());

            if (_health <= 0f)
            {
                Die();
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (visual == null)
            {
                yield break;
            }

            Color original = visual.color;
            for (int i = 0; i < 3; i++)
            {
                visual.color = new Color(1f, 0.35f, 0.35f, 1f);
                yield return new WaitForSeconds(0.08f);
                visual.color = original;
                yield return new WaitForSeconds(0.08f);
            }
        }

        private void Die()
        {
            _dead = true;
            Died?.Invoke();

            if (visualAnimator != null)
            {
                visualAnimator.PlayTimed(PlayerVisualAnimator.Clip.Dead, Mathf.Max(0.3f, respawnDelay - 0.15f));
            }

            if (controller != null)
            {
                controller.enabled = false;
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            _dead = false;
            _health = maxHealth;
            _mana = maxMana;
            _invulnerableUntil = Time.time + 1.5f;
            transform.position = respawnPoint;

            if (controller != null)
            {
                controller.enabled = true;
                controller.TeleportTo(respawnPoint);
            }

            RaiseHealth();
            RaiseMana();
            Respawned?.Invoke();
        }

        private void RaiseHealth()
        {
            HealthChanged?.Invoke(_health, maxHealth);
        }

        private void RaiseMana()
        {
            ManaChanged?.Invoke(_mana, maxMana);
        }

        public void Configure(PlayerController playerController, SpriteRenderer sprintRenderer, Vector3 respawn,
            PlayerVisualAnimator animator)
        {
            controller = playerController;
            visual = sprintRenderer;
            respawnPoint = respawn;
            visualAnimator = animator;
        }
    }
}
