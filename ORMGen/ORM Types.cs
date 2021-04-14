using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ORMGen
{
	/// <summary>ORMTableInfo data model base class
	/// </summary>
	public class ORMTableInfo
	{
		/// <summary>
		/// Type name or name for genegation type
		/// </summary>
		public string Name { get; set; }
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
		public string IdProperty { get => GetIdPropertyName(); set { id_property = value; } }
		/// <summary>
		/// Text property that give a representation of the object
		/// </summary>
		public string TextProperty { get; set; }
		/// <summary>
		/// Declare the data table object as read-only and the data will not be modified, use for codegeneration
		/// </summary>
		public bool Readonly { get; set; }
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

		string id_property;
		string GetIdPropertyName()
		{
			if (id_property == null)
			{
				if (id_property == null)
				{
					if (Keys == null || Keys.Length != 1)
						throw new NullReferenceException("Need to set 'IdProperty' ORM attribute value or define one key property");

					id_property = Keys?[0].Name;
				}

				return id_property;
			}

			return id_property;
		}
		/// <summary>
		/// Exception safe try get IdProperty name
		/// </summary>
		/// <returns>IdProperty name or null</returns>
		public bool TryGetIdPropertyName(out string IdProperty)
		{
			if (id_property == null && Keys != null && Keys.Length == 1)
			{
				id_property = Keys[0].Name;
			}

			IdProperty = id_property;

			return id_property != null;
		}

		DBProviderEnum db_provider = DBProviderEnum.MSSql;
		/// <summary>
		/// Selected the SQL data provider
		/// </summary>
		public DBProviderEnum DBProvider { get => db_provider; internal set => UseDBProvider(value); }
		/// <summary>
		/// Selecting a the SQL data provider
		/// </summary>
		/// <param name="provider"></param>
		public void UseDBProvider(DBProviderEnum provider)
		{
			if (provider == db_provider)
				return;

			db_provider = provider;
			DBFriendly = provider switch
			{
				DBProviderEnum.MSSql => MSSQLScriptBuilder.DBFriendly,
				DBProviderEnum.MySQL => MySQLScriptBuilder.DBFriendly,
				DBProviderEnum.OracleSQL => OracleSQLScriptBuilder.DBFriendly,
				DBProviderEnum.PostgreSQL => PostgreSQLScriptBuilder.DBFriendly,
				_ => MSSQLScriptBuilder.DBFriendly

			};
		}
		static Func<string, string> db_friendly = MSSQLScriptBuilder.DBFriendly;
		/// <summary>
		/// String formatting function for compatibility names with the provider
		/// </summary>
		public Func<string, string> DBFriendly { get => db_friendly; internal set => UseDBFriendly(value); }
		/// <summary>
		/// String formatting function for compatibility names with the provider
		/// </summary>
		/// <param name="db_friendly_func">Func&lt;string, string&gt; db_friendly_func</param>
		public void UseDBFriendly(Func<string, string> db_friendly_func)
		{
			if (db_friendly_func == db_friendly)
				return;

			db_friendly = db_friendly_func;
		}

		ORMRulEnum mapping_rules = ORMRulEnum.DBReplaceUnderscoresWithSpaces | ORMRulEnum.ViewHumanitaize;
		/// <summary>
		/// Default rules
		/// </summary>
		public ORMRulEnum Rules { get => mapping_rules; set => UseRules(value); }
		/// <summary>
		/// Set mapping rules
		/// </summary>
		public void UseRules(ORMRulEnum rules)
		{
			if (rules != mapping_rules)
				mapping_rules = rules;
		}


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		internal protected static ORMRulEnum default_mapping_rules = ORMRulEnum.DBReplaceUnderscoresWithSpaces | ORMRulEnum.ViewHumanitaize;
		internal protected static DBProviderEnum default_db_provider { get; set; } = DBProviderEnum.MSSql;
		internal protected static Func<string, string> default_db_friendly = MSSQLScriptBuilder.DBFriendly;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		/// <summary>
		/// Set a default databse provider
		/// </summary>
		/// <param name="provider">database provider</param>
		public static void SetDefaultDBProvider(DBProviderEnum provider)
		{
			default_db_provider = provider;
			default_db_friendly = provider switch
			{
				DBProviderEnum.MSSql => MSSQLScriptBuilder.DBFriendly,
				DBProviderEnum.MySQL => MySQLScriptBuilder.DBFriendly,
				DBProviderEnum.OracleSQL => OracleSQLScriptBuilder.DBFriendly,
				DBProviderEnum.PostgreSQL => PostgreSQLScriptBuilder.DBFriendly,
				_ => MSSQLScriptBuilder.DBFriendly

			};
		}
		/// <summary>
		/// Set a default db friendly function
		/// </summary>
		/// <param name="db_friendly_func">db friendly function</param>
		public static void SetDefaultDBFriendly(Func<string, string> db_friendly_func)
		{
			default_db_friendly = db_friendly_func;
		}
		/// <summary>
		/// Set default mapping rules
		/// </summary>
		public static void SetDefaultRules(ORMRulEnum rules)
		{
			default_mapping_rules = rules;
		}


		/// <summary>
		/// Default parameterless constructor
		/// </summary>
		public ORMTableInfo()
		{
			UseDBProvider(default_db_provider);
			UseRules(default_mapping_rules);
		}
		/// <summary>
		/// Constructor with selecting databse provider and rules
		/// </summary>
		public ORMTableInfo(DBProviderEnum? provider = null, ORMRulEnum? rules = null)
		{
			UseDBProvider(provider ?? default_db_provider);
			UseRules(rules ?? default_mapping_rules);
		}
		/// <summary>
		/// Constructor with specified ORMTable ttpe
		/// </summary>
		/// <param name="type"></param>
		public ORMTableInfo(Type type) : this (type, null, null)
		{ }

		/// <summary>
		/// Constructor with specified ORMTable type, optional selecting DB provider, rules, and filling from metadata
		/// </summary>
		public ORMTableInfo(Type type, DBProviderEnum? provider = null, ORMRulEnum? rules = null)
		{
			FillProperties(type,  provider, rules);
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		internal protected void FillProperties(Type type, DBProviderEnum? provider = null, ORMRulEnum? rules = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		{
			UseDBProvider(provider ?? default_db_provider);
			UseRules(rules ?? default_mapping_rules);

			Type = type;
			Name = Type.Name;

			// Collect metadata

			TypeInfo table_type_info = Type.GetTypeInfo();

			var current_rules = Rules;
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
				SetRules(rule_attr, ORMRulEnum.__ViewMask);
			}

			TableName = table_attr.TableName ?? ORMHelper.ByDBRule(table_type_info.Name, current_rules);
			Title = table_attr.Title ?? ORMHelper.ByViewRule(table_type_info.Name, current_rules);
			IdProperty = table_attr.IdProperty;
			TextProperty = table_attr.TextProperty;
			Readonly = table_attr.Readonly;
			As = table_attr.As ?? table_type_info.Name;
			As = Regex.Replace(ORMHelper.RemoveBrackets(As).ToLower(), $@"[\W\s_\~\!\@\#\$\%\^\'\""\`\&\*\(\)\[\]]+", "_");

			// For all properties:

			var props = table_type_info.GetProperties();
			var all_orm_pi = new List<ORMPropertyInfo>(props.Length);

			foreach (var prop_info in props)
			{
				// apply property rules to current_rules
				foreach (var rule_attr in prop_info.GetCustomAttributes<ORMRuleSwitcherAttribute>())
				{
					SetRules(rule_attr, ORMRulEnum.__DBMask);
					SetRules(rule_attr, ORMRulEnum.__ViewMask);
				}

				// create ORMPropertyInfo
				var orm_pi = new ORMPropertyInfo() { Parent = this, Type = prop_info.PropertyType, Name = prop_info.Name };

				// apply property attributes
				var orm_prop_attr = prop_info.GetCustomAttribute<ORMPropertyAttribute>();
				if (orm_prop_attr != null)
				{
					orm_pi.Title = orm_prop_attr.Title ?? ORMHelper.ByViewRule(orm_pi.Name, current_rules & ORMRulEnum.__ViewMask);
					orm_pi.Field = orm_prop_attr.Field ?? ORMHelper.ByDBRule(orm_pi.Name, current_rules & ORMRulEnum.__DBMask);
					orm_pi.Format = orm_prop_attr.Format;
					orm_pi.isKey = orm_prop_attr.isKey;
					orm_pi.Readonly = orm_prop_attr.Readonly;
					orm_pi.RefType = orm_prop_attr.RefType;
					orm_pi.Hide = orm_prop_attr.Hide;
				}

				if (orm_pi.Title == null) orm_pi.Title = ORMHelper.ByViewRule(orm_pi.Name, current_rules & ORMRulEnum.__ViewMask);
				if (orm_pi.Field == null) orm_pi.Field = ORMHelper.ByDBRule(orm_pi.Name, current_rules & ORMRulEnum.__DBMask);

				all_orm_pi.Add(orm_pi);
			}
			Props = all_orm_pi.ToArray();
			Keys = all_orm_pi.Where(prop => prop.isKey).ToArray();
			References = all_orm_pi.Where(prop => prop.RefType != null).ToArray();
		}
	}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static partial class ORMHelper
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	{
		/// <summary>
		/// Regex for convert name to valid CS name
		/// </summary>
		public static readonly Regex ToValidNameRegex = new Regex($@"[\W\s_\~\!\@\#\$\%\^\'\""\`\&\*\(\)\[\]]+");
		/// <summary>
		/// Remove special characters at start and end
		/// </summary>
		/// <param name="str">input string</param>
		/// <returns>Trimmed string without special characters at the start and end</returns>
		public static string RemoveBrackets(string str) => str?.Trim(' ', '[', ']', '"', '\'', '`');
		/// <summary>
		/// Select properties except specified
		/// </summary>
		/// <param name="orm">ORM instance</param>
		/// <param name="filter">comma-separated property names to be excluded</param>
		/// <returns>Enumeration of properties</returns>
		public static IEnumerable<ORMPropertyInfo> Reject(this ORMTableInfo orm, string filter)
		{
			var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			return orm.Props.Where(orm_pi => Array.IndexOf<string>(parts, orm_pi.Name) < 0);
		}
		/// <summary>
		/// Properties except specified
		/// </summary>
		/// <param name="props">this enumeration properties</param>
		/// <param name="filter">comma-separated property names to be excluded</param>
		/// <returns></returns>
		public static IEnumerable<ORMPropertyInfo> Reject(this IEnumerable<ORMPropertyInfo> props, string filter)
		{
			var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			return props.Where(orm_pi => Array.IndexOf<string>(parts, orm_pi.Name) < 0);
		}
		/// <summary>
		/// Select of specified properties
		/// </summary>
		/// <param name="orm">ORM instance</param>
		/// <param name="filter">comma-separated property names to be selected</param>
		/// <returns>Enumeration of properties</returns>
		public static IEnumerable<ORMPropertyInfo> Select(this ORMTableInfo orm, string filter)
		{
			var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			return orm.Props.Where(orm_pi => Array.IndexOf<string>(parts, orm_pi.Name) >= 0);
		}


		/// <summary>
		/// Formatting a list of mapped properties/fields for select script (fields part)
		/// </summary>
		/// <returns>Fields part of select script</returns>
		public static string ForSelect(this IEnumerable<ORMPropertyInfo> props) => string.Join(",", props.Select(orm_pi => orm_pi.Parent.DBFriendly(orm_pi.Field) + " as " + orm_pi.Name));
		/// <summary>
		/// Formatting a list of mapped properties passed from an object for select script condition (where part)
		/// </summary>
		/// <returns>(string) condition for where part</returns>
		public static string ForSelectConditionKeys(this IEnumerable<ORMPropertyInfo> props) => string.Join(" and ", props.Select(orm_pi => orm_pi.Parent.DBFriendly(orm_pi.Field) + "=@" + orm_pi.Name));
		/// <summary>
		/// Formatting a list of fields for insert script
		/// </summary>
		/// <returns>Fields list for insert script</returns>
		public static string ForInsert(this IEnumerable<ORMPropertyInfo> props) => string.Join(",", props.Where(orm_pi => !orm_pi.Readonly).Select(orm_pi => orm_pi.Parent.DBFriendly(orm_pi.Field)));
		/// <summary>
		/// Formatting a list of values for insert script
		/// </summary>
		/// <returns>values part of script</returns>
		public static string ForInsertValues(this IEnumerable<ORMPropertyInfo> props) => string.Join(",", props.Where(orm_pi => !orm_pi.Readonly).Select(orm_pi => "@" + orm_pi.Name));
		/// <summary>
		/// Formatting a list of values passed from an object for update script (set part)
		/// </summary>
		/// <returns>(string) for set part</returns>
		public static string ForUpdateSet(this IEnumerable<ORMPropertyInfo> props) => string.Join(", ", props.Where(orm_pi => !orm_pi.isKey && !orm_pi.Readonly).Select(prop => prop.Parent.DBFriendly(prop.Field) + "=@" + prop.Name));


		/// <summary>
		/// Formatting a full properties list of mapped properties passed from an object for select script
		/// </summary>
		/// <returns>(string) condition for where part</returns>
		public static string ForSelectFields(this ORMTableInfo orm) => string.Join(",", orm.Props.Select(orm_pi => orm.DBFriendly(orm_pi.Field) + " as " + orm_pi.Name));
		/// <summary>
		/// Formatting a key properties list for select script condition (where part)
		/// </summary>
		/// <returns>(string) condition for where part</returns>
		public static string ForSelectConditionKeys(this ORMTableInfo orm) => string.Join(" and ", orm.Keys?.Select(orm_pi => orm.DBFriendly(orm_pi.Field) + "=@" + orm_pi.Name));
		/// <summary>
		/// Formatting a list of fields for insert script
		/// </summary>
		/// <param name="orm">ORMTableInfo instance</param>
		/// <returns>Fields list for insert script</returns>
		public static string ForInsertFields(this ORMTableInfo orm) => string.Join(",", orm.Props.Where(orm_pi => !orm_pi.Readonly).Select(orm_pi => orm.DBFriendly(orm_pi.Field)));
		/// <summary>
		/// Formatting a list of values for insert script
		/// </summary>
		/// <returns>values part of script</returns>
		public static string ForInsertValues(this ORMTableInfo orm) => string.Join(",", orm.Props.Where(orm_pi => !orm_pi.Readonly).Select(orm_pi => "@" + orm_pi.Name));
		/// <summary>
		/// Formatting a full list of values passed from an object for update script (set part)
		/// </summary>
		/// <returns>(string) for set part</returns>
		public static string ForUpdateSet(this ORMTableInfo orm) => string.Join(", ", orm.Props.Where(orm_pi => !orm_pi.isKey && !orm_pi.Readonly).Select(prop => orm.DBFriendly(prop.Field) + "=@" + prop.Name));


		///// <summary>
		///// Matchch a field name by the specified rule
		///// </summary>
		///// <param name="orm_pi">ORM property info</param>
		///// <param name="rule">Rule</param>
		///// <returns>name of field</returns>
		//public static string ByDBRule(this ORMPropertyInfo orm_pi, ORMRulEnum rule) => ByDBRule(orm_pi.Name, rule);
		///// <summary>
		///// Transformation by the specified rule
		///// </summary>
		///// <param name="orm_pi">ORM property info</param>
		///// <param name="rule">Rule</param>
		///// <returns>Title</returns>
		//public static string ByViewRule(this ORMPropertyInfo orm_pi, ORMRulEnum rule) => ByViewRule(orm_pi.Name, rule);
		/// <summary>
		/// Matching, external access
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="rule">Rule</param>
		/// <returns>Matched name</returns>
		public static string ByDBRule(string name, ORMRulEnum rule) => (rule & ORMRulEnum.__DBMask) switch
		{
			ORMRulEnum.DBReplaceUnderscoresWithSpaces => name.Replace('_', ' '),
			ORMRulEnum.DBRemoveUnderscoresAndCapitalize =>
				name.Split('_')
				.Where(part => !string.IsNullOrWhiteSpace(part))
				.Select(part => Char.ToUpper(part[0], CultureInfo.CurrentCulture) + part[1..])
				.ToString(),
			_ => name
		};
		/// <summary>
		/// Transformation, external access
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="rule">Rule</param>
		/// <returns>Transformed name</returns>
		public static string ByViewRule(string name, ORMRulEnum rule) => (rule & ORMRulEnum.__ViewMask) switch
		{
			ORMRulEnum.ViewUnderscoresReplaceSpaces => name.Replace('_', ' '),
			ORMRulEnum.ViewWithUppercase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Replace('_', ' ')),
			ORMRulEnum.ViewHumanitaize => Humanitaize(name),
			_ => name
		};
		/// <summary>
		/// Humanitaize transformation, external access
		/// </summary>
		/// <param name="_name">Name</param>
		/// <returns>Transformed name</returns>
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

	/// <summary>ORM data mapping generic class derived from base class
	/// filled with metadata when created>
	/// </summary>
	public class ORMTableInfo<T> : ORMTableInfo
	{
		/// <summary>
		/// Default parameterless constructor of a specified ORMTable
		/// </summary>
		public ORMTableInfo() : base(typeof(T), null, null)
		{ }
		/// <summary>
		/// Constructor of a specified ORMTable with optional selecting DB provider, rules, and filling from metadata
		/// </summary>
		public ORMTableInfo(DBProviderEnum? provider = null, ORMRulEnum? rules = null) : base(typeof(T), provider, rules)
		{ }
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
		public string IdProperty { get; init; }
		/// <summary>
		/// Use TextProperty for codogeneration
		/// </summary>
		public string TextProperty { get; init; }
		/// <summary>
		/// Declare the data table object as read-only and the data will not be modified, use for codegeneration
		/// </summary>
		public bool Readonly { get; init; }
	}

	/// <summary>
	/// Represent property attributes. Object filled with metadata describing the data table property and its mapping.
	/// </summary>
	public class ORMPropertyInfo
	{
		/// <summary>
		/// ORMTable parent of property
		/// </summary>
		public ORMTableInfo Parent;
		/// <summary>
		/// Type of value, use Type for codogeneration
		/// </summary>
		public Type Type { get; set; }
		/// <summary>
		/// Name of property, use Name for codogeneration
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Title of property value, use Title for codogeneration
		/// </summary>
		public string Title { get; set; }
		/// <summary>
		/// Mapping property to a field of data table, use Field for script building
		/// </summary>
		public string Field { get; set; }
		/// <summary>
		/// Format for property value, use Format for codogeneration
		/// </summary>
		public string Format { get; set; }
		/// <summary>
		/// Indicates that the property value is the key value, use isKey to generate and script together.
		/// </summary>
		public bool isKey { get; set; }
		/// <summary>
		/// Readonly means not writeable to database, for script building
		/// </summary>
		public bool Readonly { get; set; }
		/// <summary>
		/// Referencing tagged ORMTable or other data table classes, use RefType to co-generation
		/// </summary>
		public Type RefType { get; set; }
		/// <summary>
		/// Not displayed property, for codogeneration
		/// </summary>
		public bool Hide { get; set; }
		/// <summary>
		/// Default constructor
		/// </summary>
		public ORMPropertyInfo() { }
		/// <summary>
		/// constructor with parameters
		/// </summary>
		/// <param name="parent">ORMTable parent for property</param>
		/// <param name="name"></param>
		/// <param name="value_type">Type of value</param>
		public ORMPropertyInfo(ORMTableInfo parent, string name, Type value_type)
		{
			Parent = parent;
			Name = name;
			Type = value_type;
		}
	}
	/// <summary>
	/// Object contains data mapping and other metadata.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMPropertyAttribute : Attribute
	{
		/// <summary>
		/// Title of property value, use Title for codogeneration
		/// </summary>
		public string Title { get; init; }
		/// <summary>
		/// Mapping property to a field of data table, use Field for script building
		/// </summary>
		public string Field { get; init; }
		/// <summary>
		/// Format for property value, use Format for codogeneration
		/// </summary>
		public string Format { get; init; }
		/// <summary>
		/// Indicates that the property value is the key value, use isKey to generate and script together.
		/// </summary>
		public bool isKey { get; init; }
		/// <summary>
		/// Readonly means not writeable to database, for script building
		/// </summary>
		public bool Readonly { get; init; }
		/// <summary>
		/// Referencing tagged ORMTable or other data table classes, use RefType to co-generation
		/// </summary>
		public Type RefType { get; init; }
		/// <summary>
		/// Not displayed property, for codogeneration
		/// </summary>
		public bool Hide { get; init; }
	}
	/// <summary>
	/// Shortly definition of a key property, something like this [ORMKey]
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMKey : ORMPropertyAttribute
	{
		/// <summary>
		/// Shortly definition of a key property, something like this [ORMKey]
		/// </summary>
		public ORMKey() { isKey = true; }
	}
	/// <summary>
	/// Shortly definition of a readonly field of data table, something like this [ORMReadonly]
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMReadonly : ORMPropertyAttribute
	{
		/// <summary>
		/// Shortly definition of a readonly field of data table, something like this [ORMReadonly]
		/// </summary>
		public ORMReadonly() { Readonly = true; }
	}
	/// <summary>
	/// Shortly definition of a readonly key field of data table, something like this [ORMReadonlyKey]
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMReadonlyKey : ORMPropertyAttribute
	{
		/// <summary>
		/// Shortly definition of a readonly key field of data table, something like this [ORMReadonlyKey]
		/// </summary>
		public ORMReadonlyKey() { isKey = true; Readonly = true; }
	}
	/// <summary>
	/// Eenumeration of rules for mapping to database and view
	/// </summary>
	public enum ORMRulEnum
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		Unassigned = 0,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		/// <summary>
		/// Map name to database unchanged
		/// </summary>
		DBNameAsIs = 1,
		/// <summary>
		/// Replace the name with spaces for underscores to match against the database
		/// </summary>
		DBReplaceUnderscoresWithSpaces = 2,
		/// <summary>
		/// Remove underscores and capitalize Name to match against the database
		/// </summary>
		DBRemoveUnderscoresAndCapitalize = 4,

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		__DBMask = 31,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Map to view Name unchanged
		/// </summary>
		ViewNameAsIs = 32,
		/// <summary>
		/// Remove underscores and capitalize Name to view
		/// </summary>
		ViewUnderscoresReplaceSpaces = 64,
		/// <summary>
		/// To view title with uppercase
		/// </summary>
		ViewWithUppercase = 128,
		/// <summary>
		/// To view title humanitize
		/// </summary>
		ViewHumanitaize = 256,

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		__ViewMask = 480
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
	/// <summary>
	/// ORM rule switcher attribute, effect scoped all subsequent within the class attributes until the next switch
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ORMRuleSwitcherAttribute : Attribute
	{
		/// <summary>
		/// Assigned rules
		/// </summary>
		public ORMRulEnum Rules { get; init; }
		/// <summary>
		/// Attribute constructor
		/// </summary>
		/// <param name="rules"></param>
		public ORMRuleSwitcherAttribute(params ORMRulEnum[] rules)
		{
			ORMRulEnum _rules = 0;
			foreach (var rule in rules)
				_rules |= rule;

			Rules = _rules;
		}
	}

	/// <summary>
	/// ORM data table Base class of ORM data table, used to build extensions
	/// </summary>
	public class ORMData
	{
	}

	/// <summary>
	/// Enumeration of SQL providers
	/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public enum DBProviderEnum { MSSql, MySQL, OracleSQL, PostgreSQL }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	internal static partial class OracleSQLScriptBuilder
	{
		public static string DBFriendly(string name) => name.StartsWith('"') ? name : ('"' + name + '"');
	}

	internal static partial class MSSQLScriptBuilder
	{
		public static string DBFriendly(string name) => name.StartsWith('[') ? name : ("[" + name + "]");
	}

	internal static partial class PostgreSQLScriptBuilder
	{
		public static string DBFriendly(string name) => name.StartsWith('"') ? name : ('"' + name + '"');
	}

	internal static partial class MySQLScriptBuilder
	{
		public static string DBFriendly(string name) => name.StartsWith('"') ? name : ('`' + name + '`');
	}
}

