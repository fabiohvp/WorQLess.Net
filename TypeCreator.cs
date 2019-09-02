using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using WorQLess.Boosters;

namespace WorQLess
{
    public class TypeCreator
    {
        public static List<KeyValuePair<string, IBooster>> Boosters;
        private Dictionary<string, Type> BuiltTypes;

        public virtual ModuleBuilder ModuleBuilder { get; set; }

        static TypeCreator()
        {
            Boosters = new List<KeyValuePair<string, IBooster>>
            {
                new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "as", new AsBooster()),
                new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "count", new CountBooster()),
				//new KeyValuePair<string, IBooster>(BoosterPrefix + "implements", new ImplementsBooster()), //not working yet
				new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "orderByAsc", new OrderByAscBooster()),
                new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "orderByDesc", new OrderByDescBooster()),
                new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "projectAs", new ProjectAsBooster()),
				//new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "selectMany", new SelectManyBooster()), //parameter is wrong
				new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "select", new SelectBooster()),
                new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "sum", new SumBooster()),
                new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "take", new TakeBooster()),
                new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "where", new WhereBooster()),
            };
        }

        public TypeCreator(string assemblyName)
        {
            BuiltTypes = new Dictionary<string, Type>();

            var _assemblyName = new AssemblyName() { Name = assemblyName };
            ModuleBuilder = AssemblyBuilder
                .DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(_assemblyName.Name);
            //moduleBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
        }

        public IFieldExpression BuildExpression(Type sourceType, JArray jArray, bool x = true)
        {
            var initialParameter = Expression.Parameter(sourceType, string.Empty);
            Expression body = initialParameter;

            var fields = new Dictionary<string, IFieldExpression>();
            GetExpressionFromArray(sourceType, fields, jArray, sourceType, body, initialParameter, "o");

            if (x)
            {
                var dynamicType = CreateInstanceType(fields);
                body = CreateInstance(fields, dynamicType);
                return new FieldExpression(body, initialParameter, dynamicType);
            }
            return fields[fields.Select(o => o.Key).First()];
        }

        public IFieldExpression GetExpressionFromValue(Type sourceType, JValue jValue, Type type, Expression expression, ParameterExpression initialParameter)
        {
            var name = jValue.Value.ToString();
            var propertyInfo = type.GetProperty(name);

            if (!propertyInfo.PropertyType.IsPrimitiveType())
            {
                throw new InvalidOperationException($"{jValue.Path}.{name} cannot be queried directly, list the columns explicitly");
            }
            else if (propertyInfo.GetCustomAttribute<NotExposeAttribute>() != null)
            {
                throw new MissingFieldException($"{jValue.Path}.{name} does not exist or is not exposed");
            }

            var _expression = Expression.Property(expression, name);
            return new FieldExpression(_expression, initialParameter);
        }

        public Dictionary<string, IFieldExpression> GetExpressionFromObject(Type sourceType, JObject jObject, Type type, Expression expression, ParameterExpression initialParameter, string level)
        {
            var fields = new Dictionary<string, IFieldExpression>();
            var properties = jObject.Properties();

            foreach (var property in properties)
            {
                GetExpressionFromProperty(sourceType, fields, property, type, expression, initialParameter, level);
            }

            return fields;
        }

        public IBooster GetBooster(string name)
        {
            var booster = Boosters
                .Where(o => name.StartsWith(o.Key))
                .Select(o => o.Value)
                .FirstOrDefault();

            return booster;
        }

        public void GetExpressionFromProperty(Type sourceType, Dictionary<string, IFieldExpression> fields, JProperty property, Type type, Expression expression, ParameterExpression initialParameter, string level)
        {
            var booster = GetBooster(property.Name);

            if (booster != default(IBooster))
            {
                booster.Boost(this, sourceType, type, fields, property, expression, initialParameter);
            }
            else if (property.Value is JValue)
            {
                var jValue = (JValue)property.Value;
                var fieldValue = GetExpressionFromValue(sourceType, jValue, type, expression, initialParameter);
                fields.Add(property.Name, fieldValue);
            }
            else if (property.Value is JObject)
            {
                var _jObject = (JObject)property.Value;
                var _fields = GetExpressionFromObject(sourceType, _jObject, type, expression, initialParameter, level);
                var instanceType = CreateInstanceType(_fields);
                var instance = CreateInstance(_fields, instanceType);

                fields.Add
                (
                    property.Name,
                    new FieldExpression(instance, initialParameter, instanceType)
                );
            }
            else if (property.Value is JArray)
            {
                if (type.GetProperty(property.Name).GetCustomAttribute<NotExposeAttribute>() != null)
                {
                    throw new MissingFieldException($"{property.Path} does not exist or is not exposed");
                }

                var _expression = Expression.Property(expression, property.Name);
                var _type = type.GetProperty(property.Name).PropertyType;
                var jArray = (JArray)property.Value;
                var _fields = new Dictionary<string, IFieldExpression>();
                GetExpressionFromArray(sourceType, _fields, jArray, _type, _expression, initialParameter, level + level);

                foreach (var _field in _fields)
                {
                    fields.Add(_field.Key, _field.Value);
                }
            }
        }

        public void GetExpressionFromArray(Type sourceType, Dictionary<string, IFieldExpression> fields, JArray jArray, Type type, Expression expression, ParameterExpression initialParameter, string level)
        {
            foreach (var item in jArray)
            {
                if (item is JValue)
                {
                    var jValue = (JValue)item;
                    var fieldValue = GetExpressionFromValue(sourceType, jValue, type, expression, initialParameter);
                    fields.Add(jValue.ToString(), fieldValue);
                }
                else if (item is JObject)
                {
                    var _fields = GetExpressionFromObject(sourceType, (JObject)item, type, expression, initialParameter, level);

                    foreach (var _field in _fields)
                    {
                        fields.Add(_field.Key, _field.Value);
                    }
                }
                else if (item is JArray)
                {
                    GetExpressionFromArray(sourceType, fields, (JArray)item, type, expression, initialParameter, level);
                }
            }
        }

        public Expression CreateInstance(Dictionary<string, IFieldExpression> properties, Type dynamicType)
        {
            var members = properties
                .ToDictionary(o => o.Key, o => o.Value);

            var bindings = dynamicType
                .GetProperties()
                .Select(p => Expression.Bind(p, members[p.Name].Expression))
                .OfType<MemberBinding>();

            var instance = Expression.New(dynamicType.GetConstructor(Type.EmptyTypes));
            var instanceWithProperties = Expression.MemberInit(instance, bindings);
            return instanceWithProperties;
        }

        public Type CreateInstanceType(IDictionary<string, IFieldExpression> properties)
        {
            var fields = properties
                .ToDictionary(o => o.Key, o => o.Value.Type);

            var className = GetClassName(fields);

            if (BuiltTypes.ContainsKey(className))
            {
                return BuiltTypes[className];
            }

            var typeBuilder = ModuleBuilder.DefineType
            (
                className,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable
            );

            var interfaces = properties.SelectMany(o => o.Value.Interfaces);

            foreach (var @interface in interfaces)
            {
                typeBuilder.AddInterfaceImplementation(@interface);
            }

            foreach (var field in fields)
            {
                FieldBuilder _field = typeBuilder.DefineField("m" + field.Key, field.Value, FieldAttributes.Private);
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(field.Key, PropertyAttributes.None, field.Value, null);

                MethodAttributes getSetAttr = MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName
                    | MethodAttributes.Virtual;

                MethodBuilder getter = typeBuilder.DefineMethod("get_" + field.Key, getSetAttr, field.Value, Type.EmptyTypes);

                ILGenerator getIL = getter.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, _field);
                getIL.Emit(OpCodes.Ret);

                MethodBuilder setter = typeBuilder.DefineMethod("set_" + field.Key, getSetAttr, null, new Type[] { field.Value });

                ILGenerator setIL = setter.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Stfld, _field);
                setIL.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getter);
                propertyBuilder.SetSetMethod(setter);
            }

            BuiltTypes[className] = typeBuilder.CreateTypeInfo();
            return BuiltTypes[className];
        }

        public string GetClassName(Dictionary<string, Type> fields, string nestedPrefix = "_")
        {
            //TODO: optimize the type caching -- if fields are simply reordered, that doesn't mean that they're actually different types, so this needs to be smarter
            var key = string.Empty;
            var ordered = fields.OrderBy(o => o.Key);

            foreach (var field in ordered)
            {
                key += nestedPrefix + ";" + field.Key + ";" + field.Value.Name + ";";
            }

            return key;
        }
    }
}