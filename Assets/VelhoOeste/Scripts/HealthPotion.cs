using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Poção de cura: cai com gravidade, fica no chão e ao encostar no jogador
    /// restaura 25% da vida máxima (configurável).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthPotion : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 1f)] private float healPercent = 0.25f;

        private bool _used;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision != null)
            {
                TryPickup(collision.collider);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryPickup(other);
        }

        private void TryPickup(Collider2D other)
        {
            if (_used || other == null)
            {
                return;
            }

            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats == null || !stats.IsAlive)
            {
                return;
            }

            _used = true;
            GameAudio.Instance?.PlayerHeal();
            stats.Heal(stats.MaxHealth * healPercent);
            Destroy(gameObject, 0.05f);
        }

        public void Configure(float percent)
        {
            healPercent = Mathf.Clamp(percent, 0.05f, 1f);
        }
    }
}
