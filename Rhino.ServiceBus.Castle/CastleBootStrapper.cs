using System;
using System.IO;
using System.Reflection;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Actions;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Castle
{
    public class CastleBootStrapper : AbstractBootStrapper
    {
        private IWindsorContainer container;

        public CastleBootStrapper()
        {
        }

        public CastleBootStrapper(IWindsorContainer container)
        {
            this.container = container;
        }

        protected IWindsorContainer Container
        {
            get { return container; }
        }

        public override void InitializeContainer()
        {
            CreateContainer();
            ConfigureContainer();

            var facility = new RhinoServiceBusFacility();
            ConfigureBusFacility(facility);
            container.Kernel.AddFacility("rhino.esb", facility);
        }

        protected virtual void ConfigureBusFacility(RhinoServiceBusFacility facility)
        {
        }

        public override void ExecuteDeploymentActions(string user)
        {
            foreach (var action in container.ResolveAll<IDeploymentAction>())
            {
                action.Execute(user);
            }
        }

        public override void ExecuteEnvironmentValidationActions()
        {
            foreach (var action in container.ResolveAll<IEnvironmentValidationAction>())
            {
                action.Execute();
            }
        }

        public override IStartableServiceBus GetStartableServiceBus()
        {
            return container.Resolve<IStartableServiceBus>();
        }

        public override void Dispose()
        {
            container.Dispose();
        }

        protected virtual void CreateContainer()
        {
            if (container != null)
                return;

            container = File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile)
                            ? new WindsorContainer(new XmlInterpreter())
                            : new WindsorContainer();
        }

        protected virtual void ConfigureContainer()
        {
            container.Register(
                 AllTypes.FromAssembly(Assembly)
                     .BasedOn<IDeploymentAction>(),
                 AllTypes.FromAssembly(Assembly)
                     .BasedOn<IEnvironmentValidationAction>()
                 );
            RegisterConsumersFrom(Assembly);
        }

        protected virtual void RegisterConsumersFrom(Assembly assembly)
		{
			container.Register (
				 AllTypes
					.FromAssembly (assembly)
					.Where (type =>
						typeof (IMessageConsumer).IsAssignableFrom (type) &&
						typeof (IOccasionalMessageConsumer).IsAssignableFrom (type) == false &&
						IsTypeAcceptableForThisBootStrapper (type)
					)
					.Configure (registration =>
					{
						registration.LifeStyle.Is (LifestyleType.Transient);
						ConfigureConsumer (registration);
					})
				);
		}

    	protected virtual void ConfigureConsumer(ComponentRegistration registration)
    	{
    		registration.Named(registration.Implementation.Name);
    	}
    }
}