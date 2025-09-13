using FirebirdApi.Models;
using FirebirdApi.Services;
using FirebirdApi.Data;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
// removed Filters due to incompatibility

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Desabilitar validação automática de modelo para permitir mais flexibilidade
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Firebird API",
        Version = "v1",
        Description = "API para gerenciamento de bases de dados Firebird (configuração múltipla e operações legadas).",
        Contact = new OpenApiContact
        {
            Name = "Projeto FirebirdApi"
        }
    });

    // Incluir comentários XML para documentação enriquecida
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // Ativar anotações via atributos
    options.EnableAnnotations();
});


// CORS: permitir chamadas do app desktop (Vite dev server e Electron)
const string CorsPolicyName = "AllowDesktopDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsPolicyName, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:8000",
                "http://127.0.0.1:8000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
            // .AllowCredentials(); // habilite se precisar enviar cookies/autenticação
    });
});


// Registrar HttpClient
builder.Services.AddHttpClient();

// Registrar HttpClient nomeado para o servidor cloud
builder.Services.AddHttpClient("CloudServer", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FirebirdApi-Client/1.0");
});

// Registrar o serviço de configuração de bases de dados
builder.Services.AddScoped<IDatabaseConfigService, DatabaseConfigService>();

// Registrar o serviço Firebird (será configurado dinamicamente)
builder.Services.AddScoped<IFirebirdService, FirebirdService>();

// Registrar o serviço de comandos Firebird para gRPC
builder.Services.AddScoped<IFirebirdCommandService, FirebirdCommandService>();

// Registrar o serviço gRPC Client
builder.Services.AddScoped<IGrpcClientService, GrpcClientService>();

// Registrar gRPC Client - CONFIGURAÇÃO DINÂMICA
var grpcServerUrl = builder.Configuration["GrpcServer:Url"] ?? "http://localhost:7001";
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddGrpcClient<DashBirdServer.Protos.DashBirdService.DashBirdServiceClient>(options =>
{
    options.Address = new Uri(grpcServerUrl);
    options.ChannelOptionsActions.Add(channelOptions =>
    {
        if (isDevelopment)
        {
            // Para desenvolvimento, aceitar certificados SSL inválidos
            channelOptions.HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
        }
        else
        {
            // Para produção, usar configuração padrão (com SSL)
            channelOptions.HttpHandler = new HttpClientHandler();
        }
    });
});

// Log da configuração
Console.WriteLine($"🔗 XXXCliente gRPC configurado para: {grpcServerUrl}");
Console.WriteLine($"🔗 Ambiente: {(isDevelopment ? "Desenvolvimento" : "Produção")}");

// Registrar o serviço MachineId
builder.Services.AddSingleton<IMachineIdService, MachineIdService>();

// Registrar o serviço de informações do sistema
builder.Services.AddSingleton<ISystemInfoService, SystemInfoService>();

// Configurar SQLite
var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DashBird");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "dashbird_local.db");

builder.Services.AddDbContext<LocalDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Registrar o serviço de armazenamento SQLite
builder.Services.AddScoped<ISqliteStorageService, SqliteStorageService>();

// Registrar o serviço de snapshots
builder.Services.AddScoped<ISnapshotService, SnapshotService>();

// Registrar o serviço de armazenamento de tokens (manter compatibilidade)
builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();

// Registrar o gerenciador de nós locais (scoped para compatibilidade com DbContext)
builder.Services.AddScoped<LocalNodeManagerService>();

// Registrar IHttpContextAccessor para o AuthService (deve vir antes do AuthService)
builder.Services.AddHttpContextAccessor();

// Registrar o serviço de autenticação (deve vir depois dos HttpClients e IHttpContextAccessor)
builder.Services.AddScoped<IAuthService, AuthService>();



// Registrar o serviço de streaming de comandos
builder.Services.AddSingleton<ICommandStreamService, CommandStreamService>();
builder.Services.AddScoped<INodeRegistrationGrpcService, NodeRegistrationGrpcService>();
builder.Services.AddHostedService<CommandStreamHostedService>();

// Registrar o serviço de startup automático do streaming
builder.Services.AddHostedService<StreamingStartupService>();

// Registrar o serviço de nós locais
builder.Services.AddScoped<ILocalNodeService, LocalNodeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Firebird API v1");
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);
app.UseAuthorization();
app.MapControllers();

// Configurar porta e logging
var port = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:8000";
Console.WriteLine($"🚀 API iniciando na porta: {port}");

// Configurar a URL para o app rodar
app.Urls.Clear();
app.Urls.Add(port);

// Configurar shutdown graceful
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configurar handlers de shutdown
lifetime.ApplicationStopping.Register(async () =>
{
    logger.LogInformation("🛑 Aplicação recebeu sinal de parada...");
    
    try
    {
        // Aguardar shutdown dos serviços
        var commandStreamService = app.Services.GetService<ICommandStreamService>();
        if (commandStreamService is CommandStreamService cmdService)
        {
            logger.LogInformation("🔄 Parando CommandStreamService...");
            
            // Aguardar com timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await cmdService.StopStreamingAsync();
                logger.LogInformation("✅ CommandStreamService parado com sucesso");
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("⚠️ Timeout ao parar CommandStreamService");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erro ao parar CommandStreamService");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Erro durante shutdown dos serviços");
    }
});

lifetime.ApplicationStopped.Register(() =>
{
    logger.LogInformation("🛑 Aplicação parou completamente");
});

// Inicializar banco de dados SQLite
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
    try
    {
        context.Database.Migrate();
        logger.LogInformation("✅ Banco de dados SQLite inicializado com migrações");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Erro ao inicializar banco de dados SQLite");
    }
}

logger.LogInformation("🚀 Iniciando aplicação...");
app.Run();
