using System.ComponentModel.DataAnnotations;

namespace TaskManagerApi.Models
{
    // DTO para creación de tareas
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
        public string Description { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;
    }

    // DTO para actualización de tareas
    public class UpdateTaskDto
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
        public string Description { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }
    }

    // DTO para respuesta (incluye todos los campos)
    public class TaskResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
