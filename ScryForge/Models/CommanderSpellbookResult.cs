namespace ScryForge.Models;

public class CommanderSpellbookResult
{
    public List<string> GameChangerCards { get; set; } = new();
    public List<string> MassLandDenialCards { get; set; } = new();
    public List<string> ExtraTurnCards { get; set; } = new();

    public List<ComboDetail> MassLandDenialCombos { get; set; } = new();
    public List<ComboDetail> ExtraTurnCombos { get; set; } = new();
    public List<ComboDetail> LockCombos { get; set; } = new();
    public List<ComboDetail> ControlAllOpponentsCombos { get; set; } = new();
    public List<ComboDetail> ControlSomeOpponentsCombos { get; set; } = new();
    public List<ComboDetail> SkipTurnsCombos { get; set; } = new();
    public List<ComboDetail> TwoCardCombos { get; set; } = new();
}

public class ComboDetail
{
    public string Description { get; set; } = string.Empty;
    public string ManaNeeded { get; set; } = string.Empty;
    public int ManaValueNeeded { get; set; }
    public List<string> CardsUsed { get; set; } = new();
    public List<string> ZoneLocations { get; set; } = new();
    public List<string> FeaturesProduced { get; set; } = new();
}