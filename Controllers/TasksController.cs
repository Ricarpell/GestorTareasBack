using Microsoft.AspNetCore.Mvc;
using TaskManagerApi.Data;
using TaskManagerApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace TaskManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly TaskContext _context;
        private readonly ILogger<TasksController> _logger;

        public TasksController(TaskContext context, ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            try
            {
                var tasks = await _context.Tasks.Find(_ => true).ToListAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tareas");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error al obtener tareas",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(string id)
        {
            try
            {
                if (!ObjectId.TryParse(id, out _))
                {
                    return BadRequest("ID no válido");
                }

                var task = await _context.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();

                if (task == null)
                {
                    return NotFound();
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener tarea con ID: {id}");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error al obtener tarea",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask([FromBody] TaskItem task)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var newTask = new TaskItem
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Title = task.Title,
                    Description = task.Description,
                    IsCompleted = task.IsCompleted,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Tasks.InsertOneAsync(newTask);

                return CreatedAtAction(nameof(GetTask), new { id = newTask.Id }, newTask);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogError(ex, "Error de clave duplicada");
                return Conflict("Ya existe una tarea con ese identificador");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tarea");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error al crear la tarea",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(string id, [FromBody] TaskItem task)
        {
            try
            {
                if (!ObjectId.TryParse(id, out _) || !ObjectId.TryParse(task.Id, out _))
                {
                    return BadRequest("ID no válido");
                }

                if (id != task.Id)
                {
                    return BadRequest("ID de la tarea no coincide");
                }

                // Asegurarse de que no se modifique el CreatedAt
                task.CreatedAt = (await _context.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync())?.CreatedAt ?? DateTime.UtcNow;

                var result = await _context.Tasks.ReplaceOneAsync(t => t.Id == id, task);

                if (result.MatchedCount == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar tarea con ID: {id}");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error al actualizar tarea",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(string id)
        {
            try
            {
                if (!ObjectId.TryParse(id, out _))
                {
                    return BadRequest("ID no válido");
                }

                var result = await _context.Tasks.DeleteOneAsync(t => t.Id == id);

                if (result.DeletedCount == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar tarea con ID: {id}");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error al eliminar tarea",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }
    }
}