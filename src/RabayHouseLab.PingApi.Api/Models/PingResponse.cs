namespace RabayHouseLab.PingApi.Api.Models;

/// <summary>
/// Representa a resposta padronizada do endpoint de verificação de saúde (ping).
/// </summary>
/// <param name="Message">Mensagem de resposta. Para o endpoint /ping, o valor esperado é "pong".</param>
public sealed record PingResponse(string Message);
