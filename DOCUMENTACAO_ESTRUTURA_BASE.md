# DOCUMENTAÇÃO DA ESTRUTURA DA BASE DE DADOS
## Sistema ERP Híbrido - Padaria/Panificação + Hospedagem/Restaurante

> 🔙 **Voltar ao Projeto**: [Dash Bird — Desktop + API Firebird](README.md)

---

---

## **1. VISÃO GERAL DO SISTEMA**

Este é um **sistema ERP empresarial completo** que atende a dois segmentos principais:
- **Padaria/Panificação** (funcionalidade principal)
- **Hospedagem/Restaurante** (funcionalidade secundária)

O sistema demonstra uma **arquitetura madura e robusta**, adequada para empresas de médio a grande porte que operam no setor de alimentação, especialmente padarias e panificadoras com operações complexas de produção e distribuição.

---

## **2. MÓDULO PRINCIPAL: PADARIA/PANIFICAÇÃO**

### **2.1 Gestão de Produtos**
- **`PRODUTO`** - Cadastro principal de produtos
- **`GRADE_PRODUTO`** - Controle de variações (tamanhos, sabores)
- **`GRUPO_VENDA`** - Categorização de produtos
- **`SUBGRUPO_VENDA`** - Subcategorias
- **`IMG_PRODUTOS`** - Imagens dos produtos
- **`HIST_PRODUTO`** - Histórico de alterações
- **`KIT_PRODUTO`** - Produtos em kit
- **`ITEM_KIT_PRODUTO`** - Itens dos kits

### **2.2 Sistema de Receitas**
- **`RECEITA`** - Cadastro de receitas de produção
- **`ITEM_RECEITA`** - Ingredientes de cada receita
- **`FASE_RECEITA`** - Etapas de produção
- **`GRUPO_RECEITA`** - Categorização de receitas
- **`RECEITA_VARIAVEL`** - Receitas com ingredientes variáveis
- **`TEMPO_RECEITA`** - Controle de tempo de produção
- **`ITEM_RECEITA_FASE`** - Itens por fase da receita

### **2.3 Controle de Produção**
- **`OS_PRODUCAO`** - Ordens de serviço de produção
- **`ITEM_OS_PRODUCAO`** - Itens da ordem de produção
- **`ITEM_OS_PRODUCAO_FASE`** - Fases da produção
- **`QTD_OS_PRODUCAO`** - Quantidades produzidas

### **2.4 Gestão de Estoque**
- **`LOCAL_ESTOQUE`** - Locais de armazenamento
- **`NOME_ESTOQUE`** - Nomenclatura dos estoques
- **`MOV_ESTOQUE`** - Movimentações de estoque
- **`SALDOESTOQUE`** - Controle de saldos
- **`TIPO_ESTOQUE`** - Tipos de estoque (matéria-prima, produto acabado)
- **`STATUS_ESTOQUE`** - Status dos itens em estoque

---

## **3. MÓDULO DE VENDAS E COMERCIAL**

### **3.1 Sistema de Vendas**
- **`PEDIDO`** - Pedidos de venda
- **`ITEMPEDIDO`** - Itens dos pedidos
- **`FAT_PEDIDO`** - Faturamento de pedidos
- **`ENTREGA_PEDIDO`** - Controle de entregas
- **`ENTREGADOR`** - Cadastro de entregadores
- **`TAXA_ENTREGA`** - Taxas de entrega por região
- **`HORARIO_ENTREGA`** - Horários de entrega

### **3.2 Controle de Preços**
- **`PRECOVENDA`** - Preços de venda (até 5 níveis)
- **`PRECOVENDA_CALC`** - Preços calculados
- **`PRECO_MEDIO_VENDA`** - Preço médio
- **`PRECO_VENDA_INI`** - Preço inicial
- **`PRECO_MARGEM_PEDIDO`** - Controle de margem

### **3.3 Gestão de Clientes**
- **`CLIENTES`** - Cadastro principal
- **`CLASSE_CLI`** - Classificação de clientes
- **`BAIRROS`** - Bairros para entrega
- **`CIDADES`** - Cidades
- **`CLIENTE_AUX`** - Dados auxiliares de clientes

---

## **4. MÓDULO FINANCEIRO**

### **4.1 Controle de Caixa**
- **`CAIXA`** - Cadastro de caixas
- **`MOV_CAIXA`** - Movimentações de caixa
- **`LOG_CAIXA`** - Log de operações
- **`CONF_CAIXA`** - Configurações de caixa
- **`LOG_CAIXAPAF`** - Log PAF (Programa Aplicativo Fiscal)
- **`LOG_MOV_CAIXA`** - Log de movimentações

