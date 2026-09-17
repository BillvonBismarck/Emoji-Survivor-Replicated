-- Project-side entry for Play In Editor; original Maker entry stays unchanged.
EmojiEditorBootstrap = ScriptObject()

function EmojiEditorBootstrap:Start()
    print("[EmojiEditorBootstrap] Loading main.lua in PIE")
    EMOJI_EDITOR_RESOURCE_PREFIX = "assets/"
    local engineRequire = require
    local projectModules = {main=true, Config=true, SaveData=true, Achievement=true, MockCloud=true,
        battle=true, ui=true, fx=true, meta=true, social=true, utils=true}
    require = function(name)
        local root = name:match("^[^./]+")
        if projectModules[root] then
            return engineRequire("scripts/" .. name:gsub("%.", "/"))
        end
        return engineRequire(name)
    end
    require("main")
    Start()
    print("[EmojiEditorBootstrap] Game initialized")
end

function EmojiEditorBootstrap:Stop()
    if Stop then Stop() end
end
