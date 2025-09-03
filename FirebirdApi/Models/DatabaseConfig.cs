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
        /// <summary>Tamanho do arquivo da base de dados em bytes.</summary>
        public long? FileSizeBytes { get; set; }
        /// <summary>Data da última verificação do tamanho do arquivo.</summary>
        public DateTime? LastSizeCheck { get; set; }
        /// <summary>ID do nó desktop que possui esta database.</summary>
        public string? DesktopNodeId { get; set; }
        /// <summary>Referência para o nó desktop (navegação).</summary>
        public object? DesktopNode { get; set; }
        /// <summary>ID do usuário que possui esta database.</summary>
        public string? UserId { get; set; }
        /// <summary>Referência para o usuário (navegação).</summary>
        public object? User { get; set; }
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
