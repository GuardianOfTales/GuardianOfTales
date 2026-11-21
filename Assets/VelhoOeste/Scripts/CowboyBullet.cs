using UnityEngine;

namespace Vazio
{
    /// <summary>Bala de revolver dos cowboys. A versao carregada causa o triplo do dano.</summary>
    [DisallowMultipleComponent]
    public sealed class CowboyBullet : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float speed = 13f;
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField, Min(0.1f)] private float lifeTime = 2.5f;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField] private float groundY = float.NegativeInfinity;

        private Vector2 _direction;
        private float _life;
        private PlayerStats _target;

        public float TotalDamage => damage * damageMultiplier;

        public void Launch(Vector2 direction, PlayerStats target)
        {
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            _target = target;
            _life = 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _life += dt;
            transform.position += (Vector3)(_direction * (speed * dt));

            if (_life >= lifeTime || transform.position.y < groundY - 0.5f)
            {
                Destroy(gameObject);
                return;
            }

            if (_target == null)
            {
                _target = FindAnyObjectByType<PlayerStats>();
            }

            if (_target == null || !_target.IsAlive)
            {
                return;
            }

            Vector2 toTarget = (Vector2)_target.transform.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude <= 0.55f * 0.55f)
            {
                _target.TakeDamage(TotalDamage, transform.position);
                Destroy(gameObject);
            }
        }

        public void Configure(float bulletSpeed, float bulletDamage, float multiplier, float floor)
        {
            speed = bulletSpeed;
            damage = bulletDamage;
            damageMultiplier = multiplier;
            groundY = floor;
        }
    }
}
