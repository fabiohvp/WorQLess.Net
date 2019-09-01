using System;

namespace WorQLess.Net
{

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class NotExposeAttribute : Attribute
	{
	}
}