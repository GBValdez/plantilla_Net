using System.ComponentModel.DataAnnotations;
namespace project.utils.dto;

public class pagQueryDto
{
    [Range(1, 100)] public int pageSize { get; set; } = 20;
    [Range(1, int.MaxValue)] public int pageNumber { get; set; } = 1;
    public bool all { get; set; } = false;
}
