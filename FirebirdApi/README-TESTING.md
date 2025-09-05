# Guia de Testes de Conectividade - FirebirdAPI

Este guia fornece instruções para testar a conectividade entre o FirebirdAPI e o DashBirdServer sem precisar fazer deploy completo.

## 🚀 Início Rápido

### 1. Teste Rápido de Conectividade
```powershell
# Teste básico de conectividade
.\test-connection.ps1

# Teste com URLs específicas
.\test-connection.ps1 -ServerUrl "https://seu-servidor.com" -GrpcUrl "https://seu-servidor-grpc.com"
```

### 2. Teste Completo de Integração
```powershell
# Executar todos os testes
.\test-integration.ps1

# Executar apenas testes de API (pular gRPC)
.\test-integration.ps1 -SkipGrpc

# Executar apenas testes gRPC (pular API)
.\test-integration.ps1 -SkipApi

# Modo verboso para mais detalhes
.\test-integration.ps1 -Verbose
```

### 3. Executar FirebirdAPI Localmente
```powershell
# Executar com configurações de teste
.\run-local-test.ps1

# Executar sem executar testes de conectividade
.\run-local-test.ps1 -SkipTests

# Executar em porta específica
.\run-local-test.ps1 -Port 9000
```

## 📋 Scripts Disponíveis

### `test-connection.ps1`
Testa conectividade básica:
- ✅ Resolução DNS
- ✅ Conectividade HTTP/HTTPS
- ✅ Conectividade gRPC (porta)
- ✅ Validação de certificado SSL

**Parâmetros:**
- `-ServerUrl`: URL do servidor API (padrão: https://dashbird-server.holomn.com.br)
- `-GrpcUrl`: URL do servidor gRPC (padrão: https://dashbird-server-grpc.holomn.com.br)
- `-Timeout`: Timeout em segundos (padrão: 30)

### `test-api-endpoints.ps1`
Testa endpoints específicos da API:
- ✅ Health Check
- ✅ User Register
- ✅ User Login
- ✅ Anonymous Node Register
- ✅ User Profile

**Parâmetros:**
- `-ServerUrl`: URL do servidor API
- `-Timeout`: Timeout em segundos

### `test-grpc-connection.ps1`
Testa conectividade gRPC específica:
- ✅ Compilação de cliente gRPC
- ✅ Teste de conexão gRPC
- ✅ Validação de canal gRPC

**Parâmetros:**
- `-GrpcUrl`: URL do servidor gRPC
- `-Timeout`: Timeout em segundos

### `test-integration.ps1`
Executa todos os testes de integração:
- ✅ Conectividade básica
- ✅ Endpoints da API
- ✅ Conectividade gRPC
- ✅ Relatório consolidado

**Parâmetros:**
- `-ServerUrl`: URL do servidor API
- `-GrpcUrl`: URL do servidor gRPC
- `-Timeout`: Timeout em segundos
- `-SkipGrpc`: Pular testes gRPC
- `-SkipApi`: Pular testes de API
- `-Verbose`: Modo verboso

### `run-local-test.ps1`
Executa o FirebirdAPI localmente para testes:
- ✅ Configuração de ambiente
- ✅ Execução de testes (opcional)
- ✅ Inicialização do servidor

**Parâmetros:**
- `-Environment`: Ambiente de configuração (padrão: LocalTest)
- `-Port`: Porta do servidor (padrão: 8000)
- `-SkipTests`: Pular testes de conectividade
- `-Verbose`: Modo verboso

## ⚙️ Configurações

### Arquivos de Configuração
- `appsettings.json`: Configuração padrão
- `appsettings.Test.json`: Configuração para testes de produção
- `appsettings.LocalTest.json`: Configuração para testes locais

### Variáveis de Ambiente
- `ASPNETCORE_ENVIRONMENT`: Define qual arquivo de configuração usar
- `ASPNETCORE_URLS`: Define a URL do servidor

## 🔧 Solução de Problemas

### Problema: "Servidor não acessível"
1. Verifique se o servidor está rodando
2. Verifique a conectividade de rede
3. Verifique se as URLs estão corretas
4. Execute `.\test-connection.ps1` para diagnóstico

### Problema: "Certificado SSL inválido"
1. Para desenvolvimento local, o código ignora certificados SSL
2. Para produção, verifique se o certificado está válido
3. Execute `.\test-connection.ps1` para verificar SSL

### Problema: "Timeout na conexão"
1. Aumente o timeout com `-Timeout 60`
2. Verifique a latência de rede
3. Verifique se o servidor está respondendo

### Problema: "gRPC não conecta"
1. Verifique se a porta gRPC está aberta
2. Execute `.\test-grpc-connection.ps1` para diagnóstico
3. Verifique se o servidor gRPC está rodando

## 📊 Interpretando Resultados

### Códigos de Saída
- `0`: Todos os testes passaram
- `1`: Alguns testes falharam (80%+ de sucesso)
- `2`: Muitos testes falharam (< 80% de sucesso)

### Status dos Testes
- ✅ **OK**: Teste passou com sucesso
- ❌ **FALHOU**: Teste falhou
- ⚠️ **AVISO**: Teste passou com avisos

## 🚀 Workflow Recomendado

1. **Antes do Deploy:**
   ```powershell
   .\test-integration.ps1
   ```

2. **Se algum teste falhar:**
   ```powershell
   .\test-connection.ps1 -Verbose
   .\test-api-endpoints.ps1 -Verbose
   .\test-grpc-connection.ps1 -Verbose
   ```

3. **Para desenvolvimento local:**
   ```powershell
   .\run-local-test.ps1
   ```

4. **Para testes rápidos:**
   ```powershell
   .\test-connection.ps1
   ```

## 📝 Logs e Debugging

### Habilitar Logs Verbosos
```powershell
# Para scripts de teste
.\test-integration.ps1 -Verbose

# Para execução local
.\run-local-test.ps1 -Verbose
```

### Logs do FirebirdAPI
Os logs são exibidos no console quando executado localmente. Para mais detalhes, configure o nível de log no `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Grpc": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## 🔗 URLs de Teste

### Produção
- API: https://dashbird-server.holomn.com.br
- gRPC: https://dashbird-server-grpc.holomn.com.br

### Desenvolvimento Local
- API: http://localhost:7000
- gRPC: http://localhost:7001

### FirebirdAPI Local
- API: http://localhost:8000
- Swagger: http://localhost:8000/swagger
