using Models;

public interface IBackgroundTTDService
{
    Task FetchRemainingThingsToDoAsync(City city);
}