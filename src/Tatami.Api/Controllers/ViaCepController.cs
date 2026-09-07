using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/viacep")]
[Authorize]
public class ViaCepController : ControllerBase
{
    private static readonly JsonSerializerOptions ViaCepJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public ViaCepController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(
        [FromQuery] string cep,
        CancellationToken cancellationToken)
    {
        var digits = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            return BadRequest(new { error = "CEP inválido." });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ViaCep");
            using var response = await client.GetAsync($"ws/{digits}/json/", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return NotFound(new { error = "CEP não encontrado." });
            }

            var payload = await response.Content.ReadFromJsonAsync<ViaCepApiResponse>(
                ViaCepJsonOptions,
                cancellationToken);

            if (payload is null || payload.Erro)
            {
                return NotFound(new { error = "CEP não encontrado." });
            }

            return Ok(new ViaCepResponse(
                payload.Cep,
                payload.Logradouro,
                payload.Bairro,
                payload.Localidade,
                payload.Uf));
        }
        catch
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Erro ao consultar CEP." });
        }
    }

    private sealed class ViaCepApiResponse
    {
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; }
        public string? Uf { get; set; }

        [JsonPropertyName("erro")]
        public bool Erro { get; set; }
    }

    private sealed record ViaCepResponse(
        string? Cep,
        string? Logradouro,
        string? Bairro,
        string? Localidade,
        string? Uf);
}
