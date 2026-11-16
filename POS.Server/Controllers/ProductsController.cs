using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Services;
using POS.Shared.Entities;

namespace POS.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }
        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetAll([FromQuery] bool onlyActive = false)
        {
            return Ok(await _productService.GetAllAsync(onlyActive));
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        [HttpGet("code/{code}")]
        public async Task<ActionResult<Product>> GetByCode(string code)
        {
            var product = await _productService.GetByCodeAsync(code);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Product>> Create(Product product)
        {
            var created = await _productService.CreateAsync(product);
            if (created == null)
            {
                return BadRequest("Datos de producto inválidos o código duplicado");
            }
            return Ok(created);
        }
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Product>> Update(int id, Product product)
        {
            product.Id = id;
            var updated = await _productService.UpdateAsync(product);
            if (!updated)
            {
                return BadRequest("No se pudo actualizar el producto (no existe, datos inválidos o código duplicado)");
            }
            return Ok(product);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
