using FlightCast.Data;
using FlightCast.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;


public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserService(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    public User? Authenticate(string username, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == username);
        if (user == null)
        {
            return null; //User not found, maybe I can add an error message here 
        }
        //var passwordHasher = new PasswordHasher<User>();
        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null; //Pasword does not match, maybe I can add an error message here 
        }

        return user; //Authentication successful, return the user. A message can be add it here
    }
    public User RegisterUser(User newUser)
    {
        newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);

        _context.Users.Add(newUser);
        _context.SaveChanges();

        return newUser;
    }

    public User DeleteUser()
    {
        throw new NotImplementedException();
    }
    public User UpdateUser()
    {
        throw new NotImplementedException();
    }
}