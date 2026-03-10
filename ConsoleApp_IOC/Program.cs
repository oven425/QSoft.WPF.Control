// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

Console.WriteLine("Hello, World!");

var builder = Host.CreateApplicationBuilder(args);

//builder.Services.AddTransient(typeof(CsvDataProvider<>));
//builder.Services.AddTransient(typeof(JsonDataProvider<>));
builder.Services.AddTransient<StringCsvDataProvider>();
builder.Services.AddTransient<IntCsvDataProvider>();
builder.Services.AddTransient<StringJsonDataProvider>();
builder.Services.AddTransient<IntJsonDataProvider>();
builder.Services.AddTransient<Result>();
builder.Services.AddHostedService<MainService>();
using IHost host = builder.Build();
await host.RunAsync();


public class MainService(Result result) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public class Result(CsvDataProvider<int> csv, JsonDataProvider<string> json)
{
    public void Show()
    {
        var csvData = csv.GetData();
        var jsonData = json.GetData();
        Console.WriteLine($"CSV Data: {csvData}");
        Console.WriteLine($"JSON Data: {jsonData}");
    }
}

public interface IDataProvider<T>
{
    T GetData();
}

public class CsvDataProvider<T>
{
    public virtual T GetData()
    {
        return default(T);
    }
}

public class JsonDataProvider<T>
{
    public virtual T GetData()
    {
        return default(T);
    }
}

// CSV Data Provider 子類別 - 回傳 List<T>
public class StringCsvDataProvider : IDataProvider<List<string>>
{
    public List<string> GetData()
    {
        return
        [
            "value1",
            "value2",
            "value3"
        ];
    }
}

public class IntCsvDataProvider : IDataProvider<List<int>>
{
    public List<int> GetData()
    {
        return
        [
            1,
            2,
            3,
            4,
            5
        ];
    }
}

// JSON Data Provider 子類別 - 回傳 object
public class StringJsonDataProvider : IDataProvider<object>
{
    public object GetData()
    {
        return new
        {
            Name = "John",
            Age = 30,
            City = "Taipei"
        };
    }
}

public class IntJsonDataProvider : IDataProvider<object>
{
    public object GetData()
    {
        return new
        {
            Count = 100,
            Status = "Active",
            Values = new[] { 10, 20, 30 }
        };
    }
}