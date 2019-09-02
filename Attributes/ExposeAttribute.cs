using System;

namespace WorQLess.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class ExposeAttribute : Attribute
	{
	}
}