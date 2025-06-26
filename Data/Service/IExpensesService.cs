using FlightCast.Models;

public interface IExpensesService
{
    Task<IEnumerable<Expense>> GetAll();
    Task Add(Expense expense);
}