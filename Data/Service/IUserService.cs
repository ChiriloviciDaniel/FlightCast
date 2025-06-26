using FlightCast.Models;

public interface IUserService
{

    User? Authenticate(string username, string password);
    User RegisterUser(User user);
    User UpdateUser();
    User DeleteUser();

}