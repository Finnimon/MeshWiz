namespace MeshWiz.Slicer;

[Flags]
public enum TcpOptions
{
    None = 0,
    
    
    MovementModeMask=0b11<<0,
    Travel = 0b00<<0,
    StepOver=0b01<<0,
    Subtractive=0b10<<0,
    Additive=0b11<<0,
    
    
    CoolingEnabled=1<<2,
    HeatbedEnabled=1<<3,
    SuctionEnabled=1<<4,
    
    
}