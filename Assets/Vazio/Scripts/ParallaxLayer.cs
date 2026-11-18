using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Desloca horizontalmente uma tira de imagens repetidas para criar
    /// profundidade (parallax) no corredor do Vazio.
    /// A tira e reenrolada pelo proprio passo da imagem, portanto nunca deixa
    /// buracos na tela.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Range(-1f, 1.5f)] private float parallaxFactor = 0.8f;
        [SerializeField, Min(0.01f)] private float tileWidth = 12f;

        private float _startX;
        private float _startCameraX;
        private float _layerY;
        private float _layerZ;

        private void Start()
        {
            CacheOrigin();
        }

        private void CacheOrigin()
        {
            _startX = transform.position.x;
            _layerY = transform.position.y;
            _layerZ = transform.position.z;
            _startCameraX = cameraTransform != null ? cameraTransform.position.x : 0f;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                return;
            }

            float cameraDelta = cameraTransform.position.x - _startCameraX;
            float offset = Mathf.Repeat(cameraDelta * parallaxFactor, tileWidth);

            transform.position = new Vector3(_startX + offset, _layerY, _layerZ);
        }

        /// <summary>Configuracao usada pelo construtor de cena.</summary>
        public void Configure(Transform cameraReference, float factor, float width)
        {
            cameraTransform = cameraReference;
            parallaxFactor = factor;
            tileWidth = Mathf.Max(0.01f, width);

            if (Application.isPlaying)
            {
                CacheOrigin();
            }
        }
    }
}
