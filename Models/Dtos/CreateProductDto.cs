namespace Store.Models.Dtos;

public class CreateProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public class UpdateProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageBase64 { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public string PostedByEmail { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
}