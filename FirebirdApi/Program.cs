using FirebirdApi.Models;
using FirebirdApi.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;
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

// Registrar o serviço de configuração de bases de dados
builder.Services.AddSingleton<IDatabaseConfigService, DatabaseConfigService>();

// Registrar o serviço Firebird (será configurado dinamicamente)
builder.Services.AddScoped<IFirebirdService, FirebirdService>();

// Registrar o serviço gRPC Client
builder.Services.AddScoped<IGrpcClientService, GrpcClientService>();

// Registrar o serviço MachineId
builder.Services.AddScoped<IMachineIdService, MachineIdService>();

// Registrar o serviço de autenticação
builder.Services.AddScoped<IAuthService, AuthService>();

// Registrar IHttpContextAccessor para o AuthService
builder.Services.AddHttpContextAccessor();

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

app.Run();
