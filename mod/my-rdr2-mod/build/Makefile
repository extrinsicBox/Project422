CC = g++
CFLAGS = -I../include -Wall -Wextra -std=c++11
LDFLAGS = -shared -o my_rdr2_mod.asi

SRC = ../src/main.cpp \
      ../src/NativeTrainer/main.cpp \
      ../src/NativeTrainer/keyboard.cpp \
      ../src/Pools/script.cpp

all: my_rdr2_mod.asi

my_rdr2_mod.asi: $(SRC)
	$(CC) $(CFLAGS) $(SRC) $(LDFLAGS)

clean:
	rm -f my_rdr2_mod.asi

.PHONY: all clean