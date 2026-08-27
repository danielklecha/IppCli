# IppCli (`ipp-cli`)

[![NuGet](https://img.shields.io/nuget/v/IppCli.svg)](https://www.nuget.org/packages/IppCli)
[![NuGet downloads](https://img.shields.io/nuget/dt/IppCli.svg)](https://www.nuget.org/packages/IppCli)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/danielklecha/SharpIppNext/blob/master/LICENSE.txt)

<img src="icon.png" alt="IppCli Logo" width="128" height="128" />

**IppCli** is a cross-platform command-line tool for interacting with Internet Printing Protocol (IPP) printers, print servers (e.g. CUPS), and IPP System Services. Built on .NET 10 with [SharpIppNext](https://github.com/danielklecha/SharpIppNext) and [Spectre.Console](https://spectreconsole.net/).

## Features

- **Standard IPP Operations**: Complete coverage of RFC 8011, RFC 3995, RFC 3996, RFC 3998, PWG 5100.5, PWG 5100.13, and PWG 5100.22.
- **Top-Level Commands**: Direct, standard IPP operation commands without nested subcommands.
- **Flexible Options & JSON Support**: Configure individual parameters via command-line flags or supply full/partial JSON payloads (inline or via `@file.json`).
- **Multiple Output Formats**: Rich formatted Spectre.Console tree (`Tree`) or machine-readable indented JSON (`Json`).
- **SSL / TLS Flexibility**: Built-in `-k` / `--insecure` option to bypass SSL validation for local devices with self-signed certificates.
- **Packaged as .NET Tool**: Install globally using `dotnet tool install -g IppCli` and execute anywhere via `ipp-cli`.

## Installation

### As a Global .NET Tool

```bash
dotnet tool install -g IppCli
```

### From Source

```bash
git clone https://github.com/danielklecha/IppCli.git
cd IppCli
dotnet build -c Release
```

## Options & Syntax

### Core Options

All commands support the following core settings:

| Option | Description | Default |
|---|---|---|
| `-o, --output <FORMAT>` | Output format (`Tree` or `Json`) | `Tree` |
| `-k, --insecure, --ignore-ssl-errors` | Bypass SSL / TLS certificate validation errors | `false` |
| `-t, --timeout <SECONDS>` | HTTP request timeout in seconds | `30` |
| `-r, --request <JSON>` | Full IPP request JSON string or `@file.json` | |
| `--version <VERSION>` | IPP protocol version (`1.0`, `1.1`, `2.0`, `2.1`, `2.2`) | `1.1` |
| `--request-id <ID>` | IPP request identifier | `1` |
| `-h, --help` | Display command help and list available options | |

### Attribute Groups & Prefix Conventions

IPP operation parameters are organized by attribute group prefixes:

- **Operation Attributes (`--op-*`)**: Parameters sent in the operation attributes group.
  - `--op <JSON>`: Full Operation Attributes JSON string or `@file.json`
  - `--op-printer-uri <URI>`: Target printer or service URI (e.g. `ipp://...`, `ipps://...`, `http://...`)
  - `--op-job-uri <URI>`: Target job URI
  - `--op-job-id <ID>`: Target job ID
  - `--op-document-number <NUM>`: Document sequence number
  - `--op-requesting-user-name <USER>`: Requesting user name (defaults to current OS user)
  - `--op-requested-attributes <ATTRS>`: Comma-separated list of attributes to query
  - `--op-which-jobs <VALUE>`: Job queue filter (`completed`, `not-completed`, `all`)
  - `--op-my-jobs`: Filter jobs submitted by the current user
  - `--op-message <TEXT>`: Reason or message associated with the operation
- **Job Template Attributes (`--jta-*`)**: Job configuration settings.
  - `--jta <JSON>`: Full Job Template Attributes JSON string or `@file.json`
  - `--jta-copies <COUNT>`: Number of copies
  - `--jta-sides <VALUE>`: Duplex mode (`one-sided`, `two-sided-long-edge`, `two-sided-short-edge`)
  - `--jta-print-color-mode <VALUE>`: Color mode (`color`, `monochrome`, `auto`)
  - `--jta-media <NAME>` / `--jta-media-col <JSON>`: Media type, size, and tray collection
- **Printer Attributes (`--pa-*`)**: Printer administrative settings.
  - `--pa <JSON>`: Full Printer Attributes JSON string or `@file.json`
  - `--pa-printer-info <TEXT>`, `--pa-printer-location <TEXT>`, etc.
- **Document Template Attributes (`--dta-*`)**: Document-level settings.
  - `--dta <JSON>`: Full Document Template Attributes JSON string or `@file.json`
  - `--dta-copies <COUNT>`, `--dta-media-col <JSON>`, etc.
- **Document Stream (`--document <PATH>`)**: File path to upload when using `print-job` or `send-document`.

## Commands

All IPP operations are available as top-level commands:

| Command | Description |
|---|---|
| `ipp-cli activate-printer` | Activate printer |
| `ipp-cli cancel-current-job` | Cancel the currently printing job |
| `ipp-cli cancel-document` | Cancel a document |
| `ipp-cli cancel-job` | Cancel a specific print job |
| `ipp-cli cancel-jobs` | Cancel multiple print jobs |
| `ipp-cli cancel-my-jobs` | Cancel all jobs submitted by current user |
| `ipp-cli cancel-subscription` | Cancel subscription |
| `ipp-cli close-job` | Close a multi-document job |
| `ipp-cli create-job` | Create an empty multi-document print job |
| `ipp-cli create-job-subscriptions` | Create job subscriptions |
| `ipp-cli create-printer-subscriptions` | Create printer subscriptions |
| `ipp-cli cups-get-printers` | Get all printers known to CUPS server |
| `ipp-cli deactivate-printer` | Deactivate printer |
| `ipp-cli disable-all-printers` | Disable all printers on the system |
| `ipp-cli disable-printer` | Disable printer from accepting new jobs |
| `ipp-cli enable-all-printers` | Enable all printers on the system |
| `ipp-cli enable-printer` | Enable printer to accept new jobs |
| `ipp-cli get-document-attributes` | Get document attributes |
| `ipp-cli get-documents` | List documents belonging to a job |
| `ipp-cli get-job-attributes` | Get attributes of a specific job |
| `ipp-cli get-jobs` | List print jobs from the printer queue |
| `ipp-cli get-notifications` | Get notifications for a subscription |
| `ipp-cli get-printer-attributes` | Get attributes of the target printer |
| `ipp-cli get-printer-supported-values` | Get supported values for printer attributes |
| `ipp-cli get-printers` | Get printers from system |
| `ipp-cli get-resource-attributes` | Get resource attributes |
| `ipp-cli get-resources` | Get resources on the system |
| `ipp-cli get-subscription-attributes` | Get subscription attributes |
| `ipp-cli get-subscriptions` | List active subscriptions |
| `ipp-cli get-system-attributes` | Get system attributes |
| `ipp-cli get-system-supported-values` | Get system supported values |
| `ipp-cli get-user-printer-attributes` | Get user printer attributes |
| `ipp-cli hold-job` | Hold a pending job from scheduling |
| `ipp-cli hold-new-jobs` | Hold all newly submitted jobs |
| `ipp-cli identify-printer` | Identify physical printer |
| `ipp-cli pause-all-printers` | Pause all printers on the system |
| `ipp-cli pause-printer` | Pause printer scheduling and processing |
| `ipp-cli print-job` | Submit a print job with document data |
| `ipp-cli promote-job` | Promote a job to the front of the queue |
| `ipp-cli purge-jobs` | Purge all jobs from the printer queue |
| `ipp-cli release-held-new-jobs` | Release all held new jobs |
| `ipp-cli release-job` | Release a previously held job |
| `ipp-cli renew-subscription` | Renew subscription |
| `ipp-cli restart-job` | Restart a completed or retained job |
| `ipp-cli restart-printer` | Restart the printer |
| `ipp-cli restart-system` | Restart the IPP system |
| `ipp-cli resubmit-job` | Resubmit an existing job |
| `ipp-cli resume-all-printers` | Resume all printers on the system |
| `ipp-cli resume-job` | Resume a previously suspended job |
| `ipp-cli resume-printer` | Resume printer scheduling and processing |
| `ipp-cli send-document` | Send a document stream to a created job |
| `ipp-cli set-document-attributes` | Set document attributes |
| `ipp-cli set-job-attributes` | Set settable job attributes |
| `ipp-cli set-printer-attributes` | Set printer attributes |
| `ipp-cli shutdown-all-printers` | Shutdown all printers on the system |
| `ipp-cli shutdown-printer` | Shutdown the printer |
| `ipp-cli startup-all-printers` | Startup all printers on the system |
| `ipp-cli startup-printer` | Startup the printer |
| `ipp-cli suspend-current-job` | Suspend current printing job |
| `ipp-cli validate-document` | Validate document template attributes |
| `ipp-cli validate-job` | Validate if job attributes would be accepted |

## Examples

### Query Printer Attributes
```bash
ipp-cli get-printer-attributes --op-printer-uri ipp://192.168.1.50:631/ipp/print
```

### Print a PDF File in Duplex
```bash
ipp-cli print-job --op-printer-uri ipps://printer.local/ipp/print --document document.pdf --jta-copies 2 --jta-sides TwoSidedLongEdge -k
```

### List Pending Jobs in JSON
```bash
ipp-cli get-jobs --op-printer-uri ipp://192.168.1.50:631/ipp/print --op-which-jobs not-completed -o Json
```

### Cancel a Specific Job
```bash
ipp-cli cancel-job --op-printer-uri ipp://192.168.1.50:631/ipp/print --op-job-id 105 --op-message "User canceled"
```

### Query Document Attributes
```bash
ipp-cli get-document-attributes --op-printer-uri ipp://192.168.1.50:631/ipp/print --op-job-id 105 --op-document-number 1
```

### Set Printer Attributes
```bash
ipp-cli set-printer-attributes --op-printer-uri ipp://localhost:631/printers/LaserJet --pa-printer-info "Office Color Laser" --pa-printer-location "Room 302"
```

### List All CUPS Printers
```bash
ipp-cli cups-get-printers --op-printer-uri ipp://localhost:631
```

### Supply Full Request JSON from File
```bash
ipp-cli print-job --request @request.json
```

## License

This project is licensed under the [MIT License](LICENSE).
