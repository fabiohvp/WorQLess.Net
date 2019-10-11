using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using WorQLess.Attributes;
using WorQLess.Models;

namespace WorQLess.Extensions
{
    public class Reflection
    {
        private static object CreateInstance(Type instanceType, object @params)
        {
            if (instanceType.GetCustomAttribute<ExposeAttribute>() == null)
            {
                throw new InvalidOperationException($"{instanceType.Name} is not exposed");
            }

            var instance = Activator.CreateInstance(instanceType);

            if (@params != null)
            {
                if (@params is JArray)
                {
                    var property = instanceType
                        .GetProperty(nameof(IRawArguments.Arguments));

                    property
                        .SetValue(instance, @params);
                }
                else
                {
                    var args = ((JObject)@params).Properties();

                    try
                    {
                        foreach (var arg in args)
                        {
                            var property = instanceType
                                .GetProperty(arg.Name);
                            var value = arg.Value
                                .ToObject(property.PropertyType);

                            property
                                .SetValue(instance, value);
                        }
                    }
                    catch (NullReferenceException)
                    {
                        throw GetMismatchException(instanceType);
                    }
                    catch (JsonSerializationException)
                    {
                        throw GetMismatchException(instanceType);
                    }
                }
            }

            return instance;
        }

        private static object CreateInstance(Type sourceType, Type targetType, Type[] genericTypes, object @params)
        {
            if (targetType.ContainsGenericParameters)
            {
                var constructorTypesCount = targetType
                    .GetGenericArguments()
                    .Length;

                genericTypes = genericTypes
                    .Take(constructorTypesCount)
                    .ToArray();

                for (var i = 0; i < genericTypes.Length; i++)
                {
                    if (genericTypes[i].IsGenericTypeDefinition)
                    {
                        genericTypes[i] = MakeGenericType(genericTypes[i], sourceType);
                    }
                }

                targetType = targetType
                    .MakeGenericType(genericTypes);
            }

            return CreateInstance(targetType, @params);
        }

        private static object CreateInstance(IDictionary<string, Type> types, Type sourceType, string name, Type[] genericTypes, object @params)
        {
            var targetType = GetTypeof(types, name);
            return CreateInstance(sourceType, targetType, genericTypes, @params);
        }

        public static object CreateProjection(Type sourceType, string name, Type[] genericTypes, object @params)
        {
            return CreateInstance(WQL.ProjectionsTypes, sourceType, name, genericTypes, @params);
        }

        public static object CreateRule(Type sourceType, string name, Type[] genericTypes, object @params)
        {
            return CreateInstance(WQL.RulesTypes, sourceType, name, genericTypes, @params);
        }

        public static object CreateWorkflow(Type sourceType, string name, Type[] genericTypes, object @params)
        {
            return CreateInstance(WQL.WorkflowsTypes, sourceType, name, genericTypes, @params);
        }

        internal static Type GetTypeof(IDictionary<string, Type> types, string name)
        {
            if (name == default(string))
            {
                name = nameof(Queryable.Select);
            }

            var type = types
                .Where(o => o.Key.EndsWith(name))
                .Select(o => o.Value)
                .FirstOrDefault();
            return type;
        }

        private static Type MakeGenericType(Type sourceType, Type parameter)
        {
            var definitionStack = new Stack<Type>();
            var type = sourceType;
            while (!type.IsGenericTypeDefinition)
            {
                definitionStack.Push(type.GetGenericTypeDefinition());
                type = type.GetGenericArguments()[0];
            }
            type = type.MakeGenericType(parameter);
            while (definitionStack.Count > 0)
                type = definitionStack.Pop().MakeGenericType(type);
            return type;
        }

        private static FieldAccessException GetMismatchException(Type instanceType)
        {
            var dic = instanceType
                .GetProperties()
                .Where(o =>
                    o.GetCustomAttribute<NotExposeAttribute>() == null
                    && o.Name != nameof(Enflow.IStateRule<object>.Predicate)
                    && o.Name != nameof(Enflow.IStateRule<object>.Description)
                );

            var availableTypes = new StringBuilder();

            foreach (var item in dic)
            {
                var genericArguments = item.PropertyType.GetGenericArguments();
                var genericArgumentsDescription = string.Empty;

                if (genericArguments.Any())
                {
                    genericArgumentsDescription = $"<{string.Join(", ", genericArguments.Select(o => o.Name))}>";
                }

                availableTypes.AppendLine($"{item.PropertyType.Name}{genericArgumentsDescription} {item.Name}");
            }

            return new FieldAccessException($"Properties mismatch, available properties from {instanceType.Name} are: {Environment.NewLine}{availableTypes.ToString()}");
        }
    }
}