//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Reflection;
//using System.Reflection.Emit;
//using WorQLess.Models;
//using WorQLess.Requests;

//namespace WorQLess.Extensions
//{
//    public class TypeCreator2 : IDisposable
//    {
//        private int Count = 0;
//        private Dictionary<string, Type> BuiltTypes;

//        public virtual ModuleBuilder ModuleBuilder { get; set; }

//        public TypeCreator2(string assemblyName)
//        {
//            BuiltTypes = new Dictionary<string, Type>();

//            var _assemblyName = new AssemblyName() { Name = assemblyName };
//            ModuleBuilder = AssemblyBuilder
//                .DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run)
//                .DefineDynamicModule(_assemblyName.Name);
//            //moduleBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
//        }

//        public virtual IFieldExpression BuildExpression(Type sourceType, JArray jArray, bool createAnonymousProjection = true, ParameterExpression parameter = null)
//        {
//            if (parameter == null)
//            {
//                parameter = Expression.Parameter(sourceType, string.Empty);
//            }

//            Expression body = parameter;

//            var fields = new Dictionary<string, IFieldExpression>();
//            GetExpressionFromArray(sourceType, fields, jArray, sourceType, body, parameter, "o");

//            if (!createAnonymousProjection && fields.Count == 1)
//            {
//                return fields[fields.Select(o => o.Key).First()];
//            }

//            var dynamicType = CreateInstanceType(fields);
//            body = CreateInstance(fields, dynamicType);
//            return new FieldExpression(body, parameter, dynamicType);
//        }

//        public virtual IFieldExpression BuildExpression(Type sourceType, IProjectionRequest projectionRequest, bool createAnonymousProjection = false)
//        {
//            var returnType = sourceType;

//            if (!string.IsNullOrEmpty(projectionRequest?.Name))
//            {
//                var projectionType = projectionRequest.Type;

//                if (typeof(IWorQLessDynamicProjection).IsAssignableFrom(projectionType))
//                {
//                    var args = (JArray)projectionRequest.Args;

//                    var z = BuildExpression(sourceType, args, createAnonymousProjection);

//                    return z;



//                    var queryType = typeof(IQueryable<>).MakeGenericType(sourceType);
//                    var parameter = Expression.Parameter(queryType);
//                    Expression body = parameter;

//                    var firstArg = new JArray(args.First());
//                    var booster = new SelectBooster();
//                    var selectProjection = booster.Boost2(WQL.TypeCreator, sourceType, firstArg, body, parameter);

//                    if (args.Count > 1)
//                    {
//                        var otherArgs = new JArray(args.Skip(1));
//                        var otherProjections = WQL.TypeCreator.BuildExpression(selectProjection.Expression.Type, otherArgs, false);

//                        return selectProjection.Combine(otherProjections, parameter);
//                    }
//                    else
//                    {
//                        selectProjection.Parameter = parameter;
//                    }

//                    return selectProjection;
//                }
//                else if (typeof(IWorQLessProjection).IsAssignableFrom(projectionType))
//                {
//                    return WQL.TypeCreator.BuildExpression(sourceType, (JArray)projectionRequest.Args);
//                }
//            }

//            return default(IFieldExpression);
//        }

//        public virtual IFieldExpression GetExpressionFromValue(Type sourceType, JValue jValue, Type type, Expression expression, ParameterExpression parameter)
//        {
//            var name = jValue.Value.ToString();
//            var propertyInfo = type.GetProperty(name);

//            if (!propertyInfo.PropertyType.IsPrimitiveType())
//            {
//                throw new InvalidOperationException($"{jValue.Path}.{name} cannot be queried directly, list the columns explicitly");
//            }
//            else if (propertyInfo.GetCustomAttribute<NotExposeAttribute>() != null)
//            {
//                throw new MissingFieldException($"{jValue.Path}.{name} does not exist or is not exposed");
//            }

//            var _expression = Expression.Property(expression, name);
//            return new FieldExpression(_expression, parameter);
//        }

//        public virtual IDictionary<string, IFieldExpression> GetExpressionFromObject(Type sourceType, JObject jObject, Type type, Expression expression, ParameterExpression parameter, string level)
//        {
//            var fields = new Dictionary<string, IFieldExpression>();
//            GetExpressionFromObject(sourceType, fields, jObject, type, expression, parameter, level);
//            return fields;
//        }

//        public virtual void GetExpressionFromObject(Type sourceType, IDictionary<string, IFieldExpression> fields, JObject jObject, Type type, Expression expression, ParameterExpression parameter, string level)
//        {
//            var properties = jObject.Properties();

//            foreach (var property in properties)
//            {
//                GetExpressionFromProperty(sourceType, fields, property, type, expression, parameter, level);
//            }
//        }

//        public virtual void GetExpressionFromProperty(Type sourceType, IDictionary<string, IFieldExpression> fields, JProperty property, Type type, Expression expression, ParameterExpression parameter, string level)
//        {
//            var propertyName = property.Name.TrimEnd('_');

//            if (WQL.Boosters.ContainsKey(propertyName))
//            {
//                WQL.Boosters[propertyName].Boost(this, sourceType, type, fields, property, expression, parameter);
//            }
//            else if (property.Value is JValue)
//            {
//                var jValue = (JValue)property.Value;
//                var fieldValue = GetExpressionFromValue(sourceType, jValue, type, expression, parameter);
//                fields.Add(property.Name, fieldValue);
//            }
//            else if (property.Value is JObject)
//            {
//                var _jObject = (JObject)property.Value;
//                var _fields = GetExpressionFromObject(sourceType, _jObject, type, expression, parameter, level);
//                var instanceType = CreateInstanceType(_fields);
//                var instance = CreateInstance(_fields, instanceType);

