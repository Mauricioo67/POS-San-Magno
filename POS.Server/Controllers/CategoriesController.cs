using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Services;
using POS.Shared.Entities;

namespace POS.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        [HttpGet]
        public async Task<ActionResult<List<Category>>> GetAll()
        {
            return Ok(await _categoryService.GetAllAsync());
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Category>> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Category>> Create(Category category)
        {
            var created = await _categoryService.CreateAsync(category);
            if (created == null)
            {
                return BadRequest("Datos de categoría inválidos");
            }
            return Ok(created);
        }
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Category>> Update(int id, Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest("El nombre de la categoría es obligatorio");
            }
            category.Id = id;
            var updated = await _categoryService.UpdateAsync(category);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(category);
        }
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _categoryService.DeleteAsync(id);
            if (!deleted)
            {
                return BadRequest("No se pudo eliminar la categoría (no existe o tiene productos asociados)");
            }
            return NoContent();
        }
    }
}
