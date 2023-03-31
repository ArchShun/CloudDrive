namespace CloudDrive.Entities;

public enum FileType
{
    Video = 0b1,
    Audio = 0b10,
    Picture = 0b100,
    Document = 0b1000,
    Application = 0b10000,
    BitTorrent = 0b100000,
    Other =0b1000000,
    All = 0b1111111
}
