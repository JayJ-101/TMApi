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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);

            if (task == null)
                return NotFound();
            return Ok(task);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem task)
        {
            if (id != task.Id)
                return BadRequest();

            var existingTask = await _context.TaskItems.FindAsync(id);

            if (existingTask == null)
                return NotFound();

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.IsCompleted = task.IsCompleted;

            await _context.SaveChangesAsync();

            return Ok(existingTask);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);

            if (task == null)
                return NotFound();


            _context.TaskItems.Remove(task);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
