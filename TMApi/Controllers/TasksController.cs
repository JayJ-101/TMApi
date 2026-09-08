using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMApi.Data;
using TMApi.Models;
using TMApi.Services;

namespace TMApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    [Authorize]
    public class TasksController    : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
             _taskService = taskService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var tasks = await _taskService.GetTasksAsync();

            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            var task = await _taskService.CreateTaskAsync(dto);

            return CreatedAtAction(
                nameof(GetTask),
                new { id = task.Id },
                task);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var task = await _taskService.GetTaskAsync(id);

            if (task == null)
                return NotFound();
            
            return Ok(task);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto dto)
        {
            var task = await _taskService.UpdateTaskAsync(
                 id,
                 dto);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var deleted = await _taskService.DeleteTaskAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
