using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMApi.Data;
using TMApi.Models;

namespace TMApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class TasksController    : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetTasks()
        {
            var tasks = _context.TaskItems.ToList();
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(TaskItem task)
        {
            task.CreatedAt = DateTime.UtcNow;
            task.IsCompleted = false;

            _context.TaskItems.Add(task);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTasks), 
                new { id = task.Id },
                task);

        }

        
    }
}
