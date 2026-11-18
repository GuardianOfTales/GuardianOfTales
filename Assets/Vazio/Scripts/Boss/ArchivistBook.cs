using System;
using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Livro giratorio invocado pelo Arquivista. Voa na direcao do jogador,
    /// gira, tem collider e e destruido ao acertar o jogador, a magia do
    /// jogador, o chao ou as plataformas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArchivistBook : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float speed = 7.5f;
        [SerializeField, Min(0.1f)] private float lifetime = 6f;
        [SerializeField, Min(0f)] private float damage = 2f;
        [SerializeField, Min(0f)] private float spinSpeed = 540f;

        private Vector2 _direction = Vector2.left;
        private float _life;
        private float _damageValue;
        private bool _dead;
        private SpriteRenderer _renderer;

        public event Action<ArchivistBook> Destroyed;

        public bool IsAlive => !_dead;

        private void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _life = lifetime;
            _damageValue = damage;
        }

        public void Launch(Vector2 direction, Transform player, Sprite sprite, float damageOverride)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
            _life = lifetime;
            _damageValue = damageOverride > 0f ? damageOverride : damage;

            if (_renderer != null)
            {
                if (sprite != null)
                {
                    _renderer.sprite = sprite;
                }

                _renderer.color = Color.white;
            }

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Kill();
                return;
            }

            transform.position += (Vector3)(_direction * (speed * Time.deltaTime));
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

            // Destroi ao sair dos limites da arena para nao acumular objetos.
            if (transform.position.x < -20f || transform.position.x > 130f
                || transform.position.y < -10f || transform.position.y > 30f)
            {
                Kill();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_dead || other == null)
            {
                return;
            }

            if (other.GetComponentInParent<ArchivistBoss>() != null)
            {
                return;
            }

            if (other.GetComponent<MagicProjectile>() != null)
            {
                Kill();
                return;
            }

            PlayerStats player = other.GetComponentInParent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(_damageValue, transform.position);
                Kill();
                return;
            }

            if (other.GetComponent<ArchivistBook>() != null)
            {
                return;
            }

            // Chao, paredes e plataformas: colliders solidos (nao triggers).
            if (!other.isTrigger)
            {
                Kill();
            }
        }

        public void TakeDamage(float amount, Vector2 sourcePosition)
        {
            Kill();
        }

        private void Kill()
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            Destroyed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
