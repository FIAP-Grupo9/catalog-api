using FCG.BuildingBlocks.Auth;
using FCG.BuildingBlocks.Messaging;
using FCG.BuildingBlocks.Middleware;
using FCG.Catalog.Api.Application.Pricing;
using FCG.Catalog.Api.Application.Services;
using FCG.Catalog.Api.Domain.Repositories;
using FCG.Catalog.Api.Infrastructure.Consumers;
using FCG.Catalog.Api.Infrastructure.Data;
using FCG.Catalog.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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

    builder.Services.AddScoped<IJogoRepository, JogoRepository>();
    builder.Services.AddScoped<IPromocaoRepository, PromocaoRepository>();
    builder.Services.AddScoped<IBibliotecaJogoRepository, BibliotecaJogoRepository>();
    builder.Services.AddScoped<IJogoService, JogoService>();
    builder.Services.AddScoped<IPromocaoService, PromocaoService>();
    builder.Services.AddScoped<IBibliotecaJogoService, BibliotecaJogoService>();
    builder.Services.AddSingleton<IPricingService, PricingService>();

    builder.Services.AddFcgMessaging(builder.Configuration, "catalog", x =>
    {
        x.AddConsumer<PaymentProcessedConsumer>();
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Database.EnsureCreated();
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
