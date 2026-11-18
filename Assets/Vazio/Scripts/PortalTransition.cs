using System.Collections;
using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Portal que separa o corredor da arena do Vazio.
    /// Ao entrar na area de escuridao (BoxCollider2D com Is Trigger), faz
    /// fade to black, teleporta o jogador para o ponto de spawn da arena,
    /// trava a camera no enquadramento da arena e revela a cena com fade in.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class PortalTransition : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private Transform arenaSpawn;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private GameObject bossToActivate;

        [Header("Tempos (segundos, nao escalados)")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.55f;
        [SerializeField, Min(0f)] private float holdBlackDuration = 0.15f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.7f;

        [Header("Filtro")]
        [SerializeField] private string playerTag = "Player";

        private bool _running;

        public bool IsTransitioning => _running;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;

            if (fadeGroup == null)
            {
                fadeGroup = FindFadeGroup();
            }

            if (cameraFollow == null)
            {
                Camera main = Camera.main;
                if (main != null)
                {
                    cameraFollow = main.GetComponent<CameraFollow2D>();
                }
            }

            // Comeca totalmente transparente para nao cobrir a cena.
            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.blocksRaycasts = false;
                fadeGroup.interactable = false;
            }
        }

        private static CanvasGroup FindFadeGroup()
        {
            GameObject go = GameObject.Find("Canvas_Transicao/Fade");
            return go != null ? go.GetComponent<CanvasGroup>() : null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_running || other == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            {
                return;
            }

            PlayerController controller = other.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = other.GetComponentInParent<PlayerController>();
            }

            if (controller == null)
            {
                return;
            }

            StartCoroutine(Run(controller));
        }

        private IEnumerator Run(PlayerController controller)
        {
            _running = true;

            GameObject player = controller.gameObject;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();

            // Congela o jogador durante a transicao.
            controller.enabled = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            yield return Fade(0f, 1f, fadeOutDuration);
            yield return new WaitForSecondsRealtime(holdBlackDuration);

            // Teleporte instantaneo com a tela totalmente preta.
            if (arenaSpawn != null)
            {
                player.transform.position = arenaSpawn.position;
            }

            if (cameraFollow != null)
            {
                cameraFollow.SnapToArena();
            }

            // O Arquivista so "nasce" quando o jogador atravessa o portal.
            if (bossToActivate != null && !bossToActivate.activeSelf)
            {
                bossToActivate.SetActive(true);
            }

            if (body != null)
            {
                body.simulated = true;
                body.linearVelocity = Vector2.zero;
            }

            controller.enabled = true;

            yield return Fade(1f, 0f, fadeInDuration);

            _running = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (fadeGroup == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                fadeGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            fadeGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            fadeGroup.alpha = to;
        }

        /// <summary>Configuracao usada pelo construtor de cena.</summary>
        public void Configure(CanvasGroup fade, Transform spawn, CameraFollow2D follow, GameObject boss)
        {
            fadeGroup = fade;
            arenaSpawn = spawn;
            cameraFollow = follow;
            bossToActivate = boss;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.6f);
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
            }

            if (arenaSpawn != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(arenaSpawn.position, 0.5f);
            }
        }
    }
}
