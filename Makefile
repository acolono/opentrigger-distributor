CONFIGURATION ?= Release
ILREPACK = mono $(shell readlink -f `find packages/ -name 'ILRepack.exe'`)
TARGET ?= Build
INSTALL_ROOT = /
INSTALL_DEB ?= no

.PHONY: build install bundle test deb

build: packages
	xbuild /tv:4.0 /p:Configuration=$(CONFIGURATION) /t:$(TARGET)
	
bundle: build
	cd com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/ && \
	$(ILREPACK) /out:libdistributor.o /wildcards /ndebug libdistributor.dll *.dll && \
	$(ILREPACK) /out:distributord distributord.exe /ndebug libdistributor.o
	
install:
	install -v com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/distributord $(INSTALL_ROOT)usr/local/bin/
	mkdir -p $(INSTALL_ROOT)etc/opentrigger/
	mkdir -p $(INSTALL_ROOT)etc/opentrigger/distributor/
	install -v -m 0664 supervisor/etc_readme $(INSTALL_ROOT)etc/opentrigger/distributor/README
	install -v supervisor/hci.sh $(INSTALL_ROOT)usr/local/bin/othciinit
	install -v -m 0664 supervisor/distributor.conf $(INSTALL_ROOT)etc/supervisor/conf.d/distributord.conf

packages:
	nuget restore

test: build
	nunit-console com.opentrigger.distributor/tests/bin/$(CONFIGURATION)/*.dll

deb:
	checkinstall -D --default --install=$(INSTALL_DEB) --fstrans=yes --pkgversion `git describe --tags | sed -e 's/^v//'` \
	--pkgname opentrigger-distributor -A all --pkglicense MIT --maintainer 'info@acolono.com' --pkgsource 'https://github.com/acolono/opentrigger-distributor' \
	--pkgrelease $(CONFIGURATION) --requires 'mono-runtime (>= 4), supervisor' make install
	