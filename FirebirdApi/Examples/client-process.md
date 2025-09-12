#documento para definir o processo do cliente

Assumindo a arquitetura:
    Cliente:
    - /desktop = electronJS
    - /FireBirdAPI = netcore API para o client

    Server (clould ambient):
    - /DashBirdServer = netcore API para o Server (clould)
    - /dashbird-web-panel = nextjs panel para gestão do clould

cenário 1: Cliente Deslogado
1) o cliente deve ser inciado pelo electronJS
2) ElectronJS deve chamar a FireBirdAPI para registrar o nó.
3) FireBirdAPI recebe os dados do nó (Alias) e registra o nó local.
4) FireBirdAPI, quando atualizar os dados do nó local, atualiza o nó no server, registando anonimamente (deslogado)
5) Quando usuário logar -> atualiza o nó no server para aquele userId, e define como "não anonimo"
6) cria um log de sincronizaçao
7) 
cenário 2: Cliente Logado
1) o cliente deve ser inciado pelo electronJS
2) ElectronJS deve chamar a FireBirdAPI para registrar o nó.
3) FireBirdAPI recebe os dados do nó (Alias) e registra o nó local.
4) FireBirdAPI, quando atualizar os dados do nó local, atualiza o nó no server, registando atualizado os dados do nó sempre.
5) Quando usuário deslogar -> apenas sai do cliente.
6) cria um log de sincronizaçao

cenário 3: sync de dados:
1) QUando cliente terminou processo do register, vamos iniciar o stremming
2) Cliente inicia stremmming com o gRPC Server
3) Cliente aguarda comandos do server
4) Cliente envia comandos para o server
5) caso haja base de dados, calcula o tamanho e envia para o server (apenas meta dados sem o arquivo mdf)
6) cria um log de sincronizaçao

cenário 4: backup de base
1) assim que streamming concluiu e o regisro do nó estiver ok, verifica as bases, zip o arquivo com a data de hoje
2) transfere via upload para o server
3) cria um log de sincronizaçao




2. Register:
	- adicionar perfil e vincular a usuário
	- conta pendente
	- envio de confirmação de email
	- adicionar cnpj principal
	- adicionar telefone
	- criar termmos de uso
	- criar termos de privacidade
	- remover o register do client (colocar	 botão para o clould)
	- vincular nó pelo sevidor com o nodeId.