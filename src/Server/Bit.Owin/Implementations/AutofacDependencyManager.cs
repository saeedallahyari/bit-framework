﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;

namespace Bit.Owin.Implementations
{
    public class AutofacDependencyManager : IDependencyManager, IAutofacDependencyManager
    {
        private ContainerBuilder _containerBuilder;
        private ILifetimeScope _container;

        public virtual IDependencyManager Init()
        {
            UseContainerBuilder(new ContainerBuilder());
            return this;
        }

        public void UseContainerBuilder(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (_containerBuilder != null)
                throw new InvalidOperationException("Container builder has been set already");
            _containerBuilder = builder;
            _containerBuilder.Register((context, parameter) => (IDependencyManager)this).SingleInstance();
            _containerBuilder.Register((context, parameter) => (IServiceProvider)this).SingleInstance();
            _containerBuilder.Register((context, parameter) => (IAutofacDependencyManager)this).SingleInstance();
        }

        private IDependencyManager SetContainer(ILifetimeScope container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (_container != null)
                throw new InvalidOperationException("Container has been set already");
            _container = container;
            return this;
        }

        public virtual IDependencyManager BuildContainer()
        {
            SetContainer(_containerBuilder.Build());
            return this;
        }

        public virtual bool IsInited()
        {
            return _container != null;
        }

        public void UseContainer(ILifetimeScope lifetimeScope)
        {
            SetContainer(lifetimeScope);
        }

        public virtual ILifetimeScope GetContainer()
        {
            if (!IsInited())
                throw new InvalidOperationException("Container is not prepared, build it first.");

            return _container;
        }

        public virtual ContainerBuilder GetContainerBuidler()
        {
            if (_containerBuilder == null)
                throw new InvalidOperationException("Container builder is not prepared, Either call Init or UseContainerBuilder first");

            return _containerBuilder;
        }

        public virtual IDependencyResolver CreateChildDependencyResolver(Action<IDependencyManager> childDependencyManagerCustomizer = null)
        {
            IAutofacDependencyManager childDependencyManager = new AutofacDependencyManager();

            ILifetimeScope container = GetContainer().BeginLifetimeScope(containerBuilder =>
            {
                if (childDependencyManagerCustomizer != null)
                {
                    childDependencyManager.UseContainerBuilder(containerBuilder);
                    childDependencyManagerCustomizer((IDependencyManager)childDependencyManager);
                }
            });

            childDependencyManager.UseContainer(container);

            return (IDependencyResolver)childDependencyManager;
        }

        public virtual TService Resolve<TService>(string name = null)
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<TService>(name);

            return container.Resolve<TService>();
        }

        public virtual IEnumerable<TService> ResolveAll<TService>(string name = null)
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<IEnumerable<TService>>(name);

