using Microsoft.AspNetCore.Mvc;
using TaskManagerApi.Data;
using TaskManagerApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Obtiene todas las tareas
        /// </summary>
         [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAll()
    {
        var tasks = await _context.Tasks.Find(_ => true).ToListAsync();
        return Ok(tasks.Select(t => new TaskResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            IsCompleted = t.IsCompleted,
            CreatedAt = t.CreatedAt
        }));
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> Create([FromBody] CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            IsCompleted = dto.IsCompleted,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Tasks.InsertOneAsync(task);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            IsCompleted = task.IsCompleted,
            CreatedAt = task.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskDto dto)
    {
        var update = Builders<TaskItem>.Update
            .Set(t => t.Title, dto.Title)
            .Set(t => t.Description, dto.Description)
            .Set(t => t.IsCompleted, dto.IsCompleted);

        await _context.Tasks.UpdateOneAsync(t => t.Id == id, update);
        return NoContent();
    }



        /// <summary>
        /// Obtiene una tarea por ID
        /// </summary>
        /// <param name="id">ID de la tarea</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TaskItem>> GetById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("ID no válido");

            try
            {
                var task = await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
                return task != null ? Ok(task) : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener tarea con ID: {id}");
                return Problem("Error interno al obtener tarea", statusCode: 500);
            }
        }

        /// <summary>
        /// Crea una nueva tarea
        /// </summary>
        /// <param name="task">Datos de la tarea</param>
        

        /// <summary>
        /// Actualiza una tarea existente
        /// </summary>
        /// <param name="id">ID de la tarea</param>
        /// <param name="task">Datos actualizados</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(string id, [FromBody] TaskItemDto taskDto)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("ID no válido");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingTask = await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
                if (existingTask == null)
                    return NotFound();

                var update = Builders<TaskItem>.Update
                    .Set(t => t.Title, taskDto.Title)
                    .Set(t => t.Description, taskDto.Description)
                    .Set(t => t.IsCompleted, taskDto.IsCompleted);

                var result = await _tasks.UpdateOneAsync(t => t.Id == id, update);

                return result.MatchedCount > 0 ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar tarea con ID: {id}");
                return Problem("Error interno al actualizar tarea", statusCode: 500);
            }
        }

        /// <summary>
        /// Elimina una tarea
        /// </summary>
        /// <param name="id">ID de la tarea</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("ID no válido");

            try
            {
                var result = await _tasks.DeleteOneAsync(t => t.Id == id);
                return result.DeletedCount > 0 ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar tarea con ID: {id}");
                return Problem("Error interno al eliminar tarea", statusCode: 500);
            }
        }
    }

    public class TaskItemDto
    {
        [Required(ErrorMessage = "El título es obligatorio")]
        public string Title { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Description { get; set; }

        public bool IsCompleted { get; set; }
    }
}
