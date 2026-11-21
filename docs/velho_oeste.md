# Velho Oeste — mundo dos cowboys

Cena: `Assets/VelhoOeste/Scenes/WildWestSampleScene.unity` — montada pelo menu
**Vazio ▸ Velho Oeste ▸ Montar Mundo** (`WestWorldBuilder`), que só recria o nó
`JogoVelhoOeste`; o cenário do sample não é tocado.

## Conteúdo

| Elemento | Detalhe |
|---|---|
| **Jogador** | controlador/arte do Vazio (A/D, Espaço, **pulo duplo**, Shift, S, magia) |
| **Ondas** | 6 cowboys (1 ou 2, 50/50) sorteados **na vista do jogador**; ao zerar, **5 s** e vem outra onda, até x ≈ 296 (área do chefe) |
| **Chefe** | Cowboy 3 no fim do mapa (x = 318), **escala 1,8**, 48 de vida |
| **Portal** | surge 1,6 s após a morte do chefe e leva para a cena `Vazio` |

## Pulo duplo

`PlayerController`: `airJumps = 1`, `airJumpManaCost = 20` (mesmo valor do dash).
O primeiro pulo é grátis; o segundo só acontece se houver 20 de mana. A carga volta a
1 ao tocar o chão.

## Ataques do chefe

| Ataque | Comportamento |
|---|---|
| **Rajada** | 5 tiros com 0,16 s entre eles (animação `Shot`), a cada 3,4 s |
| **Tiro carregado** | segura `Charging` por 1,25 s e dispara uma bala grande com brilho, **3× dano** (6 contra 2) |

## HUD (assets do pacote `Assets/UI`)

| Barra | Assets | Posição |
|---|---|---|
| Vida | `Health_*` | canto superior esquerdo |
| Mana | `Mana_*` | abaixo da vida |
| Vida do cowboy | `Archivist_*` (moldura/fundo/fill/ícone) | rodapé central |

A barra do chefe aparece suavemente quando o jogador está a ≤ 26 u dele (`WestHud`).

## Validação (Play Mode)

- Onda 1: 6 cowboys em volta do jogador (offsets −4, 4, 3, 4, −4, 2) ✔
- Onda zerada → **5 s** → onda 2 com mais 6 ✔
- Pulo duplo: mana 100 → 81 e altura 2,36 → 4,20 ✔
- HUD: vida 80%, mana 94%; barra do chefe com alpha 1 e fill 100% perto dele ✔
- Portal do chefe → cena `Vazio` ✔

## Backups

- `docs/backups/Vazio_20261118_1345.unity`
- `docs/backups/WildWest_20261118_1345.unity`

## Correções de tiro

- **Mira:** os disparos (inimigos e chefe) miram no **centro do colisor do jogador no momento do tiro**.
  Antes somavam +0,9 u à posição (que já é o centro do corpo), o que fazia a bala subir na diagonal.
  A bala continua **reta** depois de sair — não é teleguiada.
- **Origem:** o ponto de disparo (FirePoint) agora fica na altura do cano (0,5 , 0,95 local) nos dois
  casos; no chefe a escala 1,8 multiplica esse offset, então a bala sai da arma e não do alto da cabeça.

## Correção: magia atravessava os cowboys

O `CircleCollider2D` do `MagicProjectile.prefab` tinha **offset (0, 1,78)** local. Como o
projétil gira para a direção do tiro, esse offset invertia:

- atirando para a **esquerda** (rotação 180°) o colisor descia ~0,8 u e entrava no cowboy → acertava;
- atirando para a **direita** (rotação 0°) o colisor subia ~0,8 u e passava **por cima** do cowboy → atravessava.

Correção: `offset = (0,0)` e raio 0,5 (colisor sempre centrado no projétil, em qualquer direção).
Validado: os dois sentidos matam o cowboy.
