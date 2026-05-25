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
public class ProductController : ControllerBase
{
    private readonly Client _supabaseClient;

    public ProductController(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto, IFormFile image)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            if (string.IsNullOrWhiteSpace(dto.ProductName))
                return BadRequest(new { Message = "Product name is required" });

            if (image == null || image.Length == 0)
                return BadRequest(new { Message = "Image is required" });

            // Convert image to base64
            string imageBase64;
            using (var stream = image.OpenReadStream())
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBase64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            var product = new Product
            {
                Id = Guid.NewGuid().ToString(),
                ProductName = dto.ProductName,
                Description = dto.Description,
                ImageBase64 = imageBase64,
                Price = dto.Price,
                InStock = dto.InStock,
                Category = dto.Category,
                PostedByEmail = userEmail,
                PostedByUserId = userId,
                PostedAt = DateTime.UtcNow
            };

            var response = await _supabaseClient.From<Product>().Insert(product);
            if (response == null)
                return BadRequest(new { Message = "Failed to create product" });

            return Ok(new { Message = "Product created successfully", Id = product.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(string id, [FromForm] UpdateProductDto dto, IFormFile? image)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            var existingProduct = await _supabaseClient.From<Product>()
                .Where(x => x.Id == id)
                .Single();

            if (existingProduct == null)
                return NotFound(new { Message = "Product not found" });

            if (!string.IsNullOrWhiteSpace(dto.ProductName))
                existingProduct.ProductName = dto.ProductName;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                existingProduct.Description = dto.Description;

            if (!string.IsNullOrWhiteSpace(dto.Category))
                existingProduct.Category = dto.Category;

            if (image != null && image.Length > 0)
            {
                // Convert new image to base64
                string imageBase64;
                using (var stream = image.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    imageBase64 = Convert.ToBase64String(memoryStream.ToArray());
                }
                existingProduct.ImageBase64 = imageBase64;
            }

            existingProduct.Price = dto.Price;
            existingProduct.InStock = dto.InStock;
            existingProduct.PostedByEmail = userEmail;
            existingProduct.PostedByUserId = userId;

            await _supabaseClient.From<Product>().Update(existingProduct);

            return Ok(new { Message = "Product updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            var existingProduct = await _supabaseClient.From<Product>()
                .Where(x => x.Id == id)
                .Single();

            if (existingProduct == null)
                return NotFound(new { Message = "Product not found" });

            await _supabaseClient.From<Product>()
                .Where(x => x.Id == id)
                .Delete();

            return Ok(new { Message = "Product deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProducts()
    {
        try
        {
            var products = await _supabaseClient.From<Product>()
                .Order(x => x.PostedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var productDtos = products.Models.Select(p => new ProductDto
            {
                Id = p.Id,
                ProductName = p.ProductName,
                Description = p.Description,
                ImageBase64 = p.ImageBase64,
                Price = p.Price,
                InStock = p.InStock,
                Category = p.Category,
                PostedByEmail = p.PostedByEmail,
                PostedAt = p.PostedAt
            }).ToList();

            return Ok(productDtos);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}