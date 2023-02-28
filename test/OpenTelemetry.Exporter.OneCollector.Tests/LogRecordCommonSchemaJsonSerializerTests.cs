// <copyright file="LogRecordCommonSchemaJsonSerializerTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.Exporter.OneCollector.Tests;

public class LogRecordCommonSchemaJsonSerializerTests
{
    [Fact]
    public void EmptyLogRecordJsonTest()
    {
        string json = GetLogRecordJson(1, (index, logRecord) => { });

        Assert.Equal(
            "{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{\"severityText\":\"Trace\",\"severityNumber\":1}}\n",
            json);
    }

    [Fact]
    public void MultipleEmptyLogRecordJsonTest()
    {
        string json = GetLogRecordJson(2, (index, logRecord) => { });

        Assert.Equal(
            "{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{\"severityText\":\"Trace\",\"severityNumber\":1}}\n"
            + "{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{\"severityText\":\"Trace\",\"severityNumber\":1}}\n",
            json);
    }

    [Theory]
    [InlineData(LogLevel.Trace, "Trace", 1)]
    [InlineData(LogLevel.Debug, "Debug", 5)]
    [InlineData(LogLevel.Information, "Information", 9)]
    [InlineData(LogLevel.Warning, "Warning", 13)]
    [InlineData(LogLevel.Error, "Error", 17)]
    [InlineData(LogLevel.Critical, "Critical", 21)]
    [InlineData(LogLevel.None, "Trace", 1)]
    public void LogRecordLogLevelJsonTest(LogLevel logLevel, string severityText, int severityNumber)
    {
        string json = GetLogRecordJson(1, (index, logRecord) =>
        {
            logRecord.LogLevel = logLevel;
        });

        Assert.Equal(
            $"{{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{{\"severityText\":\"{severityText}\",\"severityNumber\":{severityNumber}}}}}\n",
            json);
    }

    [Theory]
    [InlineData("MyClass.Company", null)]
    [InlineData("MyClass.Company", "MyEvent")]
    public void LogRecordCategoryNameAndEventNameJsonTest(string categoryName, string? eventName)
    {
        string json = GetLogRecordJson(1, (index, logRecord) =>
        {
            logRecord.CategoryName = categoryName;
            logRecord.EventId = new(0, eventName);
        });

        Assert.Equal(
            $"{{\"ver\":\"4.0\",\"name\":\"{categoryName}.{eventName ?? "Name"}\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{{\"severityText\":\"Trace\",\"severityNumber\":1}}}}\n",
            json);
    }

    [Fact]
    public void LogRecordTimestampJsonTest()
    {
        string json = GetLogRecordJson(1, (index, logRecord) =>
        {
            logRecord.Timestamp = DateTime.SpecifyKind(new DateTime(2023, 1, 18, 10, 18, 0), DateTimeKind.Utc);
        });

        Assert.Equal(
            "{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2023-01-18T10:18:00Z\",\"iKey\":\"o:tenant-token\",\"data\":{\"severityText\":\"Trace\",\"severityNumber\":1}}\n",
            json);
    }

    [Fact]
    public void LogRecordOriginalFormatBodyJsonTest()
    {
        string json = GetLogRecordJson(1, (index, logRecord) =>
        {
            logRecord.StateValues = new List<KeyValuePair<string, object?>> { new KeyValuePair<string, object?>("{OriginalFormat}", "hello world") };
            logRecord.FormattedMessage = "goodbye world";
        });

        Assert.Equal(
            $"{{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{{\"severityText\":\"Trace\",\"severityNumber\":1,\"body\":\"hello world\"}}}}\n",
            json);
    }

    [Fact]
    public void LogRecordFormattedMessageBodyJsonTest()
    {
        string json = GetLogRecordJson(1, (index, logRecord) =>
        {
            logRecord.FormattedMessage = "goodbye world";
        });

        Assert.Equal(
            $"{{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{{\"severityText\":\"Trace\",\"severityNumber\":1,\"body\":\"goodbye world\"}}}}\n",
            json);
    }

