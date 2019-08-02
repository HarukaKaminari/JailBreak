-----------------------
-- Name: Decode txt to binary
-- Author: Haruka
-----------------------
-- Merge and decode multiple .txt files to a single binary file(specifically .zip file).
-- The .txt files should be named as a pattern of "????.txt" from 0000 to 9999,
-- and placed inside LuaScriptData/DecodeTxtToBinary folder.
-- Then a reset should be executed manually, and the generated file will appear
-- in the same folder with the filename of "output.zip".
-----------------------

-- The contents of all files
local m_FileContents = "";
local m_ContentsByte = {};

function main()
    m_FileContents = "";
    m_ContentsByte = {};
    loadAllTxtFiles();
    decode();
    saveBinaryFile();
end

function loadAllTxtFiles()
    -- Try load files whose names have the pattern of "????.txt" until loading fails
    for i = 0, 9999 do
        -- fileName pattern: ????.txt (from 0000 to 9999)
        local fileName = string.format("%s/%04d.txt", emu.getScriptDataFolder(), i);
        -- Load the file
        local fs = io.open(fileName, "r");
        if fs == nil then
            -- Load failed, terminate
            break;
        else
            -- Load succeeded, merge contents
            emu.log("Load file '" ..fileName .. "'. Ready to decode!");
            m_FileContents = m_FileContents .. fs:read("*a");
            fs:close();
        end
    end
    emu.log("Total size = " .. m_FileContents:len());
end

function decode()
    -- Get all the bytes of the contents
    local chars = {};
    for i = 1, m_FileContents:len() do
        table.insert(chars, m_FileContents:byte(i));
    end
    local offset = 1;
    local data = (chars[offset] - 0x20) | ((chars[offset + 1] - 0x20) << 6);
    offset = offset + 1;
    local curBitCount = 12;
    while true do
        if curBitCount <= 0 then
            break;
        end
        local lower8bit = data & 0xFF;
        table.insert(m_ContentsByte, lower8bit);
        data = data >> 8;
        curBitCount = curBitCount - 8;
        if curBitCount < 8 then
            offset = offset + 1;
            if offset < #chars + 1 then
                local newData = chars[offset] - 0x20;
                data = data | (newData << curBitCount);
                curBitCount = curBitCount + 6;
                -- Maybe still not enough for one byte (8 bit), try another fetching
                if curBitCount < 8 then
                    offset = offset + 1;
                    if offset < #chars + 1 then
                        local newData2 = chars[offset] - 0x20;
                        data = data | (newData2 << curBitCount);
                        curBitCount = curBitCount + 6;
                    end
                end
            end
        end
    end
end

function saveBinaryFile()
    local fileName = string.format("%s/output.zip", emu.getScriptDataFolder(), i);
    emu.log("Open file '" .. fileName .. "'. Ready to save!");
    local fs = io.open(fileName, "wb");
    for i = 1, #m_ContentsByte do
        fs:write(string.char(m_ContentsByte[i]));
    end
    fs:flush();
    fs:close();
    emu.log("Decoding finished!");
end

-- The main function will be executed when game resets
emu.addEventCallback(main, emu.eventType.reset);
emu.displayMessage("Script", "Decode txt to binary");
