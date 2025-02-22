# Std.Out

Captures the output of services to assist with debugging.  
This project is tailored towards AWS, and is not suitable for general purpose diagnostics.  

![Console Standard Out Visualization](assets/ConsoleStandardOut.webp)  

# Nuget

[.NET CLI](https://www.nuget.org/packages/md.stdout.cli)
```console
dotnet tool install --global md.stdout.cli
```

**RUN**
```console
stdout verb [options] [flags]
```

# Tracing

Pulls data from various sources, and displays them:
* **CloudWatch:** Gathers related messages across log streams, and groups.
* **S3:** Download assets files.
* **DynamoDB:** Load items.

# CLI

**Flags**
```
stdout verb [options] --nolog

--nolog | -nl: Disable logging to the console.
```

**CloudWatch**
```console
stdout cw --key appname --cid c6b8c804-34cb-4cf7-b762-d24b644831e9

--key | -k: The name of the configuration in app settings, that defines the log groups to query, and general filter rules.
--cid | -c: The Correlation Id to filter the logs by.
```

**S3**
```console
stdout s3 --key appname --cid c6b8c804-34cb-4cf7-b762-d24b644831e9

--key | -k: The name of the configuration in app settings, that defines the bucket, and path prefix.
--cid | -c: The Correlation Id is part of (or all) of the key, that target files are found under the prefix, and correlation id.

stdout s3 --key appname --path assets/plaintext/c6b8c804-34cb-4cf7-b762-d24b644831e9

--key | -k: The name of the configuration in app settings, that defines the bucket, and path prefix.
--path | -p: The prefix key path of a S3 bucket, to retrieve files from (i.e. a static path, not merged with a correlation id at runtime).
```

**DynamoDB**
```console
stdout db --key appname --cid c6b8c804-34cb-4cf7-b762-d24b644831e9

--key | -k: The name of the configuration in app settings, that defines the table name, and index to use.
--cid | -c: The Correlation Id stored in a table's index. Used to get the item's pk, and sk values; in order to load the db item.

stdout db --key appname --partitionkey pk_value

--key | -k: The name of the configuration in app settings, that defines the table name, and index to use.
--partitionkey | -pk: The Partition Key for an item.

stdout db --key appname --partitionkey pk_value --sortkey sk_value

--key | -k: The name of the configuration in app settings, that defines the table name, and index to use.
--partitionkey | -pk: The Partition Key for an item.
--sortkey | -sk: The Sort Key for an item.
```

# AppSettings

The `appsettings.json` file is found at the tool's installed location: `%USERPROFILE%\.dotnet\tools`  
From there the relative path is: `.store\md.stdout.cli\{VERSION}\md.stdout.cli\{VERSION}\tools\{RUNTIME}\any`  
Where `{VERSION}` is the installed package's version, i.e "**2.0.0**".  
Where `{RUNTIME}` is the installed package's runtime, i.e. "**net8.0**".  

```json
{
  "CloudWatch": {
    "Defaults": {
      "Display": "console|chrome|firefox",
      "Limit": "25",
      "RelativeHours": "1",
      "IsPresentFieldName": "isStructuredLog",
      "CorrelationIdFieldName": "eventProperties.correlationId",
      "Fields": [
        "@timestamp",
        "level",
        "message"
      ],
      "Filters": [
        {
          "Field": "level",
          "Value": "INFO"
        }
      ]
    },
    "Sources": {
      "AppName": {
        "LogGroups": [
          "/aws/lambda/lambda-one",
          "/aws/lambda/lambda-two"
        ]
      }
    }
  },
  "S3": {
    "Defaults": {
      "Display": "console|chrome|firefox",
      "ContentType": "json|text"
    },
    "Sources": {
      "AppName": {
        "Bucket": "bucketName",
        "Prefix": "assets/plaintext/<CID>/",
        "Files": [
          "myfile.json"
        ]
      }
    }
  },
  "DynamoDb": {
    "Defaults": {
      "Display": "console|chrome|firefox",
      "PartitionKeyName": "pk",
      "SortKeyName": "sk"
    },
    "Sources": {
      "AppName": {
        "TableName": "dbCustomers",
        "IndexName": "gsi1",
        "IndexPartitionKeyName": "gsi1pk",
        "IndexSortKeyName": "gsi1sk",
        "IndexPartitionKeyMask": "pk_<CID>",
        "IndexSortKeyMask": "sk_<CID>",
        "Projection": [
          "name",
          "email"
        ]
      }
    }
  }
}
```

`Defaults` are applied to all `Sources` that don't override the property value with their own.   
The "app names" under `Sources` are matched to the `--key` command line argument. 

If sensible defaults can be applied to all (*or most*) sources, then you would only need to set what's different for each source.  
Custom settings can be applied for each "app" under `Sources`.  

Each verb: **cw**, **s3**, and **db**, have their own `Defaults`, and `Sources` sections in app settings.   

## CloudWatch

* `Display:` How you'd like to view the output; console or web browser (_optional: console_).
* `LogGroups:` An array of log group names from AWS CloudWatch (_required_).
* `Limit:` The maximum number of logs to return for a query (_optional: 25_).
* `RelativeHours:` The number of hours to look backwards from "now" (_optional: 1_).
* `IsPresentFieldName:` Selects logs with a particular field that must exist (_optional: omitted from query_).
* `CorrelationIdFieldName:` The field name that contains an Id, that groups all logs together for a particular request (_optional: omitted from query_).
* `Fields:` The CloudWatch fields to select from the query (_optional: @timestamp, @message_).
* `Filters:` Clauses to add to the query, each filter will be in the form: "_and key = value_" (_optional: omitted from query_).

## S3

* `Display:` How you'd like to view the output; console or web browser (_optional: console_).
* `Bucket:` The S3 buckname name, where your logging / debugging output files are stored (_required_).
* `Prefix:` The key path where your files for a particular request can be found under. The Correlation Id from the command line is merged with `<CID>` (_optional: when not using a correlation id_).
* `ContentType:` The expected file contents, used for pretty printing / formatting; only `json`, and `text` are supported for now (_optional_).
* `Files:` The filenames to download, if found under the prefix path (_optional: downloads all matches_).

## DynamoDB

* `Display:` How you'd like to view the output; console or web browser (_optional: console_).
* `TableName:` The name of the DynamoDB table (_required_).
* `PartitionKeyName:` The table's Partition Key name (_required_).
* `SortKeyName:` The table's Sort Key name (_optional_).
* `IndexName:` The table's index name, where the correlation id makes up part of the index's pk, or sk (_optional: when not using a correlation id_).
* `IndexPartitionKeyName:` The name of the index's Partition Key (_optional: when not using a correlation id_).
* `IndexPartitionKeyMask:` The format of the index's pk, the correlation id from the command line is merged with `<CID>` (_optional: when not using a correlation id_).
* `IndexSortKeyName:` The name of the index's Sort Key (_optional_).
* `IndexSortKeyMask:` The format of the index's sk, the correlation id from the command line is merged with `<CID>` (_optional_).
* `Projection:` The item(*s*) attribute(*s*) to select (_optional: returns all attributes_).

# Credits
* [Icon](https://www.flaticon.com/free-icon/bird_2630452) made by [Vitaly Gorbachev](https://www.flaticon.com/authors/vitaly-gorbachev) from [Flaticon](https://www.flaticon.com/)
* [Standard Out Visualization](https://chatgpt.com/) generated from chatgpt (*DALL.E / OpenAI*).

# Changelog

## 1.0.0

* Created console app project, and readme file.

## 1.0.1

* Added diagnostics to determine why settings aren't loading.

## 1.0.2

* Removed diagnostics, fixed pathing issues to the settings file.

## 1.0.3

* Added support for downloading S3 files, and displaying their contents in the browser.

## 2.0.0

* Some breaking changes to the app settings - allowed the browser, or console display options for every target type (_instead of just S3_).
* Added support for DynamoDB items, loading by keys directly, and correlation Id via an index.