    [Fact]
    public void LogRecordResourceJsonTest()
    {
        var resource = ResourceBuilder.CreateEmpty()
            .AddAttributes(new Dictionary<string, object>
            {
                ["resourceKey1"] = "resourceValue1",
                ["resourceKey2"] = "resourceValue2",
            })
            .Build();

        string json = GetLogRecordJson(1, (index, logRecord) => { }, resource);

        Assert.Equal(
            $"{{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{{\"severityText\":\"Trace\",\"severityNumber\":1,\"resourceKey1\":\"resourceValue1\",\"resourceKey2\":\"resourceValue2\"}}}}\n",
            json);
    }

    [Fact]
    public void LogRecordScopesJsonTest()
    {
        var scopeProvider = new ScopeProvider(
            new List<KeyValuePair<string, object?>> { new KeyValuePair<string, object?>("scope1Key1", "scope1Value1"), new KeyValuePair<string, object?>("scope1Key2", "scope1Value2") },
            new List<KeyValuePair<string, object?>> { new KeyValuePair<string, object?>("scope2Key1", "scope2Value1") });

        string json = GetLogRecordJson(1, (index, logRecord) => { }, scopeProvider: scopeProvider);

        Assert.Equal(
            $"{{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{{\"severityText\":\"Trace\",\"severityNumber\":1,\"scope1Key1\":\"scope1Value1\",\"scope1Key2\":\"scope1Value2\",\"scope2Key1\":\"scope2Value1\"}}}}\n",
            json);
    }

    [Fact]
    public void LogRecordStateValuesJsonTest()
    {
        string json = GetLogRecordJson(1, (index, logRecord) =>
        {
            logRecord.StateValues = new List<KeyValuePair<string, object?>> { new KeyValuePair<string, object?>("stateKey1", "stateValue1"), new KeyValuePair<string, object?>("stateKey2", "stateValue2") };
        });

        Assert.Equal(
            $"{{\"ver\":\"4.0\",\"name\":\"Namespace.Name\",\"time\":\"2032-01-18T10:11:12Z\",\"iKey\":\"o:tenant-token\",\"data\":{{\"severityText\":\"Trace\",\"severityNumber\":1,\"stateKey1\":\"stateValue1\",\"stateKey2\":\"stateValue2\"}}}}\n",
            json);
    }

    private static string GetLogRecordJson(
        int numberOfLogRecords,
        Action<int, LogRecord> writeLogRecordCallback,
        Resource? resource = null,
        ScopeProvider? scopeProvider = null)
    {
        var serializer = new LogRecordCommonSchemaJsonSerializer(
            new("Namespace", "Name"),
            "tenant-token");

        using var stream = new MemoryStream();

        var logRecords = new LogRecord[numberOfLogRecords];

        for (int i = 0; i < numberOfLogRecords; i++)
        {
            var logRecord = (LogRecord)Activator.CreateInstance(typeof(LogRecord), nonPublic: true)!;

            logRecord.Timestamp = DateTime.SpecifyKind(new DateTime(2032, 1, 18, 10, 11, 12), DateTimeKind.Utc);

            if (scopeProvider != null)
            {
                var setScopeProviderMethod = typeof(LogRecord).GetProperty("ScopeProvider", BindingFlags.Instance | BindingFlags.NonPublic)?.SetMethod
                    ?? throw new InvalidOperationException("LogRecord.ScopeProvider.Set could not be found reflectively.");

                setScopeProviderMethod.Invoke(logRecord, new object[] { scopeProvider });
            }

            writeLogRecordCallback(i, logRecord);
            logRecords[i] = logRecord;
        }

        var batch = new Batch<LogRecord>(logRecords, numberOfLogRecords);

        serializer.SerializeBatchOfItemsToStream(
            resource ?? Resource.Empty,
            in batch,
            stream,
            initialSizeOfPayloadInBytes: 0,
            out var result);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class ScopeProvider : IExternalScopeProvider
    {
        private readonly List<KeyValuePair<string, object?>>[] scopes;

        public ScopeProvider(params List<KeyValuePair<string, object?>>[] scopes)
        {
            this.scopes = scopes;
        }

        public void ForEachScope<TState>(Action<object, TState> callback, TState state)
        {
            foreach (var scope in this.scopes)
            {
                callback(scope, state);
            }
        }

        public IDisposable Push(object state)
        {
            throw new NotImplementedException();
        }
    }
}