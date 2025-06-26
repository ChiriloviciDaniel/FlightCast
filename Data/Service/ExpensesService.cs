using FlightCast.Data;
using FlightCast.Models;
using Microsoft.EntityFrameworkCore;

public class ExpensesService : IExpensesService
{

    private readonly AppDbContext _context;
    public ExpensesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task Add(Expense expense)
    {

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

      //  throw new NotImplementedException();
    }

    public async Task<IEnumerable<Expense>> GetAll()
    {
        var expenses = await _context.Expenses.ToListAsync();
        return expenses;
        //throw new NotImplementedException();
    }
}