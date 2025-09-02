# Debug - Verificação de Conexão

## Como Testar a Funcionalidade

### 1. Abra o Console do Navegador
- Pressione `F12` ou `Ctrl+Shift+I`
- Vá para a aba "Console"

### 2. Verifique os Logs
Quando você entrar na tela do NodeManager, deve ver logs como:

```
🔄 Verificação inicial de conexão do nó...
🔍 Verificando conexão no servidor... (verificação forçada)
📡 Resposta do servidor: { isConnected: false, wasUnbound: true, hasNode: false }
🚫 Nó foi desvinculado remotamente, limpando localStorage... { wasUnbound: true, wasConnected: true, isNowConnected: false }
🧹 Nó removido do localStorage: { machineId: "..." }
```

### 3. Teste Manual
1. Clique no botão "🔄 Verificar Conexão"
2. Deve ver logs similares aos acima
3. A interface deve atualizar para mostrar o nó como desvinculado

### 4. Verifique o localStorage
- Vá para `F12` > `Application` > `Local Storage`
- Verifique se a chave `connected_nodes` não contém mais o seu nó

## Logs Esperados

### Quando Nó Está Conectado:
```
✅ Nó encontrado no localStorage como conectado: { machineId: "...", nodeId: "..." }
```

### Quando Nó Foi Desvinculado:
```
🔍 Verificando conexão no servidor... (verificação forçada)
📡 Resposta do servidor: { isConnected: false, wasUnbound: true, hasNode: false }
🚫 Nó foi desvinculado remotamente, limpando localStorage...
🧹 Nó removido do localStorage: { machineId: "..." }
```

### Verificação Periódica:
```
⏰ Iniciando verificação periódica de conexão...
🔄 Verificação periódica de conexão do nó...
🔍 Verificando conexão no servidor... (verificação forçada)
```

### Verificação por Foco:
```
👁️ Janela ganhou foco, verificando conexão do nó...
🔍 Verificando conexão no servidor... (verificação forçada)
```

## Problemas Comuns

### 1. Não Vê Logs
- Verifique se está na aba correta do console
- Verifique se o filtro do console não está bloqueando os logs
- Recarregue a página e tente novamente

### 2. Interface Não Atualiza
- Verifique se há erros no console
- Clique no botão "🔄 Verificar Conexão" manualmente
- Verifique se o localStorage foi limpo

### 3. Verificação Não Funciona
- Verifique se está logado
- Verifique se o token é válido
- Verifique se a API está rodando

## Comandos de Debug

### Limpar localStorage Manualmente:
```javascript
localStorage.removeItem('connected_nodes')
```

### Verificar Estado do localStorage:
```javascript
console.log(JSON.parse(localStorage.getItem('connected_nodes') || '[]'))
```

### Forçar Verificação:
```javascript
// No console do navegador
window.location.reload()
```
