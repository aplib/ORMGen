using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace ORMGen.Builders
{
    /// <summary>
    /// For Microsoft SQL server data access orm builder
    /// </summary>
    public static class MSSQLORMBuilder
    {
        /// <summary>
        /// Link types only for reference to existing type,
        /// this link may be incorrect due to duplicate names or assembly binding is incorrect.
        /// Eerrors will be detected during compilation of builded souce code and runtime.
        /// If referebce target ORM type hasn't created yet
        /// need a second pass with existing types to bind.
        /// By default reference may writed as string type.
        /// Expensive.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="table_name">name of database table</param>
        /// <param name="domain">application domain for metadata exploring</param>
        /// <param name="parent_list">optional list of ORMTableInfo for iteration and reqursive calling</param>
        /// <returns></returns>
        public static IList<ORMTableInfo> ORMFromSCHEMA(IDbConnection conn, string table_name, AppDomain domain, IList<ORMTableInfo> parent_list = null)
        {
            var result = new List<ORMTableInfo>();
            var orm_table_name = ORMHelper.RemoveBrackets(table_name);
            var orm_name = ORMHelper.ToValidNameRegex.Replace(orm_table_name, "_");

            var already_exist = parent_list?.FirstOrDefault(orm => ORMHelper.RemoveBrackets(orm.TableName) == orm_table_name);
            if (already_exist != null)
            {
                result.Add(already_exist);
                return result;
            }
            ORMTableInfo.SetDefaultDBProvider(DBProviderEnum.MSSql);

            // Construct orm & attribytes

            var orm = new ORMTableInfo() { Name = orm_name, TableName = table_name, As = orm_name.ToLower() };

            try
            {
                // types linking to existing type only for reference
                // this link may be incorrect due to duplicate names or assembly binding is incorrect
                // errors will be  detected during compilation of builded souce code and runtime

                // try by table name
                foreach (var assembly in domain.GetAssemblies())
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var ormtable_attr = type.GetCustomAttribute<ORMTableAttribute>();
                        if (ormtable_attr != null && ORMHelper.RemoveBrackets(ormtable_attr.TableName) == table_name)
                        {
                            orm.Type = type;
                            // there:

                            break;
                        }
                    }
                }
                // try by structure or class name
                foreach (var assembly in domain.GetAssemblies())
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var ormtable_attr = type.GetCustomAttribute<ORMTableAttribute>();
                        if (ormtable_attr != null && (type.Name == table_name || type.Name == ORMHelper.ToValidNameRegex.Replace(table_name, "_")) && ormtable_attr.TableName.Blank())
                        {
                            orm.Type = type;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($@"Exception during explore metadata for resolving orm type for {orm.Name}");
                Console.WriteLine(e.Message);
            }

            result.Add(orm);

            // load schema

            var schema_columns = conn.Query<SCHEMA_COLUMNS>($@"select * from INFORMATION_SCHEMA.COLUMNS order by ORDINAL_POSITION asc");
            var columns = schema_columns.Where(row => row.TABLE_NAME.Trim() == table_name);
            var schema_pks = conn.Query<sp_pkeys>($@"EXEC sp_pkeys [{table_name}]");
            var schema_fks = conn.Query<sp_foreign_keys_rowset2>($@"EXEC sp_foreign_keys_rowset2 [{table_name}]");

            // construct orm properties

            var properties = new List<ORMPropertyInfo>();
            foreach (var row in columns)
            {
                // orm property attributes

                var type = ParseDataType(row.DATA_TYPE);
                var name = ORMHelper.ToValidNameRegex.Replace(row.COLUMN_NAME, "_");
                var title = ORMHelper.ByViewRule(row.COLUMN_NAME, orm.Rules);
                var field = ORMHelper.ByDBRule(row.COLUMN_NAME, orm.Rules);
                var orm_pi = new ORMPropertyInfo(orm, name, type) { Title = title, Field = field };

                // from schema

                orm_pi.isKey = schema_pks.Any(row => row.COLUMN_NAME == field);
                var target_table_name = (schema_fks.FirstOrDefault(row => row.FK_COLUMN_NAME == field))?.PK_TABLE_NAME;
                if (target_table_name != null)
                {
                    // create referenced orm info types

                    var orm_list = ORMFromSCHEMA(conn, target_table_name, domain, result);

                    result.AddRange(orm_list.Where(orm => !result.Contains(orm)));
                    var ref_target_orm = result.FirstOrDefault(orm => ORMHelper.RemoveBrackets(orm.TableName) == target_table_name);
                    if (ref_target_orm == null)
                        ref_target_orm = result.FirstOrDefault(orm => (orm.Name == target_table_name || type.Name == ORMHelper.ToValidNameRegex.Replace(table_name, "_")));

                    if (ref_target_orm != null && ref_target_orm.Type != null)
                    {
                        // check ref target type exists

                        orm_pi.RefType = ref_target_orm.Type;
                        // there:
                        // link type only for reference to existing type
                        // this link may be incorrect due to duplicate names or assembly binding is incorrect
                        // errors will be  detected during compilation of builded souce code
                    }
                    if (orm_pi.RefType == null)
                    {
                        Console.WriteLine($@"Unresolved target orm type for reference property {orm.Name}.{orm_pi.Name}");
                        // The target ORM type hasn't created yet
                        // need a second pass with existing types to bind.
                        // For default write as string:
                        orm_pi.RefType = typeof(string);
                    }
                }
                properties.Add(orm_pi);
            }

            orm.Props = properties.ToArray();

            return result;
        }

        static Dictionary<string, Type> sql_types = new Dictionary<string, Type>()
        {
            { "bigint", typeof(long) },
            { "binary", typeof(byte[]) },
            { "bit", typeof(bool) },
            { "char", typeof(string) },
            { "date", typeof(DateTime) },
            { "datetime", typeof(DateTime) },
            { "datetime2", typeof(DateTime) },
            { "datetimeoffset", typeof(DateTimeOffset) },
            { "decimal", typeof(decimal) },
            { "float", typeof(double) },
            { "image", typeof(double) },
            { "int", typeof(int) },
            { "money", typeof(decimal) },
            { "nchar", typeof(string) },
            { "ntext", typeof(string) },
            { "numeric", typeof(decimal) },
            { "nvarchar", typeof(string) },
            { "real", typeof(Single) },
            { "rowversion", typeof(byte[]) },
            { "smalldatetime", typeof(DateTime) },
            { "smallint", typeof(Int16) },
            { "smallmoney", typeof(decimal) },
            { "sql_variant", typeof(object) },
            { "text", typeof(string) },
            { "time", typeof(TimeSpan) },
            { "timestamp", typeof(byte[]) },
            { "tinyint", typeof(byte) },
            { "uniqueidentifier", typeof(Guid) },
            { "varbinary", typeof(byte[]) },
            { "varchar", typeof(string) },
        };
        static Type ParseDataType(string name)
        {
            return sql_types.TryGetValue(name, out var type)
                ? type
                : typeof(string);
        }
    }

    // query schema types

    record SCHEMA_COLUMNS //::generated
    {
        public string TABLE_CATALOG { get; set; }
        public string TABLE_SCHEMA { get; set; }
        public string TABLE_NAME { get; set; }
        public string COLUMN_NAME { get; set; }
        public int ORDINAL_POSITION { get; set; }
        public string COLUMN_DEFAULT { get; set; }
        public string IS_NULLABLE { get; set; }
        public string DATA_TYPE { get; set; }
        public int CHARACTER_MAXIMUM_LENGTH { get; set; }
        public int CHARACTER_OCTET_LENGTH { get; set; }
        public byte NUMERIC_PRECISION { get; set; }
        public short NUMERIC_PRECISION_RADIX { get; set; }
        public int NUMERIC_SCALE { get; set; }
        public short DATETIME_PRECISION { get; set; }
        public string CHARACTER_SET_CATALOG { get; set; }
        public string CHARACTER_SET_SCHEMA { get; set; }
        public string CHARACTER_SET_NAME { get; set; }
        public string COLLATION_CATALOG { get; set; }
        public string COLLATION_SCHEMA { get; set; }
        public string COLLATION_NAME { get; set; }
        public string DOMAIN_CATALOG { get; set; }
        public string DOMAIN_SCHEMA { get; set; }
        public string DOMAIN_NAME { get; set; }
    }

    record sp_pkeys //::generated
    {
        public string TABLE_QUALIFIER { get; set; }
        public string TABLE_OWNER { get; set; }
        public string TABLE_NAME { get; set; }
        public string COLUMN_NAME { get; set; }
        public short KEY_SEQ { get; set; }
        public string PK_NAME { get; set; }
    }

    record sp_foreign_keys_rowset2 //::generated
    {
        public string PK_TABLE_CATALOG { get; set; }
        public string PK_TABLE_SCHEMA { get; set; }
        public string PK_TABLE_NAME { get; set; }
        public string PK_COLUMN_NAME { get; set; }
        public string PK_COLUMN_GUID { get; set; }
        public string PK_COLUMN_PROPID { get; set; }
        public string FK_TABLE_CATALOG { get; set; }
        public string FK_TABLE_SCHEMA { get; set; }
        public string FK_TABLE_NAME { get; set; }
        public string FK_COLUMN_NAME { get; set; }
        public string FK_COLUMN_GUID { get; set; }
        public string FK_COLUMN_PROPID { get; set; }
        public int ORDINAL { get; set; }
        public string UPDATE_RULE { get; set; }
        public string DELETE_RULE { get; set; }
        public string PK_NAME { get; set; }
        public string FK_NAME { get; set; }
        public short DEFERRABILITY { get; set; }
    }
}



