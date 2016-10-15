VERSION = $(shell git describe --tags | sed -e 's/^v//')
ARCH = all
PKGBASE = opentrigger
PKGNAME = $(PKGBASE)-v$(VERSION)_$(ARCH)
PKGDEB = $(PKGNAME).deb
BINTRAYDIST = jessie
BINTRAYCOMP = main

.PHONY: build publish

build:
	mkdir -p $(PKGNAME)/DEBIAN
	cp $(PKGBASE).control $(PKGNAME)/DEBIAN/control
	sed -i 's/__VERSION__/$(VERSION)/g' $(PKGNAME)/DEBIAN/control
	fakeroot dpkg-deb --build $(PKGNAME)
	rm -rf $(PKGNAME) 2> /dev/null || true
	dpkg-deb -I $(PKGNAME).deb
	
publish:
	@test -n "$(BINTRAYAUTH)" || { echo "Error: BINTRAYAUTH not defined" ; false ; }
	@test -f "$(PKGNAME).deb" || { echo "Error: $(PKGNAME).deb does not exist" ; false ; }
	@curl -u "$(BINTRAYAUTH)" -X POST -H "Content-Type: application/json" -d '{"name":"$(VERSION)","desc":"$(PKGBASE) v$(VERSION)"}' https://bintray.com/api/v1/packages/ao/opentrigger/$(PKGBASE)/versions
	@curl -u "$(BINTRAYAUTH)" -X PUT --data-binary "@$(PKGDEB)" -H "X-Bintray-Publish: 1" -H "X-Bintray-Override: 1" -H "X-Bintray-Debian-Distribution: $(BINTRAYDIST)" -H "X-Bintray-Debian-Component: $(BINTRAYCOMP)" -H "X-Bintray-Debian-Architecture: $(ARCH)" 'https://bintray.com/api/v1/content/ao/opentrigger/$(PKGBASE)/$(VERSION)/pool/main/o/$(PKGDEB)'
	
clean:
	rm -f *.deb