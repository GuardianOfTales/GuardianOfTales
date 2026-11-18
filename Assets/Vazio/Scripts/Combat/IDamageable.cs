using UnityEngine;

namespace Vazio
{
    /// <summary>Qualquer coisa que possa receber dano da magia do jogador ou dos ataques do boss.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount, Vector2 sourcePosition);
    }
}
