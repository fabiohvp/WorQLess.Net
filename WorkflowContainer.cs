using Enflow;
using System;
using System.Collections.Generic;
using System.Linq;
using WorQLess.Boosters;
using WorQLess.Extensions;
using WorQLess.Requests;

namespace WorQLess
{
    public interface IWorkflowContainer
    {
        string Entity { get; set; }
        string Name { get; set; }
        object Args { get; set; }
        bool Evaluate { get; set; }
        WorkflowOperand Operand { get; set; }

        object Execute(Type sourceType, dynamic data, dynamic lastResult);

        IWorkflowRequest Request { get; set; }
        IEnumerable<IRuleContainer> Rules { get; set; }

        IWorQLessDynamic Projection { get; }

        IQueryable<T> ApplyRules<T>(IQueryable<T> collection);
        IEnumerable<T> ApplyRules<T>(IEnumerable<T> collection);
        IQueryable<U> ApplyProjection<T, U>(IQueryable<T> collection);
        IEnumerable<U> ApplyProjection<T, U>(IEnumerable<T> collection);
        IEnumerable<X> ApplyEvaluate<X>(IQueryable<X> collection);
        IEnumerable<X> ApplyEvaluate<X>(IEnumerable<X> collection);
    }

    public class WorkflowContainer : IWorkflowContainer
    {
        public virtual string Entity { get; set; }
        public virtual IWorkflowRequest Request { get; set; }
        public virtual string Name { get; set; }
        public virtual object Args { get; set; }
        public virtual bool Evaluate { get; set; }
        public virtual WorkflowOperand Operand { get; set; }
        public virtual IEnumerable<IRuleContainer> Rules { get; set; }

        public IWorQLessDynamic Projection { get; private set; }


        public WorkflowContainer(IWorkflowRequest request)
        {
            var rules = new List<IRuleContainer>();

            foreach (var _rule in request.Rules)
            {
                rules.Add(new RuleContainer(_rule));
            }

            if (!string.IsNullOrEmpty(request.ProjectAs))
            {
                request.Project = new ProjectionRequest { Name = request.ProjectAs };
            }

            Name = request.Name;
            Entity = request.Entity;
            Args = request.Args;
            Evaluate = request.Evaluate;
            Operand = request.Operand;
            Request = request;
            Rules = rules;
        }

        public Type GetQueryType<T>()
        {
            return typeof(IQueryable<T>);
        }

        internal class SwapVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            private readonly System.Linq.Expressions.Expression _source, _replacement;

            public SwapVisitor(System.Linq.Expressions.Expression source, System.Linq.Expressions.Expression replacement)
            {
                _source = source;
                _replacement = replacement;
            }

