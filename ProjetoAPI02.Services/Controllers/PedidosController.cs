using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjetoAPI02.Domain.Entities;
using ProjetoAPI02.DomainServices;
using ProjetoAPI02.Services.Models.Requests;
using ProjetoAPI02.Services.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoAPI02.Services.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post(PedidoPostRequest request, 
            [FromServices] ClienteDomainService clienteDomainService, 
            [FromServices] PedidoDomainService pedidoDomainService)
        {
            try
            {
                //obter os dados do cliente autenticado..
                //utilizando o email do cliente gravado no TOKEN de autenticação
                //User.Identity.Name -> retorna o email do cliente gravado no TOKEN
                var email = User.Identity.Name; //retorna o email do cliente autenticado
                var cliente = clienteDomainService.ObterCliente(email);

                //criando uma lista de itens do pedido
                var itensPedido = new List<ItemPedido>();
                foreach (var item in request.ItensPedido)
                {
                    itensPedido.Add(new ItemPedido
                    {
                        IdItemPedido = Guid.NewGuid(),
                        IdProduto = Guid.Parse(item.IdProduto),
                        Quantidade = item.Quantidade
                    });
                }

                //criando o objeto pedido..
                var pedido = new Pedido
                {
                    IdPedido = Guid.NewGuid(),
                    DataPedido = DateTime.Parse(request.DataPedido),
                    ValorTotal = decimal.Parse(request.ValorPedido)
                };

                //gravar o pedido
                pedidoDomainService.CadastrarPedido(pedido, itensPedido, cliente);

                //objeto para retorno da requisição
                var response = new PostOkResponse
                {
                    Mensagem = "Pedido cadastrado com sucesso."
                };

                return Ok(response);
            }
            catch(Exception)
            {
                //retornar status de erro HTTP 500
                return StatusCode(500, "Erro ao processar o pedido.");
            }
        }
    }
}
