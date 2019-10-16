[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) ![VSTS Build Status](https://carlabs.visualstudio.com/Getit/_apis/build/status/Getit-PR)


<p align="center"><img src= "getit-logo-640x.png"></img></p>

# Getit
A GraphQL Query Builder For C#

Getit is a simple package that allows you go build GraphQL queries. 
It also allows RAW queries if you don't want to use the query builder.
Currently Getit only builds queries and does not help with mutations. Getit
does allow for passing a *Raw* query to your server so you can
use it for passing anything you might create right on through. 

## Change Log
* **V2.x.x** - Updated Get() method so it will now throw an exception 
on any error. Previously would return `null`. This has some implications on 
error handling when sending multiple queries. If any fail,
an exception is now raised. See error handling section. This is possibly a breaking change 
from V1.
* **v1.x.x** - Initial Release

## Installation
Install from Nuget, or your IDE's package manager
```
Install-Package Carlabs.Getit
```

Supports:
* .NET Standard 1.3

## Usage
```csharp
// Setup Getit, config and a couple of queries

Config config = new Config("https://randy.butternubs.com/graphql");
Getit getit = new Getit(config);
IQuery userQuery = getit.Query();

userQuery
    .Name("User")
    .Select("userId", "firstName", "lastName", "phone")
    .Where("userId", "331")
    .Where("lastName", "Calhoon")
    .Comment("My First Getit Query");

// Fire the request to the sever as specified by the config

Console.WriteLine(await getit.Get<string>(userQuery));
```

## What is Getit?

The Getit query builder sits on top of the C# GraphQL client that can be found on Github
[Github : graphql-client](https://github.com/graphql-dotnet/graphql-client)

In addition  support mapping of the results back to concrete defined 
types for easy access. Don't like that, get it as a JObject, or plain old JSON.

The main idea is to allow building queries programatically but simple. Common terms, similar
to SQL are used to keep things familliar.

Also LOOK AT THE SOURCE for additional parameter, features that
may have not been covered here!

Let's breakdown a simple GraphQL query and write it in Getit's querybuilder.
```csharp
{
    NearestDealer(zip: "91403", make: "aston martin") {
    distance
    rating
    Dealer {
      name
      address
      city
      state
      zip
      phone
    }
  }
}
```
This query has a simple set of parameters, and a select field, along with what I'll call a
**sub-select**. Query Name is `NearestDealer` with the follwing parameters `zip` and `make`.
Both are of string type, although they can be any GraphQL type including enums.

Now lets write this with the Getit Querybuilder
```csharp
subSelectDealer
    .Name("Dealer")
    .Select("name", "address", "city", "state", "zip", "phone");

nearestDealerQuery
    .Name("NearestDealer")
    .Select("distance")
    .Select("rating")
    .Select(subSelectDealer)
    .Where("zip", "91302")
    .Where("make", "aston martin");
```
It's pretty straight forward. You can also pass in dictionary type objects to both the `Select` and 
the `Where` parts of the statement. Here is another way to write the same query -
```csharp
Dictionary<string, object> whereParams = new Dictionary<string, object>
{
    {"make", "aston martin"},
    {"zip",  "91403"}
};

List<object> selList = new List<object>(new object[] {"distance", "rating", subSelectDealer});

nearestDealerQuery
    .Name("NearestDealer")
    .Select(selList)
    .Where(whereParams);
```
When appropriate (You figure it out) you can use the following data types, 
which can also contain nested data types, so you can have an list of strings 
and such nested structures.

These C# types are supported:
* `string`
* `int`
* `float`
* `double`
* `EnumHelper` (For enumerated types)
* `KeyValuePair<string, object>`
* `IList<object>`
* `IDictionary<string, object>`

#### A more complex example with nested parameter data types
```csharp
// Create a List of Strings for passing as an ARRAY type parameter
List<string> modelList = new List<string>(new[] {"DB7", "DB9", "Vantage"});
List<object> recList = new List<object>(new object[] {"rec1", "rec2", "rec3"});

// Here is a From/To parameter object
Dictionary<string, object> recMap = new Dictionary<string, object>
{
    {"from", 10},
    {"to",   20},
};

// try a more complicate dict with sub structs, list and map
Dictionary<string, object> fromToPrice = new Dictionary<string, object>
{
    {"from", 88000.00},
    {"to", 99999.99},
    {"recurse", recList},
    {"map", recMap}
};

// Now collect them all up in a final dictionary and Getit will traverse
// and build the GraphQL query
Dictionary<string, object> myWhere = new Dictionary<string, object>
{
    {"make", "aston martin"},
    {"state", "ca"},
    {"limit", 2},
    {"trims", trimList},
    {"models", modelList},
    {"price", fromToPrice}
};

List<object> selList = new List<object>(new object[] {"id", subSelect, "name", "make", "model"});

// Now finally build the query
query
    .Name("DealerInventory")
    .Select("some_more", "things", "in_a_select")   // another way to set selects 
    .Select(selList)                                // select list object
    .Where(myWhere)                                 // add the nested Parameters
    .Where("id_int", 1)                             // add some more
    .Where("id_double", 3.25)
    .Where("id_string", "some_sting_id")
    .Comment("A complicated GQL Query with getit");
```
So as you can see you can express parameters as list or objects, and nested as well. 
since this is a made up example your mileage may vary if you type in the code, but give
it a try and see what the generated GraphQL query looks like, it likely won't be pretty.
## More Examples
### Configuration, Creation, Use Options
Their are a coupe of ways to set up Getit for use. Here are a few that are all 
basically the same. Getit will allow a class config to be set, or you can set it 
per-call to the Get().

```csharp
    // Create an instance of Getit, set it directly with the Config
    Config config = new Config("http://haystack.calhoon.com");
    IGetit getit = new Getit(config);

    // Create without a config (must pass config to Get())
    IGetit getit = new Getit();

    // Or if you want create Getit and set config
    IGetit getit = new Getit();

    // Defer setting the configuration 
    IConfig config = new Config("http://haystack.calhoon.com");
    getit.Config = config;

    // Getting a new query via the Dispenser (Factory)
    IQuery aQuery = getit.Query();
    
    // You could also just have done this 
    // IQuery aQuery = new Query()

    // You can use the Getit's instance's config if set
    // to eliminate extra params when calling the Get().

    // Exectue the query, notice that CONFIG is not required
    JObject jOb = await getit.Get<JObject>(aQuery);

    // If you don't want to set or use Getit's instance config 
    // just pass it with each Get() call
    JObject jOb = await getit.Get<JObject>(aQuery, config);
```

### Multiple `Query` Query
```csharp
// Setup Getit, and a couple of queries
Getit getit = new Getit();
Config config = new Config();
config.SetUrl("https://randy.butternubs.com/graphql");

IQuery nearestDealerQuery = getit.Query();
IQuery subSelectDealer = getit.Query();

// A sub-select build the same way a query is
subSelectDealer
    .Name("Dealer")
    .Select("name", "address", "city", "state", "zip", "phone");

nearestDealerQuery
    .Name("NearestDealer")
    .Select("distance")
    .Select(subSelectDealer)
    .Where("zip", "91302")
    .Where("make", "aston martin");

// Dump the generated query
Console.WriteLine(nearestDealerQuery);

// Fire the request to the sever as specified by the config

// This would get the string of JSON that was returned
// **** getit.Get<T>() is the way to execute the query against the server ****
// T can be a String, JObject, or your type. See Get() for more info
Console.WriteLine(await getit.Get<string>(nearestDealerQuery, config));

// If we had a matching C# NearestDealer Object that matched the GraphQL response (EXACTLY)
// it will be populated from the query into a list of those object. 
// This shows how the type is specified for the mapping.
List<NearestDealer> objResults = await getit.Get<List<NearestDealer>>(nearestDealerQuery, config);

// use `ConsoleDump` package if you want to see the object on the console
objResults.Dump();

// Want it as a JObject? You are covered as well
JObject jO = await getit.Get<JObject>(nearestDealerQuery, config);
```
### Raw GraphQL query
While the query builder is helpful, their are some cases where it's just simpler
to pass a raw or prebuilt query to the GraphQL server. This is acomplished by using the *Raw()* query method.
In this example we have on the server a query that responds to a `Version` number request.

The GraphQL JSON response from the `Make` query would look like this -
```json
{
  "Make": [
    {
      "id": 121,
      "name": "aston martin"
    }
  ]
}
```

#### Example RAW query code
```csharp
    // Create an instance of Getit and the Config
    Getit getit = new Getit();
    Config config = new Config();

    // Set a URL to your graphQL endpoint (fake endpoint)
    config.SetUrl("https://randy.butternubs.com/graphql");

    // Dispense a query
    IQuery aQuery = getit.Query();

    // Set the RAW GraphQL query
    aQuery.Raw(@"{Make(name: "Kia") {id name }}");

    // Exectue the call
    JObject jOb = await getit.Get<JObject>(aQuery, config);

    // Dump the JSON String (Via the JObject)
    Console.WriteLine(jOb);
```
#### RAW Example Console Output
```json
{
  "data": {
    "Alias": [
      {
        "id": 121,
        "name": "Aston Martin"
      }
    ]
  }
}
```

### Batched Queries
With GraphQL you can send batches of queries in a single request. 
Getit supports that functionality and it can save extra network requests. Essentiall
you can pass any generated query to the `Batch()` method and it will be stuffed into
the call. Using JObject's or JSON string returns from `Get()` is usually how you get the 
blob of data back from batched queries.
```csharp
nearestDealerQuery
    .Name("NearestDealer")
    .Select("distance")
    .Select(subSelectDealer)
    .Where("zip", "91302")
    .Where("make", "aston martin");

batchQuery.Raw("{ Version }");          // Get the version
batchQuery.Batch(nearestDealerQuery);   // Batch up the nearest dealer query

// Make the call to the server as expected (string of JSON returned)
Console.WriteLine(await getit.Get<string>(batchQuery, config));
```

### Query Alias
Sometimes it's handy to be able to call a query but have it respond with a different
name. Generally if You call a GraphQL query with a specific name, you get that back as 
the resulting data set element. This can be a problem with batch queries hitting the same
endpoint. Or if you want to have the name of the data object being returned 
just be different. Getit allows the support for aliases. The proper name of the 
query should be set in the Name() method, but additionally you can add an `Alias()` call
to set that. So back to a simple example query with an alias-

```csharp
{
    AstonNearestDealer:NearestDealer(zip: "91403", make: "aston martin") {
    distance
    rating
    Dealer {
      name
      address
      city
      state
      zip
      phone
    }
  }
}
```
This query has a simple set of parameters, and a select field, along with what I'll call a
**sub-select**. Query Name is `NearestDealer` with the follwing parameters `zip` and `make`.
Both are of string type, although they can be any GraphQL type including enums.

Now lets write this with the Getit Querybuilder
```csharp
subSelectDealer
    .Name("Dealer")
    .Select("name", "address", "city", "state", "zip", "phone");

nearestDealerQuery
    .Name("NearestDealer")
    .Alias("AstonNearestDealer")
    .Select("distance")
    .Select("rating")
    .Select(subSelectDealer)
    .Where("zip", "91302")
    .Where("make", "aston martin");
```
The response (JObject) would be something like this -
```json
{
  "AstonNearestDealer": [
    {
      "distance": 7.5,
      "Dealer": {
        "name": "Randy Butternubs Aston Martin",
        "address": "1234 Haystack Calhoon Road",
        "city": "Hank Kimballville",
        "state": "CA",
        "zip": "91302",
        "phone": "(818) 887-7111",
      }
    }
  ]
}
...

```
### GraphQL Enums
When working with GraphQL you have a few different datatypes. One issue with the
Getit's s query builder is that their would be no way to differentiate a string and an Enumeration.
It uses the data type to determine how to build the query so in cases where you need an real 
GraphQL enumerations their is a simple helper class that can be used.

Example how to generate an Enumeration
```csharp
EnumHelper GqlEnumEnabled = new EnumHelper().Enum("ENABLED");
EnumHelper GqlEnumDisabled = new EnumHelper("DISABLED");
EnumHelper GqlEnumConditionNew = new EnumHelper("NEW");
EnumHelper GqlEnumConditionUsed = new EnumHelper("USED");
```

Example creating a dictionary for a select (GraphQL Parameters)
```csharp
Dictionary <string, object> mySubDict = new Dictionary<string, object>;
{
    {"Make", "aston martin"},
    {"Model", "DB7GT"},
    {"Condition", GqlEnumConditionNew}, // Used it in a Dictionary
};

query.Name("CarStats")
    .Select("listPrice", "horsepower", "color")
    .Where(myDict)
    .Where("_debug", GqlEnumDisabled)       // Used it in a where
    .Comment("Using Enums");
```
This will generate a query that looks like this (well part of it anyway)
```csharp
{
    CarStats(Make:"aston martin", Model:"DB7GT", Condition:NEW, _debug:DISABLED)
    { 
        listPrice
        horsepower
        color
    }
}
```
### Handling Errors
**Getit version 2.X Error Handling**
Changed for version 2 onwards the Get() method will no longer return `null` 
in response to error situations. For client errors, empty data or 
any graphQL related errors an exception will be throw. Additional data based on 
the exception will be stored in the thrown exception.

```csharp
using GraphQL.Common.Response;

// Setup Getit, config and a couple of queries

Config config = new Config("https://randy.butternubs.com/graphql");
Getit getit = new Getit(config);
IQuery userQuery = getit.Query();

userQuery
    .Name("User")
    .Select("userId", "firstName", "lastName", "phone")
    .Where("userId", "331")
    .Where("lastName", "Calhoon")
    .Comment("My First Getit Query");

// Fire the request to the sever catch any issues
// Check the GraphQL client for list of it's possible exceptions.
// Get it will also throw `ArgumentException` and `ArgumentNullException`

try
{
    string resp = await getit.Get<string>(userQuery);
}
catch(Exception ex)
{
    // Exceptions can OPTIONALLY have additional data depending on the cause

    // "request"  - String of query set to server
    if (e.Data.Contains("request"))
    {

        Console.WriteLine("Found Request Query");
        Console.WriteLine(e.Data["request"]);
    }

    // "gqlErrors" - Array of GraphQLError's
    if (e.Data.Contains("gqlErrors"))
    {
        Console.WriteLine("Found gqlErrors");

        // Note data type returned is an Aarry of GraphQLError
        GraphQLError [] errs = (GraphQLError []) e.Data["gqlErrors"];

        foreach (GraphQLError err in errs)
        {
            // Look at any of the elements in the error, here just the message
            Console.WriteLine(err.Message);
        }
    }
}
...

```

## Todo's
* Test on more then the single GraphQL server we use
* Expose More GraphQLClient functionality
* Stuff that is broken as it is discovered
* Mutations if needed
* Other missing GQL support
* More Organized Docs

