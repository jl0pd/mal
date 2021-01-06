all: build

EXE = ./Mal/bin/release/net5.0/linux-x64/publish/Mal

build:
	dotnet publish -c release Mal/Mal.csproj --self-contained -r linux-x64 -p:PublishSingleFile=true

step0_repl.exe: build
	cp $(EXE) ./step0_repl.exe

step1_read_print.exe: build
	cp $(EXE) ./step1_read_print.exe

step2_eval.exe: build
	cp $(EXE) ./step2_eval.exe

clean:
	rm -r Mal/bin Mal/obj
	rm *.exe