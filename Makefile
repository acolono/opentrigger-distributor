CONFIGURATION = Release
ILREPACK = mono $(shell readlink -f `find packages/ -name 'ILRepack.exe'`)
TARGET = Build

.PHONY: build install bundle

build: packages
	xbuild /tv:4.0 /p:Configuration=$(CONFIGURATION) /t:$(TARGET)
	
bundle: build
	cd com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/ && \
	$(ILREPACK) /out:libdistributor.o /wildcards /ndebug libdistributor.dll *.dll && \
	$(ILREPACK) /out:distributord distributord.exe /ndebug libdistributor.o
	
install:
	install -v com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/distributord /usr/local/bin/
	mkdir -p /etc/opentrigger/
	touch /etc/opentrigger/distributord.json
	install -v supervisor/hci.sh /usr/local/bin/othciinit
	install -v supervisor/distributor.conf /etc/supervisor/conf.d/distributord.conf

packages:
	nuget restore