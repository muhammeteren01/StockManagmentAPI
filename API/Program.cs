using API;
using API.Data;
using API.Services;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Core.Abstractions;
using Core.Settings;
using Core.Validations.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.DependencyResolvers;
using Service.DependencyResolvers;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule(new AutofacRepositoryModule());
    containerBuilder.RegisterModule(new AutofacServiceModule());
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.Configure<SeedSettings>(builder.Configuration.GetSection(SeedSettings.SectionName));
builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

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
app.Run();
