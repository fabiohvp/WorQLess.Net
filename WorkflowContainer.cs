using System;
using System.Collections.Generic;
using System.Linq;
using Enflow;
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

		object Execute(Type sourceType, dynamic data);

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

		public object Execute(Type sourceType, dynamic data)
		{
			var returnType = sourceType;

			if (!string.IsNullOrEmpty(Request.Project?.Name))
			{
				if (Request.Project.Name == "Select")
				{
					var fieldExpression = WQL.TypeCreator.BuildExpression(sourceType, (Newtonsoft.Json.Linq.JArray)Request.Project.Args);
					returnType = fieldExpression.Type;

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