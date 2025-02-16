# Std.Out

Captures output of a program to assist with debugging.  
This project is tailored towards AWS services, and is not suitable for general purpose diagnostics.  

![Console Standard Out Visualization](assets/ConsoleStandardOut.webp)  

# Nuget

[.NET CLI](https://www.nuget.org/packages/md.stdout.cli)
```console
dotnet tool install --global md.stdout.cli
```

**RUN**
```console
stdout verb [options]
```

# Tracing

Pulls data from various sources, and displays them:
* **CloudWatch:** Gathers related messages across log streams, and groups.
* **S3:** Download assets files.
* **DynamoDB:** Load related records (_WIP_).

# CLI

**CloudWatch**
```console
stdout cw --key appname --cid b6408f5a-6893-4fb7-b996-3946371ab57f

--key: The name of the configuration in app settings, that defines the log groups to query, and general filter rules.
--cid: The Correlation Id to filter the logs by.
```

**S3**
```console
stdout s3 --key appname --cid b6408f5a-6893-4fb7-b996-3946371ab57f

--key: The name of the configuration in app settings, that defines the bucket, and path prefix.
--cid: The Correlation Id is part of (or all) of the key, that target files are found under the prefix, and correlation id.
```

**DynamoDB**
```console
stdout db --key appname --pk b6408f5a-6893-4fb7-b996-3946371ab57f --sk 2022-01-01

--key: The name of the configuration in app settings, that defines the table name, and index to use.
--pk: The Partition Key for an item.
--sk: The Sort Key for an item. If not provided, all sks found under the pk are returned.
```

# AppSettings

The `appsettings.json` file is found at the tool's installed location: `%USERPROFILE%\.dotnet\tools`  
From there the relative path is: `.store\md.stdout.cli\{VERSION}\md.stdout.cli\{VERSION}\tools\{RUNTIME}\any`  
Where `{VERSION}` is the installed package's version, i.e "**1.0.2**".  
Where `{RUNTIME}` is the installed package's runtime, i.e. "**net8.0**".  

```json
{
  "CloudWatch": {
    "Defaults": {
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
        },
        {
          "Field": "eventProperties.customerId",
          "Value": "12345678"
        }
      ]
    },
    "Sources": {
      "AppName": {
        "LogGroups": [
          "/aws/lambda/lambda-one",
          "/aws/lambda/lambda-two"
        ]
      },
      "AnotherAppName": {
        "LogGroups": [
          "/aws/lambda/lambda-three"
        ],
        "Filters": [
          {
            "Field": "level",
            "Value": "ERROR"
          }
        ]
      }
    }
  },
  "S3": {
    "Defaults": {
      "ContentType": "json",
      "BrowserDisplay": "chrome"
    },
    "Sources": {
      "AppName": {
        "Bucket": "bucketName",
        "Prefix": "assets/text/<CID>/"
      }
    }
  }
}
```

## CloudWatch

`Defaults` are applied to all `Sources` that don't override the property value with their own.  
In this example `AnotherAppName` overrides the `Filters` value from `Defaults`.   
The "app names" under `Sources` are matched to the `--key` command line argument.  
```console
stdout cw --key appname --cid 3ee9222f-ed70-475f-8fdc-ee56d1f439da
```

If sensible defaults can be applied to all sources, then you would only need to set the `LogGroups` for each logical "app".  
Otherwise you can have custom settings for each app under `Sources`.  

* `LogGroups:` An array of log group names from AWS CloudWatch (_required_).
* `Limit:` The maximum number of logs to return for a query (_optional: 25_).
* `RelativeHours:` The number of hours to look backwards from "now" (_optional: 1_).
* `IsPresentFieldName:` Selects logs with a particular field that must exist (_optional: omitted from query_).
* `CorrelationIdFieldName:` The field name that contains an Id, that groups all logs together for a particular request (_optional: omitted from query_).
* `Fields:` The CloudWatch fields to select from the query (_optional: @timestamp, @message_).
* `Filters:` Clauses to add to the query, each filter will be in the form: "_and key = value_" (_optional: omitted from query_).

## S3

`Defaults` are applied to all `Sources` that don't override the property value with their own.  
In this example `AppName` inherits the `ContentType`, and `BrowserDisplay` values from `Defaults`.  
The "app names" under `Sources` are matched to the `--key` command line argument.  
```console
stdout s3 --key appname --cid 3ee9222f-ed70-475f-8fdc-ee56d1f439da
```

* `Bucket:` The S3 buckname name, where your logging / debugging output files are stored (_required_).
* `Prefix:` The key path where your files for a particular request can be found under. The Correlation Id from the command line is merged with `<CID>` (_required_).
* `ContentType:` The expected file contents, used for pretty printing / formatting; only json and the raw contents are supported for now (_optional_).
* `BrowserDisplay:` The preferred browser to open when displaying the file contents; only chrome and firefox on windows are supported for now (_required_).


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
