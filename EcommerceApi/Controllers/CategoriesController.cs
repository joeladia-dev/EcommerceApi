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
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CategoriesController(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll()
    {
        var categories = await _db.Categories.Include(c => c.Products).AsNoTracking().ToListAsync();
        var dtos = _mapper.Map<List<CategoryDto>>(categories);
        return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(dtos));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(int id)
    {
        var category = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound(ApiResponse<CategoryDto>.FailureResponse("Category not found"));
        var dto = _mapper.Map<CategoryDto>(category);
        return Ok(ApiResponse<CategoryDto>.SuccessResponse(dto));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(CategoryCreateDto input)
    {
        var entity = _mapper.Map<Category>(input);
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync();
        var dto = _mapper.Map<CategoryDto>(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse<CategoryDto>.SuccessResponse(dto, "Category created successfully"));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Update(int id, CategoryUpdateDto input)
    {
        if (id != input.Id) return BadRequest(ApiResponse.FailureResponse("ID mismatch."));
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return NotFound(ApiResponse.FailureResponse("Category not found"));
        entity.Name = input.Name;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResponse("Category updated successfully"));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null) return NotFound(ApiResponse.FailureResponse("Category not found"));
        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResponse("Category deleted successfully"));
    }

    [HttpGet("{id:int}/products")]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProductsForCategory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null, [FromQuery] string? sort = null)
    {
        var exists = await _db.Categories.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound(ApiResponse<List<ProductDto>>.FailureResponse("Category not found"));
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 20;

        var query = _db.Products.AsNoTracking().Where(p => p.CategoryId == id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);
        query = sort switch
        {
            "name" => query.OrderBy(p => p.Name),
            "-name" => query.OrderByDescending(p => p.Name),
            "price" => query.OrderBy(p => p.Price),
            "-price" => query.OrderByDescending(p => p.Price),
            _ => query.OrderBy(p => p.Id)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var dtos = _mapper.Map<List<ProductDto>>(items);
        return Ok(ApiResponse<List<ProductDto>>.SuccessResponse(dtos));
    }
}
