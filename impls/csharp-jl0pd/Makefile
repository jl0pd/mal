all: build

EXE = ./Mal/bin/Debug/net5.0/linux-x64/publish/Mal

build:
	dotnet publish Mal/Mal.csproj --self-contained -r linux-x64 -p:PublishSingleFile=true

step0_repl.exe: build
	cp $(EXE) ./step0_repl.exe

clean:
	rm -r bin obj
