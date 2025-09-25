
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Store.Models;

[Table("cart_items")]
public class CartItem : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; }
    [Column("user_id")]
    public string UserId { get; set; }
    [Column("user_email")]
    public string UserEmail { get; set; }
    [Column("product_id")]
    public string ProductId { get; set; }
    [Column("product_name")]
    public string ProductName { get; set; }
    [Column("description")]
    public string Description { get; set; }
    [Column("image_base64")]
    public string ImageBase64 { get; set; }
    [Column("price")]
    public decimal Price { get; set; }
    [Column("quantity")]
    public int Quantity { get; set; }
    [Column("added_at")]
    public DateTime AddedAt { get; set; }
}

public class CreateCartItemDto
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

public class CartItemDto
{
    public string Id { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public string Description { get; set; }
    public string ImageBase64 { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string UserEmail { get; set; }
    public DateTime AddedAt { get; set; }
    public decimal TotalPrice { get; set; }
}