#!/bin/sh

# if nobody else has a mac then message me whenever
# builds are needed for a release and i'll run this
# on my m1 mac.                               ~moss

cd "$(dirname "$0")"
cd ../src
dotnet publish --runtime osx-x64 --self-contained
dotnet publish --runtime osx-arm64 --self-contained

# ok we have the binaries lets uhhhhhhhhh

# sign them. yeah

# ok make sure your mac's keychain has a codesigning cert in it called "LROverhaul Signing Cert"

codesign -s "LROverhaul Signing Cert" -f ../build/net8.0/osx-x64/publish/*
codesign -s "LROverhaul Signing Cert" -f ../build/net8.0/osx-arm64/publish/*

# ok now we actually make the bundles

rm -rf ../build/bundles

mkdir -p ../build/bundles/osx-x64/LROverhaul.app
mkdir -p ../build/bundles/osx-arm64/LROverhaul.app

cp -R ../Bundling/Template.app/* ../build/bundles/osx-x64/LROverhaul.app
cp -R ../Bundling/Template.app/* ../build/bundles/osx-arm64/LROverhaul.app

mv ../build/net8.0/osx-x64/publish/* ../build/bundles/osx-x64/LROverhaul.app/Contents/MacOS/
mv ../build/net8.0/osx-arm64/publish/* ../build/bundles/osx-arm64/LROverhaul.app/Contents/MacOS/

codesign -s "LROverhaul Signing Cert" -f ../build/bundles/osx-x64/LROverhaul.app
codesign -s "LROverhaul Signing Cert" -f ../build/bundles/osx-arm64/LROverhaul.app

# and then assuming create-dmg is installed

create-dmg --codesign "LROverhaul Signing Cert" --icon LROverhaul.app 100 100 --app-drop-link 350 100 ../build/bundles/LROverhaul-osx-x64.dmg ../build/bundles/osx-x64/

create-dmg --codesign "LROverhaul Signing Cert" --icon LROverhaul.app 100 100 --app-drop-link 350 100 ../build/bundles/LROverhaul-osx-arm64.dmg ../build/bundles/osx-arm64/
