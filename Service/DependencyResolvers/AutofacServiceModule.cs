using Autofac;
using Core.Services;
using Service.Services;
using Module = Autofac.Module;

namespace Service.DependencyResolvers;

/// <summary>Service katmanı bağımlılıklarını Autofac ile kaydeder.</summary>
public class AutofacServiceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(GenericService<>))
            .As(typeof(IGenericService<>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(typeof(CompanyService).Assembly)
            .Where(t => t.Name.EndsWith("Service") && !t.IsGenericTypeDefinition)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}
