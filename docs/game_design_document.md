# GAME DESIGN DOCUMENT (GDD)

**Título Provisório:** O Bibliotecário e as Páginas do Tempo
**Gênero:** Plataforma 2D / Ação Side-scroller
**Estilo Visual:** Pixel Art com Efeito Parallax
**Engine Recomendada:** Unity

---

## 1. Visão Geral e História
*   **A Premissa:** Um velho Bibliotecário abre um livro proibido que fragmenta o tempo. Para consertar a realidade, ele precisa viajar por diferentes épocas e coletar as páginas perdidas (que ganharam vida e se transformaram em chefões).
*   **O Plot Twist (A Virada):** O Arquivista, um aliado que fica na Biblioteca (Hub do jogo) instruindo o jogador, secretamente deseja o poder do livro proibido. No final, ele trai o Bibliotecário e se revela como o Boss Final no "Vazio".
*   **Transição de Fases:** Ao derrotar um chefe, ele explode em partículas (páginas voando usando o Particle System do Unity, desaparecendo gradualmente). Uma Página Brilhante Gigante surge no centro da tela. Ao tocá-la, uma cutscene em vídeo pré-renderizado (IA) é exibida mostrando a conversa na Biblioteca, e o jogador é levado ao próximo mundo.

---

## 2. Controles e Inputs (Teclado e Mouse)
O jogo foca em habilidade, mobilidade e reflexos rápidos.
*   **A e D:** Andar (Esquerda / Direita).
*   **Espaço:** Pulo simples com altura fixa.
*   **S (Segurar):** Agachar. Reduz a caixa de colisão (hitbox) pela metade, permitindo desviar de projéteis altos (como o laser do boss ou tiros de cowboy). O jogador não pode atirar enquanto agachado.
*   **Botão Esquerdo do Mouse ou Tecla J:** Atacar (Magia).
*   **Shift:** Dash / Teleporte curto. Empurra o jogador para frente na direção em que está olhando. Pode ser usado no ar.

---

## 3. Mecânicas do Jogador (O Bibliotecário)
*   **Vida (HP):** Representada por 5 corações na interface (estilo Zelda). Cada dano comum retira meio coração.
    *   *Invencibilidade Temporária (i-frames):* Ao tomar dano, o jogador é empurrado para trás (Knockback) e o sprite pisca rápido por 500 ms, ficando invencível durante esse tempo.
    *   *Colisão Físico:* Pular em cima da cabeça dos inimigos causa dano às pernas do jogador.
*   **Mana e Magia Azul:**
    *   *O Tiro:* Dispara em linha reta. Possui **Auto-aim** (mira automática no inimigo mais próximo dentro de um alcance máximo).
    *   *Cadência:* Disparo rápido de 0.3 segundos (aprox. 3 tiros por segundo).
    *   *Custo e Recarga:* Cada tiro gasta 10% da mana. A barra recarrega automaticamente por completo ao longo de 15 segundos.
    *   *Punição:* Se a mana zerar completamente, o jogador sofre status de **Lentidão** até que a barra recupere pelo menos 10%.
*   **O Dash (Teleporte):**
    *   Gasta 5% da mana.
    *   Tempo de recarga (Cooldown) de 2 segundos para usar novamente.
    *   O jogador fica invencível durante a animação, podendo atravessar ataques ou inimigos.
*   **Punição de Morte (Game Over):**
    *   Cair em buracos ou perder todos os corações causa morte instantânea.
    *   A tela escurece e exibe um texto gigante "VOCÊ MORREU" com os botões "Tentar Novamente" e "Menu Principal".
    *   Apertar tentar novamente retorna o jogador para o **início do mundo** (perdendo todo o progresso da fase), a menos que ele tenha alcançado o Checkpoint.

---

## 4. O Sistema de Checkpoint e Cura
*   **Checkpoint pré-Boss:** Uma estátua de livro. Ao passar por ela, o jogo salva. Se o jogador morrer no Boss, ele renasce nesta estátua com **100% de Vida e 100% de Mana**.
*   **Drop de Poções:** A cada 5 "Mobs Fortes" derrotados (Infectados ou Cowboys do Laço), o quinto inimigo dropa diretamente uma poção.
    *   *Efeito:* Cura meio coração.
    *   *Comportamento:* A poção fica no chão por apenas **3 segundos** e depois desaparece. É coletada ao encostar.

---

## 5. Mundos, Inimigos e Level Design

