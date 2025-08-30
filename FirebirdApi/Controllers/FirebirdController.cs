using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Services;
using System.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirebirdController : ControllerBase
    {
        private readonly IFirebirdService _firebirdService;

        public FirebirdController(IFirebirdService firebirdService)
        {
            _firebirdService = firebirdService;
        }

        [HttpGet("test-connection")]
        [SwaggerOperation(Summary = "Testar conexão", Description = "Testa conexão usando a base padrão ou a informada via query string 'databaseId'.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TestConnection([FromQuery] string? databaseId = null)
        {
            try
            {
                var isConnected = await _firebirdService.TestConnectionAsync(databaseId);
                return Ok(new { success = isConnected, message = isConnected ? "Conexão bem-sucedida!" : "Falha na conexão", databaseId = databaseId ?? "default" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("execute-query")]
        [SwaggerOperation(Summary = "Executar SELECT", Description = "Executa uma consulta e retorna um array de objetos.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request, [FromQuery] string? databaseId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { success = false, message = "Query não pode estar vazia" });
                }

                var result = await _firebirdService.ExecuteQueryAsync(request.Query, databaseId);
                return Ok(new { success = true, data = ConvertDataTableToJson(result), databaseId = databaseId ?? "default" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("execute-non-query")]
        [SwaggerOperation(Summary = "Executar comando DML/DDL", Description = "Executa INSERT/UPDATE/DELETE/DDL e retorna linhas afetadas.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExecuteNonQuery([FromBody] QueryRequest request, [FromQuery] string? databaseId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { success = false, message = "Query não pode estar vazia" });
                }

                var rowsAffected = await _firebirdService.ExecuteNonQueryAsync(request.Query, databaseId);
                return Ok(new { success = true, rowsAffected, databaseId = databaseId ?? "default" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("execute-scalar")]
        [SwaggerOperation(Summary = "Executar escalar", Description = "Executa uma consulta escalar (ex: SELECT COUNT(*)) e retorna um valor.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExecuteScalar([FromBody] QueryRequest request, [FromQuery] string? databaseId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { success = false, message = "Query não pode estar vazia" });
                }

                var result = await _firebirdService.ExecuteScalarAsync(request.Query, databaseId);
                return Ok(new { success = true, data = result, databaseId = databaseId ?? "default" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private object ConvertDataTableToJson(DataTable dt)
        {
            var rows = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                rows.Add(dict);
            }
            return rows;
        }
    }

    public class QueryRequest
    {
        /// <summary>Comando SQL a ser executado.</summary>
        public string Query { get; set; } = string.Empty;
    }
}
