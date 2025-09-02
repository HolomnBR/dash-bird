# Integração da API .NET Core com Electron

Este projeto agora inclui integração automática com a API .NET Core FirebirdApi.

## Como Funciona

### 1. Verificação Automática da API
- Quando o aplicativo Electron é iniciado, ele automaticamente verifica se a API está rodando
- Se a API não estiver rodando, ela é iniciada automaticamente
- A API roda na porta 8000 por padrão

### 2. Build da API
- O projeto inclui scripts para fazer build da API .NET Core
- A API é compilada como executável standalone para Windows
- Os arquivos da API são incluídos no release final do Electron

### 3. Gerenciamento da API
- O aplicativo pode iniciar, parar e reiniciar a API
- Status da API é monitorado em tempo real
- Logs da API são exibidos no console do Electron

## Scripts Disponíveis

### Build da API
```bash
pnpm build:api
```
- Compila a API .NET Core
- Gera executável standalone para Windows
- Copia arquivos para `desktop/api-dist/`

### Build Completo
```bash
pnpm build:full
```
- Executa build da API + build do Electron
- Prepara tudo para o release

### Build para Windows
```bash
pnpm build:windows
```
- Executa build completo
- Gera instalador Windows com electron-builder
- Inclui a API no pacote final

## Estrutura de Arquivos

```
desktop/
├── api-dist/           # API compilada (gerado pelo build)
├── scripts/
│   ├── build-api.js    # Script de build da API
│   └── api-manager.js  # Gerenciador da API
├── electron/
│   └── main.ts         # Main process com integração da API
└── dist/               # Build do frontend
```

## Endpoints da API

### Health Check
- `GET /health` - Verifica se a API está funcionando

### Swagger
- `GET /swagger` - Documentação da API

## Configuração

### Porta da API
- Padrão: 8000
- Configurável via variável de ambiente `ASPNETCORE_URLS`

### Build da API
- Target: .NET 9.0
- Runtime: win-x64
- Self-contained: true
- Single file: true
- Trimmed: true

## Troubleshooting

### API não inicia
1. Verifique se o .NET 9.0 está instalado
2. Execute `pnpm build:api` para recompilar
3. Verifique logs no console do Electron

### Erro de porta
1. Verifique se a porta 8000 está livre
2. Configure outra porta via `ASPNETCORE_URLS`
3. Atualize o `api-manager.js` com a nova porta

### Build falha
1. Verifique se o projeto FirebirdApi compila
2. Execute `dotnet restore` no diretório da API
3. Verifique se todas as dependências estão instaladas

## Desenvolvimento

### Modo Dev
- A API deve estar rodando em `http://localhost:8000`
- O frontend se conecta via CORS configurado

### Modo Produção
- A API é incluída no executável final
- Inicia automaticamente com o aplicativo
- Comunicação local via localhost
