-- Loenn plugin for KirbyHelperMechanics/K_SwitchGate.
-- Placeholder visual: grey rectangle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local switchGate = {}

switchGate.name = "KirbyHelperMechanics/K_SwitchGate"
switchGate.depth = -9999
switchGate.minimumSize = {8, 8}
switchGate.nodeLimits = {1, 1}
switchGate.nodeLineRenderType = "line"
switchGate.nodeVisibility = "always"

switchGate.placements = {
    name = "k_switch_gate",
    data = {
        width = 16,
        height = 16,
        flag = "touch_switch",
    }
}

local fillColor = {0.56, 0.56, 0.56, 0.7}
local lineColor = {0.15, 0.15, 0.15, 1.0}

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

local function pushRect(sprites, x, y, width, height, fill, line)
    push(sprites, drawableRectangle.fromRectangle("fill", x, y, width, height, fill))
    push(sprites, drawableRectangle.fromRectangle("line", x, y, width, height, line))
end

function switchGate.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local sprites = {}

    pushRect(sprites, x, y, width, height, fillColor, lineColor)

    return sprites
end

function switchGate.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 16, entity.height or 16)
end

return switchGate
