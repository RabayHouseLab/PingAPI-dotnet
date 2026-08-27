using Microsoft.AspNetCore.Mvc;
using PingApi.Api.Models;

namespace PingApi.Api.Controllers;

/// <summary>
/// Controller responsável por verificar a disponibilidade da API.
/// </summary>
[ApiController]
[Route("ping")]
[Produces("application/json")]
public sealed class PingController : ControllerBase
{
    /// <summary>
    /// Verifica se a API está em execução.
    /// </summary>
    /// <returns>Retorna um objeto JSON com a mensagem "pong".</returns>
    /// <response code="200">API está respondendo corretamente.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PingResponse), StatusCodes.Status200OK)]
    public ActionResult<PingResponse> Get()
    {
        return Ok(new PingResponse("pong"));
    }
}
