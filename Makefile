CONFIGURATION ?= Release
ILREPACK = mono $(shell readlink -f `find packages/ -name 'ILRepack.exe'`)
TARGET ?= Build
INSTALL_ROOT = /

.PHONY: build install bundle test

build: packages
	xbuild /tv:4.0 /p:Configuration=$(CONFIGURATION) /t:$(TARGET)
	
bundle: build
	cd com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/ && \
	$(ILREPACK) /out:libdistributor.o /wildcards /ndebug libdistributor.dll *.dll && \
	$(ILREPACK) /out:distributord distributord.exe /ndebug libdistributor.o
	
install:
	install -v com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/distributord $(INSTALL_ROOT)usr/local/bin/
	mkdir -p $(INSTALL_ROOT)etc/opentrigger/
	touch $(INSTALL_ROOT)etc/opentrigger/distributord.json
	install -v supervisor/hci.sh $(INSTALL_ROOT)usr/local/bin/othciinit
	install -v -m 664 supervisor/distributor.conf $(INSTALL_ROOT)etc/supervisor/conf.d/distributord.conf

packages:
	nuget restore

test: build
	nunit-console com.opentrigger.distributor/tests/bin/$(CONFIGURATION)/*.dll