            public override System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression node)
            {
                return node == _source ? _replacement : base.Visit(node);
            }
        }

        public IFieldExpression Chain<TIn, TInterstitial, TOut>(IFieldExpression _inner, IFieldExpression _outer, System.Linq.Expressions.ParameterExpression initialParameter)
        {
            var inner = _inner.GetLambdaExpression<TIn, TInterstitial>();
            var outer = _outer.GetLambdaExpression<TInterstitial, TOut>();
            var visitor = new SwapVisitor(outer.Parameters[0], inner.Body);
            var expression = visitor.Visit(outer.Body);
            return new FieldExpression(expression, initialParameter);
        }

        public object Execute(Type sourceType, dynamic data, dynamic lastResult)
        {
            var returnType = sourceType;

            if (!string.IsNullOrEmpty(Request.Project?.Name))
            {
                if (Request.Project.Name == "Select")
                {
                    var fields = new Dictionary<string, IFieldExpression>();
                    var booster = new SelectBooster();
                    var queryType = (Type)typeof(WorkflowContainer)
                        .GetMethod(nameof(GetQueryType))
                        .MakeGenericMethod(sourceType)
                        .Invoke(this, null);

                    var initialParameter = System.Linq.Expressions.Expression.Parameter(queryType);
                    System.Linq.Expressions.Expression body = initialParameter;
                    var jArray = (Newtonsoft.Json.Linq.JArray)Request.Project.Args;
                    var jArray2 = new Newtonsoft.Json.Linq.JArray(jArray.First());

                    var x = booster.Select(WQL.TypeCreator, sourceType, jArray2, body, initialParameter);

                    var z = new Newtonsoft.Json.Linq.JArray(jArray.Skip(1));


                    var fieldExpression = WQL.TypeCreator.BuildExpression(x.Expression.Type, z, false);
                    returnType = fieldExpression.Type;


                    fieldExpression = (IFieldExpression)typeof(WorkflowContainer)
                        .GetMethod(nameof(Chain))
                        .MakeGenericMethod(sourceType, x.Type, returnType)
                        .Invoke(this, new object[] { x, fieldExpression, initialParameter });

                    Projection = (IWorQLessDynamic)Reflection
                        .CreateProjection(sourceType, Request.Project.Name, new Type[] { sourceType, returnType }, Request.Project.Args);
                    Projection.FieldExpression = fieldExpression;
                }
                else
                {
                    var projectionInterface = Request.Project.Type
                        .GetInterfaces()
                        .FirstOrDefault(o =>
                            o.IsGenericType
                            && o.GetGenericTypeDefinition() == typeof(IProjection<,>)
                        );

                    if (projectionInterface != null)
                    {
                        returnType = projectionInterface.GenericTypeArguments.LastOrDefault();
                    }

                    Projection = (IWorQLessDynamic)Reflection
                        .CreateProjection(sourceType, Request.Project.Name, new Type[] { sourceType, returnType }, Request.Project.Args);

                    if (Request.Project != null)
                    {
                        if (Request.Project is IWorQLessWorkflowContainer)
                        {
                            ((IWorQLessWorkflowContainer)Request.Project).WorkflowContainer = this;
                        }
                    }
                }
            }

            var workflow = Reflection
                .CreateWorkflow(sourceType, Name, new Type[] { sourceType, returnType }, Args);

            if (workflow is IWorQLessWorkflowContainer)
            {
                ((IWorQLessWorkflowContainer)workflow).WorkflowContainer = this;
            }

            if (workflow is IWorQLessWorkflowJoin)
            {
                ((IWorQLessWorkflowJoin)workflow).PreviousWorkflowContainerResult = lastResult;
            }

            var execute = workflow
                .GetType()
                .GetMethod(nameof(Enflow.IWorkflow<object, object>.Execute));

            try
            {
                var result = execute
                    .Invoke(workflow, new object[] { data });

                return result;
            }
            catch (Exception ex)
            {
                throw ex?.InnerException ?? ex;
            }
        }

        public IQueryable<T> ApplyRules<T>(IQueryable<T> collection)
        {
            if (Rules.Any())
            {
                var predicate = Rules
                    .GetStateRule<T>()
                    .Predicate;
                collection = collection
                    .Where(predicate);
            }

            return collection;
        }

        public IEnumerable<T> ApplyRules<T>(IEnumerable<T> collection)
        {
            if (Rules.Any())
            {
                var predicate = Rules
                    .GetStateRule<T>()
                    .Predicate
                    .Compile();
                collection = collection
                    .Where(predicate);
            }

            return collection;
        }

        public IQueryable<U> ApplyProjection<T, U>(IQueryable<T> collection)
        {
            if (string.IsNullOrEmpty(Request.Project?.Name))
            {
                return (IQueryable<U>)collection;
            }

            var predicate = ((IProjection<T, U>)Projection)
                .Predicate;
            var retorno = collection
                .Select(predicate);

            return retorno;
        }

        public IEnumerable<U> ApplyProjection<T, U>(IEnumerable<T> collection)
        {
            if (string.IsNullOrEmpty(Request.Project?.Name))
            {
                return (IEnumerable<U>)collection;
            }

            var predicate = ((IProjection<T, U>)Projection)
                .Predicate
                .Compile();
            var retorno = collection
                .Select(predicate);

            return retorno;
        }

        public IEnumerable<X> ApplyEvaluate<X>(IQueryable<X> collection)
        {
            if (Evaluate)
            {
                return collection
                    .Take(WQL.Limit)
                    .ToList();
            }

            return collection;
        }

        public IEnumerable<X> ApplyEvaluate<X>(IEnumerable<X> collection)
        {
            if (Evaluate)
            {
                return collection
                    .Take(WQL.Limit)
                    .ToList();
            }

            return collection;
        }
    }
}