using UnityEngine;

namespace Vazio
{
    /// <summary>Camera do velho oeste: segue o jogador na horizontal.</summary>
    [DisallowMultipleComponent]
    public sealed class WestCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float minX = -20f;
        [SerializeField] private float maxX = 320f;
        [SerializeField, Min(0f)] private float smooth = 6f;
        [SerializeField] private float offsetY = 2.5f;

        private bool _locked;
        private Vector3 _lockedPosition;

        /// <summary>Trava a camera numa posicao fixa (usado pelas arenas).</summary>
        public void LockAt(Vector3 position)
        {
            _lockedPosition = position;
            _locked = true;
            transform.position = position;
        }

        /// <summary>Volta a seguir o jogador.</summary>
        public void Unlock()
        {
            _locked = false;
        }

        public bool IsLocked => _locked;

        public void Configure(Transform follow, float left, float right)
        {
            target = follow;
            minX = left;
            maxX = right;
        }

        private void LateUpdate()
        {
            if (_locked)
            {
                transform.position = _lockedPosition;
                return;
            }

            if (target == null)
            {
                return;
            }

            float x = Mathf.Clamp(target.position.x, minX, maxX);
            Vector3 desired = new Vector3(x, transform.position.y, transform.position.z);
            transform.position = smooth <= 0f
                ? desired
                : Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
            _ = offsetY;
        }
    }
}
