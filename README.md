# HyacineProxy

HyacineProxy is a proxy server for intercepting and modifying HTTP/HTTPS requests. Its main purpose is to redirect requests from specific domains and URLs to private servers.

## Features

- Redirecting requests from specified domains
- Blocking specific URLs
- Automatically ignoring selected domains
- Configuration through JSON file

## Requirements

- .NET 9.0 or higher

## Installation

1. Download the latest version from releases or build the project from source:

```bash
git clone https://github.com/ultard/HyacineProxy.git
cd HyacineProxy
dotnet build -c Release
```

## Usage

1. Run the application:

```bash
dotnet HyacineProxy.dll
```

or use precompiled binaries from [actions](https://github.com/ultard/HyacineProxy/actions)

On first launch, a `config.json` configuration file will be automatically created with default settings.

2. To stop the proxy server, press Ctrl+C.

## Configuration

- `ProxyPort`: Port on which the proxy server will run
- `Dispatch` and `SDK`: Settings for redirecting requests to local servers
    - `Domain`: Domain of the local server
    - `Port`: Port of the local server
    - `RedirectTrigger`: Triggers for redirecting requests
- `AlwaysIgnoreDomains`: List of domains that are always ignored
- `RedirectDomains`: List of domains for redirection
- `BlockUrls`: List of URLs that will be blocked

## Dependencies

- [Unobtanium.Web.Proxy](https://www.nuget.org/packages/Unobtanium.Web.Proxy/)

## License

This project is distributed under the [MIT](LICENSE) license.