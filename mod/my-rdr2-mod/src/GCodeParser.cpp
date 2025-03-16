// filepath: /my-rdr2-mod/src/GCodeParser.cpp
#include "GCodeParser.h"
#include <fstream>
#include <sstream>

GCodeParser::GCodeParser(const std::string& filePath) : filePath(filePath) {}

std::vector<GCodeCommand> GCodeParser::parse() {
    std::vector<GCodeCommand> commands;
    std::ifstream file(filePath);
    std::string line;

    while (std::getline(file, line)) {
        if (line[0] == 'G') {
            GCodeCommand cmd;
            std::istringstream iss(line);
            std::string token;

            while (iss >> token) {
                if (token[0] == 'X') cmd.x = std::stof(token.substr(1));
                else if (token[0] == 'Y') cmd.y = std::stof(token.substr(1));
                else if (token[0] == 'Z') cmd.z = std::stof(token.substr(1));
                else if (token[0] == 'A') cmd.a = std::stof(token.substr(1));
                else if (token[0] == 'B') cmd.b = std::stof(token.substr(1));
                else if (token[0] == 'F') cmd.f = std::stof(token.substr(1));
            }

            commands.push_back(cmd);
        }
    }

    return commands;
}