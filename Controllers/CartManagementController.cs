
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.Security.Claims;
using System;
using Store.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    [HttpPost]
    [Authorize(Roles = "user,Admin")]
    public async Task<IActionResult> AddToCart([FromBody] CreateCartItemDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            if (string.IsNullOrEmpty(dto.ProductId))
                return BadRequest(new { Message = "Product ID is required" });

            if (dto.Quantity <= 0)
                return BadRequest(new { Message = "Quantity must be greater than 0" });

            // Fetch the product
            var product = await _supabaseClient.From<Product>()
                .Where(x => x.Id == dto.ProductId)
                .Single();

            if (product == null)
                return NotFound(new { Message = "Product not found" });

            // Check if cart item already exists for this user and product
            var existingCartItem = await _supabaseClient.From<CartItem>()
                .Where(x => x.UserId == userId && x.ProductId == dto.ProductId)
                .Single();

            if (existingCartItem != null)
            {
                // Update quantity by adding the new quantity
                existingCartItem.Quantity += dto.Quantity;
                await _supabaseClient.From<CartItem>().Update(existingCartItem);
                return Ok(new { Message = "Cart item quantity updated successfully", Id = existingCartItem.Id });
            }
            else
            {
                // Create new cart item
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    UserEmail = userEmail,
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Description = product.Description,
                    ImageBase64 = product.ImageBase64,
                    Price = product.Price,
                    Quantity = dto.Quantity,
                    AddedAt = DateTime.UtcNow
                };

                var response = await _supabaseClient.From<CartItem>().Insert(cartItem);
                if (response == null)
                    return BadRequest(new { Message = "Failed to add to cart" });

                return Ok(new { Message = "Added to cart successfully", Id = cartItem.Id });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "user,Admin")]
    public async Task<IActionResult> UpdateCartItem(string id, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            if (dto.Quantity <= 0)
                return BadRequest(new { Message = "Quantity must be greater than 0" });

            var existingCartItem = await _supabaseClient.From<CartItem>()
                .Where(x => x.Id == id)
                .Single();

            if (existingCartItem == null)
                return NotFound(new { Message = "Cart item not found" });

            if (existingCartItem.UserId != userId)
                return Unauthorized(new { Message = "You are not authorized to update this cart item" });

            existingCartItem.Quantity = dto.Quantity;

            await _supabaseClient.From<CartItem>().Update(existingCartItem);

            return Ok(new { Message = "Cart item updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "user,Admin")]
    public async Task<IActionResult> RemoveCartItem(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            var existingCartItem = await _supabaseClient.From<CartItem>()
                .Where(x => x.Id == id)
                .Single();

            if (existingCartItem == null)
                return NotFound(new { Message = "Cart item not found" });

            if (existingCartItem.UserId != userId)
                return Unauthorized(new { Message = "You are not authorized to remove this cart item" });

            await _supabaseClient.From<CartItem>()
                .Where(x => x.Id == id)
                .Delete();

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

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            var cartItems = await _supabaseClient.From<CartItem>()
                .Where(x => x.UserId == userId)
                .Order(x => x.AddedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var cartItemDtos = cartItems.Models.Select(c => new CartItemDto
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductName = c.ProductName,
                Description = c.Description,
                ImageBase64 = c.ImageBase64,
                Price = c.Price,
                Quantity = c.Quantity,
                UserEmail = c.UserEmail,
                AddedAt = c.AddedAt,
                TotalPrice = c.Quantity * c.Price
            }).ToList();

            var totalCartAmount = cartItemDtos.Sum(item => item.TotalPrice);

            return Ok(new
            {
                CartItems = cartItemDtos,
                TotalCartAmount = totalCartAmount
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}