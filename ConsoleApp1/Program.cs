using ConsoleApp1.Classes;
using ConsoleApp1.Interface;
using ConsoleApp1.Service;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
  public static async Task Main(string[] args)
  {
    var services = new ServiceCollection();
    services.AddMemoryCache();
    services.AddStackExchangeRedisCache(options =>
    {
      options.Configuration = "localhost:6379";
    });
    services.AddSingleton<IHybridCache, HybridCache>();
    services.AddSingleton<DatabaseService>();

    var serviceProvider = services.BuildServiceProvider();
    var cache = serviceProvider.GetService<IHybridCache>();
    var dbService = serviceProvider.GetService<DatabaseService>();

    string cacheKey = "user_123";

    Console.WriteLine("to start press any key");
    Console.ReadKey();

    //simulate database queries
    for (int i = 0; i < 100; i++)
    {
      var userData = await cache.GetOrSetAsync(cacheKey, () => dbService.GetUserFromDatabase(123), TimeSpan.FromSeconds(3));
      Console.WriteLine(userData);
      Thread.Sleep(500);
    }
  }
}