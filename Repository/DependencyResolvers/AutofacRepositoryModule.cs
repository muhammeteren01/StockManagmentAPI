using Autofac;
using Core.Repositories;
using Core.UnitOfWork;
using Repository.Repositories;
using Module = Autofac.Module;

namespace Repository.DependencyResolvers;

/// <summary>Repository katmanı bağımlılıklarını Autofac ile kaydeder.</summary>
public class AutofacRepositoryModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(GenericRepository<>))
            .As(typeof(IGenericRepository<>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(typeof(CompanyRepository).Assembly)
            .Where(t => t.Name.EndsWith("Repository") && !t.IsGenericTypeDefinition)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterType<UnitOfWork.UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();
    }
}
