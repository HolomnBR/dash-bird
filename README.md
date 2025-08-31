## Dash Bird — Desktop + API Firebird

Aplicação desktop (Electron + React + Vite) para explorar e operar bases Firebird, integrada a uma API .NET 9 que gerencia múltiplas configurações de bancos (arquivo .FDB) e fornece endpoints para consultas SQL, metadados e utilidades.

### Estrutura do repositório

- `desktop`: App desktop em Electron + React + Vite
- `FirebirdApi`: API .NET 9 para Firebird (Swagger incluso)
- `FirebirdTest`: Console de teste de conexão Firebird
- `Firebird259`: Binários/distribuição do Firebird 2.5 (para referência local)

### Requisitos

- Node.js 20+ e pnpm
- .NET 9 SDK
- Firebird Server (p. ex., 2.5/3/4) instalado e rodando
- Acesso a um arquivo `.FDB` válido

### Portas e URLs

- API: `http://localhost:5000` (porta padrão para produção e desenvolvimento)
- Electron/React (dev): `http://localhost:5173`

O app desktop está configurado para chamar a API em `http://localhost:5000` por padrão (`desktop/src/api/http.ts`).

---

## Como rodar

### 1) API (.NET)

1. Abra um terminal na pasta `FirebirdApi`
2. Execute:

```
dotnet restore
dotnet run
```

Por padrão os perfis de execução expõem:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:7195`

Swagger/OpenAPI: `http://localhost:5000/swagger`

Configuração base inicial: `FirebirdApi/config.json` traz um exemplo de base `TAVAGUA` com `id = "default"`. Bases adicionais e alterações dinâmicas são persistidas em `database-configs.json` no diretório de execução.

Observação: o `appsettings.json` contém um caminho absoluto de exemplo para `TAVAGUA.FDB`. Ajuste para o caminho válido no seu ambiente.

### 2) App Desktop (Electron + React)

1. Abra outro terminal na pasta `desktop`
2. Instale as dependências:

```
pnpm install
```

3. Ambiente de desenvolvimento (HMR + Electron):

```
pnpm dev
```

Isso abre a janela Electron apontando para `http://localhost:5173`.

#### Se Electron falhar ao inicializar (Ex.: "Electron failed to install correctly")

1. Na pasta `desktop`, rode o instalador do Electron (script já adicionado ao `package.json`):

```
pnpm run electron:install
```

2. Tente novamente:

```
pnpm dev
```

Caso o problema persista, apague `desktop/node_modules/electron` e rode o instalador novamente.

#### Se o Vite apontar dependências ausentes (Ex.: `react-router-dom`, `lucide-react`)

Instale-as dentro de `desktop`:

```
pnpm add react-router-dom lucide-react
```

Depois reinicie o dev server:

```
pnpm dev
```

4. Build de produção do front e do processo main/preload:

```
pnpm build
pnpm start:prod
```

Se desejar rodar somente Electron com o bundle gerado, use `pnpm start:prod` (carregando `dist-electron/main.js`).

---

## Funcionalidades

- Gerenciar múltiplas bases: listar, adicionar, atualizar, remover (soft delete)
- Definir base padrão e testá-la
- Listar tabelas e ler schemas de tabelas específicas
- Executar queries SQL (SELECT), non-query (INSERT/UPDATE/DELETE/DDL) e scalar

Endpoints principais (API):
- `GET /api/DatabaseConfig/databases`
- `POST /api/DatabaseConfig/databases`
- `GET /api/DatabaseConfig/databases/{id}`
- `PUT /api/DatabaseConfig/databases/{id}`
- `DELETE /api/DatabaseConfig/databases/{id}`
- `GET /api/DatabaseConfig/databases/{id}/test-connection`
- `GET /api/DatabaseConfig/databases/{id}/tables`
- `POST /api/DatabaseConfig/databases/{id}/table-schema`
- `GET /api/DatabaseConfig/default-database`
- `POST /api/DatabaseConfig/default-database`

Operações Firebird (legado):
- `GET /api/Firebird/test-connection`
- `POST /api/Firebird/execute-query`
- `POST /api/Firebird/execute-non-query`
- `POST /api/Firebird/execute-scalar`

---

## Fluxo do App Desktop

- Home: exibe lista de bases configuradas (consome `GET /api/DatabaseConfig/databases`)
- Settings: mostra identificador da máquina, permite salvar um alias local (localStorage)
- DatabaseDetails: consulta/mostra detalhes, testa conexão, abre lista de tabelas e Query Tool, cria novas bases
- QueryTool: placeholder para editor de consultas (em breve)

Integração Electron:
- `electron/main.ts` expõe handlers IPC (`system:getInfo`, `dialog:openFile`)
- `electron/preload.ts` publica `window.system` para o renderer

---

## Configuração e Persistência

- Base estática inicial: `FirebirdApi/config.json`
- Configurações dinâmicas: `database-configs.json` (gerado no diretório de execução da API)
- Base padrão: definida em `ProjectSettings.DefaultDatabaseId` (padrão: `default`)

Importante: credenciais e caminhos de banco são armazenados em JSON. Para produção, considere criptografar segredos e proteger o diretório de execução.

---

## Notas de segurança

- Use HTTPS em produção e configure CORS apropriadamente
- Evite expor credenciais; considere um cofre de segredos
- Valide e sanitize comandos SQL se forem originados de input de usuário
- Restrinja operações sensíveis por autenticação/autorização

---

## Troubleshooting

- Não conecta no Firebird:
  - Verifique serviço do Firebird ativo e porta 3050 aberta
  - Confirme caminho do `.FDB` e permissões de leitura
  - Teste com `FirebirdTest` (console) para validar a string de conexão
- API não responde:
  - Confirme `dotnet run` na pasta `FirebirdApi`
  - Cheque URL `http://localhost:5175/swagger`
- Desktop não lista bases:
  - Verifique se a API está rodando na mesma porta configurada em `desktop/src/api/http.ts`

---

## Licenças e Créditos

- Firebird® é marca registrada de seus respectivos mantenedores
- Este projeto usa `FirebirdSql.Data.FirebirdClient`


