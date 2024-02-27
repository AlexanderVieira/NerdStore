using Microsoft.AspNetCore.Mvc;
using NerdStore.Catalogo.Application.Services;
using NerdStore.Core.Services.WebAPI.Controllers;

namespace NerdStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogoController : MainController
    {
        private readonly IProdutoAppService _produtoAppService;

        public CatalogoController(IProdutoAppService produtoAppService)
        {
            _produtoAppService = produtoAppService;
        }

        [HttpGet("produtos")]
        public async Task<IActionResult> ObterProdutos()
        {
            var response = await _produtoAppService.ObterTodos();

            if (response == null || !response.Any()) return ProcessarRespostaMensagem(StatusCodes.Status404NotFound, "Não existem dados para exibição.");
            
            return RespostaPersonalizada(response);
        }

        [HttpGet("produto-detalhe/{id}")]
        public async Task<IActionResult> ObterProduto(Guid id)
        {
            var response = await _produtoAppService.ObterPorId(id);

            if (response == null) return ProcessarRespostaMensagem(StatusCodes.Status404NotFound, "Produto não encontrado.");
            
            return RespostaPersonalizada(response);
        }

        [HttpGet("produto-categoria/{codigo}")]
        public async Task<IActionResult> ObterProdutosPorCategoria(int codigo)
        {
            var response = await _produtoAppService.ObterPorCategoria(codigo);

            if (response == null || !response.Any()) return ProcessarRespostaMensagem(StatusCodes.Status404NotFound, "Não existem dados para exibição.");

            return RespostaPersonalizada(response);
        }
    }
}
