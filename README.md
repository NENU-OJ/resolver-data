# resolver-data
Generates data for the rank resolver.

# Usage
```
resolver-data 1.0.0
Copyright (C) 2020 resolver-data

ERROR(S):
  Required option 'h, host' is missing.
  Required option 'u, user' is missing.
  Required option 'p, passwd' is missing.
  Required option 'c, contest' is missing.

  -h, --host       Required. Set database host to connect.

  -P, --port       (Default: 3306) Set database port to connect.

  -u, --user       Required. Set database user to connect.

  -p, --passwd     Required. Set database password to connect.

  -c, --contest    Required. Set contest ID.

  -o, --output     (Default: contest.json) Set file to output.

  --help           Display this help screen.

  --version        Display version information.

```
Example: `resolver-data -h localhost -u root -p db_nenu_oj -c 5`

Then use [acm-resolver](https://github.com/lixin-wei/acm-resolver) to render the generated file, such as `contest.json`.

## Build
Simply execute `dotnet build`.
