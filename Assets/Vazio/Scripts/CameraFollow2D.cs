using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Camera ortografica do mundo Vazio.
    /// No corredor ela acompanha o jogador suavemente (com leve antecipacao no
    /// eixo X e limites nas extremidades). Ao detectar a entrada na arena, ela
    /// transiciona para um enquadramento fixo que cobre toda a arena e nunca
    /// mais se move.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [Header("Alvo")]
        [SerializeField] private Transform target;

        [Header("Corredor (seguir suavemente)")]
        [SerializeField] private float corridorMinX = 1f;
        [SerializeField] private float corridorMaxX = 76f;
        [SerializeField] private float corridorCenterY = 5.1f;
        [SerializeField] private float corridorOrthoSize = 5.2f;
        [SerializeField, Min(0f)] private float lookAhead = 1.8f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.22f;
        [SerializeField, Min(1f)] private float maxFollowSpeed = 40f;

        [Header("Arena (travar e cobrir tudo)")]
        [SerializeField] private float arenaTriggerX = 78f;
        [SerializeField] private Vector2 arenaCenter = new Vector2(93f, 6f);
        [SerializeField] private Vector2 arenaSize = new Vector2(26f, 12f);
        [SerializeField, Range(1f, 1.5f)] private float arenaPadding = 1.06f;
        [SerializeField, Min(0.1f)] private float arenaTransitionDuration = 0.9f;

        private Camera _camera;
        private Vector3 _followVelocity;
        private float _smoothOrthoVelocity;

        private bool _arenaLocked;
        private bool _transitioning;
        private float _transitionElapsed;
        private Vector3 _transitionFromPosition;
        private Vector3 _transitionToPosition;
        private float _transitionFromOrtho;
        private float _transitionToOrtho;

        /// <summary>Verdadeiro depois que a camera travou na arena.</summary>
        public bool IsLockedInArena => _arenaLocked;
        public bool IsTransitioning => _transitioning;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = corridorOrthoSize;
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (_arenaLocked)
            {
                // Estatica: nenhuma atualizacao de posicao ou zoom.
                return;
            }

            if (_transitioning)
            {
                UpdateTransition();
                return;
            }

            if (target.position.x >= arenaTriggerX)
            {
                BeginArenaTransition();
                return;
            }

            UpdateFollow();
        }

        private void UpdateFollow()
        {
            float desiredX = target.position.x + lookAhead * GetFacingSign();
            desiredX = Mathf.Clamp(desiredX, corridorMinX, corridorMaxX);

            Vector3 desired = new Vector3(desiredX, corridorCenterY, _camera.transform.position.z);

            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                desired,
                ref _followVelocity,
                smoothTime,
                maxFollowSpeed,
                Time.deltaTime);

            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize,
                corridorOrthoSize,
                ref _smoothOrthoVelocity,
                0.25f);
        }

        private float GetFacingSign()
        {
            PlayerController controller = target.GetComponent<PlayerController>();
            return controller != null ? Mathf.Sign(controller.Facing) : 1f;
        }

        private void BeginArenaTransition()
        {
            _transitioning = true;
            _transitionElapsed = 0f;
            _transitionFromPosition = _camera.transform.position;
            _transitionToPosition = new Vector3(arenaCenter.x, arenaCenter.y, _camera.transform.position.z);
            _transitionFromOrtho = _camera.orthographicSize;
            _transitionToOrtho = CalculateArenaOrthoSize();
            _followVelocity = Vector3.zero;
        }

        private void UpdateTransition()
        {
            _transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_transitionElapsed / arenaTransitionDuration);
            float eased = t * t * (3f - 2f * t); // smoothstep

            _camera.transform.position = Vector3.Lerp(_transitionFromPosition, _transitionToPosition, eased);
            _camera.orthographicSize = Mathf.Lerp(_transitionFromOrtho, _transitionToOrtho, eased);

            if (t >= 1f)
            {
                _transitioning = false;
                _arenaLocked = true;
                _camera.transform.position = _transitionToPosition;
                _camera.orthographicSize = _transitionToOrtho;
            }
        }

        /// <summary>
        /// Tamanho ortografico que garante que a arena inteira caiba na tela,
        /// tanto na largura quanto na altura, para qualquer proporcao de tela.
        /// </summary>
        private float CalculateArenaOrthoSize()
        {
            float aspect = _camera.aspect > 0.01f ? _camera.aspect : 16f / 9f;
            float orthoForHeight = arenaSize.y * 0.5f;
            float orthoForWidth = arenaSize.x * 0.5f / aspect;
            return Mathf.Max(orthoForHeight, orthoForWidth) * arenaPadding;
        }

        /// <summary>
        /// Trava a camera imediatamente no enquadramento da arena, sem transicao.
        /// Usado pelo portal: como a tela esta preta, o salto de camera e' invisivel.
        /// </summary>
        public void SnapToArena()
        {
            _transitioning = false;
            _arenaLocked = true;
            _transitionToPosition = new Vector3(arenaCenter.x, arenaCenter.y, _camera.transform.position.z);
            _transitionToOrtho = CalculateArenaOrthoSize();
            _camera.transform.position = _transitionToPosition;
            _camera.orthographicSize = _transitionToOrtho;
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            float x = Mathf.Clamp(target.position.x + lookAhead * GetFacingSign(), corridorMinX, corridorMaxX);
            _camera.transform.position = new Vector3(x, corridorCenterY, _camera.transform.position.z);

            if (_camera.orthographicSize <= 0.01f)
            {
                _camera.orthographicSize = corridorOrthoSize;
            }
        }

        /// <summary>Configuracao usada pelo construtor de cena do Vazio.</summary>
        public void Configure(Transform followTarget, float minX, float maxX, float centerY,
            float orthoSize, float triggerX, Vector2 center, Vector2 size)
        {
            target = followTarget;
            corridorMinX = minX;
            corridorMaxX = maxX;
            corridorCenterY = centerY;
            corridorOrthoSize = orthoSize;
            arenaTriggerX = triggerX;
            arenaCenter = center;
            arenaSize = size;

            _arenaLocked = false;
            _transitioning = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.4f, 1f, 0.9f);
            Gizmos.DrawWireCube(new Vector3(arenaCenter.x, arenaCenter.y, 0f),
                new Vector3(arenaSize.x, arenaSize.y, 0f));

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawLine(new Vector3(arenaTriggerX, -2f, 0f), new Vector3(arenaTriggerX, 14f, 0f));
        }
    }
}
