# Cert Tool #
A Tool for x509 certificate information and testing

## Usage ##
```
CertTool (action) [options]

Actions:
 1. File                      Certificate info for file or folder
 2. Domain                    Certificate info for url or domain

Options:
 --help                       Show this help
 -i (resource)                Input file, folder, url, or domain
 -c                           Also show chain
 -e                           Also show extensions
 -v                           Validate certificate
 -vo                          Validate certificate offline only
 -x [file]                    Export the certificate as a file
 -xt (export type)            Select the export type (default DER)
 -q                           Suppress certificate error messages
 -r                           Recurse folders when the input is a folder
 -s (pattern)                 Folder search pattern (see below)

Export Types:
 1. DER                       Distinguished Encoding Rules - ASN.1
 2. PEM                       Privacy Enhanced Mail - RFC7468
 3. PFX                       Personal Information Exchange - RFC7292

Search Pattern Info:
 Search Pattern can be a combination of literal and wildcard characters, but it doesn't support regular expressions. The following wildcard specifiers are permitted in the search pattern:
 Wildcard specifier           Matches
  * (asterisk)                 Zero or more characters in that position
  ? (question mark)            Zero or one character in that position
 Characters other than the wildcard are literal characters. For example, the string "*t" searches for all names in ending with the letter "t". The searchPattern string "s*" searches for all names in path beginning with the letter "s"
```

## Notes ##
* https://badssl.com/

## TODO ##
* add cert store action
* test certs with private keys
