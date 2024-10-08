using Enyim.Caching;
using Enyim.Caching.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddEnyimMemcached(options =>
{
    options.Servers = new List<Server>()
    {
        new Server(){Address = builder.Configuration.GetValue<string>("Memcached:Address"),Port = builder.Configuration.GetValue<int>("Memcached:Port")}
    };

});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseEnyimMemcached();

app.UseHttpsRedirection();



app.MapPost("add-cache", async (
    [FromServices] IMemcachedClient _memclient,
    [FromBody] CacheModel cacheModel) =>
{
    var addResult = await _memclient.AddAsync(
         key: cacheModel.Key,
         value: cacheModel.Value,
         timeSpan: TimeSpan.FromSeconds(cacheModel.ExpirationSecond));

    if (addResult)
        return Results.Ok();

    return Results.BadRequest("The data to be cached already exists !");

});

app.MapPost("set-cache", async (
    [FromServices] IMemcachedClient _client,
    [FromBody] CacheModel cacheModel) =>
{
    await _client.SetAsync(cacheModel.Key, cacheModel.Value, TimeSpan.FromSeconds(cacheModel.ExpirationSecond));

});

app.MapPut("replace-cache", async (
    [FromServices] IMemcachedClient _client,
    [FromBody] CacheModel cacheModel) =>
{
    await _client.ReplaceAsync(cacheModel.Key, cacheModel.Value, TimeSpan.FromMinutes(cacheModel.ExpirationSecond));

});

app.MapPut("increment-by-key", (
      [FromServices] IMemcachedClient _client,
      [FromBody] CacheIncrementOrDecrementModel model) =>
{
    return _client.Increment(model.Key, model.DefaultValue, model.IncrementOrDecrementValue);
});


app.MapPut("decrement-by-key", (
      [FromServices] IMemcachedClient _client,
      [FromBody] CacheIncrementOrDecrementModel model) =>
{
    return _client.Decrement(model.Key, model.DefaultValue, model.IncrementOrDecrementValue);
});

app.MapDelete("delete-all-cache", async (
    [FromServices] IMemcachedClient _client) =>
{

    await _client.FlushAllAsync();
    return Results.Ok();
});

app.MapDelete("delete-cache-by-key", async (
    [FromServices] IMemcachedClient _client,
    [FromQuery] string key) =>
{

    var removeResult = await _client.RemoveAsync(key);

    return removeResult;

});


app.MapGet("get-cache-by-name", async (
    [FromServices] IMemcachedClient _client,
    string key) =>
{
    return Results.Ok(await _client.GetAsync<string>(key));

});

app.MapPut("update-cache-expiration", async (
    [FromServices] IMemcachedClient _client,
    string key) =>
{

    return await _client.TouchAsync(key, TimeSpan.FromMinutes(1));
});

app.MapPut("append", async (
    [FromServices] IMemcachedClient _client,
    [FromBody] CacheAppendOrPrependModel model) =>
{
   var appendResult =  _client.Append(model.key, GetBytes(model.value));

    if (appendResult)
        return Results.Ok("Operation successful");

    return Results.BadRequest("Operation failed");
});

app.MapPut("prepend", (
    [FromServices] IMemcachedClient _client,
    [FromBody] CacheAppendOrPrependModel model) =>
{
    var prependResult = _client.Prepend(model.key, GetBytes(model.value));

    if (prependResult)
        return Results.Ok("Operation successful");

    return Results.BadRequest("Operation failed");
});


app.Run();


static byte[] GetBytes(string value) => Encoding.UTF8.GetBytes(value);

internal record CacheModel(string Key, string Value, double ExpirationSecond);
internal record CacheIncrementOrDecrementModel(string Key, ulong DefaultValue, ulong IncrementOrDecrementValue);
internal record CacheAppendOrPrependModel(string key, string value);