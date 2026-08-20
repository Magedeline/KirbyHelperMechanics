local starBlock = {}

starBlock.name = "KirbyHelperMechanics/K_StarBlock"
starBlock.depth = -1

local sizeTextures = {
    normal = "objects/KHM/kirby/starblock/normal",
    large = "objects/KHM/kirby/starblock/large",
    oversized = "objects/KHM/kirby/starblock/oversized",
}

function starBlock.texture(room, entity)
    return sizeTextures[entity.size or "normal"] or sizeTextures.normal
end

function starBlock.justification(room, entity)
    return {0.0, 0.0}
end

starBlock.fieldInformation = {
    size = {
        options = {"normal", "large", "oversized"},
        editable = false,
    }
}

starBlock.placements = {
    name = "k_star_block",
    data = {
        size = "normal",
    }
}

return starBlock
