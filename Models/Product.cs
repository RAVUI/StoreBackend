using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;


namespace Store.Models;

[Table("products")]
public class Product : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; }
    [Column("product_name")]
    public string ProductName { get; set; }
    [Column("description")]
    public string Description { get; set; }
    [Column("image_base64")]
    public string ImageBase64 { get; set; }
    [Column("price")]
    public decimal Price { get; set; }
    [Column("in_stock")]
    public bool InStock { get; set; }
    [Column("posted_by_email")]
    public string PostedByEmail { get; set; }
    [Column("posted_by_user_id")]
    public string PostedByUserId { get; set; }
    [Column("posted_at")]
    public DateTime PostedAt { get; set; }
}

public class CreateProductDto
{
    public string ProductName { get; set; }
    public string Description { get; set; }
    public string ImageBase64 { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public class UpdateProductDto
{
    public string ProductName { get; set; }
    public string Description { get; set; }
    public string ImageBase64 { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public class ProductDto
{
    public string Id { get; set; }
    public string ProductName { get; set; }
    public string Description { get; set; }
    public string ImageBase64 { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public string PostedByEmail { get; set; }
    public DateTime PostedAt { get; set; }
}