﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests;

// see https://stackoverflow.com/questions/1408175/execute-unit-tests-serially-rather-than-in-parallel
[Collection("Sequential")]
public class TestMaxBytes
{
    private readonly ITestOutputHelper _output;

    public TestMaxBytes(ITestOutputHelper output)
    {
        _output = output;
    }

    private IDistributedFileStoreCacheStringWithExtras SetupCache(int maxBytes)
    {
        var services = new ServiceCollection();
        services.AddDistributedFileStoreCache(options =>
        {
            options.WhichInterface = DistributedFileStoreCacheInterfaces.DistributedFileStoreStringWithExtras;
            options.PathToCacheFileDirectory = TestData.GetTestDataDir();
            options.SecondPartOfCacheFileName = GetType().Name;
            options.TurnOffStaticFilePathCheck = true;

            options.MaxBytesInJsonCacheFile = maxBytes;
        });
        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IDistributedFileStoreCacheStringWithExtras>();
    }

    [Theory]
    [InlineData(100, 1)]
    [InlineData(200, 2)]
    public void TestFailsOnMaxBytes(int maxBytes, int numValues)
    {
        //SETUP
        var cache = SetupCache(maxBytes);
        cache.ClearAll();

        //ATTEMPT
        cache.Set("Test1", "123456789012345678901234567890", null);
        cache.Set("Test2", "123456789012345678901234567890", null);

        //VERIFY
        cache.GetAllKeyValues().Count.ShouldEqual(numValues);
    }

    [Fact]
    public void TestJsonSerializerOptionsUnsafeRelaxedJsonEscaping()
    {
        var x = new Dictionary<string, short[]> {
            {"Test1", new short[] { 1,2,3,40,60,255 }},
            {"Test2", new short[] { 400, 32000 }}
        };
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var jsonString = JsonSerializer.Serialize(x, options);
        _output.WriteLine(jsonString);

        var result = JsonSerializer.Deserialize<Dictionary<string, short[]>>(jsonString);

        result.ShouldEqual(x);
    }

}