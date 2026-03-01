using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ScryForge.Models;
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

            if ((commanders == null || commanders.Count == 0) && decklistLines.Count > 0)
            {
                commanders = new List<string> { decklistLines[0].Trim() };
                decklistLines = decklistLines.Skip(1).ToList();
            }

            var main = decklistLines.Select(line => new { card = line.Trim(), quantity = 1 }).ToList();
            var commanderList = (commanders ?? new List<string>()).Select(c => new { card = c.Trim(), quantity = 1 }).ToList();

            var payload = new { main, commanders = commanderList };

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
                Debug.Write(json);
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when calling find-my-combos");
                return null;
            }
        }

        public async Task<CommanderSpellbookResult?> FindMyCombosSimpleAsync(
    List<string> decklistLines,
    List<string>? commanders = null,
    CancellationToken ct = default)
        {
            try
            {
                var json = await FindMyCombosAsync(decklistLines, commanders, ct);
                Debug.Write(json);
                if (json == null) return null;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var result = new CommanderSpellbookResult();

                // Helper: card lists
                void ParseCardList(string propertyName, List<string> targetList)
                {
                    if (!root.TryGetProperty(propertyName, out var array)) return;
                    targetList.AddRange(array.EnumerateArray()
                        .Select(c => c.GetProperty("name").GetString() ?? string.Empty));
                }

                ParseCardList("gameChangerCards", result.GameChangerCards);
                ParseCardList("massLandDenialCards", result.MassLandDenialCards);
                ParseCardList("extraTurnCards", result.ExtraTurnCards);

                // Helper: generic combos
                void ParseComboArray(string propertyName, List<ComboDetail> targetList)
                {
                    if (!root.TryGetProperty(propertyName, out var combos)) return;

                    foreach (var combo in combos.EnumerateArray())
                    {
                        var comboDetail = new ComboDetail
                        {
                            Description = combo.GetProperty("description").GetString() ?? string.Empty,
                            ManaNeeded = combo.GetProperty("manaNeeded").GetString() ?? string.Empty,
                            ManaValueNeeded = combo.GetProperty("manaValueNeeded").GetInt32()
                        };

                        // CardsUsed
                        if (combo.TryGetProperty("uses", out var uses))
                        {
                            foreach (var use in uses.EnumerateArray())
                            {
                                if (use.TryGetProperty("card", out var card) &&
                                    card.TryGetProperty("name", out var name) &&
                                    !string.IsNullOrEmpty(name.GetString()))
                                {
                                    comboDetail.CardsUsed.Add(name.GetString()!);
                                }
                            }
                        }

                        // FeaturesProduced
                        if (combo.TryGetProperty("produces", out var produces))
                        {
                            foreach (var prod in produces.EnumerateArray())
                            {
                                if (prod.TryGetProperty("feature", out var feature) &&
                                    feature.TryGetProperty("name", out var featureName) &&
                                    !string.IsNullOrEmpty(featureName.GetString()))
                                {
                                    comboDetail.FeaturesProduced.Add(featureName.GetString()!);
                                }
                            }
                        }

                        targetList.Add(comboDetail);
                    }
                }

                // Parse normal combo types
                ParseComboArray("massLandDenialCombos", result.MassLandDenialCombos);
                ParseComboArray("extraTurnCombos", result.ExtraTurnCombos);
                ParseComboArray("lockCombos", result.LockCombos);
                ParseComboArray("controlAllOpponentsCombos", result.ControlAllOpponentsCombos);
                ParseComboArray("controlSomeOpponentsCombos", result.ControlSomeOpponentsCombos);
                ParseComboArray("skipTurnsCombos", result.SkipTurnsCombos);

                // Parse two-card combos (extra "combo" laag)
                if (root.TryGetProperty("twoCardCombos", out var twoCardCombos))
                {
                    foreach (var item in twoCardCombos.EnumerateArray())
                    {
                        if (!item.TryGetProperty("combo", out var combo)) continue;

                        var comboDetail = new ComboDetail
                        {
                            Description = combo.GetProperty("description").GetString() ?? string.Empty,
                            ManaNeeded = combo.GetProperty("manaNeeded").GetString() ?? string.Empty,
                            ManaValueNeeded = combo.GetProperty("manaValueNeeded").GetInt32()
                        };

                        if (combo.TryGetProperty("uses", out var uses))
                        {
                            foreach (var use in uses.EnumerateArray())
                            {
                                if (use.TryGetProperty("card", out var card) &&
                                    card.TryGetProperty("name", out var name) &&
                                    !string.IsNullOrEmpty(name.GetString()))
                                {
                                    comboDetail.CardsUsed.Add(name.GetString()!);
                                }
                            }
                        }

                        if (combo.TryGetProperty("produces", out var produces))
                        {
                            foreach (var prod in produces.EnumerateArray())
                            {
                                if (prod.TryGetProperty("feature", out var feature) &&
                                    feature.TryGetProperty("name", out var featureName) &&
                                    !string.IsNullOrEmpty(featureName.GetString()))
                                {
                                    comboDetail.FeaturesProduced.Add(featureName.GetString()!);
                                }
                            }
                        }

                        result.TwoCardCombos.Add(comboDetail);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
                return null;
            }
        }
    }
}