            return container.Resolve<IEnumerable<TService>>();
        }

        public virtual TService ResolveOptional<TService>(string name = null)
            where TService : class
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<TService>(name);

            return container.ResolveOptional<TService>();
        }

        public virtual object Resolve(TypeInfo serviceType, string name = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed(name, serviceType);

            return container.Resolve(serviceType);
        }

        public virtual object ResolveOptional(TypeInfo serviceType, string name = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed(name, serviceType);

            return container.ResolveOptional(serviceType);
        }

        public virtual IEnumerable<object> ResolveAll(TypeInfo serviceType, string name = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return (IEnumerable<object>)container.ResolveNamed(name, serviceType);

            return (IEnumerable<object>)container.Resolve(serviceType);
        }

        public virtual object GetService(TypeInfo serviceType)
        {
            return GetContainer().ResolveOptional(serviceType);
        }

        public virtual object GetService(Type serviceType)
        {
            return GetContainer().ResolveOptional(serviceType);
        }

        public virtual IDependencyManager Register<TService, TImplementation>(string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExciting = true)
            where TImplementation : class, TService
        {
            return Register(new[] { typeof(TService).GetTypeInfo() }, typeof(TImplementation).GetTypeInfo(), name, lifeCycle, overwriteExciting);
        }

        public virtual IDependencyManager RegisterInstance<TService>(TService obj, bool overwriteExciting = true, string name = null)
            where TService : class
        {
            return RegisterInstance(obj, new[] { typeof(TService).GetTypeInfo() }, overwriteExciting, name);
        }

        public virtual IDependencyManager RegisterAssemblyTypes(Assembly[] assemblies, Predicate<TypeInfo> predicate = null)
        {
            GetContainerBuidler().RegisterAssemblyTypes(assemblies)
                .Where(t => predicate == null || predicate(t.GetTypeInfo()))
                .PropertiesAutowired(wiringFlags: PropertyWiringOptions.PreserveSetValues);

            return this;
        }

        /// <summary>
        /// Register an un-parameterised generic type, e.g. IRepository&lt;&gt;. Concrete types will be made as they are requested, e.g. with IRepository&lt;Customer&gt;
        /// </summary>
        public IDependencyManager RegisterGeneric(TypeInfo serviceType, TypeInfo implementationType, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance)
        {
            return RegisterGeneric(new[] { serviceType }, implementationType, lifeCycle);
        }

        public IDependencyManager RegisterGeneric(TypeInfo[] servicesType, TypeInfo implementationType, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance)
        {
            IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle> registration = GetContainerBuidler().RegisterGeneric(implementationType)
                .PropertiesAutowired(wiringFlags: PropertyWiringOptions.PreserveSetValues)
                .As(servicesType);

            if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager RegisterUsing<T>(Func<T> factory, string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExciting = true)
        {
            return RegisterUsing(() => factory(), new[] { typeof(T).GetTypeInfo() }, name, lifeCycle, overwriteExciting);
        }

        public virtual IDependencyManager RegisterUsing(Func<object> factory, TypeInfo serviceType, string name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExciting = true)
        {
            return RegisterUsing(factory, new[] { serviceType }, name, lifeCycle, overwriteExciting);
        }

        public virtual IDependencyManager RegisterUsing(Func<object> factory, TypeInfo[] servicesType, string name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExciting = true)
        {
            IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> registration = GetContainerBuidler().Register((context, parameter) => factory.DynamicInvoke())
                .As(servicesType);

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
            {
                foreach (TypeInfo serviceType in servicesType)
                    registration = registration.Named(name, serviceType);
            }

            if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager Register(TypeInfo serviceType, TypeInfo implementationType, string name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExciting = true)
        {
            return Register(new[] { serviceType }, implementationType, name, lifeCycle, overwriteExciting);
        }

        public virtual IDependencyManager Register(TypeInfo[] servicesType, TypeInfo implementationType, string name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExciting = true)
        {
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            if (servicesType == null)
                throw new ArgumentNullException(nameof(servicesType));

            IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration = GetContainerBuidler().RegisterType(implementationType)
                    .PropertiesAutowired(wiringFlags: PropertyWiringOptions.PreserveSetValues)
                    .As(servicesType);

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
            {
                foreach (TypeInfo serviceType in servicesType)
                    registration = registration.Named(name, serviceType);
            }

            if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager RegisterInstance(object obj, TypeInfo serviceType, bool overwriteExciting = true, string name = null)
        {
            return RegisterInstance(obj, new [] { serviceType }, overwriteExciting, name);
        }

        public virtual IDependencyManager RegisterInstance(object obj, TypeInfo[] servicesType, bool overwriteExciting = true, string name = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> registration = GetContainerBuidler().RegisterInstance(obj).As(servicesType);

            if (name != null)
            {
                foreach (TypeInfo serviceType in servicesType)
                    registration = registration.Named(name, serviceType);
            }

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            return this;
        }

        public virtual bool IsRegistered<TService>()
        {
            return GetContainer().IsRegistered<TService>();
        }

        public virtual bool IsRegistered(TypeInfo serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            return GetContainer().IsRegistered(serviceType);
        }

        public virtual void Dispose()
        {
            _container?.Dispose();
        }
    }
}
