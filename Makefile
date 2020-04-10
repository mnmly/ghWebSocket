VERSION := $(shell xmllint --xpath "//*[local-name()='Project']/*[local-name()='PropertyGroup']/*[local-name()='ReleaseVersion']/text()" ghWebSocket/ghWebSocket.csproj)
DIR = ghWebSocket/bin/Release

version:
	@echo $(VERSION)

manifest:
	sed -i -- 's/[[:digit:]]\.[[:digit:]]\.[[:digit:]]/$(VERSION)/g' $(DIR)/manifest.yml
	rm $(DIR)/manifest.yml--

build: manifest
	cd $(DIR) && /Applications/RhinoWIP.app/Contents/Resources/bin/yak build

publish:
	/Applications/RhinoWIP.app/Contents/Resources/bin/yak push $(target)

install:
	/Applications/RhinoWIP.app/Contents/Resources/bin/yak install ghWebSocket
	/Applications/Rhinoceros.app/Contents/Resources/bin/yak install ghWebSocket
