using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore
{
    public static class QueryTypeConfigurationExtensions
    {
        private static readonly MethodInfo entityMethod = typeof(ModelBuilder).GetTypeInfo().GetMethods().Single(x => (x.Name == "Entity") && (x.IsGenericMethod == true) && (x.GetParameters().Length == 0));

        private static Type FindQueryType(Type type)
        {
            var interfaceType = type.GetInterfaces().First(x => (x.GetTypeInfo().IsGenericType == true) && (x.GetGenericTypeDefinition() == typeof(IQueryTypeConfiguration<>)));
            return interfaceType.GetGenericArguments().First();
        }

        private static readonly Dictionary<Assembly, IEnumerable<Type>> typesPerAssembly = new Dictionary<Assembly, IEnumerable<Type>>();

        public static ModelBuilder ApplyConfiguration<T>(this ModelBuilder modelBuilder, IQueryTypeConfiguration<T> configuration) where T : class
        {
            var entityType = FindQueryType(configuration.GetType());

            dynamic entityTypeBuilder = entityMethod
                .MakeGenericMethod(entityType)
                .Invoke(modelBuilder, new object[0]);

            configuration.Configure(entityTypeBuilder);

            return modelBuilder;
        }

        public static ModelBuilder AddQueryTypeConfigurationFromAssembly(this ModelBuilder modelBuilder, Assembly asm)
        {
            IEnumerable<Type> configurationTypes;

            if (typesPerAssembly.TryGetValue(asm, out configurationTypes) == false)
            {
                typesPerAssembly[asm] = configurationTypes = asm
                    .GetTypes()
                    .Where(x => (x.GetTypeInfo().IsClass == true) && (x.GetTypeInfo().IsAbstract == false) && (x.GetInterfaces().Any(y => (y.GetTypeInfo().IsGenericType == true) && (y.GetGenericTypeDefinition() == typeof(IQueryTypeConfiguration<>)))));
            }

            var configurations = configurationTypes.Select(x => Activator.CreateInstance(x));

            foreach (dynamic configuration in configurations)
            {
                ApplyConfiguration(modelBuilder, configuration);
            }

            return modelBuilder;
        }
    }
}
