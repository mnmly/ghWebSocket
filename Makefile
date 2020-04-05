build:
	cd ./ghWebSocket/bin/Release && /Applications/RhinoWIP.app/Contents/Resources/bin/yak build

publish:
	/Applications/RhinoWIP.app/Contents/Resources/bin/yak push $(target)
