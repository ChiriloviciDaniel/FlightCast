using FlightCast.Data;
using FlightCast.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightCast.Controllers;

public class ExpensesController : Controller
{
    // This controller will handle the expenses-related actions.
    // For now, we can just return a view for the index action.
    private readonly IExpensesService _expensesService;
    public ExpensesController(IExpensesService expensesService)
    {
        _expensesService = expensesService;
    }

    public async Task<IActionResult> Index()
    {
        // Here you would typically retrieve expenses from the database
        // and pass them to the view.
        var expenses = await _expensesService.GetAll();
        return View(expenses);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Expense expense)
    {
        if (ModelState.IsValid)
        {
            await _expensesService.Add(expense);
            //_context.Expenses.Add(expense);
            //await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        return View();
    }

    // Additional actions for creating, editing, deleting expenses can be added here.
}
