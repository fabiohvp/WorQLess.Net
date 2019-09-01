using System.Collections.Generic;

namespace System
{
	internal static class TypeHelper
	{
		public static bool IsPrimitiveType(this Type type)
		{
			var realType = Nullable.GetUnderlyingType(type);

			if (realType == null)
			{
				realType = type;
			}

			return realType == typeof(String)
				|| realType == typeof(Char)
				|| realType == typeof(Boolean)
				|| realType == typeof(Byte)
				|| realType == typeof(Int16)
				|| realType == typeof(Int32)
				|| realType == typeof(Int64)
				|| realType == typeof(UInt16)
				|| realType == typeof(UInt32)
				|| realType == typeof(UInt64)
				|| realType == typeof(IntPtr)
				|| realType == typeof(Single)
				|| realType == typeof(Double)
				|| realType == typeof(Decimal);
		}
	}
}