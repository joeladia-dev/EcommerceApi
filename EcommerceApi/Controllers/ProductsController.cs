using AutoMapper;
using EcommerceApi.Common;
using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ProductsController(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] int? categoryId = null, [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null, [FromQuery] string? sort = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 20;

        var query = _db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);

        query = sort switch
        {
            "id" => query.OrderBy(p => p.Id),
            "-id" => query.OrderByDescending(p => p.Id),
            "name" => query.OrderBy(p => p.Name),
            "-name" => query.OrderByDescending(p => p.Name),
            "price" => query.OrderBy(p => p.Price),
            "-price" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<ProductDto>>(items);
        return Ok(ApiResponse<List<ProductDto>>.SuccessResponse(dtos));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound(ApiResponse<ProductDto>.FailureResponse("Product not found"));
        var dto = _mapper.Map<ProductDto>(product);
        return Ok(ApiResponse<ProductDto>.SuccessResponse(dto));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(ProductCreateDto input)
    {
        if (input.CategoryId.HasValue)
        {
            var catExists = await _db.Categories.AnyAsync(c => c.Id == input.CategoryId.Value);
            if (!catExists) return BadRequest(ApiResponse<ProductDto>.FailureResponse("CategoryId does not exist."));
        }

        var entity = _mapper.Map<Product>(input);
        _db.Products.Add(entity);
        await _db.SaveChangesAsync();
        var dto = _mapper.Map<ProductDto>(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse<ProductDto>.SuccessResponse(dto, "Product created successfully"));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Update(int id, ProductUpdateDto input)
    {
        if (id != input.Id) return BadRequest(ApiResponse.FailureResponse("ID mismatch."));

        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null) return NotFound(ApiResponse.FailureResponse("Product not found"));
        if (input.CategoryId.HasValue)
        {
            var catExists = await _db.Categories.AnyAsync(c => c.Id == input.CategoryId.Value);
            if (!catExists) return BadRequest(ApiResponse.FailureResponse("CategoryId does not exist."));
        }

        entity.Name = input.Name;
        entity.Description = input.Description;
        entity.Price = input.Price;
        entity.Stock = input.Stock;
        entity.CategoryId = input.CategoryId;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResponse("Product updated successfully"));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound(ApiResponse.FailureResponse("Product not found"));

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResponse("Product deleted successfully"));
    }
}

