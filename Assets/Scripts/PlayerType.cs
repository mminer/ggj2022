using System;

[Flags]
public enum PlayerType
{
    Player1 = 1,
    Player2 = 2,

    Both = Player1 | Player2,
    None = 0,
}
