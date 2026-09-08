using TMApi.Models;

namespace TMApi.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetTasksAsync();

        Task<TaskItem> GetTaskAsync(int id);

        Task<TaskItem> CreateTaskAsync(CreateTaskDto dto);

        Task<TaskItem?> UpdateTaskAsync(int id,
            UpdateTaskDto dto);

        Task<bool> DeleteTaskAsync(int id);

    }
}
