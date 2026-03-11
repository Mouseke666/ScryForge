using ScryForge.Models;
using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class CommanderSpellbookStep(ICommanderSpellbookService spellBookService, ILogger<CommanderSpellbookStep> logger) : IPipelineStep
{
    public string Name => "Finding Combo's";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        CommanderSpellbookResult? commanderSpellbookResult = await spellBookService.FindMyCombosSimpleAsync(context.DeckListLines);

        if (commanderSpellbookResult != null && !commanderSpellbookResult!.IsEmpty)
        {
            DisplayCombos(commanderSpellbookResult);
        }
        else
        {
            Console.WriteLine("No combos were found in Commander Spellbook result.");
        }
    }

    private static void DisplayCombos(CommanderSpellbookResult combos)
    {
        if (combos == null) return;

        bool isFirstSection = true;

        void PrintStringList(string title, List<string> items)
        {
            if (items == null || items.Count == 0) return;

            if (!isFirstSection)
                Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== {title} ===");
            Console.ResetColor();

            foreach (var item in items)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(item);
                Console.ResetColor();
            }

            isFirstSection = false;
        }

        void PrintComboList(string title, List<ComboDetail> comboList)
        {
            if (comboList == null || comboList.Count == 0) return;

            if (!isFirstSection)
                Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== {title} ===");
            Console.ResetColor();

            int index = 1;
            foreach (var combo in comboList)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{index++}. {combo.Description}");
                Console.ResetColor();

                if (!string.IsNullOrEmpty(combo.ManaNeeded))
                {
                    var firstTurnPart = combo.ManaNeeded;
                    var otherTurnPart = string.Empty;

                    int idx = combo.ManaNeeded.IndexOf("with", StringComparison.OrdinalIgnoreCase);
                    if (idx > 0)
                    {
                        firstTurnPart = combo.ManaNeeded.Substring(0, idx).Trim();
                        otherTurnPart = combo.ManaNeeded.Substring(idx).Trim();
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"   Mana Needed (first turn): {firstTurnPart}");
                    if (!string.IsNullOrEmpty(otherTurnPart))
                        Console.WriteLine($"   Mana Needed (other turns): {otherTurnPart}");
                    Console.WriteLine($"   Mana Value: {combo.ManaValueNeeded}");
                    Console.ResetColor();
                }

                if (combo.CardsUsed.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"   Cards Used: {string.Join(", ", combo.CardsUsed)}");
                    Console.ResetColor();
                }

                if (combo.FeaturesProduced.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"   Features Produced: {string.Join(", ", combo.FeaturesProduced)}");
                    Console.ResetColor();
                }
            }

            isFirstSection = false;
        }

        PrintStringList("Game Changer Cards", combos.GameChangerCards);
        PrintStringList("Mass Land Denial Cards", combos.MassLandDenialCards);
        PrintStringList("Extra Turn Cards", combos.ExtraTurnCards);

        PrintComboList("Mass Land Denial Combos", combos.MassLandDenialCombos);
        PrintComboList("Extra Turn Combos", combos.ExtraTurnCombos);
        PrintComboList("Lock Combos", combos.LockCombos);
        PrintComboList("Control All Opponents Combos", combos.ControlAllOpponentsCombos);
        PrintComboList("Control Some Opponents Combos", combos.ControlSomeOpponentsCombos);
        PrintComboList("Skip Turns Combos", combos.SkipTurnsCombos);
        PrintComboList("Two-Card Combos", combos.TwoCardCombos);
    }
}