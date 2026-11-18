using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Magia azul do jogador: voa em linha reta (com leve correcao de rota em
    /// direcao ao alvo) e e destruida ao acertar inimigos, cenario ou ao
    /// alcancar a distancia maxima.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MagicProjectile : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float speed = 17f;
        [SerializeField, Min(1f)] private float maxDistance = 30f;
        [SerializeField, Min(0f)] private float damage = 6f;
        [SerializeField, Min(0f)] private float homing = 4f;

        private Vector2 _direction = Vector2.right;
        private Vector2 _origin;
        private Transform _target;

        public void Launch(Vector2 direction, Transform target)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _target = target;
            _origin = transform.position;
            ApplyRotation();
        }

        private void Update()
        {
            if (_target != null)
            {
                Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.04f)
                {
                    _direction = Vector2.Lerp(_direction, toTarget.normalized, homing * Time.deltaTime).normalized;
                }
            }

            transform.position += (Vector3)(_direction * (speed * Time.deltaTime));
            ApplyRotation();

            if (Vector2.Distance(_origin, transform.position) >= maxDistance)
            {
                Destroy(gameObject);
            }
        }

        private void ApplyRotation()
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            // Nao colide com o proprio jogador nem com outras magias.
            if (other.GetComponentInParent<PlayerStats>() != null)
            {
                return;
            }

            if (other.GetComponent<MagicProjectile>() != null)
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(damage, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
