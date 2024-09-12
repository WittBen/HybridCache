namespace ConsoleApp1.Service;

public class DatabaseService
{
  public async Task<string> GetUserFromDatabase(int userId)
  {
    await Task.Delay(1);
    return $"{DateTime.Now}: User {userId} - some data";
  }
}
