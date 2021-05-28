#!/bin/bash
set -xe
#####################################################
# Generate private key and self-signed certificate. #
#####################################################
certificateFile="codesign"
certificatePassword=$(openssl rand -base64 12)
# certificate request (see https://apple.stackexchange.com/q/359997)
cat >$certificateFile.conf <<EOL
  [ req ]
  distinguished_name = req_name
  prompt = no
  [ req_name ]
  CN = Local Self-Signed
  [ extensions ]
  basicConstraints=critical,CA:false
  keyUsage=critical,digitalSignature
  extendedKeyUsage=critical,1.3.6.1.5.5.7.3.3
  1.2.840.113635.100.6.1.14=critical,DER:0500
EOL
# generate key
openssl genrsa -out $certificateFile.key 2048
# generate self-signed certificate
openssl req -x509 -new -config $certificateFile.conf -nodes -key $certificateFile.key -extensions extensions -sha256 -out $certificateFile.crt
# wrap key and certificate into PKCS12
openssl pkcs12 -export -inkey $certificateFile.key -in $certificateFile.crt -out $certificateFile.p12 -passout pass:$certificatePassword
#######################
# Import certificate. #
#######################
keychain="graphics.keychain"
keychainPassword="graphics"
# Create a new keychain.
security create-keychain -p $keychainPassword $keychain
# Make the keychain default so xcodebuild uses it.
security default-keychain -s $keychain
# Unlock keychain.
security unlock-keychain -p $keychainPassword $keychain
# Import p12 into Keychain
security import $certificateFile.p12 -P "$certificatePassword" -T /usr/bin/codesign # -T /usr/bin/productsign
# List stuff in the keychain. ???
security list-keychains -s `security list-keychains | xargs` ~/Library/Keychains/bokken.keychain
# Make the keychain the default for Xcode stuff and codesign.
security set-key-partition-list -S apple-tool:,apple: -s -k $keychainPassword $keychain