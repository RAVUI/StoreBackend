namespace Store.Models.Dtos;

public class UpdateRoleDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}