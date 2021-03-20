using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ORMGen
{
	/// <summary>ORM data mapping base class
	/// </summary>
	public class ORMTableInfo
	{
		/// <summary>
		/// Access to object type
		/// </summary>
		public Type Type;
		/// <summary>
		/// Title for data table
		/// </summary>
		public string Title { get; set; }
		/// <summary>
		/// Use for code generation
		/// </summary>
		public string As { get; set; }
		string _TableName { get; set; }
		/// <summary>
		/// Data table name
		/// </summary>
		public string TableName { get => DBFriendly(_TableName); set => _TableName = value; }
		/// <summary>
		/// The row identification of the database table, can be a key, composite key, hash property. If not specified using a class attribute, then a single key is selected. Otherwise, an exception is thrown.
		/// </summary>
		public string IdProperty { get => GetIdPropertyName(); set { idProperty = value; } }
		/// <summary>
		/// Text property that give a representation of the object
		/// </summary>
		public string TextProperty { get; set; }
		/// <summary>
		/// List of all properties
		/// </summary>
		public ORMPropertyInfo[] Props { get; set; }
		/// <summary>
		/// List of key properties
		/// </summary>
		public ORMPropertyInfo[] Keys { get; set; }
		/// <summary>
		/// List of reference properties
		/// </summary>
		public ORMPropertyInfo[] References { get; set; }

		string idProperty;
		string GetIdPropertyName()
		{
			if (idProperty == null && Keys.Length != 1)
				throw new NullReferenceException("Need to set 'IdProperty' ORM attribute value or define one key property");

			return idProperty ?? Keys[0].Name;
		}

		public static DBProviderEnum db_provider { get; internal set; } = DBProviderEnum.MSSql;

		public DBProviderEnum DBProvider { get; internal set; }
		/// <summary>
		/// Selecting the SQL data provider
		/// </summary>
		/// <param name="provider"></param>
		public void UseDBProvider(DBProviderEnum provider)
		{
			DBProvider = provider;
			DBFriendly = provider switch
			{
				DBProviderEnum.MSSql => MSSQLScriptBuilder.DBFriendly,
				DBProviderEnum.MySQL => MySQLScriptBuilder.DBFriendly,
				DBProviderEnum.OracleSQL => OracleSQLScriptBuilder.DBFriendly,
				DBProviderEnum.PostgreSQL => MSSQLScriptBuilder.DBFriendly
			};
		}
		public Func<string, string> DBFriendly { get; internal set; } = MSSQLScriptBuilder.DBFriendly;
		/// <summary>
		/// String formatting for compatibility with the provider
		/// </summary>
		/// <param name="Func<string, string> db_friendly_func"></param>
		public void UseDBFriendly(Func<string, string> db_friendly_func) => DBFriendly = db_friendly_func;
	}



	public static class ORMHelper
	{
		/// <summary>
		/// Select properties other than those listed
		/// </summary>
		/// <param name="filter">comma-separated property names to be excluded</param>
		/// <returns>Enumeration of properties</returns>
		public static IEnumerable<ORMPropertyInfo> Reject(this ORMTableInfo orm, string filter)
		{
			var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			return orm.Props.Where(orm_pi => !parts.Contains(orm_pi.Name));
		}
		/// <summary>
		/// Select of specific properties
		/// </summary>
		/// <param name="filter">comma-separated property names to be selected</param>
		/// <returns>Enumeration of properties</returns>
		public static IEnumerable<ORMPropertyInfo> Select(this ORMTableInfo orm, string filter)
		{
			var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			return orm.Props.Where(orm_pi => parts.Contains(orm_pi.Name));
		}
		/// <summary>
		/// Formatting a list of mapped properties/fields for select script (fields part)
		/// </summary>
		/// <returns>Fields part of select script</returns>
		public static string ForSelect(this IEnumerable<ORMPropertyInfo> props) => string.Join(",", props.Select(prop => prop.parent.DBFriendly(prop.Field) + " as " + prop.Name));
		/// <summary>
		/// Formatting a list of mapped properties passed from an object for select script condition (where part)
		/// </summary>
		/// <returns>(string) condition for where part</returns>
		public static string ForSelectConditionKeys(this IEnumerable<ORMPropertyInfo> props) => string.Join(" and ", props.Select(prop => prop.parent.DBFriendly(prop.Field) + "=@" + prop.Name));
		/// <summary>
		/// Formatting a list of mapped properties passed from an object for insert script (values part)
		/// </summary>
		/// <returns>(string) for values part</returns>
		public static string ForInsertValues(this IEnumerable<ORMPropertyInfo> props) => string.Join(",", props.Select(prop => "@" + prop.Name));
		/// <summary>
		/// Formatting a list of values passed from an object for update script (set part)
		/// </summary>
		/// <returns>(string) for set part</returns>
		public static string ForUpdateSet(this IEnumerable<ORMPropertyInfo> props) => string.Join(", ", props.Where(prop => !prop.isKey && !prop.Readonly).Select(prop => prop.parent.DBFriendly(prop.Field) + "=@" + prop.Name));

		/// <summary>
		/// Formatting a full properties list of mapped properties passed from an object for select script condition (where part)
		/// </summary>
		/// <returns>(string) condition for where part</returns>
		public static string ForSelectFields(ORMTableInfo orm) => string.Join(",", orm.Props.Select(prop => prop.parent.DBFriendly(prop.Field) + " as " + prop.Name));
		/// <summary>
		/// Formatting a key properties list for select script condition (where part)
		/// </summary>
		/// <returns>(string) condition for where part</returns>
		public static string ForSelectConditionsKeys(ORMTableInfo orm) => string.Join(" and ", orm.Keys.Select(prop => orm.DBFriendly(prop.Field) + "=@" + prop.Name));
		/// <summary>
		/// Formatting a full list of mapped properties passed from an object for insert script (values part)
		/// </summary>
		/// <returns>(string) for values part</returns>
		public static string ForInsertValues(ORMTableInfo orm) => string.Join(",", orm.Props.Select(prop => "@" + prop.Name));
		/// <summary>
		/// Formatting a full list of values passed from an object for update script (set part)
		/// </summary>
		/// <returns>(string) for set part</returns>
		public static string ForUpdateSet(ORMTableInfo orm) => string.Join(", ", orm.Props.Where(prop => !prop.isKey && !prop.Readonly).Select(prop => prop.parent.DBFriendly(prop.Field) + "=@" + prop.Name));

	}
	
	/// <summary>ORM data mapping generic class derived from base class
	/// filled with metadata when created>
	/// </summary>
	public class ORMTableInfo<T> : ORMTableInfo
	{
		/// <summary>
		/// Create a specified ORMTable filled from metadata object
		/// </summary>
		public ORMTableInfo()
		{
			Type = typeof(T);

			// Collect metadata

			TypeInfo table_type_info = Type.GetTypeInfo();

			var current_rules = ORMRulEnum.DBNameAsIs | ORMRulEnum.ForPresentHumanitaize;
			void SetRules(ORMRuleSwitcherAttribute attr, ORMRulEnum mask)
			{
				if ((attr.Rules & mask) != 0)
					current_rules = (current_rules & ~mask) | (attr.Rules & mask);
			};

			var table_attr = table_type_info.GetCustomAttribute<ORMTableAttribute>();
			if (table_attr == null)
				throw new Exception($"ORMGen ORMTableInfo<{table_type_info.Name}>(): Table(TableAttribute) need for type");

			// set class rules to current_rules
			foreach (var rule_attr in table_type_info.GetCustomAttributes<ORMRuleSwitcherAttribute>())
			{
				SetRules(rule_attr, ORMRulEnum.__DBMask);
				SetRules(rule_attr, ORMRulEnum.__ForPresentMask);
			}

			// SetRules rules
			TableName = table_attr.TableName ?? table_type_info.Name.AccordDBRule(current_rules & ORMRulEnum.__DBMask);
			Title = (table_attr.Title ?? table_type_info.Name).AccordPRRule(current_rules & ORMRulEnum.__ForPresentMask);
			IdProperty = table_attr.IdProperty;
			TextProperty = table_attr.TextProperty;
			As = table_attr.As ?? table_type_info.Name;
			As = Regex.Replace(As.Trim().ToLower(), @"[\W\s_\~\!\@\#\$\%\^\&\*\(\)\[\]]+", "_");

			// For all properties:

			var all_props_orm_info = new List<ORMPropertyInfo>();
			var props = table_type_info.GetProperties();

			foreach (var prop_info in props)
			{
				// apply property rules to current_rules
				foreach (var rule_attr in prop_info.GetCustomAttributes<ORMRuleSwitcherAttribute>())
				{
					SetRules(rule_attr, ORMRulEnum.__DBMask);
					SetRules(rule_attr, ORMRulEnum.__ForPresentMask);
				}

				// create ORMPropertyInfo
				var orm_pi = new ORMPropertyInfo() { parent = this, Type = prop_info.PropertyType, Name = prop_info.Name };

				// apply property attributes
				var orm_prop_attr = prop_info.GetCustomAttribute<ORMPropertyAttribute>();
				if (orm_prop_attr != null)
				{
					orm_pi.Field = orm_prop_attr.Field ?? prop_info.Name.AccordDBRule(current_rules & ORMRulEnum.__DBMask);
					orm_pi.Format = orm_prop_attr.Format;
					orm_pi.Title = orm_prop_attr.Title ?? prop_info.Name.AccordPRRule(current_rules & ORMRulEnum.__ForPresentMask);
					orm_pi.isKey = orm_prop_attr.isKey;
					orm_pi.Readonly = orm_prop_attr.Readonly;
					orm_pi.RefType = orm_prop_attr.RefType;
				}

				if (orm_pi.Title == null) orm_pi.Title = orm_pi.Name.AccordPRRule(current_rules & ORMRulEnum.__ForPresentMask);
				if (orm_pi.Field == null) orm_pi.Field = orm_pi.Name.AccordDBRule(current_rules & ORMRulEnum.__DBMask);

				all_props_orm_info.Add(orm_pi);
			}
			Props = all_props_orm_info.ToArray();
			Keys = all_props_orm_info.Where(prop => prop.isKey).ToArray();
			References = all_props_orm_info.Where(prop => prop.RefType != null).ToArray();
		}
	}

	internal static class InternalORMHelper
	{
		public static string AccordDBRule(this string name, ORMRulEnum rule) => rule switch
		{
			ORMRulEnum.DBReplaceUnderscoresWithSpaces => name.Replace('_', ' '),
			ORMRulEnum.DBRemoveUnderscoresAndCapitalize =>
				name.Split('_')
				.Where(part => !string.IsNullOrWhiteSpace(part))
				.Select(part => Char.ToUpper(part[0], CultureInfo.CurrentCulture) + part[1..])
				.ToString(),
			_ => name
		};

		public static string AccordPRRule(this string name, ORMRulEnum rule) => rule switch
		{
			ORMRulEnum.ForPresentUnderscoresReplaceSpaces => name.Replace('_', ' '),
			ORMRulEnum.ForPresentToTitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Replace('_', ' ')),
			ORMRulEnum.ForPresentHumanitaize => Humanitaize(name),
			_ => name
		};

		public static string Humanitaize(this string _name)
		{
			var name = _name.Replace('_', ' ');
			var sb = new StringBuilder(Char.ToUpper(name[0]));
			for (int i = 1, c = name.Length - 1; i < c; i++)
			{
				var _char = name[i];
				if (Char.IsDigit(_char))
					sb.Append(_char);
				else if (i > 0 && i < c && Char.IsUpper(_char) && Char.IsLower(name[i - 1]) && Char.IsLower(name[i + 1]))
				{ sb.Append(' '); sb.Append(Char.ToLowerInvariant(_char)); }
				else if (i > 0 && Char.IsLetter(name[i - 1]) && Char.IsDigit(_char))
				{ sb.Append(' '); sb.Append(_char); }
				else
					sb.Append(_char);
			}
			return name;
		}
	}

	/// <summary>
	/// ORMTable attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
	public class ORMTableAttribute : Attribute
	{
		/// <summary>
		/// Use title for codogeneration
		/// </summary>
		public string Title { get; init; }
		/// <summary>
		/// Use As for codogeneration
		/// </summary>
		public string As { get; init; }
		/// <summary>
		/// Use TableName for script building
		/// </summary>
		public string TableName { get; init; }
		/// <summary>
		/// Use IdProperty for codogeneration
		/// </summary>
		public string IdProperty { get; set; }
		/// <summary>
		/// Use TextProperty for codogeneration
		/// </summary>
		public string TextProperty { get; set; }
	}

	/// <summary>
	/// Object filled from metadata object describing the data table property and its mapping
	/// </summary>
	public class ORMPropertyInfo
	{
		internal ORMTableInfo parent;
		/// <summary>
		/// Type of value, use Type for codogeneration
		/// </summary>
		public Type Type { get; internal set; }
		/// <summary>
		/// Name of property, use Name for codogeneration
		/// </summary>
		public string Name { get; internal set; }
		/// <summary>
		/// Title of property value, use Title for codogeneration
		/// </summary>
		public string Title { get; internal set; }
		/// <summary>
		/// Mapping to a field of data table, use Field for script building
		/// </summary>
		public string Field { get; internal set; }
		/// <summary>
		/// Format for property value, use Format for codogeneration
		/// </summary>
		public string Format { get; internal set; }
		/// <summary>
		/// It`s a key value, use isKey for codogeneration and script building
		/// </summary>
		public bool isKey { get; internal set; }
		/// <summary>
		/// Readonly means not writeable to database, for script building
		/// </summary>
		public bool Readonly { get; internal set; }
		/// <summary>
		/// Referencing tagged ORMTable or other data table classes, use RefType to co-generation
		/// </summary>
		public Type RefType { get; internal set; }
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMPropertyAttribute : Attribute
	{
		public string Title { get; init; }
		public string Field { get; init; }
		public string Format { get; internal set; }
		public bool isKey { get; init; }
		public bool Readonly { get; init; }
		public Type RefType { get; init; }
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMKey : ORMPropertyAttribute
	{
		public ORMKey() { isKey = true; }
	}
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class Readonly : ORMPropertyAttribute
	{
		public Readonly() { Readonly = true; }
	}
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ReadonlyKey : ORMPropertyAttribute
	{
		public ReadonlyKey() { isKey = true; Readonly = true; }
	}

	public enum ORMRulEnum
	{
		DBNameAsIs = 1,                     // Default value
		DBReplaceUnderscoresWithSpaces = 2,     // Replace underscores with spaces
		DBRemoveUnderscoresAndCapitalize = 4,  // Remove underscores and capitalize

		__DBMask = 31,

		ForPresentNameAsIs = 32,             // Default value,
		ForPresentUnderscoresReplaceSpaces = 64,
		ForPresentToTitleCase = 128,
		ForPresentHumanitaize = 256,

		__ForPresentMask = 480
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMRuleSwitcherAttribute : Attribute
	{
		public ORMRulEnum Rules { get; init; }

		public ORMRuleSwitcherAttribute(params ORMRulEnum[] rules)
		{
			ORMRulEnum _rules = 0;
			foreach (var rule in rules)
				_rules |= rule;

			if ((_rules & ORMRulEnum.__DBMask) == 0)
				_rules |= ORMRulEnum.DBNameAsIs;

			if ((_rules & ORMRulEnum.__ForPresentMask) == 0)
				_rules |= ORMRulEnum.ForPresentNameAsIs;

			Rules = _rules;
		}
	}

	public class ORMData
	{
	}

	public enum DBProviderEnum { MSSql, MySQL, OracleSQL, PostgreSQL }

	public static class OracleSQLScriptBuilder
	{
		public static string DBFriendly(string name) => name.StartsWith('"') ? name : ('"' + name + '"');
	}
	
	public static class MSSQLScriptBuilder
	{
		public static string DBFriendly(string name) => name.StartsWith('[') ? name : ("[" + name + "]");
	}

	public static class PostgreSQLScriptBuilder
	{
		public static string DBFriendly(string name) =>name.StartsWith('"') ? name : ('"' + name + '"');
	}

	public static class MySQLScriptBuilder
	{
		public static string DBFriendly(string name) => name.StartsWith('"') ? name : ('`' + name + '`');
	}
}

