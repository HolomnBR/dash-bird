# documento para facilitar descrever o que precisa ser feito:

## arquitetetural geral:
 Client (desktop ambient):
    - /desktop = electronJS
    - /FireBirdAPI = netcore API para o client

    Server (clould ambient):
    - /DashBirdServer = netcore API para o Server (clould)
    - /dashbird-web-panel = nextjs painel para gestão do clould


1) regra geral do ambiente "clould" web-panel + server:
   - dashbird-web-panel sempre faz requisição via API do nextJS
   -  nunca fazer request para o Server direto pelo client.
   -  Sempre fazer esse "redirecionamento" para esconder a API do server para o painel de gestão.
   - quando fazer login ou register, deve persistir o token localmente no SqlLite. Seguindo: ReactJS -> API Nextjs -> Server API -> retorna token -> salva SQLLite -> autoriza no ractjs

2) regra geral para registro de nó local e clould:
   - registra o no local (electron)
   - registra na API local
   - registrar no API server
   - start stramming gRPC com server (espera comandos)
   - start sync databases

3) processo de sync da databases configs
   - inicia sync do register
   - registra estado de nó atual
   - lista databases, calcula size atualiza SQLLite
   - envia databases para clould (atualiza se existe se não cria novo)  (apenas configs dados)
  
4) processo de snapshot e update (nó local):
   - lista databases, calcula size e atualiza SQLLite
   - pega por database, faz o snapshot da database
   - lista tabela da database
   - para cada tabela da database, lista as colunas, nomes e tipos.
   - para cada tabela da dastabase, faz um select count de registros para saber quantidade de registro por tabela (pensar em performance)

5) processo de backup de databases fisicas (files):
   - lista datbases locais
   - calcula size atualiza sqllite local
   - pega file local (físico)
   - faz upload para endpoint no server
  