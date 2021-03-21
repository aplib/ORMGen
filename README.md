# ORMGen
 Minimal helper library for metaprogramming, code/script generation. Data mapper (dapper wrapper), metadata types.


 Initialize objectject
------------------------------------------------------------

Features:
--------
Helper object populated class metadata and specialized custom attributes.
Can be used for code and script generation.

Creation:
--------

```
var orm = new ORMTableInfo<SomeClass>();
orm.UseDBProvider(DBProviderEnum.PostgreSQL);

or

var orm = new ORMTableInfo<SomeClass>(DBProviderEnum.PostgreSQL);
```


Something like this:
--------


```
[ORMTable(TableName = "Some table", TextProperty = "Text")]
public class SomeClass
{
    [ORMReadonlyKey]
    public int Id { get; set; }
    public string Text { get; set; }
        
}

var script = $@"select {orm.ForSelectFields()} from {orm.TableName}";
var enum_objects = conn.Query<SomeClass>(script);

// select one

var some_object = enum_objects.First();
script = $@"select {orm.ForSelectFields()} from {orm.TableName} where {orm.ForSelectConditionKeys()}";
var data_object = conn.QuerySingle<SomeClass>(script, some_object);

data_object.Text = " some text ...";
script = $@"update table {orm.TableName} set {orm.Select("Text").ForUpdateSet()} where {orm.ForSelectConditionKeys()}";
conn.Execute(script, data_object);

```