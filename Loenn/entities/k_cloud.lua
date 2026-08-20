-- Loenn plugin for KirbyHelperMechanics/K_Cloud.
-- Placeholder visual: pale rectangle (pink if fragile). Swap the sprite function for a
-- drawableSprite once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local cloud = {}

cloud.name = "KirbyHelperMechanics/K_Cloud"
cloud.depth = -9990
cloud.minimumSize = {8, 8}

cloud.placements = {
    {
        name = "k_cloud",
        data = { width = 32, fragile = false }
    },
    {
        name = "k_cloud_fragile",
        data = { width = 32, fragile = true }
    },
}

local function push(sprites, rectangle)
    local result = rectangle:getDrawableSprite()

    if result[1] ~= nil then
        for _, sprite in ipairs(result) do
            table.insert(sprites, sprite)
        end
    else
        table.insert(sprites, result)
    end
end

function cloud.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width = entity.width or 32
    local fillColor = entity.fragile and {1.0, 0.70, 0.90, 1.0} or {0.91, 0.91, 1.0, 1.0}
    local sprites = {}

    push(sprites, drawableRectangle.fromRectangle("fill", x, y, width, 6, fillColor))
    push(sprites, drawableRectangle.fromRectangle("line", x, y, width, 6, {0.0, 0.0, 0.0, 0.4}))

    return sprites
end

function cloud.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 32, 6)
end

return cloud
