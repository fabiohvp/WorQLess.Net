using System;
using WorQLess.Net.Extensions;

namespace WorQLess.Net.Requests
{
	public interface IProjectionRequest : IRequest
	{
		Type Type { get; }
	}

	public class ProjectionRequest : IProjectionRequest
	{
		public virtual string Name { get; set; }
		public virtual object Args { get; set; }

		public virtual Type Type
		{
			get
			{
				if (string.IsNullOrEmpty(Name))
				{
					return null;
				}

				return Reflection.GetTypeof(WQL.ProjectionsTypes, Name);
			}
		}
	}
}