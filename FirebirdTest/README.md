# Teste de Conexão Firebird

Projeto simples para testar se a conexão com Firebird funciona em C#.

## Arquivos

- **Program.cs** - Teste básico com string de conexão hardcoded
- **ProgramWithConfig.cs** - Teste que lê configuração do appsettings.json
- **appsettings.json** - Configurações de conexão

## Como Executar

### Teste Básico
```bash
dotnet run
```

### Teste com Configuração
```bash
dotnet run --project . --configuration Release
```

## O que o Teste Faz

1. **Tenta conectar** com o banco TAVAGUA.FDB
2. **Exibe informações** da conexão (versão, database, etc.)
3. **Executa uma query** simples para verificar se funciona
4. **Lista tabelas** do sistema (se conseguir conectar)
5. **Mostra erros detalhados** se falhar

## Configuração

Edite o `appsettings.json` com suas configurações:

```json
{
  "Firebird": {
    "Server": "localhost",
    "Port": 3050,
    "Database": "..\\FirebirdApi\\dbs\\TAVAGUA.FDB",
    "Username": "SYSDBA",
    "Password": "masterkey",
    "Charset": "UTF8"
  }
}
```

## Possíveis Problemas

- **Servidor não está rodando** - Instale e inicie o Firebird
- **Porta incorreta** - Verifique se é 3050
- **Credenciais erradas** - SYSDBA/masterkey são padrão
- **Caminho do banco** - Verifique se TAVAGUA.FDB existe
- **Firebird não instalado** - Baixe do site oficial

## Resultado Esperado

Se tudo funcionar, você verá:
```
✅ CONEXÃO BEM-SUCEDIDA!
Versão do servidor: WI-V4.0.5.2704 Firebird 4.0
Database: C:\Projetos\estudos\forti\dash\FirebirdApi\dbs\TAVAGUA.FDB
DataSource: localhost
✅ Query executada com sucesso! Resultado: 123
```

## Próximos Passos

Se a conexão funcionar, você pode:
1. Usar a API completa (FirebirdApi)
2. Criar aplicações mais complexas
3. Integrar com outros sistemas
