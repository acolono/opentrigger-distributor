CONFIGURATION = Debug

build:
	xbuild /tv:4.0 /p:Configuration=$(CONFIGURATION) /t:Rebuild
	
bundle:
	cd com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/ && \
	mkbundle -o distributord distributord.exe *.dll
	
install:
	install -v com.opentrigger.distributor/cli/bin/$(CONFIGURATION)/distributord /usr/local/bin/