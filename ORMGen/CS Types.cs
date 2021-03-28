using System;
using System.Collections.Generic;
using System.Linq;

namespace ORMGen.Builders
{
    /// <summary>
    /// Common C# type extension
    /// </summary>
	public static partial class CSHelper
	{
		/// <summary>
		/// Check if type is nullable
		/// </summary>
		/// <param name="type">Type type</param>
		/// <returns>True if type is Nullable&lt;&gt;</returns>
		public static bool isNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;


        static Dictionary<Type, string> type_names = new()
        {
            { typeof(sbyte), "sbyte" },
            { typeof(byte), "byte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },

            { typeof(sbyte?), "sbyte?" },
            { typeof(byte?), "byte?" },
            { typeof(short?), "short?" },
            { typeof(ushort?), "ushort?" },
            { typeof(int?), "int?" },
            { typeof(uint?), "uint?" },
            { typeof(long?), "long?" },
            { typeof(ulong?), "ulong?" },

            { typeof(char), "char" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(bool), "bool" },
            { typeof(decimal), "decimal" },

            { typeof(char?), "char?" },
            { typeof(float?), "float?" },
            { typeof(double?), "double?" },
            { typeof(bool?), "bool?" },
            { typeof(decimal?), "decimal?" },

            { typeof(DateTime), "DateTime" },
            { typeof(DateTime?), "DateTime?" },

        };

        /// <summary>
        /// Get type name C# syntax
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>String Type name</returns>
        static string CSTypeSyntax(this Type type)
        {
            if (type_names.TryGetValue(type, out var name))
                return name;

            if (type.IsGenericType)
                return type_names[type] = type.Name.Split('`')[0] + string.Join(',', type.GetGenericArguments().Select(paramtype => CSTypeSyntax(paramtype)));

            return type.Name;
        }
    }

}

