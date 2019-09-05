using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using WorQLess.Boosters;
using WorQLess.Extensions;
using WorQLess.Models;
using WorQLess.Net.Extensions;
using WorQLess.Requests;
using WorQLess.Rules;
using WorQLess.Workflows;

namespace WorQLess
{
    public class WQL
    {
        public static TypeCreator TypeCreator;
        public static string BoosterPrefix;
        internal static IDictionary<string, IBooster> Boosters;
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
            Boosters = new Dictionary<string, IBooster>
            {
                { BoosterPrefix + "as", new AsBooster() },
                { BoosterPrefix + "count", new CountBooster() },
				//new KeyValuePair<string, IBooster>(BoosterPrefix + "implements", new ImplementsBooster()), //not working yet
				{ BoosterPrefix + "orderByAsc", new OrderByAscBooster() },
                { BoosterPrefix + "orderByDesc", new OrderByDescBooster() },
                { BoosterPrefix + "projectAs", new ProjectAsBooster() },
				//new KeyValuePair<string, IBooster>(WQL.BoosterPrefix + "selectMany", new SelectManyBooster()}, //parameter is wrong
				{ BoosterPrefix + "select", new SelectBooster() },
                { BoosterPrefix + "sum", new SumBooster() },
                { BoosterPrefix + "take", new TakeBooster() },
                { BoosterPrefix + "where", new WhereBooster() }
            };
            ProjectionsTypes = new Dictionary<string, Type>
            {
                { BoosterPrefix + "Select", typeof(SelectProjection<,>)}
            };
            RulesTypes = new Dictionary<string, Type>
            {
                { "==", typeof(EqualRule<>) },
                { "<=", typeof(LessThanOrEqualRule<>) },
                { "<", typeof(LessThanRule<>) },
                { ">=", typeof(GreaterThanOrEqualRule<>) },
                { ">", typeof(GreaterThanRule<>) }
            };
            WorkflowsTypes = new Dictionary<string, Type>
            {
                { BoosterPrefix + "Select", typeof(SelectWorkflow<,>)}
            };
            GetTableMethod = typeof(WQL).GetMethod(nameof(GetTable), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public WQL(object context)
        {
            Context = (DbContext)context;
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

        public object Execute(IWorkflowContainer workflow, dynamic data)
        {
            dynamic lastResult = data;

            if (workflow.Operand != WorkflowOperand.UseLastResult || !string.IsNullOrEmpty(workflow.Entity))
            {
                var entityType = Tables[workflow.Entity.ToLower()];
                var setMethod = GetTableMethod.MakeGenericMethod(entityType);
                data = setMethod.Invoke(this, null);
            }

            var dataType = data.GetType();

            if (dataType.IsGenericType)
            {
                dataType = dataType.GenericTypeArguments[0];
            }

            data = workflow.Execute(dataType, data, lastResult);
            return data;
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