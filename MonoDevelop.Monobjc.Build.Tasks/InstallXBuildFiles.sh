#!/bin/bash

DESTDIR="/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild/Monobjc/"

sudo mkdir -vp $DESTDIR

sudo cp -v MonobjcApplication.targets $DESTDIR
sudo cp -v ../bin/Debug/MonoDevelop.MacDev.dll $DESTDIR
sudo cp -v ../bin/Debug/MonoDevelop.Monobjc.Build.Tasks.dll* $DESTDIR
