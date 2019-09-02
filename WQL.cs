using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using WorQLess.Requests;
using WorQLess.Rules;
using WorQLess.Workflows;

namespace WorQLess
{
    public static class Extensions2
    {
        public static List<PropertyInfo> GetDbSetProperties(this DbContext context)
        {
            var dbSetProperties = new List<PropertyInfo>();
            var properties = context.GetType().GetProperties();

            foreach (var property in properties)
            {
                var setType = property.PropertyType;

                var isDbSet = setType.IsGenericType && (typeof(IDbSet<>).IsAssignableFrom(setType.GetGenericTypeDefinition()) || setType.GetInterface(typeof(IDbSet<>).FullName) != null);

                if (isDbSet)
                {
                    dbSetProperties.Add(property);
                }
            }

            return dbSetProperties;
        }
    }

    public class WQL
    {
        public static TypeCreator TypeCreator;
        public static string BoosterPrefix;
        internal static IDictionary<string, Type> ProjectionsTypes;
        internal static IDictionary<string, Type> RulesTypes;
        internal static IDictionary<string, Type> WorkflowsTypes;
        private static MethodInfo GetTableMethod;

        public DbContext Context { get; protected set; }
        private IDictionary<string, Type> Tables = new Dictionary<string, Type>();

        public static int Limit;

        static WQL()
        {
            BoosterPrefix = "$";
            Limit = 500;

            TypeCreator = new TypeCreator("WorQLessDynamicAssembly");
            ProjectionsTypes = new Dictionary<string, Type>
            {
                { BoosterPrefix + "Select", typeof(SelectProjection<,>)}
            };
            RulesTypes = new Dictionary<string, Type>
            {
                { BoosterPrefix + "==", typeof(EqualRule<>) },
                { BoosterPrefix + "<=", typeof(LessThanOrEqualRule<>) },
                { BoosterPrefix + "<", typeof(LessThanRule<>) },
                { BoosterPrefix + ">=", typeof(GreaterThanOrEqualRule<>) },
                { BoosterPrefix + ">", typeof(GreaterThanRule<>) }
            };
            WorkflowsTypes = new Dictionary<string, Type>
            {
                { BoosterPrefix + "GroupBy", typeof(GroupByWorkflow<,>)},
                { BoosterPrefix + "Select", typeof(SelectWorkflow<,>)},
                { BoosterPrefix + "Take", typeof(TakeWorkflow<>)},
                { BoosterPrefix + "Where", typeof(WhereWorkflow<,>)}
            };
            GetTableMethod = typeof(WQL).GetMethod(nameof(GetTable), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public WQL(object context)
        {
            Context = (DbContext)context;
            //        Tables = context.GetDbSetProperties()
            //.Select(o => o.PropertyType)
            //.Where(o => o.GetCustomAttribute<NotExposeAttribute>() == null)
            //.ToDictionary(o => o.Name.ToLower(), o => o);

            var tables = Context.GetDbSetProperties();

            foreach (var table in tables)
            {
                var t = table.PropertyType.GetGenericArguments().First();

                if (t.GetCustomAttribute<NotExposeAttribute>() == null)
                {
                    Tables.Add(t.Name.ToLower(), t);
                }
            }
        }

        private IQueryable<T> GetTable<T>()
            where T : class
        {
            return Context.Set<T>();//.AsExpandable();
        }

        public object Execute(IWorkflowContainer workflow, dynamic lastResult)
        {
            if (workflow.Operand != WorkflowOperand.UseLastResult)
            {
                var entityType = Tables[workflow.Entity.ToLower()];
                var setMethod = GetTableMethod.MakeGenericMethod(entityType);
                lastResult = setMethod.Invoke(this, null);
            }

            var dataType = lastResult.GetType();

            if (dataType.IsGenericType)
            {
                dataType = dataType.GenericTypeArguments[0];
            }

            lastResult = workflow.Execute(dataType, lastResult);
            return lastResult;
        }

        public List<object> Execute(IEnumerable<IWorkflowRequest> requests)
        {
            var lastResultWasAdded = false;
            var results = new List<object>();
            var lastResult = default(object);

            foreach (var request in requests)
            {
                var workflow = new WorkflowContainer(request);

                if (workflow.Operand == WorkflowOperand.UseLastResult)
                {
                    if (lastResultWasAdded)
                    {
                        results.RemoveAt(results.Count - 1);
                    }
                }

                lastResult = Execute(workflow, lastResult);

                if (workflow.Evaluate && workflow.Operand != WorkflowOperand.FireAndForget)
                {
                    lastResultWasAdded = true;
                    results.Add(lastResult);
                }
                else
                {
                    lastResultWasAdded = false;
                }
            }

            return results;
        }

        public static void RegisterProjection(string name, Type type)
        {
            ProjectionsTypes.Add(name, type);
        }

        public static void RegisterRule(string name, Type type)
        {
            RulesTypes.Add(name, type);
        }

        public static void RegisterWorkflow(string name, Type type)
        {
            WorkflowsTypes.Add(name, type);
        }
    }
}