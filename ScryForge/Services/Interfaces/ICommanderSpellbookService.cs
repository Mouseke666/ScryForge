using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ScryForge.Models;

namespace ScryForge.Services.Interfaces
{
    /// <summary>
    /// Interface voor Commander Spellbook service
    /// </summary>
    public interface ICommanderSpellbookService
    {
        /// <summary>
        /// Find combos in the given decklist
        /// </summary>
        /// <param name="decklistLines">List of lines, e.g. "1 Bolt Bend (TLE) 163"</param>
        /// <param name="commanders">Optional commander names</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Raw JSON response from find-my-combos endpoint</returns>
        Task<string?> FindMyCombosAsync(
            List<string> decklistLines,
            List<string>? commanders = null,
            CancellationToken ct = default);
        Task<CommanderSpellbookResult?> FindMyCombosSimpleAsync(List<string> decklistLines, List<string>? commanders = null, CancellationToken ct = default);
    }
}