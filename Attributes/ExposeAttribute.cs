using System;

namespace WorQLess.Net.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class ExposeAttribute : Attribute
	{
	}
}