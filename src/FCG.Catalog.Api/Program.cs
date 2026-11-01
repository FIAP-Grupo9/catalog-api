using FCG.BuildingBlocks.Auth;
using FCG.BuildingBlocks.Messaging;
using FCG.BuildingBlocks.Middleware;
using FCG.BuildingBlocks.Observability;
using FCG.Catalog.Api.Application.Pricing;
using FCG.Catalog.Api.Application.Services;
using FCG.Catalog.Api.Domain.Repositories;
using FCG.Catalog.Api.Infrastructure.Consumers;
using FCG.Catalog.Api.Infrastructure.Data;
using FCG.Catalog.Api.Infrastructure.Repositories;
using FCG.Catalog.Api.Infrastructure.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG CatalogAPI", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Bearer (emitido pelo UsersAPI).",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    });

    builder.Services.AddFcgJwtAuth(builder.Configuration);

    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ausente.");
    builder.Services.AddDbContext<CatalogDbContext>(opt => opt.UseNpgsql(connStr));

    // Persistência poliglota: o PostgreSQL segue dono do transacional (jogos,
    // biblioteca, promoções) e o MongoDB guarda as avaliações, que são
    // documentos flexíveis e de alta volumetria.
    var mongoConn = builder.Configuration["MongoDb:ConnectionString"]
                    ?? throw new InvalidOperationException("MongoDb:ConnectionString ausente.");
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));

    // Cache distribuído para as leituras repetidas de catálogo e promoções.
    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.Configuration = builder.Configuration["Redis:ConnectionString"]
                          ?? throw new InvalidOperationException("Redis:ConnectionString ausente.");
        o.InstanceName = "fcg-catalog:";
    });

    // Motor de busca independente do Postgres: o catálogo relacional continua
    // dono da verdade, mas consultas de busca (fuzzy + relevância) são servidas
    // pelo Elasticsearch, mantido em sincronia pelo JogoService a cada escrita.
    var esUri = builder.Configuration["Elasticsearch:Uri"]
                ?? throw new InvalidOperationException("Elasticsearch:Uri ausente.");
    var esSettings = new ElasticsearchClientSettings(new Uri(esUri));
    var esApiKey = builder.Configuration["Elasticsearch:ApiKey"];
    if (!string.IsNullOrWhiteSpace(esApiKey))
        esSettings = esSettings.Authentication(new ApiKey(esApiKey));
    builder.Services.AddSingleton(new ElasticsearchClient(esSettings));
    builder.Services.AddSingleton<IJogoSearchIndexer, ElasticsearchJogoIndexer>();

    builder.Services.AddScoped<IJogoRepository, JogoRepository>();
    builder.Services.AddScoped<IPromocaoRepository, PromocaoRepository>();
    builder.Services.AddScoped<IBibliotecaJogoRepository, BibliotecaJogoRepository>();
    builder.Services.AddSingleton<IAvaliacaoRepository, AvaliacaoMongoRepository>();
    builder.Services.AddScoped<IJogoService, JogoService>();
    builder.Services.AddScoped<IPromocaoService, PromocaoService>();
    builder.Services.AddScoped<IBibliotecaJogoService, BibliotecaJogoService>();
    builder.Services.AddScoped<IAvaliacaoService, AvaliacaoService>();
    builder.Services.AddSingleton<IPricingService, PricingService>();

    builder.Services.AddFcgMessaging(builder.Configuration, "catalog", x =>
    {
        x.AddConsumer<PaymentProcessedConsumer>();
    });
    builder.Services.AddFcgObservability("catalog-api");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Database.EnsureCreated();
        await DataSeeder.SeedAsync(
            db,
            app.Configuration,
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder"));

        // Best-effort: um Elasticsearch fora do ar no startup não pode impedir a
        // API de subir (mesma filosofia do cache). A busca fica indisponível até
        // o índice ser recriado, mas o CRUD do catálogo continua funcionando.
        try
        {
            var indexer = scope.ServiceProvider.GetRequiredService<IJogoSearchIndexer>();
            await indexer.EnsureIndexAsync();
            await indexer.ReindexAllAsync(await db.Jogos.Where(j => j.IsActive).ToListAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falha ao preparar o índice do Elasticsearch no startup; busca ficará indisponível até a próxima tentativa.");
        }
    }

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "CatalogAPI" }));
    app.UseFcgObservability();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CatalogAPI encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
