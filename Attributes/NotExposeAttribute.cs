using System;

namespace WorQLess
{

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class NotExposeAttribute : Attribute
	{
	}
}