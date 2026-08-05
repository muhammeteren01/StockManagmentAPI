using Autofac;
using Autofac.Extensions.DependencyInjection;
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
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
