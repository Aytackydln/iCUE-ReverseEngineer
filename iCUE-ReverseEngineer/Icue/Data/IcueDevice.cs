namespace iCUE_ReverseEngineer.Icue.Data;

public class IcueDevice
{
    public int Type { get; set; }
    public int LedsCount { get; set; }
    public int LogicalLayout { get; set; }
    public int PhysicalLayout { get; set; }
    public string Model { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int CapsMask { get; set; }
}