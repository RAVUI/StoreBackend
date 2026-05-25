// CartModels.cs

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Store.Models;

[Table("carts")]
public class Cart : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("items")]
    public string ItemsJson { get; set; } = "[]";
}

// Only store minimal data in cart
public class CartItemSimple
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class CreateCartItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

// Response DTO - only what you asked for
public class CartItemResponseDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
    public int Quantity { get; set; }
}