CONFIGURATION ?= Release
ILREPACK = mono $(shell readlink -f `find packages/ -name 'ILRepack.exe'`)
TARGET ?= Build
INSTALL_ROOT = /
INSTALL_DEB ?= no

.PHONY: build install test deb checkinstall

build: packages
	xbuild /tv:4.0 /p:Configuration=$(CONFIGURATION) /t:$(TARGET)
	cd com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/ && \
	$(ILREPACK) /out:libdistributor.o /wildcards /ndebug libdistributor.dll *.dll && \
	$(ILREPACK) /out:distributord distributord.exe /ndebug libdistributor.o

install:
	mkdir -p $(INSTALL_ROOT)etc/opentrigger/
	mkdir -p $(INSTALL_ROOT)etc/opentrigger/distributor/
	mkdir -p $(INSTALL_ROOT)usr/bin/
	mkdir -p $(INSTALL_ROOT)etc/supervisor/conf.d/
	
	install -v com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/distributord $(INSTALL_ROOT)usr/bin/
	#TODO: maybe a more generic default config?
	install -v -m 0664 com.opentrigger.distributor/cli/nrf51-config.json $(INSTALL_ROOT)etc/opentrigger/distributor/distributord.json
	install -v supervisor/hci.sh $(INSTALL_ROOT)usr/bin/othciinit
	install -v -m 0664 supervisor/distributor.conf $(INSTALL_ROOT)etc/supervisor/conf.d/distributord.conf

packages:
	nuget restore

test: build
	nunit-console com.opentrigger.distributor/tests/bin/$(CONFIGURATION)/*.dll

checkinstall:
	bash -c 'echo opentrigger.com > description-pak'
	checkinstall -D --default --install=$(INSTALL_DEB) --fstrans=yes --pkgversion `git describe --tags | sed -e 's/^v//'` \
	--pkgname opentrigger-distributor -A all --pkglicense MIT --maintainer 'info@acolono.com' --pkgsource 'https://github.com/acolono/opentrigger-distributor' \
	--pkgrelease $(CONFIGURATION) --requires 'mono-runtime \(\>= 4.2.1\), supervisor, opentrigger-otraw2q' --nodoc make install

deb: VERSION = $(shell git describe --tags | sed -e 's/^v//')
deb: ARCH = all
deb: PKGNAME = opentrigger-distributor_$(VERSION)-$(CONFIGURATION)_$(ARCH)
deb:
	rm -rf $(PKGNAME) 2> /dev/null || true
	rm -rf $(PKGNAME).deb 2> /dev/null || true
	
	mkdir -p $(PKGNAME)/DEBIAN
	cp debian/* $(PKGNAME)/DEBIAN
	sed -i 's/__ARCH__/$(ARCH)/g' $(PKGNAME)/DEBIAN/control
	sed -i 's/__VERSION__/$(VERSION)/g' $(PKGNAME)/DEBIAN/control
	make install -e INSTALL_ROOT=$(PKGNAME)$(INSTALL_ROOT)
	fakeroot dpkg-deb --build $(PKGNAME)
	dpkg-deb -I $(PKGNAME).deb
	@echo !----
	@echo ! to install the package type:
	@echo ! sudo dpkg -i $(PKGNAME).deb
	@echo !----
