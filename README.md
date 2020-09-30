# Webapp for network testing

[![Build Status](https://dev.azure.com/jannemattila/jannemattila/_apis/build/status/JanneMattila.webapp-network-tester?branchName=master&jobName=Build)](https://dev.azure.com/jannemattila/jannemattila/_build/latest?definitionId=55&branchName=master)
![Docker Pulls](https://img.shields.io/docker/pulls/jannemattila/webapp-network-tester?style=plastic)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

If you're building Azure infrastructure and plan to connect different
services together with [service endpoints](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-service-endpoints-overview)
and/or [private link](https://docs.microsoft.com/en-us/azure/private-link/private-link-overview),
then you might not always know if the setup if done correctly.
It would be easier to just test with simple test application that network setup
works as expected. This app is just for that!

You can deploy this application using container to e.g. app service or
AKS and then invoke it's exposed api to make different network operations.

## Usage

### Supported operations

It currently has support for following operations:

| Command  | Sub-command | Description                                                                                |
|----------|-------------|--------------------------------------------------------------------------------------------|
| HTTP     | GET         | Invokes GET request to the parameter url                                                   |
| HTTP     | POST        | Invokes POST request to the parameter url and passes further command to the target address |
| BLOB     | GET         | Downloads blob according to parameters defining file, container and storage account        |
| BLOB     | POST        | Uploads blob according to parameters defining file, container and storage account          |
| REDIS    | GET         | Gets item from cache according to parameters defining key and redis cache                  |
| REDIS    | SET         | Sets item from cache according to parameters defining key and redis cache                  |
| IPLOOKUP | N/A         | Gets IP address of defined in parameter                                                    |
| NSLOOKUP | N/A         | Get IP address and relevant network related information about address defined in parameter |

### Operation examples

Here are few example commands:

`HTTP GET http://target/`: Invokes GET request to target address `http://target/`.

`HTTP POST https://target/api/commands`: Invokes POST request to target address **AND**
passes along rest of the commands for further processing.

`BLOB GET file.csv files DefaultEndpointsProtocol=https;AccountName=account;AccountKey=key;EndpointSuffix=core.windows.net`: Downloads
`file.csv` from container `files` using the defined connection string as last argument.

`BLOB POST hello file.csv files DefaultEndpointsProtocol=https;AccountName=account;AccountKey=key;EndpointSuffix=core.windows.net`: Uploads
`hello` as content to file `file.csv` at container `files` using the defined connection string as last argument.

`REDIS GET mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False`:
Gets item called `mycache` from the cache using the defined connection string as last argument.

`REDIS SET hello mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False`:
Sets value `hello` to the item called `mycache` from the cache using the defined connection string as last argument.

`IPLOOKUP account.redis.cache.windows.net`:
Gets ip address of the `account.redis.cache.windows.net`.

`NSLOOKUP account.redis.cache.windows.net`:
Gets ip address and relevant network related information of the `account.redis.cache.windows.net`.

`NSLOOKUP account.redis.cache.windows.net 168.63.129.16`:
Gets ip address and relevant network related information of the `account.redis.cache.windows.net` using `168.63.129.16` (Azure DNS) as name server.

`NSLOOKUP account.redis.cache.windows.net 1.1.1.1`:
Gets ip address and relevant network related information of the `account.redis.cache.windows.net` using `1.1.1.1` (Cloudflare DNS) as name server.

### How to invoke api

You simple create json payload and send it to the exposed api endpoint `/api/commands`:

```json
{
  "commands": [
    "HTTP POST http://localhost:5000/api/commands",
    "REDIS SET value1 mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False",
    "REDIS GET mycache account.redis.cache.windows.net:6380,password=key=,ssl=True,abortConnect=False"
  ]
}
```

Here is example response:

```plain
-> Start: HTTP POST http://localhost:5000/api/commands
-> Start: REDIS SET hello2 mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
SET: mycache=value1
<- End: REDIS SET hello2 mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
-> Start: REDIS GET mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
GET: value1
<- End: REDIS GET mycache account.redis.cache.windows.net:6380,password=a44duMUReF5ll+F8YxPo0MGIz5P0Roq3ghUQGIYZpbc=,ssl=True,abortConnect=False
<- End: HTTP POST http://localhost:5000/api/commands
```

## Architecture examples

### App Service connecting to blob via service endpoint

TBD

### App Service connecting to redis cache via private link

TBD: [Note about private zones](https://docs.microsoft.com/en-us/azure/app-service/web-sites-integrate-with-vnet#azure-dns-private-zones)
