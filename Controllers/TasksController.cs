using Microsoft.AspNetCore.Mvc;
using TaskManagerApi.Data;
using TaskManagerApi.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace TaskManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly IMongoCollection<TaskItem> _tasks;
        private readonly ILogger<TasksController> _logger;

        public TasksController(TaskContext context, ILogger<TasksController> logger)
        {
            _tasks = context.Tasks;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Iniciando obtención de tareas");
                var tasks = await _tasks.Find(_ => true).ToListAsync();
                _logger.LogInformation($"Se obtuvieron {tasks.Count} tareas correctamente");
                return Ok(tasks);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Error de MongoDB al obtener tareas");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error de base de datos",
                    Detail = ex.Message,
                    Status = 500
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener tareas");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error interno del servidor",
                    Status = 500
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                _logger.LogWarning($"ID no válido recibido: {id}");
                return BadRequest("ID no válido");
            }

            try
            {
                var task = await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
                if (task == null)
                {
                    _logger.LogWarning($"No se encontró tarea con ID: {id}");
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
                    Status = 500
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> Create([FromBody] TaskItem task)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Modelo inválido recibido");
                return BadRequest(ModelState);
            }

            try
            {
                task.Id = ObjectId.GenerateNewId().ToString();
                task.CreatedAt = DateTime.UtcNow;

                await _tasks.InsertOneAsync(task);
                _logger.LogInformation($"Tarea creada con ID: {task.Id}");

                return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
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
                    Title = "Error al crear tarea",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] TaskItem task)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                _logger.LogWarning($"ID no válido recibido para actualización: {id}");
                return BadRequest("ID no válido");
            }

            if (id != task.Id)
            {
                _logger.LogWarning("ID de la tarea no coincide");
                return BadRequest("ID de la tarea no coincide");
            }

            try
            {
                var existingTask = await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
                if (existingTask == null)
                {
                    _logger.LogWarning($"No se encontró tarea para actualizar con ID: {id}");
                    return NotFound();
                }

                task.CreatedAt = existingTask.CreatedAt;

                var result = await _tasks.ReplaceOneAsync(t => t.Id == id, task);
                if (result.MatchedCount == 0)
                {
                    _logger.LogWarning($"No se pudo actualizar tarea con ID: {id}");
                    return NotFound();
                }

                _logger.LogInformation($"Tarea actualizada con ID: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar tarea con ID: {id}");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error al actualizar tarea",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                _logger.LogWarning($"ID no válido recibido para eliminación: {id}");
                return BadRequest("ID no válido");
            }

            try
            {
                var result = await _tasks.DeleteOneAsync(t => t.Id == id);
                if (result.DeletedCount == 0)
                {
                    _logger.LogWarning($"No se encontró tarea para eliminar con ID: {id}");
                    return NotFound();
                }

                _logger.LogInformation($"Tarea eliminada con ID: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar tarea con ID: {id}");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Error al eliminar tarea",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var count = await _tasks.CountDocumentsAsync(_ => true);
                return Ok(new 
                {
                    success = true,
                    message = $"Conexión exitosa. Tareas encontradas: {count}",
                    server = _tasks.Database.Client.Settings.Server.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
