# Guia de Solução de Problemas - Conexão do Nó

## Problema Identificado
O erro "Erro ao conectar nó" pode ocorrer por várias razões. Este guia ajuda a diagnosticar e resolver o problema.

## Melhorias Implementadas

### 1. Logs de Debug Detalhados
- ✅ Adicionados logs detalhados no processo de conexão do nó
- ✅ Logs mostram status da API, requisições HTTP e respostas
- ✅ Logs ajudam a identificar onde exatamente o problema ocorre

### 2. Verificação de Status da API
- ✅ Verificação automática se a API está rodando antes de tentar conectar
- ✅ Interface mostra status da API em tempo real
- ✅ Botão para reiniciar a API se necessário

### 3. Tratamento de Erros Melhorado
- ✅ Mensagens de erro mais específicas e úteis
- ✅ Diferenciação entre erros de conexão e erros da API
- ✅ Sugestões de ação para resolver problemas

### 4. Interface de Debug
- ✅ Exibição do Machine ID na interface
- ✅ Status da API visível na interface
- ✅ Botão para reiniciar API quando necessário

## Como Diagnosticar o Problema

### Passo 1: Verificar Logs
1. Abra o DevTools (F12 ou Ctrl+Shift+I)
2. Vá para a aba Console
3. Tente conectar o nó
4. Observe os logs que começam com emojis:
   - 🔗 Tentando conectar nó
   - 🌐 Fazendo requisição para
   - 📡 Resposta da API
   - ✅ Sucesso ou ❌ Erro

### Passo 2: Verificar Status da API
1. Na interface do nó, verifique se "API Status" mostra "✅ Rodando"
2. Se mostrar "❌ Parada", clique em "Reiniciar API"
3. Aguarde alguns segundos e tente conectar novamente

### Passo 3: Verificar Informações do Nó
1. Verifique se o Machine ID está sendo exibido corretamente
2. Verifique se o Node ID está presente
3. Verifique se a data de criação está correta

## Possíveis Causas e Soluções

### 1. API Não Está Rodando
**Sintomas:**
- Erro: "API não está rodando. Tente reiniciar a aplicação."
- Status da API mostra "❌ Parada"

**Solução:**
1. Clique no botão "Reiniciar API"
2. Aguarde alguns segundos
3. Tente conectar o nó novamente

### 2. Erro de Autenticação
**Sintomas:**
- Erro: "Email ou senha inválidos"
- Status HTTP 401 ou 403

**Solução:**
1. Faça logout e login novamente
2. Verifique se as credenciais estão corretas
3. Verifique se o token está válido

### 3. Erro de Conexão com Servidor
**Sintomas:**
- Erro: "Erro de conexão com a API"
- Timeout ou erro de rede

**Solução:**
1. Verifique se a API está rodando na porta 5000
2. Reinicie a aplicação
3. Verifique se não há firewall bloqueando

### 4. Nó Já Conectado
**Sintomas:**
- Erro: "Nó já está conectado a outro usuário"
- Status HTTP 409

**Solução:**
1. Verifique se o nó não está conectado a outra conta
2. Use a função de desconectar se necessário

### 5. Problema com Machine ID
**Sintomas:**
- Machine ID não está sendo gerado corretamente
- Erro relacionado ao identificador da máquina

**Solução:**
1. Reinicie a aplicação
2. Verifique se o sistema tem permissões adequadas
3. Verifique se não há problemas com o sistema de arquivos

## Comandos de Debug

### Verificar se a API está rodando:
```bash
netstat -an | findstr :5000
```

### Verificar logs da aplicação:
1. Abra o DevTools (F12)
2. Vá para Console
3. Procure por logs com emojis

### Reiniciar a API manualmente:
1. Feche a aplicação
2. Abra o Task Manager
3. Termine o processo "FirebirdApi.exe"
4. Reinicie a aplicação

## Contato para Suporte

Se o problema persistir após seguir este guia:
1. Colete os logs do console (DevTools)
2. Anote o Machine ID e Node ID
3. Descreva os passos que levam ao erro
4. Inclua screenshots da interface

## Arquivos Modificados

- `desktop/electron/main.ts` - Logs de debug e verificação de API
- `desktop/src/components/organisms/NodeManager.tsx` - Interface melhorada com status da API
- `desktop/scripts/api-manager.js` - Gerenciamento da API (já existia)

## Correções Implementadas

### ✅ **Problema Principal Resolvido**
- **Problema**: O endpoint `/api/User/bind-current-node` não existia na API local
- **Solução**: Adicionado o endpoint `bind-current-node` no `AuthController` da API local
- **Implementação**: 
  - Adicionado modelo `BindCurrentNodeRequest` em `AuthModels.cs`
  - Adicionado método `BindCurrentNodeToUserAsync` na interface `IAuthService`
  - Implementado o método no `AuthService` para fazer proxy para o servidor cloud
  - Corrigido o endpoint no `main.ts` para usar `/api/Auth/bind-current-node`

### ✅ **Erro Unauthorized Resolvido**
- **Problema**: Token de autenticação não estava sendo passado para o servidor cloud
- **Causa**: Método `GetTokenFromContext()` retornava `null`
- **Solução**: 
  - Implementado `GetTokenFromContext()` para obter token do `HttpContext`
  - Adicionado `IHttpContextAccessor` ao construtor do `AuthService`
  - Registrado `HttpContextAccessor` no `Program.cs`
  - Adicionados logs de debug para verificar token

### ✅ **Solução com localStorage Implementada**
- **Problema**: Método `checkNodeConnection` tentava chamar endpoint inexistente
- **Solução**: Implementada solução híbrida com localStorage
- **Benefícios**:
  - ✅ **Performance**: Verificação local primeiro, servidor apenas quando necessário
  - ✅ **Offline**: Funciona mesmo sem conexão com servidor
  - ✅ **Persistência**: Mantém estado entre sessões
  - ✅ **Fallback**: Endpoint `/api/Auth/nodes` adicionado como backup
- **Implementação**:
  - Armazena nós conectados no `localStorage` após bind bem-sucedido
  - Verifica localStorage primeiro, só consulta servidor se necessário
  - Limpa dados no logout
  - Logs detalhados para debug

### 🔧 **Fluxo de Conexão Corrigido**
1. **Frontend** → chama `bindCurrentNode` com `machineId`
2. **Electron** → faz requisição para `/api/Auth/bind-current-node` na API local
3. **API Local** → faz proxy para `/api/User/bind-current-node` no servidor cloud
4. **Servidor Cloud** → vincula o nó ao usuário e retorna resultado
5. **Resposta** → retorna sucesso/erro para o frontend

## Próximos Passos

1. ✅ **Recompile a API local** para incluir as novas funcionalidades
2. ✅ **Teste a conexão do nó** - agora deve funcionar corretamente
3. ✅ **Verifique os logs** para confirmar que a requisição está sendo feita
4. ✅ **Use as informações de debug** para monitorar o processo

## Como Testar

1. **Recompile a API**: Execute `dotnet build` na pasta `FirebirdApi`
2. **Reinicie a aplicação**: Feche e abra o Dash Bird novamente
3. **Tente conectar o nó**: Clique em "Conectar este nó na conta"
4. **Verifique os logs**: Abra o DevTools (F12) e observe os logs com emojis
5. **Confirme a conexão**: O nó deve aparecer como "Conectado" na interface
