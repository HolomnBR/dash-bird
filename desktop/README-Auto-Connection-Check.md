# Verificação Automática de Conexão

Este documento explica as melhorias implementadas para verificação automática de conexão de nós no DashBird Desktop.

## Funcionalidades Implementadas

### 1. Verificação Inicial
- **Quando**: Quando o componente NodeManager é montado
- **Frequência**: Uma vez por carregamento da tela
- **Log**: `🔄 Verificação inicial de conexão do nó...`

### 2. Verificação Periódica
- **Quando**: A cada 30 segundos automaticamente
- **Frequência**: Contínua enquanto a tela estiver ativa
- **Log**: `🔄 Verificação periódica de conexão do nó...`

### 3. Verificação por Foco da Janela
- **Quando**: Quando a janela da aplicação ganha foco
- **Frequência**: Sempre que o usuário volta para a aplicação
- **Log**: `👁️ Janela ganhou foco, verificando conexão do nó...`

### 4. Verificação Manual
- **Quando**: Quando o usuário clica no botão "🔄 Verificar Conexão"
- **Frequência**: Sob demanda
- **Interface**: Botão centralizado na tela

## Como Funciona

### Detecção de Desvinculação Remota
Quando qualquer uma das verificações é executada:

1. **Chama `checkNodeConnection()`**
2. **Servidor retorna `wasUnbound: true`** se o nó foi desvinculado remotamente
3. **Cliente limpa automaticamente o localStorage**
4. **Interface atualiza para mostrar nó como desvinculado**

### Logs de Debug
O sistema gera logs detalhados para facilitar o debug:

```
🔄 Verificação inicial de conexão do nó...
⏰ Iniciando verificação periódica de conexão...
🔄 Verificação periódica de conexão do nó...
👁️ Janela ganhou foco, verificando conexão do nó...
🚫 Nó foi desvinculado remotamente, limpando localStorage...
🧹 Nó removido do localStorage: { machineId: "..." }
```

## Implementação Técnica

### useEffect Hooks

```typescript
// Verificação inicial
useEffect(() => {
  if (nodeConfig && isAuthenticated && token) {
    console.log('🔄 Verificação inicial de conexão do nó...')
    checkNodeConnection()
  }
}, [nodeConfig, isAuthenticated, token, checkNodeConnection])

// Verificação periódica
useEffect(() => {
  if (!nodeConfig || !isAuthenticated || !token) return

  console.log('⏰ Iniciando verificação periódica de conexão...')
  const interval = setInterval(() => {
    console.log('🔄 Verificação periódica de conexão do nó...')
    checkNodeConnection()
  }, 30000) // 30 segundos

  return () => {
    console.log('⏹️ Parando verificação periódica de conexão...')
    clearInterval(interval)
  }
}, [nodeConfig, isAuthenticated, token, checkNodeConnection])

// Verificação por foco
useEffect(() => {
  if (!nodeConfig || !isAuthenticated || !token) return

  const handleFocus = () => {
    console.log('👁️ Janela ganhou foco, verificando conexão do nó...')
    checkNodeConnection()
  }

  window.addEventListener('focus', handleFocus)
  
  return () => {
    window.removeEventListener('focus', handleFocus)
  }
}, [nodeConfig, isAuthenticated, token, checkNodeConnection])
```

### Interface do Usuário

```typescript
{/* Botão de Verificação Manual */}
<div className="flex justify-center">
  <button
    onClick={checkNodeConnection}
    disabled={loading}
    className="bg-gray-100 text-gray-700 px-3 py-1 rounded text-sm hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed"
  >
    {loading ? 'Verificando...' : '🔄 Verificar Conexão'}
  </button>
</div>
```

## Benefícios

1. **Detecção Imediata**: Nós desvinculados remotamente são detectados rapidamente
2. **Sincronização Automática**: Interface sempre reflete o estado real do servidor
3. **Experiência do Usuário**: Não é necessário recarregar a página ou fazer logout/login
4. **Controle Manual**: Usuário pode forçar verificação quando necessário
5. **Eficiência**: Verificações inteligentes evitam chamadas desnecessárias

## Cenários de Uso

### Cenário 1: Desvinculação Remota
1. Administrador desvincula nó no painel cloud
2. Cliente detecta automaticamente em até 30 segundos
3. Interface atualiza para mostrar nó como desvinculado
4. localStorage é limpo automaticamente

### Cenário 2: Usuário Volta para a Aplicação
1. Usuário minimiza aplicação e volta depois
2. Verificação automática por foco é executada
3. Estado é sincronizado imediatamente

### Cenário 3: Verificação Manual
1. Usuário suspeita que algo mudou
2. Clica no botão "🔄 Verificar Conexão"
3. Verificação é executada imediatamente

## Configuração

### Intervalo de Verificação Periódica
Para alterar o intervalo de 30 segundos, modifique a linha:

```typescript
}, 30000) // 30 segundos
```

### Logs de Debug
Para desabilitar os logs, remova ou comente as linhas `console.log()`.

## Teste

Para testar a funcionalidade:

1. **Desvincule um nó remotamente** via API ou painel admin
2. **Aguarde até 30 segundos** ou clique em "🔄 Verificar Conexão"
3. **Verifique se a interface atualizou** automaticamente
4. **Confirme que o localStorage foi limpo** (F12 > Application > Local Storage)
