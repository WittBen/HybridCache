namespace ConsoleApp1.Interface;

public interface IHybridCache
{
  Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan duration);
  Task<T> GetAsync<T>(string key);
}