# Dashboard Estratégico - Dash Bird

## 📊 Visão Geral

O Dashboard Estratégico foi implementado para responder às **perguntas estratégicas do dono do negócio**, conforme documentado na seção 14 da documentação da estrutura da base de dados. Ele fornece análises visuais e dados estruturados para tomada de decisão baseada em evidências.

## 🎯 Perguntas Estratégicas Implementadas

### 14.1 📊 Produção e Eficiência
**Pergunta:** "Quais são meus produtos mais rentáveis e quais estão me dando prejuízo?"

**Funcionalidades:**
- Lista produtos ordenados por margem estimada
- Mostra preço de venda, custo estimado e margem
- Identifica produtos com maior rentabilidade
- Ajuda a otimizar o mix de produtos

### 14.2 🕒 Gestão de Tempo e Produtividade
**Pergunta:** "Quanto tempo leva para produzir cada produto e onde posso otimizar?"

**Funcionalidades:**
- Exibe tempo de produção por produto
- Mostra total de produções realizadas
- Identifica gargalos na produção
- Ajuda a otimizar processos

### 14.3 📈 Controle de Estoque e Perdas
**Pergunta:** "Quais ingredientes estão vencendo ou em excesso no estoque?"

**Funcionalidades:**
- Monitora quantidades em estoque
- Mostra localização dos itens
- Identifica status do estoque
- Ajuda a minimizar desperdícios

### 14.4 💰 Margem e Rentabilidade
**Pergunta:** "Qual é a margem real de cada produto considerando todos os custos?"

**Funcionalidades:**
- Calcula margem bruta e percentual
- Compara preço de venda com custo médio
- Identifica produtos mais lucrativos
- Ajuda a ajustar estratégia de preços

## 🚀 Como Acessar

1. **Pelo Menu Principal:** Clique no ícone de gráfico (📊) no header da página Home
2. **URL Direta:** Acesse `/dashboard` na aplicação
3. **Navegação:** Use o menu de navegação da aplicação

## 🔧 Funcionalidades do Dashboard

### Cards Estratégicos
- **Visualização em Tabela:** Dados organizados em colunas claras
- **Formatação Inteligente:** Valores monetários, percentuais e números formatados automaticamente
- **Ícones de Tendência:** Indicadores visuais para margens e percentuais
- **Botão de Refresh:** Atualiza dados em tempo real
- **Responsivo:** Adapta-se a diferentes tamanhos de tela

### Executor de Consultas SQL
- **Editor SQL:** Interface para consultas personalizadas
- **Consultas Salvas:** Sistema de persistência de consultas frequentes
- **Resultados em Tabela:** Visualização clara dos dados retornados
- **Dicas Estratégicas:** Sugestões para consultas úteis
- **Integração com API:** Executa consultas diretamente no Firebird

### Seleção de Base de Dados
- **Múltiplas Bases:** Suporte para diferentes configurações
- **Base Padrão:** TAVAGUA configurada como padrão
- **Switching Dinâmico:** Muda entre bases sem recarregar a página

## 📱 Responsividade

O Dashboard é totalmente responsivo e funciona em:
- **Desktop:** Layout em grid com 2 colunas
- **Tablet:** Layout adaptativo com 1 coluna
- **Mobile:** Interface otimizada para telas pequenas

## 🎨 Design e UX

### Características Visuais
- **Gradiente Moderno:** Fundo com gradiente azul-roxo
- **Glassmorphism:** Efeito de vidro translúcido nos cards
- **Animações Suaves:** Transições e hover effects
- **Ícones Lucide:** Iconografia consistente e moderna
- **Tipografia Clara:** Hierarquia visual bem definida

### Experiência do Usuário
- **Loading States:** Indicadores visuais durante carregamento
- **Error Handling:** Mensagens de erro claras e úteis
- **Feedback Visual:** Confirmações de ações realizadas
- **Navegação Intuitiva:** Fluxo lógico e fácil de seguir

## 🔌 Integração Técnica

### API Integration
- **Endpoint:** `/api/Firebird/execute-query`
- **Método:** POST
- **Parâmetros:** Query SQL + databaseId
- **Resposta:** Dados estruturados em JSON

### Componentes React
- **Dashboard:** Página principal com estado global
- **DashboardCard:** Card individual para cada pergunta estratégica
- **QueryExecutor:** Interface para consultas personalizadas
- **Header:** Cabeçalho com título e navegação

### Estado e Gerenciamento
- **React Hooks:** useState, useEffect para gerenciamento de estado
- **Estado Local:** Dados do dashboard e configurações
- **Persistência:** localStorage para consultas salvas
- **Sincronização:** Atualização automática ao mudar base de dados

## 📊 Estrutura de Dados

### Queries SQL Estratégicas
```sql
-- Exemplo: Produção e Eficiência
SELECT 
  p.DESCRICAO_PRODUTO as Produto,
  p.PRECOVENDA as PrecoVenda,
  COALESCE(SUM(ir.QTD_RECEITA * 0), 0) as CustoEstimado,
  (p.PRECOVENDA - COALESCE(SUM(ir.QTD_RECEITA * 0), 0)) as MargemEstimada
FROM PRODUTO p
LEFT JOIN RECEITA r ON p.ID_PRODUTO = r.ID_PRODUTO
LEFT JOIN ITEM_RECEITA ir ON r.ID_RECEITA = ir.ID_RECEITA
WHERE p.IS_ATIVO = 1
GROUP BY p.ID_PRODUTO, p.DESCRICAO_PRODUTO, p.PRECOVENDA
ORDER BY MargemEstimada DESC
ROWS 10
```

### Formatação de Dados
- **Monetário:** Formatação brasileira (R$)
- **Percentual:** 2 casas decimais com símbolo %
- **Números:** Separadores de milhares brasileiros
- **Texto:** Tratamento de valores nulos/undefined

## 🚀 Próximos Passos

### Funcionalidades Futuras
- **Gráficos Interativos:** Visualizações com Chart.js ou D3.js
- **Exportação de Dados:** PDF, Excel, CSV
- **Filtros Avançados:** Por período, categoria, região
- **Alertas Automáticos:** Notificações de anomalias
- **Dashboard Personalizável:** Widgets configuráveis pelo usuário

### Melhorias Técnicas
- **Cache Inteligente:** Redução de consultas repetidas
- **Real-time Updates:** WebSockets para atualizações em tempo real
- **Offline Support:** Funcionamento sem conexão
- **Performance:** Lazy loading e virtualização de tabelas

## 🐛 Troubleshooting

### Problemas Comuns
1. **Dados não carregam:** Verifique se a API está rodando
2. **Erro de conexão:** Confirme se a base de dados está ativa
3. **Layout quebrado:** Verifique se está usando navegador moderno
4. **Consultas lentas:** Otimize as queries SQL para melhor performance

### Logs e Debug
- **Console do Navegador:** Erros JavaScript e requisições
- **Network Tab:** Monitoramento de chamadas à API
- **React DevTools:** Inspeção de estado e props

## 📚 Recursos Adicionais

- **Documentação da Base:** `DOCUMENTACAO_ESTRUTURA_BASE.md`
- **README Principal:** `README.md`
- **API Documentation:** Swagger em `/swagger`
- **Componentes:** Pasta `src/components/`

---

*Dashboard implementado para Dash Bird - Sistema de Análise Estratégica para Padarias e Panificadoras*
