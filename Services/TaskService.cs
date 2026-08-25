using Microsoft.EntityFrameworkCore;
using TMApi.Data;
using TMApi.Models;

namespace TMApi.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskItem>> GetTasksAsync()
        {
            return await _context.TaskItems
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskAsync(int id)
        {
            return await _context.TaskItems
               .AsNoTracking()
               .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskItem> CreateTaskAsync(CreateTaskDto dto)
        {
            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = false
            };

            _context.TaskItems.Add(task);
            
            await _context.SaveChangesAsync();
            
            return task;
        }

        public async Task<TaskItem?> UpdateTaskAsync(int id, UpdateTaskDto dto)
        {
            var task = await _context.TaskItems.FindAsync(id);

            if (task == null)
                return null;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.IsCompleted = dto.IsCompleted;

            await _context.SaveChangesAsync();

            return task;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);

            if (task == null)
            {
                return false;
            }

            _context.TaskItems.Remove(task);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
