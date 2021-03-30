using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        /// <summary>
        /// Indicates whether a specified string is null, empty, or consists only of white-space characters.
        /// calls string.IsNullOrWhiteSpace()
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>reault of string.IsNullOrWhiteSpace()</returns>
        public static bool Blank(this string str) => str == null || string.IsNullOrWhiteSpace(str);
        /// <summary>
        /// Indicates whether a specified string is not null and contains not white-space characters.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool notBlank(this string str) => str != null && !string.IsNullOrWhiteSpace(str);

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
        public static string CSTypeSyntax(this Type type)
        {
            if (type_names.TryGetValue(type, out var name))
                return name;

            if (type.IsGenericType)
                return type_names[type] = type.Name.Split('`')[0] + string.Join(',', type.GetGenericArguments().Select(paramtype => CSTypeSyntax(paramtype)));

            return type.Name;
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class ORMBuilder
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        /// <summary>
        /// Regex for convert name to valid CS name
        /// </summary>
        public static readonly Regex ToValidNameRegex = new Regex(@"[\W\s_\~\!\@\#\$\%\^\&\*\(\)\[\]]+");
        /// <summary>
        /// Build ORMTable data model source code
        /// </summary>
        /// <param name="orm">ORMTableInfo data model</param>
        /// <param name="table_name">Name for generate code</param>
        /// <param name="output_type">Typ of output structure</param>
        /// <returns></returns>
        public static string GenORMTableTypeCode(this ORMTableInfo orm, string table_name, GenerateTypeNameEnum output_type = GenerateTypeNameEnum.Class)
        {
            ORMRulEnum current_rules = orm.Rules;

            var sb = new StringBuilder();

            var for_table_name = table_name.Trim(' ', '[', ']', '"', '`');
            var generate_name = ToValidNameRegex.Replace(for_table_name, "_");
            if (generate_name.Blank())
                throw new ArgumentException("Unassigned or invalid table name");
            generate_name = ToValidNameRegex.Replace(generate_name, "_");
            var generate_type = Enum.GetName(output_type).ToLower();

            // Append attributes

            sb.AppendLine($"[ORMRuleSwitcher(ORMRulEnum.{Enum.GetName(current_rules & ORMRulEnum.__ViewMask) ?? "ViewHumanitaize"}, ORMRulEnum.{Enum.GetName(current_rules & ORMRulEnum.__DBMask) ?? "DBReplaceUnderscoresWithSpaces"})]");

            var values = new List<string>(5);

            if (ORMHelper.ByViewRule(generate_name, current_rules) != orm.Title)
                values.Add("Title = \"" + orm.Title + "\"");

            if (table_name != ORMHelper.ByDBRule(generate_name, current_rules))
                values.Add((string)("TableName = \"" + table_name + "\""));

            if (orm.As.notBlank()) values.Add("As = \"" + orm.As + "\"");
            if (orm.IdProperty.notBlank()) values.Add("IdProperty = \"" + orm.IdProperty + "\"");
            if (orm.TextProperty.notBlank()) values.Add("TextProperty = \"" + orm.TextProperty + "\"");
            if (orm.Readonly) values.Add("Readonly = true");

            if (values.Count == 0)
                sb.AppendLine("[ORMTable]");
            else
                sb.AppendLine($@"[ORMTable({string.Join(", ", values)})]");

            sb.AppendLine($"public partial {generate_type} {generate_name} //::generated");
            sb.AppendLine("{");
            foreach (var orm_pi in orm.Props)
            {
                values.Clear();

                var for_title = ORMHelper.ByViewRule(orm_pi.Name, current_rules);
                if (for_title != orm_pi.Name || orm_pi.Title != orm_pi.Name)
                    values.Add($@"Title = ""{orm_pi.Title ?? for_title}""");

                var for_field = ORMHelper.ByDBRule(orm_pi.Name, current_rules);
                if (for_field != orm_pi.Name || orm_pi.Field != orm_pi.Name)
                    values.Add($@"Field = ""{orm_pi.Field ?? for_field}""");

                if (orm_pi.isKey) values.Add($@"isKey = true");
                if (orm_pi.Readonly) values.Add($@"Readonly = true");
                if (orm_pi.Hide) values.Add($@"Hide = true");
                if (orm_pi.RefType != null) values.Add($@"RefType = typeof({orm_pi.RefType.CSTypeSyntax()})");

                if (values.Count > 0)
                    sb.AppendLine($"    [ORMProperty({string.Join(", ", values)})]");

                sb.AppendLine($"    public {orm_pi.Type.CSTypeSyntax()} {orm_pi.Name}" + " { get; set; }");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }
        /// <summary>
        /// Output model type
        /// </summary>
        public enum GenerateTypeNameEnum
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        { Class, Struct, Record }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    }
}