### **4.2 Gestão Bancária**
- **`BANCOS`** - Cadastro de bancos
- **`CARTEIRA`** - Contas bancárias
- **`CARTEIRA_COB`** - Carteiras de cobrança
- **`CARTOES`** - Cartões de crédito/débito

### **4.3 Controle de Pagamentos**
- **`STATUS_PAG_CAIXA`** - Status de pagamentos
- **`PRAZOENTREGA`** - Prazos de entrega
- **`MESANO_PAGAMENTO`** - Controle mensal de pagamentos

---

## **5. MÓDULO DE RECURSOS HUMANOS**

### **5.1 Gestão de Funcionários**
- **`FUNCIONARIOS`** - Cadastro principal
- **`BEN_FUNCIONARIOS`** - Benefícios
- **`DEP_FUNCIONARIOS`** - Departamentos
- **`CT_FUNCIONARIOS`** - Contratos
- **`CARTAO_FUNC`** - Cartões de ponto
- **`IMG_FUNCIONARIOS`** - Imagens dos funcionários

### **5.2 Controle de Atividades**
- **`ATIVIDADES`** - Atividades dos funcionários
- **`TEMPO_POR_RECEITA`** - Tempo gasto por receita

---

## **6. MÓDULO SECUNDÁRIO: HOSPEDAGEM/RESTAURANTE**

### **6.1 Gestão de Quartos**
- **`QUARTOS`** - Cadastro de quartos
- **`GRUPO_QUARTO`** - Categorias de quartos
- **`AMBIENTE_QUARTO`** - Tipos de ambiente
- **`EVENTOS_QUARTO`** - Eventos nos quartos
- **`STATUS_QUARTO`** - Status de ocupação
- **`ACOES_QUARTO`** - Ações nos quartos

### **6.2 Sistema de Restaurante**
- **`MESAS`** - Cadastro de mesas
- **`GRUPO_MESAS`** - Agrupamento de mesas
- **`MESAS_FICHAS`** - Fichas das mesas
- **`COMANDA`** - Sistema de comandas
- **`FICHA_COMANDA_RFID`** - Comandas com RFID
- **`MESAS_JOGOS`** - Mesas para jogos

---

## **7. MÓDULO DE FORNECEDORES E COMPRAS**

### **7.1 Gestão de Fornecedores**
- **`FORNECEDORES`** - Cadastro principal
- **`CT_FORNECEDORES`** - Contratos
- **`DOCFORNECEDORES`** - Documentos
- **`IMG_FORNECEDORES`** - Imagens dos fornecedores

### **7.2 Sistema de Compras**
- **`PEDIDO_COMPRA`** - Pedidos de compra
- **`ITEM_PEDIDOCOMPRA`** - Itens dos pedidos
- **`CPPEDIDO_COMPRA`** - Controle de pedidos de compra

---

## **8. MÓDULO DE QUALIDADE E CONTROLE**

### **8.1 Controle de Qualidade**
- **`BAL_INGREDIENTES`** - Balança para ingredientes
- **`ATIVAR_BALANCA`** - Integração com balanças
- **`EXPORTA_BALANCA`** - Exportação para balanças
- **`LERBALANCA`** - Leitura de balanças

### **8.2 Sistema de Etiquetas**
- **`ETIQUETA`** - Controle de etiquetas
- **`MOD_ETIQUETA`** - Modelos de etiquetas
- **`TIPO_ETQ_BALANCA`** - Etiquetas para balança
- **`TAMANHO_ETQ_BALANCA`** - Tamanhos de etiquetas

### **8.3 Controle de Custos**
- **`MESES_PESQ_CUSTO_PRODUTO`** - Pesquisa de custos por mês
- **`PESQ_CUSTO_PRODUTO_NF`** - Pesquisa de custos por NF
- **`CONTROLAR_BASE_RECEITA`** - Controle de base de receitas

---

## **9. MÓDULO DE INTEGRAÇÃO E TECNOLOGIA**

### **9.1 Integração com Sistemas Externos**
- **`ATIVAR_WEB_CAIXA`** - Integração web com caixa
- **`INTEGRA_FAT_CAIXA`** - Integração faturamento-caixa
- **`INTEGRA_FORTIPEDIDO`** - Integração com FortiPedido
- **`IMPORTAR_PEDIDO_REDE`** - Importação de pedidos da rede

