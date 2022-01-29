using System;

[Flags]
enum Player
{
    Player1 = 1,
    Player2 = 2,

    Both = Player1 | Player2,
    None = 0,
}
