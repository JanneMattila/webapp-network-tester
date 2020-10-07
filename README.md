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
| SQL      | QUERY       | Executes SQL query according to parameters                                                 |
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

`SQL QUERY "SELECT TOP (5) * FROM [SalesLT].[Customer]" "Server=tcp:server.database.windows.net,1433;Initial Catalog=db;Persist Security Info=False;User ID=user;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"`:
Executes defined T-SQL using the defined connection string as last argument.

`IPLOOKUP account.redis.cache.windows.net`:
Gets ip address of the `account.redis.cache.windows.net`.

`NSLOOKUP account.redis.cache.windows.net`:
Gets ip address and relevant network related information of the `account.redis.cache.windows.net`.

`NSLOOKUP account.redis.cache.windows.net 168.63.129.16`:
Gets ip address and relevant network related information of the `account.redis.cache.windows.net` using `168.63.129.16` (Azure DNS) as name server.

`NSLOOKUP account.redis.cache.windows.net 1.1.1.1`:
Gets ip address and relevant network related information of the `account.redis.cache.windows.net` using `1.1.1.1` (Cloudflare DNS) as name server.

### How to invoke api

You simple create plain text payload with single command per line and send it to the exposed api endpoint `/api/commands`:

```plain
HTTP POST http://localhost:5000/api/commands
REDIS SET value1 mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
REDIS GET mycache account.redis.cache.windows.net:6380,password=key=,ssl=True,abortConnect=False
```

Here is example response:

```plain
-> Start: HTTP POST http://localhost:5000/api/commands
-> Start: REDIS SET hello2 mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
SET: mycache=value1
<- End: REDIS SET hello2 mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
-> Start: REDIS GET mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
GET: value1
<- End: REDIS GET mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
<- End: HTTP POST http://localhost:5000/api/commands
```

## Architecture examples

### Deploying network test app

Deploy [jannemattila/webapp-network-tester](https://hub.docker.com/r/jannemattila/webapp-network-tester)
container to your app service(s). Also add following application settings to the apps:

* `WEBSITE_DNS_SERVER` = `168.63.129.16`
* `WEBSITE_VNET_ROUTE_ALL` = `1`

Read more about these settings from the [documentation](https://docs.microsoft.com/en-us/azure/app-service/web-sites-integrate-with-vnet#azure-dns-private-zones).

After deployment you can test app with following request:

```bash
POST https://*yourapp*.azurewebsites.net/api/commands HTTP/1.1
```

Use simple test payload like this (you can test any domain):
```rest
NSLOOKUP github.com
```

You should get following reply:

```rest
-> Start: NSLOOKUP github.com
NS: 127.0.0.11
AUDIT: ; (1 server found)
;; Got answer:
;; ->>HEADER<<- opcode: Query, status: No Error, id: 9582
;; flags: qr rd ra; QUERY: 1, ANSWER: 2, AUTHORITY: 0, ADDITIONAL: 1

;; OPT PSEUDOSECTION:
; EDNS: version: 0, flags:; UDP: 1224; code: NoError
;; QUESTION SECTION:
github.com.                      	IN 	ANY

;; ANSWER SECTION:
github.com.                      	41 	IN 	A 	140.82.121.3
github.com.                      	1800 	IN 	HINFO 	"RFC8482" ""

;; Query time: 23 msec
;; SERVER: 127.0.0.11#53
;; WHEN: Tue Oct 06 18:05:04 Z 2020
;; MSG SIZE  rcvd: 96

RECORD: github.com. 41 IN A 140.82.121.3
RECORD: github.com. 1800 IN HINFO "RFC8482" ""

<- End: NSLOOKUP github.com
```

Now you are ready to test you configurations!

### App Service connecting to blob via service endpoint

TBD

### App Service connecting to redis cache via private endpoint

Let's validate following architecture:

![App Service connecting to redis cache via private endpoint](https://user-images.githubusercontent.com/2357647/95295327-89c21980-087f-11eb-8d5e-1b033587fbaa.png)

You can use `NSLOOKUP` command for testing the DNS setup:

```bash
NSLOOKUP account.redis.cache.windows.net
```

=> (output abbreviated)

```bash
RECORD: account.redis.cache.windows.net. 120 IN CNAME account.privatelink.redis.cache.windows.net.
```

And then try *privatelink* address:

```bash
NSLOOKUP account.privatelink.redis.cache.windows.net
```

=> (output abbreviated)

```bash
RECORD: account.privatelink.redis.cache.windows.net. 10 IN A 172.17.2.4
```

Alternative you can try:

```bash
IPLOOKUP account.privatelink.redis.cache.windows.net
```

=> (output abbreviated)

```bash
IP: 172.17.2.4
```

Above would indicate that connection would be using internal IP address.

You can also use `REDIS` command for verifying the setup:

```bash
REDIS SET hello mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
REDIS GET mycache account.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False
```

=> (output abbreviated)

```bash
SET: mycache=hello
GET: hello
```

If the `IPLOOKUP` or `NSLOOKUP` would give you external IP address (e.g. `40.86.133.156`)
then you can try to force it to use Azure DNS in the lookup:

```bash
NSLOOKUP account.redis.cache.windows.net 168.63.129.16
```

If that then works it would mean then you should check your
[application settings](https://docs.microsoft.com/en-us/azure/app-service/web-sites-integrate-with-vnet#azure-dns-private-zones).
