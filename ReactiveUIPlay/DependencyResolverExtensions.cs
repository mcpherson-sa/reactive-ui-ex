using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using ReactiveUI;
using Splat;


namespace ReactiveUIPlay2
{
    internal static class DependencyResolverExtensions
    {
        public static void RegisterViewModelsForModels(this IMutableDependencyResolver resolver, Assembly assembly)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var typesToRegister = from type in assembly.DefinedTypes.Where(x => !x.IsAbstract) 
                let @interface = type
                    .GetInterfaces()
                    .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IViewModelFor<>))
                where @interface != null
                select (type, @interface);

            foreach (var (type, @interface) in typesToRegister)
            {
                var contract = type.GetCustomAttribute<ViewContractAttribute>()?.Contract ?? string.Empty;
                var factory = TypeFactory(type);
                resolver.Register(factory, @interface, contract);
            }
        }

        private static Func<object> TypeFactory(TypeInfo typeInfo)
        {
            var parameterlessConstructor = typeInfo.DeclaredConstructors.FirstOrDefault(ci => ci.IsPublic && ci.GetParameters().Length == 0);
            if (parameterlessConstructor is null)
            {
                throw new Exception($"Failed to register type {typeInfo.FullName} because it's missing a parameterless constructor.");
            }

            return Expression.Lambda<Func<object>>(Expression.New(parameterlessConstructor)).Compile();
        }
    }
}
