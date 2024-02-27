using Microsoft.AspNetCore.Mvc;
using NerdStore.Core.Services.WebAPI.Controllers;
using NerdStore.Vendas.Application.Queries;
using System.Diagnostics;

namespace NerdStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendaController : MainController
    {
        private readonly IPedidoQueries _pedidoQueries;

        public VendaController(IPedidoQueries pedidoQueries) {
            this._pedidoQueries = pedidoQueries;
        }

        [HttpGet("carrinho/{cliente_id}")]
        public async Task<IActionResult> ObterCarrinhoCliente(Guid cliente_id) { 
            var response = await this._pedidoQueries.ObterCarrinhoCliente(cliente_id);

            if (response == null) return ProcessarRespostaMensagem(StatusCodes.Status404NotFound, "Não existem dados para exibição.");

            return RespostaPersonalizada(response);
        }

        [HttpGet("pedido/{cliente_id}")]
        public async Task<IActionResult> ObterPedidosCliente(Guid cliente_id)
        {
            var response = await this._pedidoQueries.ObterPedidosCliente(cliente_id);

            if (response == null || !response.Any()) return ProcessarRespostaMensagem(StatusCodes.Status404NotFound, "Não existem dados para exibição.");

            return RespostaPersonalizada(response);
        }
    }
}
