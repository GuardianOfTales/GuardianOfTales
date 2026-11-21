using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vazio
{
    /// <summary>Portal do velho oeste: leva o jogador para o mundo do Vazio.</summary>
    [DisallowMultipleComponent]
    public sealed class WestPortal : MonoBehaviour
    {
        [SerializeField] private string sceneName = "Vazio";
        [SerializeField, Min(0.1f)] private float cooldown = 0.4f;

        private bool _used;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || other == null)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerStats>() == null)
            {
                return;
            }

            _used = true;
            Invoke(nameof(LoadTarget), cooldown);
        }

        private void LoadTarget()
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void Configure(string target)
        {
            sceneName = target;
        }
    }
}
