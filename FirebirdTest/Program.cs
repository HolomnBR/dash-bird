using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data;

namespace FirebirdTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Teste de Conexão Firebird ===\n");
            
            // Caminho absoluto para o banco TAVAGUA.FDB
            string connectionString = "Server=localhost;" +
                                    "Port=3050;" +
                                    "Database=C:\\Projetos\\estudos\\forti\\dash\\FirebirdApi\\dbs\\TAVAGUA.FDB;" +
                                    "User=SYSDBA;" +
                                    "Password=masterkey;" +
                                    "Charset=UTF8;" +
                                    "Dialect=3;";

            Console.WriteLine($"String de conexão: {connectionString}\n");

            try
            {
                Console.WriteLine("Tentando conectar...");
                
                using (var connection = new FbConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("✅ CONEXÃO BEM-SUCEDIDA!");
                    Console.WriteLine($"Versão do servidor: {connection.ServerVersion}");
                    Console.WriteLine($"Database: {connection.Database}");
                    Console.WriteLine($"DataSource: {connection.DataSource}");
                    
                    // Teste simples: verificar se conseguimos executar uma query
                    Console.WriteLine("\nTestando execução de query...");
                    
                    using (var command = new FbCommand("SELECT COUNT(*) FROM RDB$RELATIONS", connection))
                    {
                        var result = command.ExecuteScalar();
                        Console.WriteLine($"✅ Query executada com sucesso! Resultado: {result}");
                    }
                    
                    // Listar algumas tabelas do sistema
                    Console.WriteLine("\nListando algumas tabelas do sistema...");
                    using (var command = new FbCommand("SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$VIEW_BLR IS NULL AND (RDB$SYSTEM_FLAG IS NULL OR RDB$SYSTEM_FLAG = 0) AND RDB$RELATION_NAME NOT LIKE 'RDB$%' AND RDB$RELATION_NAME NOT LIKE 'MON$%' ORDER BY RDB$RELATION_NAME", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            int count = 0;
                            while (reader.Read())
                            {
                                string tableName = reader.GetString(0).Trim();
                                Console.WriteLine($"  - {tableName}");
                                count++;
                            }
                            if (count == 10)
                                Console.WriteLine("  ... (mostrando apenas as primeiras 10)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ERRO NA CONEXÃO:");
                Console.WriteLine($"Tipo: {ex.GetType().Name}");
                Console.WriteLine($"Mensagem: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                Console.WriteLine("\nPossíveis soluções:");
                Console.WriteLine("1. Verifique se o servidor Firebird está rodando");
                Console.WriteLine("2. Verifique se a porta 3050 está correta");
                Console.WriteLine("3. Verifique se as credenciais SYSDBA/masterkey estão corretas");
                Console.WriteLine("4. Verifique se o caminho do banco está correto");
                Console.WriteLine("5. Verifique se o Firebird está instalado e configurado");
            }
            
            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }
    }
}
