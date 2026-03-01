using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class CommanderSpellbookService : ICommanderSpellbookService
    {
        private readonly HttpClient _http;
        private readonly ILogger<CommanderSpellbookService> _logger;

        public CommanderSpellbookService(IHttpClientFactory httpClientFactory,
                                         ILogger<CommanderSpellbookService> logger)
        {
            _logger = logger;
            _http = httpClientFactory.CreateClient("CommanderSpellbook");
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("ScryForge/1.0 (jouw@email.com)");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }


        public async Task<string?> FindMyCombosAsync(List<string> decklistLines, List<string>? commanders = null, CancellationToken ct = default)
        {
            if (decklistLines == null || decklistLines.Count == 0)
                return null;

            // Als er geen commanders zijn opgegeven, neem de eerste regel automatisch als commander
            if ((commanders == null || commanders.Count == 0) && decklistLines.Count > 0)
            {
                commanders = new List<string> { decklistLines[0].Trim() };
                decklistLines = decklistLines.Skip(1).ToList(); // rest wordt main deck
            }

            // Transform lines to expected API format
            var main = decklistLines.Select(line => new
            {
                card = line.Trim(),
                quantity = 1
            }).ToList();

            var commanderList = (commanders ?? new List<string>())
                .Select(c => new { card = c.Trim(), quantity = 1 })
                .ToList();

            var payload = new
            {
                main,
                commanders = commanderList
            };

            try
            {
                var response = await _http.PostAsJsonAsync(
                    "https://backend.commanderspellbook.com/estimate-bracket",
                    payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch combos: {Status} {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when calling find-my-combos");
                return null;
            }
        }
    }
}