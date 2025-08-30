using System.ComponentModel.DataAnnotations;

namespace FirebirdApi.Models
{
    public class DatabaseConfig
    {
        /// <summary>Identificador único da base de dados.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Nome amigável da base de dados.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Servidor Firebird (ex.: localhost).</summary>
        public string Server { get; set; } = string.Empty;
        /// <summary>Caminho para o arquivo .FDB. Aceita '/' ou \\.</summary>
        public string Database { get; set; } = string.Empty;
        /// <summary>Usuário do banco.</summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>Senha do banco.</summary>
        public string Password { get; set; } = string.Empty;
        /// <summary>Porta do servidor Firebird.</summary>
        public int Port { get; set; } = 3050;
        /// <summary>Charset da conexão (ex.: UTF8).</summary>
        public string Charset { get; set; } = "UTF8";
        /// <summary>Data de criação do registro.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>Indica se a configuração está ativa (soft delete).</summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Modelo para request de adição de banco de dados
    /// Permite adicionar apenas os campos essenciais, preenchendo os demais automaticamente
    /// </summary>
    public class AddDatabaseRequest
    {
        /// <summary>Nome amigável da base (opcional).</summary>
        public string? Name { get; set; }
        /// <summary>Caminho completo para o arquivo .FDB. Use '/' ou escape \\.</summary>
        [Required]
        public string Database { get; set; } = string.Empty;
        /// <summary>Servidor (padrão: localhost).</summary>
        public string? Server { get; set; }
        /// <summary>Usuário (padrão: SYSDBA).</summary>
        public string? Username { get; set; }
        /// <summary>Senha (padrão: masterkey).</summary>
        public string? Password { get; set; }
        /// <summary>Porta (padrão: 3050).</summary>
        public int? Port { get; set; }
        /// <summary>Charset (padrão: UTF8).</summary>
        public string? Charset { get; set; }
    }
}
