// CartManagementController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.Security.Claims;
using Store.Models;
using System.Text.Json;
using System.Linq;

namespace Store.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartManagementController : ControllerBase
{
    private readonly Client _supabaseClient;

    public CartManagementController(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    private async Task<Product?> GetProductById(string productId)
    {
        try
        {
            var response = await _supabaseClient
                .From<Product>()
                .Where(x => x.Id == productId)
                .Single();

            return response;
        }
        catch
        {
            return null; // Product not found or error
        }
    }

    [HttpPost]
    [Authorize(Roles = "user,Admin")]
    public async Task<IActionResult> AddToCart([FromBody] CreateCartItemDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            if (string.IsNullOrEmpty(dto.ProductId))
                return BadRequest(new { Message = "Product ID is required" });

            if (dto.Quantity <= 0)
                return BadRequest(new { Message = "Quantity must be greater than 0" });

            // Check if product exists
            var product = await GetProductById(dto.ProductId);
            if (product == null)
                return NotFound(new { Message = "Product not found" });

            var cart = await _supabaseClient.From<Cart>()
                .Where(x => x.UserId == userId)
                .Single();

            List<CartItemSimple> items;
            bool isNewCart = cart == null;

            if (isNewCart)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ItemsJson = "[]"
                };
                items = new List<CartItemSimple>();
            }
            else
            {
                items = string.IsNullOrEmpty(cart.ItemsJson) || cart.ItemsJson == "[]"
                    ? new List<CartItemSimple>()
                    : JsonSerializer.Deserialize<List<CartItemSimple>>(cart.ItemsJson) ?? new List<CartItemSimple>();
            }

            var existingItem = items.FirstOrDefault(i => i.ProductId == dto.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                items.Add(new CartItemSimple
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            cart.ItemsJson = JsonSerializer.Serialize(items);

            if (isNewCart)
                await _supabaseClient.From<Cart>().Insert(cart);
            else
                await _supabaseClient.From<Cart>().Update(cart);

            return Ok(new { Message = "Added to cart successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{productId}")]
    [Authorize(Roles = "user,Admin")]
    public async Task<IActionResult> UpdateCartItem(string productId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            if (dto.Quantity <= 0)
                return BadRequest(new { Message = "Quantity must be greater than 0" });

            var cart = await _supabaseClient.From<Cart>()
                .Where(x => x.UserId == userId)
                .Single();

            if (cart == null)
                return NotFound(new { Message = "Cart not found" });

            var items = JsonSerializer.Deserialize<List<CartItemSimple>>(cart.ItemsJson) ?? new List<CartItemSimple>();

            var item = items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                return NotFound(new { Message = "Item not found in cart" });

            item.Quantity = dto.Quantity;
            cart.ItemsJson = JsonSerializer.Serialize(items);
            await _supabaseClient.From<Cart>().Update(cart);

            return Ok(new { Message = "Cart item updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{productId}")]
    [Authorize(Roles = "user,Admin")]
    public async Task<IActionResult> RemoveCartItem(string productId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var cart = await _supabaseClient.From<Cart>()
                .Where(x => x.UserId == userId)
                .Single();

            if (cart == null)
                return NotFound(new { Message = "Cart not found" });

            var items = JsonSerializer.Deserialize<List<CartItemSimple>>(cart.ItemsJson) ?? new List<CartItemSimple>();

            var item = items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                return NotFound(new { Message = "Item not found in cart" });

            items.Remove(item);
            cart.ItemsJson = JsonSerializer.Serialize(items);
            await _supabaseClient.From<Cart>().Update(cart);

            return Ok(new { Message = "Cart item removed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "user,Admin")]
    public async Task<IActionResult> GetAllCartItems()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var cart = await _supabaseClient.From<Cart>()
                .Where(x => x.UserId == userId)
                .Single();

            if (cart == null || string.IsNullOrEmpty(cart.ItemsJson) || cart.ItemsJson == "[]")
            {
                return Ok(new { CartItems = new List<CartItemResponseDto>() });
            }

            var cartItems = JsonSerializer.Deserialize<List<CartItemSimple>>(cart.ItemsJson)!;

            var responseItems = new List<CartItemResponseDto>();

            foreach (var item in cartItems)
            {
                var product = await GetProductById(item.ProductId);
                if (product != null)
                {
                    responseItems.Add(new CartItemResponseDto
                    {
                        ProductId = product.Id,
                        ProductName = product.ProductName,
                        Price = product.Price,
                        ImageBase64 = product.ImageBase64,
                        Quantity = item.Quantity
                    });
                }
            }

            return Ok(new { CartItems = responseItems });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}