# Std.Out

Captures output of a program to assist with future debugging.  
This project is tailored towards AWS services, and is not suitable for general purpose diagnostics.  

![Console Standard Out Visualization](assets/ConsoleStandardOut.webp)  

# Tracing

Pulls data from various sources, and displays them:
* **CloudWatch:** Gathers related messages across log streams, and groups.
* **S3:** Download assets files.
* **DynamoDB:** Load related records.

# CLI

**CloudWatch**
```console
cw --key appname --cid b6408f5a-6893-4fb7-b996-3946371ab57f

--key: The name of the configuration in app settings, that defines the log groups to query, and general filter rules.
--cid: The Correlation Id to filter the logs by.
```

**S3**
```console
s3 --key appname --cid b6408f5a-6893-4fb7-b996-3946371ab57f

--key: The name of the configuration in app settings, that defines the bucket, and path prefix.
--cid: The Correlation Id is part of (or all) of the key, the target files are found under the prefix + correlation id.
```

**DynamoDB**
```console
db --key appname --pk b6408f5a-6893-4fb7-b996-3946371ab57f --sk 2022-01-01

--key: The name of the configuration in app settings, that defines the table name, and index to use.
--pk: The Partition Key for an item.
--sk: The Sort Key for an item. If not provided, all sks found under the pk are returned.
```

# AppSettings

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
  }
}
```

## CloudWatch

`Defaults` are applied to all `Sources` that don't override the property value with their own.  
In this example `AnotherAppName` overrides the `Filters` value from `Defaults`.   
The "app names" under `Sources` are matched to the `--key` command line argument.  
```console
cw --key appname --cid 3ee9222f-ed70-475f-8fdc-ee56d1f439da
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

# Credits
* [Icon](https://www.flaticon.com/free-icon/bird_2630452) made by [Vitaly Gorbachev](https://www.flaticon.com/authors/vitaly-gorbachev) from [Flaticon](https://www.flaticon.com/)
* [Standard Out Visualization](https://chatgpt.com/) generated from chatgpt (*DALL.E / OpenAI*).

# Changelog

## 1.0.0

* Created console app project, and readme file.