//                fields.Add
//                (
//                    property.Name,
//                    new FieldExpression(instance, parameter, instanceType)
//                );
//            }
//            else if (property.Value is JArray)
//            {
//                if (propertyName != "Key")
//                {
//                    if (type.GetProperty(property.Name).GetCustomAttribute<NotExposeAttribute>() != null)
//                    {
//                        throw new MissingFieldException($"{property.Path} does not exist or is not exposed");
//                    }
//                }

//                var _expression = Expression.Property(expression, property.Name);
//                var _type = type.GetProperty(property.Name).PropertyType;
//                var jArray = (JArray)property.Value;
//                var _fields = new Dictionary<string, IFieldExpression>();
//                GetExpressionFromArray(sourceType, _fields, jArray, _type, _expression, parameter, level + level);

//                foreach (var _field in _fields)
//                {
//                    fields.Add(_field);
//                }
//            }
//        }

//        public virtual void GetExpressionFromArray(Type sourceType, IDictionary<string, IFieldExpression> fields, JArray jArray, Type type, Expression expression, ParameterExpression parameter, string level)
//        {
//            foreach (var item in jArray)
//            {
//                var _fields = new Dictionary<string, IFieldExpression>();

//                if (item is JValue)
//                {
//                    var jValue = (JValue)item;
//                    var fieldValue = GetExpressionFromValue(sourceType, jValue, type, expression, parameter);
//                    _fields.Add(jValue.ToString(), fieldValue);
//                }
//                else if (item is JObject)
//                {
//                    GetExpressionFromObject(sourceType, _fields, (JObject)item, type, expression, parameter, level);
//                }
//                else if (item is JArray)
//                {
//                    GetExpressionFromArray(sourceType, _fields, (JArray)item, type, expression, parameter, level);
//                }

//                foreach (var _field in _fields)
//                {
//                    fields.Add(_field);
//                }
//            }
//        }

//        public virtual Expression CreateInstance(IDictionary<string, IFieldExpression> properties, Type dynamicType)
//        {
//            var members = properties
//                .ToDictionary(o => o.Key, o => o.Value);

//            var bindings = dynamicType
//                .GetProperties()
//                .Select(p => Expression.Bind(p, members[p.Name].Expression))
//                .OfType<MemberBinding>();

//            var instance = Expression.New(dynamicType.GetConstructor(Type.EmptyTypes));
//            var instanceWithProperties = Expression.MemberInit(instance, bindings);
//            return instanceWithProperties;
//        }

//        public virtual Type CreateInstanceType(IDictionary<string, IFieldExpression> properties)
//        {
//            var fields = properties
//                .ToDictionary(o => o.Key, o => o.Value.ReturnType);

//            var className = GetClassName(fields);

//            if (BuiltTypes.ContainsKey(className))
//            {
//                return BuiltTypes[className];
//            }

//            var typeBuilder = ModuleBuilder.DefineType
//            (
//                className,
//                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable
//            );

//            var interfaces = properties.SelectMany(o => o.Value.Interfaces);

//            foreach (var @interface in interfaces)
//            {
//                typeBuilder.AddInterfaceImplementation(@interface);
//            }

//            foreach (var field in fields)
//            {
//                FieldBuilder _field = typeBuilder.DefineField("m" + field.Key, field.Value, FieldAttributes.Private);
//                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(field.Key, PropertyAttributes.None, field.Value, null);

//                MethodAttributes getSetAttr = MethodAttributes.Public
//                    | MethodAttributes.HideBySig
//                    | MethodAttributes.SpecialName
//                    | MethodAttributes.Virtual;

//                MethodBuilder getter = typeBuilder.DefineMethod("get_" + field.Key, getSetAttr, field.Value, Type.EmptyTypes);

//                ILGenerator getIL = getter.GetILGenerator();
//                getIL.Emit(OpCodes.Ldarg_0);
//                getIL.Emit(OpCodes.Ldfld, _field);
//                getIL.Emit(OpCodes.Ret);

//                MethodBuilder setter = typeBuilder.DefineMethod("set_" + field.Key, getSetAttr, null, new Type[] { field.Value });

//                ILGenerator setIL = setter.GetILGenerator();
//                setIL.Emit(OpCodes.Ldarg_0);
//                setIL.Emit(OpCodes.Ldarg_1);
//                setIL.Emit(OpCodes.Stfld, _field);
//                setIL.Emit(OpCodes.Ret);

//                propertyBuilder.SetGetMethod(getter);
//                propertyBuilder.SetSetMethod(setter);
//            }

//            BuiltTypes[className] = typeBuilder.CreateTypeInfo();
//            return BuiltTypes[className];
//        }

//        public virtual string GetClassName(Dictionary<string, Type> fields, string nestedPrefix = "_")
//        {
//            return "Key_" + Count++;

//            //TODO: optimize the type caching -- if fields are simply reordered, that doesn't mean that they're actually different types, so this needs to be smarter
//            var key = string.Empty;
//            var ordered = fields.OrderBy(o => o.Key);

//            foreach (var field in ordered)
//            {
//                key += nestedPrefix + "@" + field.Key + "@" + field.Value.Name + "@";
//            }

//            return key;
//        }

//        public void Dispose()
//        {
//            BuiltTypes.Clear();
//            ModuleBuilder = null;
//        }
//    }
//}