### **9.2 Controle de Portas e Comunicação**
- **`PORTA_PRODUCAO`** - Porta para sistema de produção
- **`ENDERECO_LISTA_PRODUTOS`** - Endereços para listas
- **`ENDERECO_PEDIDO`** - Endereços para pedidos

---

## **10. CARACTERÍSTICAS TÉCNICAS**

### **10.1 Estrutura de Dados**
- **Sistema relacional robusto** com mais de 100 tabelas
- **Controle de versões** e histórico de alterações
- **Sistema de auditoria** com logs detalhados
- **Integração com sistemas externos** (balanças, PAF)
- **Controle de transações** e integridade referencial

### **10.2 Funcionalidades Avançadas**
- **Controle de custos** por produto
- **Gestão de margens** de lucro
- **Controle de estoque** em tempo real
- **Sistema de produção** com fases
- **Gestão de receitas** variáveis
- **Controle de qualidade** com balanças
- **Sistema de etiquetas** personalizáveis
- **Controle de entregas** e rotas

---

## **11. APLICAÇÃO NO CONTEXTO DE PADARIA**

Este sistema é **especialmente adequado para padarias** porque:

1. **Controla receitas** com ingredientes e quantidades precisas
2. **Gerencia produção** em lotes e fases controladas
3. **Controla estoque** de matérias-primas e produtos acabados
4. **Integra com balanças** para pesagem precisa de ingredientes
5. **Gerencia vendas** com controle de preços e margens
6. **Controla entregas** e rotas de distribuição
7. **Integra com PAF** para conformidade fiscal
8. **Gerencia funcionários** e produção com controle de tempo
9. **Controla qualidade** com sistema de etiquetas
10. **Gerencia custos** por produto e receita

---

## **12. TABELAS PRINCIPAIS POR FUNCIONALIDADE**

### **12.1 Gestão de Produtos**
```
PRODUTO (Tabela principal)
├── GRADE_PRODUTO (Variações)
├── GRUPO_VENDA (Categorias)
├── SUBGRUPO_VENDA (Subcategorias)
├── IMG_PRODUTOS (Imagens)
└── HIST_PRODUTO (Histórico)
```

### **12.2 Sistema de Receitas**
```
RECEITA (Tabela principal)
├── ITEM_RECEITA (Ingredientes)
├── FASE_RECEITA (Etapas)
├── GRUPO_RECEITA (Categorias)
└── RECEITA_VARIAVEL (Receitas flexíveis)
```

### **12.3 Controle de Produção**
```
OS_PRODUCAO (Ordem de produção)
├── ITEM_OS_PRODUCAO (Itens)
├── ITEM_OS_PRODUCAO_FASE (Fases)
└── QTD_OS_PRODUCAO (Quantidades)
```

### **12.4 Gestão de Vendas**
```
PEDIDO (Pedido de venda)
├── ITEMPEDIDO (Itens do pedido)
├── ENTREGA_PEDIDO (Entrega)
└── FAT_PEDIDO (Faturamento)
```

---

## **13. CONCLUSÃO**

Este é um **sistema ERP empresarial de alta complexidade** que combina:

- **Funcionalidades específicas de padaria** (receitas, produção, balanças)
- **Gestão comercial completa** (vendas, clientes, entregas)
- **Controle financeiro** (caixa, bancos, contas)
- **Gestão de recursos humanos** (funcionários, benefícios)
- **Funcionalidades de hospedagem** (quartos, restaurante)

### **13.1 Pontos Fortes**
- Arquitetura robusta e escalável
- Controle completo do ciclo de produção
- Integração com equipamentos industriais
- Sistema de auditoria e logs
- Controle de qualidade integrado

### **13.2 Aplicabilidade**
- Padarias e panificadoras de médio a grande porte
- Empresas com produção em lotes
- Negócios que necessitam controle de receitas
- Empresas com distribuição e entregas
- Estabelecimentos que integram padaria e restaurante

### **13.3 Recomendações de Uso**
- Implementação gradual por módulos
- Treinamento específico para usuários de produção
- Configuração adequada de balanças e equipamentos
- Integração com sistemas fiscais (PAF)
- Backup e manutenção regular da base de dados

---

---

## **14. NAVEGAÇÃO E LINKS**

- **📖 [README Principal](README.md)** - Visão geral do projeto Dash Bird
- **📚 [Documentação da API](FirebirdApi/README.md)** - Documentação da API .NET
- **🔧 [Scripts de Build](desktop/scripts/)** - Scripts de automação e build

---

*Documentação gerada em: 31/08/2025*  
*Base de dados analisada: snapshot_2278abeb-7066-4fd2-8b8f-8ce4ac85aada_20250831_185102.json*
