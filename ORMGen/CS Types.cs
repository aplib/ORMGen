using System;
using System.Collections.Concurrent;
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

        static ConcurrentDictionary<Type, string> type_names = new(new Dictionary<Type, string>()
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

            { typeof(string), "string" },

            { typeof(DateTime), "DateTime" },
            { typeof(DateTime?), "DateTime?" },

        });

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
        /// Build ORMTableInfo class instance from dynamic query result with default provider binding
        /// </summary>
        /// <param name="query_result">IEnumerable&lt;dynamic&gt;dapper query result</param>
        /// <param name="table_name">Table name</param>
        /// <param name="As">as short name</param>
        /// <returns>ORMTableInfo</returns>
        public static ORMTableInfo ORMFromDynamicQueryResult(IEnumerable<dynamic> query_result, string table_name, string As = null)
        {
            var columnset = new List<column_set_item>();
            var first_round = true;
            foreach (var row in query_result)
            {
                if (first_round)
                {
                    first_round = false;
                    foreach (var pair in row)
                        columnset.Add(new column_set_item() { Name = row.Key });
                }
                var i = 0;
                foreach (var pair in row)
                {
                    var colname = pair.Key;
                    var column_value = pair.Value;
                    columnset[i].Name = colname;

                    if (column_value == null)
                        columnset[i].Nullable = true;
                    else
                        columnset[i].Type = column_value.GetType();

                    i++;
                }
            }

            for (int i = 0, c = columnset.Count; i < c; i++)
            {
                Type coltype = columnset[i].Type;
                if (coltype == null)
                {
                    Console.WriteLine(columnset[i].Name + " no values, no type defined! Default: nullable string?");
                    columnset[i].Type = typeof(string);
                }
            }

            var orm = new ORMTableInfo();

            // Append attributes

            var for_table_name = ORMHelper.RemoveBrackets(table_name);
            orm.Name = ORMHelper.ToValidNameRegex.Replace(for_table_name, "_");
            orm.TableName = for_table_name;
            orm.As = As;
            orm.Title = ORMGen.ORMHelper.ByViewRule(orm.Name, orm.Rules);

            orm.Props = columnset.Select(row =>
            {
                var property_name = ORMHelper.ToValidNameRegex.Replace(row.Name, "_");

                var value_type = row.Type;
                if ((value_type.IsValueType || value_type == typeof(DateTime)) && row.Nullable)
                    value_type = Type.GetType(value_type.CSTypeSyntax() + "?");

                var orm_pi = new ORMPropertyInfo(orm, property_name, row.Type)
                {
                    Field = ORMHelper.ByDBRule(property_name, orm.Rules),
                    Title = ORMHelper.ByViewRule(property_name, orm.Rules)
                };
                return orm_pi;
            }).ToArray();

            return orm;
        }

        class column_set_item
        {
            public string Name;
            public Type Type;
            public bool Nullable;
        }

        /// <summary>
        /// Build ORMTable data model source code
        /// </summary>
        /// <param name="orm">ORMTableInfo data model</param>
        /// <param name="table_name">Name for generate code</param>
        /// <param name="output_type">Typ of output structure</param>
        /// <returns></returns>
        public static string GenORMTableTypeCode(ORMTableInfo orm, string table_name, GenerateTypeNameEnum output_type = GenerateTypeNameEnum.Class)
        {
            ORMRulEnum current_rules = orm.Rules;

            var sb = new StringBuilder();

            var for_table_name = ORMHelper.RemoveBrackets(table_name);
            var generate_name = ORMHelper.ToValidNameRegex.Replace(for_table_name, "_");
            if (generate_name.Blank())
                throw new ArgumentException("Unassigned or invalid table name");
            var generate_type = Enum.GetName(output_type).ToLower();

            // Append attributes

            sb.AppendLine($"[ORMRuleSwitcher(ORMRulEnum.{Enum.GetName(current_rules & ORMRulEnum.__ViewMask) ?? "ViewHumanitaize"}, ORMRulEnum.{Enum.GetName(current_rules & ORMRulEnum.__DBMask) ?? "DBReplaceUnderscoresWithSpaces"})]");

            var values = new List<string>(5);

            if (ORMHelper.ByViewRule(generate_name, current_rules) != orm.Title)
                values.Add("Title = \"" + orm.Title + "\"");

            if (for_table_name != ORMHelper.ByDBRule(generate_name, current_rules))
                values.Add((string)("TableName = \"" + for_table_name + "\""));

            if (orm.As.notBlank()) values.Add("As = \"" + orm.As + "\"");
            if (orm.TryGetIdPropertyName(out var id_property_name)) values.Add("IdProperty = \"" + id_property_name + "\"");
            if (orm.TextProperty.notBlank() == true) values.Add("TextProperty = \"" + orm.TextProperty + "\"");
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

                if (orm_pi.Format.notBlank()) values.Add($@"Format = {orm_pi.Format}");
                if (orm_pi.isKey) values.Add($@"isKey = true");
                if (orm_pi.Readonly) values.Add($@"Readonly = true");
                if (orm_pi.Hide) values.Add($@"Hide = true");
                if (orm_pi.RefType != null) values.Add($@"RefType = typeof({orm_pi.RefType.CSTypeSyntax()})");

                if (values.Count > 0)
                    sb.AppendLine($"    [ORMProperty({string.Join(", ", values)})]");

                sb.AppendLine($"    public {orm_pi.Type.CSTypeSyntax()} {orm_pi.Name} {{ get; set; }}");
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

