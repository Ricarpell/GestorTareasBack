using MongoDB.Driver;
using TaskManagerApi.Models;

namespace TaskManagerApi.Data
{
    public class TaskContext
    {
        private readonly IMongoDatabase _database;

        public TaskContext(IMongoClient mongoClient)
        {
            _database = mongoClient.GetDatabase("TaskManagerDb");
        }

        public IMongoCollection<TaskItem> Tasks => _database.GetCollection<TaskItem>("tasks");
    }
}
