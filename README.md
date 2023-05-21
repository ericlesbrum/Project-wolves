
# Projeto Wolves

A proposta deste teste é o desenvolvimento de um conceito de jogo eletrônico para o **party game** **Lobisomem**, também conhecido como Máfia, Cidade Dorme, Vila Dorme, Noite na Vila, ou ainda “Detetive Vítima e Assasssino"em uma versão mais enxuta.

Se você não estiver familiarizado com essa brincadeira, este vídeo explica muito bem todas as mecânicas e os principais *****roles***** envolvidos:

### Core Game Loop

- Anoitecer
  - Lobisomens decidem em conjunto qual *Player* será eliminado na rodada.
  - Vidente analisa a identidade de um *Player* a sua escolha e confirma se o mesmo é um lobisomen ou não.
  - O *Player* que foi eliminado é revelado a todosos jogadores.
  - Amanhecer
    - Todos jogadores votam para decidir outro *Player* para ser eliminado.

O loop continua, alternando entre anoitecer e amanhecer até que todos os lobisomens sejam eliminados (vitória para os aldeões) ou até que o número de aldeões seja equivalente ao número de lobisomens (vitória para os lobisomens).
## Funcionalidades

- NetworkString: Uma estrutura que implementa a interface(Responsável por guarda informações dos jogadores vivos/eliminados)
- INetworkSerializeByMemcpy para a serialização em rede de uma string de tamanho fixo.
- RoleType: Uma enumeração que representa diferentes papéis no jogo.
- PlayerCharacter: Um script anexado aos personagens do jogador. Ele implementa a interface INetworkSerializable para a serialização em rede dos dados do jogador e define métodos para definir informações do jogador e lidar com eventos de rede.
- LobbyManager: Gerencia o sistema de lobby para o jogo. Ele cria e junta lobbies, lida com atualizações de lobby, exibe informações do jogador e inicia o jogo quando as condições são atendidas.
- GameManager: Gerencia o próprio jogo, incluindo o estado do jogo, os papéis dos jogadores e a inicialização do jogo. Ele também verificar condições de jogo e criar objetos de jogo.
- Message: Representa uma mensagem exibida na interface do jogo. Possui um título, descrição e um botão de confirmação.
- Avatar: Representa um avatar de jogador no jogo. Possui um nome, uma imagem e um botão para alternar a visibilidade.
- Board: Gerencia o tabuleiro do jogo e seus elementos de interface. Contém avatares, botões e elementos de texto da interface do usuário para exibir informações do jogo, como contagem regressiva, papéis e turno.



## Rodando os testes

- Para rodar este projeto, siga as etapas a seguir:
  - Certifique-se de ter a Unity instalada em seu computador. Se você ainda não tem, você pode baixá-la no site oficial da [Unity](https://unity.com/).
  - Faça o download do projeto ou clone o projeto fornecido em seu computador.
  - Na janela de seleção do projeto, navegue até a pasta onde você extraiu o projeto e selecione a pasta do projeto.
  - Aguarde a Unity importar todos os assets e configurar o projeto.
  - Após o projeto ser carregado, verifique se não há erros ou mensagens de aviso na aba "Console" da Unity.  
  - Agora você está pronto para executar o projeto.
  - Para realizar a build do projeto, acesse a parte de "Build Settings" no menu "File"
  - Selecione Build e escolha a pasta de destino
  - Abra o jogo e realize os testes
  - Lembrando que o número máximo de jogadores deve ser de pelo menos 6 e no máximo 16.



## Feedback

Se você tiver algum feedback, por favor me mande por meio ericlespbrum@gmail.com