### Mundo 1: A Idade Média
*   **Ambientação:** Corredor 2D (Floresta/Masmorra/Vila a definir pelos modeladores).
*   **Obstáculos Ambientais:** Poças de lodo verde (infecção). Pisar nelas causa lentidão ao jogador e ele recebe o dobro de dano de qualquer fonte enquanto estiver em cima.
*   **Mobs Comuns:**
    *   **Ratinho:** Rápido. Dano de contato (meio coração). Vida: Morre com 2 tiros mágicos.
    *   **Infectado (Mob Forte):** Lento. Cospe veneno. O veneno causa dano direto (1 coração inteiro) se acertar o jogador em cheio, ou fica no chão por 2 segundos virando uma poça que causa lentidão. Vida: Morre com 5 tiros. Animação de ataque: joga a cabeça para frente.

### Mundo 2: O Velho Oeste
*   **Ambientação:** Corredor 2D.
*   **Obstáculos Ambientais:** Caixas de madeira no cenário onde o jogador deve usar a mecânica de agachar (S) para se proteger dos tiros inimigos.
*   **Mobs Comuns:**
    *   **Cowboy Atirador:** Fica parado para atirar. Atira em linha reta na altura do peito, com pausas de 1.5s entre os tiros. O projétil **não** pode ser destruído pela magia do jogador (exige pulo ou agachamento).
    *   **Cowboy do Laço (Mob Forte):** Se o laço atingir o jogador, não tira vida, mas paralisa o Bibliotecário por 2 segundos, deixando-o vulnerável a outros tiros.

### Mundo 3: O Vazio (Fase Final)
*   **Ambientação:** Começa como um longo corredor vazio e sem inimigos, com apenas o eco dos passos do Bibliotecário para criar forte tensão psicológica, terminando diretamente na arena do boss.

---

## 6. Lutas Contra os Chefões (Boss Fights)

| Chefe | Arena | Comportamento e Padrão de Ataques |
| :--- | :--- | :--- |
| **Rato Gigante** (Idade Média) | Sala fechada, paredes sujas com lodo da infecção. | Fica parado e a cada 5 segundos dá um grande salto na direção do jogador. Durante a luta, cospe veneno no chão e invoca Ratinhos comuns. *Death-Skill:* Ao morrer, libera uma onda massiva final de ratos que o jogador precisa pular ou desviar antes de pegar a página. |
| **Cowboy** (Velho Oeste) | Fim da rua/Trem. | Move-se pela tela, mas precisa parar os pés no chão no exato momento de atirar. Usa bananas de dinamite que causam dano em área. |
| **O Arquivista** (Vazio) | Coliseu sombrio com tons de roxo. Chão firme, sem buracos. | **Fase 1:** Atira laser roxo horizontal, obrigando o jogador a agachar. **Fase 2:** Corrompe o chão com poças de magia, obrigando o jogador a pular. **Fase 3:** Invoca uma parede de livros voadores, obrigando o jogador a atirar para abrir caminho ou usar o Dash (Shift) na hora exata para atravessá-los. |

*(Nota de UI: Todos os chefes possuem uma Barra de Vida grande e visível na tela).*

---

## 7. Áudio e Trilha Sonora
*   **Trilha Sonora (BGM):**
    *   *Idade Média:* Tom de mistério e decadência, com órgãos e sinos lentos (16-bit).
    *   *Velho Oeste:* Violão clássico, bumbos compassados e assobios tensos.
    *   *O Vazio:* Silêncio no corredor (apenas som de vento). Na luta contra o Arquivista: Corais épicos, acelerados e batidas eletrônicas de "falha no tempo".
*   **Efeitos Sonoros (SFX):**
    *   *Passos:* Som duplo de bota e a bengala batendo ("Tuc... Tuc...").
    *   *Magia:* Som de energia arcana ("Pew" com eco).
    *   *Dash:* Vento cortando rápido ("Woosh").
    *   *Pulo:* Tecido chacoalhando e impacto suave no chão.
    *   *Dano sofrido:* Gemido rouco do velho e som de vidro estilhaçando rápido (indicando o coração quebrando).
    *   *Inimigos:* Grunhidos e guinchos estridentes (Ratos) ou som seco de impacto mágico ao serem atingidos.

---

## 8. Interface de Usuário (UI/UX)
*   **HUD durante o jogo:**
    *   Canto Superior Esquerdo: Ícones dos 5 Corações (esvaziando em metades). Logo abaixo, a Barra de Mana Azul (esvaziando e enchendo continuamente).
    *   Cabeça dos inimigos: Pequenas barras de vida visíveis.
    *   Inimigos atingidos piscam em branco brevemente.
*   **Menus:**
    *   *Menu Principal:* Fundo visual da biblioteca. Botões: Novo Jogo, Continuar, Opções, Sair.
    *   *Menu de Pausa:* Paralisa o tempo do jogo totalmente. Botões: Continuar, Opções (Volume de Música e SFX separados), Voltar ao Menu Principal.
*   **Timer:** Não existe cronômetro visível durante a gameplay, ele aparece apenas na tela final de conclusão do jogo como um desafio extra de Speedrun para o jogador.