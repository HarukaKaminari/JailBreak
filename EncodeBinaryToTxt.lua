-----------------------
-- Name: Encode binary to txt
-- Author: Haruka
-----------------------
-- Encode binary file(specifically .zip file) to multiple .txt files.
-- The .zip files should be named as "input.zip",
-- and placed inside LuaScriptData/DecodeTxtToBinary folder.
-- Then a reset should be executed manually, and the generated files will appear
-- in the same folder with the filename of "????.txt" from 0000.
-- The number of files depends on the size of the input file.
-----------------------

-- The contents of all files
local m_ContentsByte = {};
local m_Ret = {};
local m_Capacity = 320 * 1024;

function main()
    m_ContentsByte = {};
    m_Ret = {};
    loadBinaryFile();
    encode();
    saveTxtFiles();
end

function loadBinaryFile()
    local fileName = string.format("%s/input.zip", emu.getScriptDataFolder());
    -- Load the file
    local fs = io.open(fileName, "rb");
    if fs ~= nil then
        emu.log("Load file '" ..fileName .. "'. Ready to encode!");
        while true do
            local tmp = fs:read(1);
            if tmp == nil then
                break;
            end
            table.insert(m_ContentsByte, string.byte(tmp));
        end
        fs:close();
    end
    emu.log("Total size = " .. #m_ContentsByte);
end

function encode()
    local offset = 1;
    local data = m_ContentsByte[offset] | (m_ContentsByte[offset + 1] << 8);
    offset = offset + 1;
    local curBitCount = 16;
    local container = {};
    while true do
        -- Exit the loop if processing completed
        if curBitCount <= 0 then
            break;
        end
        -- Get the lower 6 bit
        local lower6bit = data & 0x3F;
        -- Convert to ASCII
        local c = lower6bit + 0x20;
        table.insert(container, c);
        -- If the container is full, then save it to the list and prepare a new one
        if #container >= m_Capacity then
            table.insert(m_Ret, container);
            container = {};
        end
        -- Get the remains
        data = data >> 6;
        curBitCount = curBitCount - 6;
        -- Fetch the next byte if the remains has less than 6 bits
        if curBitCount < 6 then
            -- Prepare the next byte
            offset = offset + 1;
            -- Fetch the next byte if not processed all data
            if offset < #m_ContentsByte + 1 then
                local newData = m_ContentsByte[offset];
                data = data | (newData << curBitCount);
                curBitCount = curBitCount + 8;
            end
        end
    end
    -- Finally save the last container to the list
    -- If the file size is precisely n times of the capacity, then the last container will be empty
    -- However this will cause nothing harmful. An empty container will be discarded when saving files
    table.insert(m_Ret, container);
end

function saveTxtFiles()
    for i = 1, #m_Ret do
        local fileName = string.format("%s/%04d.txt", emu.getScriptDataFolder(), i - 1);
        emu.log("Open file '" .. fileName .. "'. Ready to save!");
        local tmp = m_Ret[i];
        local fs = io.open(fileName, "w");
        for j = 1, #tmp do
            fs:write(string.char(tmp[j]));
        end
        fs:flush();
        fs:close();
    end
    emu.log("Encoding finished!");
end

-- The main function will be executed when game resets
emu.addEventCallback(main, emu.eventType.reset);
emu.displayMessage("Script", "Encode binary to txt");
