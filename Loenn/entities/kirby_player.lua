local kirby_player = {}

kirby_player.name = "KHM/K_Player"
kirby_player.depth = 0
kirby_player.justification = {0.5, 1.0}
kirby_player.texture = "characters/KHM/kirby/sitDown00"
kirby_player.placements = {
    name = "kirby_player",
    data = {
        isDefaultSpawn = false
    }
}

return kirby_player