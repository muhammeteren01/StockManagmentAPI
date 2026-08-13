using API;
using API.Data;
using API.Middleware;
using API.Services;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Core.Abstractions;
using Core.Settings;
using Core.Validations.Users;
using FluentValidation;
using Integration.Sysmond.Api;
using Integration.Sysmond.Api.Controllers;
using Integration.Sysmond.Service.DependencyResolvers;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.DependencyResolvers;
using Serilog;
using Service.DependencyResolvers;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        containerBuilder.RegisterModule(new AutofacRepositoryModule());
        containerBuilder.RegisterModule(new AutofacServiceModule());
        containerBuilder.RegisterModule(new AutofacSysmondModule());
    });

    builder.Services.AddControllers()
        .AddApplicationPart(typeof(SysmondController).Assembly);
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();
    builder.Services.Configure<SeedSettings>(builder.Configuration.GetSection(SeedSettings.SectionName));
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddSysmondIntegration(builder.Configuration);
    builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

    var app = builder.Build();

    // Dıştan içe: CorrelationId → RequestLogging → ExceptionHandler
    app.UseCorrelationId();
    app.UseRequestLogging();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Management API v1");
            options.RoutePrefix = "swagger";
        });
    }

    await DbSeeder.SeedSuperAdminAsync(app.Services);

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Application starting. Environment={Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
