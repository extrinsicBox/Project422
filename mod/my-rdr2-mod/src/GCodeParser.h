// filepath: /my-rdr2-mod/src/GCodeParser.h
#pragma once
#include <string>
#include <vector>

struct GCodeCommand {
    float x, y, z;
    float a, b;
    float f;
};

class GCodeParser {
public:
    GCodeParser(const std::string& filePath);
    std::vector<GCodeCommand> parse();

private:
    std::string filePath;
};