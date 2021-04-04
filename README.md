# ORMGen
 .NET Minimal helper library, data mapper (dapper wrapper), metadata types, for metaprogramming, code/script generation.


Features:
--------
1. Helper object populated class metadata and specialized custom attributes.
Can be used for code and script generation.
2. Builders

ORMBuilder.ORMFromDynamicQueryResult
ORMBuilder.GenORMTableTypeCode

ORMGen.Builders packet

MSSQLORMBuilder.ORMFromSCHEMA


Object initialization:
--------

```
var orm = new ORMTableInfo<SomeClass>();
orm.UseDBProvider(DBProviderEnum.PostgreSQL);

or

var orm = new ORMTableInfo<SomeClass>(DBProviderEnum.MSSql);

// optional:

orm.UseRules(ORMRulEnum.ViewHumanitaize | ORMRulEnum.DBReplaceUnderscoresWithSpaces);

```

Something like this:
--------


```
// for ORM class by defult assigned rules for auto mapping fields:

[ORMRuleSwitcher(ORMRulEnum.ViewHumanitaize, ORMRulEnum.DBReplaceUnderscoresWithSpaces)]

switching can be inside of class, usually not required

// Model definition:

[ORMTable(TableName = "Some table", TextProperty = "Text")]
public class SomeClass
{
    [ORMReadonlyKey]
    public int Id { get; set; }
    public string Text { get; set; }
}

// select (using Dapper)

var script = $@"select {orm.ForSelectFields()} from {orm.TableName}";
var enum_objects = conn.Query<SomeClass>(script);

// select one

var some_object = enum_objects.First();
script = $@"select {orm.ForSelectFields()} from {orm.TableName} where {orm.ForSelectConditionKeys()}";
var data_object = conn.QuerySingle<SomeClass>(script, some_object);

// update

data_object.Text = " some text ...";
script = $@"update table {orm.TableName} set {orm.Select("Text").ForUpdateSet()} where {orm.ForSelectConditionKeys()}";
conn.Execute(script, data_object);
```

you can control the composition of fields using the following methods: Select or Reject