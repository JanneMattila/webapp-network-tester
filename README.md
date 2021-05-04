# Webapp for network testing

[![Build Status](https://dev.azure.com/jannemattila/jannemattila/_apis/build/status/JanneMattila.webapp-network-tester?branchName=master&jobName=Build)](https://dev.azure.com/jannemattila/jannemattila/_build/latest?definitionId=55&branchName=master)
[![Docker Pulls](https://img.shields.io/docker/pulls/jannemattila/webapp-network-tester?style=plastic)](https://hub.docker.com/r/jannemattila/webapp-network-tester)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

If you're building Azure infrastructure and plan to connect different
services together with [service endpoints](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-service-endpoints-overview)
and/or [private link](https://docs.microsoft.com/en-us/azure/private-link/private-link-overview),
then you might not always know if the setup if done correctly.
It would be easier to just test with simple test application that network setup
works as expected. This app is just for that!

You can deploy this application using container to e.g. app service or
AKS and then invoke it's exposed api to make different network operations.

## How to create image locally

```bash
# Build container image
docker build . -f src/WebApp/Dockerfile -t webapp-network-tester:latest

# Run container using command
docker run -it --rm -p "2001:80" webapp-network-tester:latest
``` 

If you want to publish your image to ACR ([instructions](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli)):

```bash
$acrName = "<your ACR name>"
# Login
az acr login --name $acrName

# Tag image
docker tag webapp-network-tester "$acrName.azurecr.io/webapp-network-tester"

# Push image
docker push "$acrName.azurecr.io/webapp-network-tester"
```

## Usage

### Supported operations

It currently has support for following operations:

| Command  | Sub-command | Description                                                                                |
| -------- | ----------- | ------------------------------------------------------------------------------------------ |
| HTTP     | GET         | Invokes GET request to the parameter url                                                   |
| HTTP     | POST        | Invokes POST request to the parameter url and passes further command to the target address |
| BLOB     | GET         | Downloads blob according to parameters defining file, container and storage account        |
| BLOB     | POST        | Uploads blob according to parameters defining file, container and storage account          |
| REDIS    | GET         | Gets item from cache according to parameters defining key and redis cache                  |
| REDIS    | SET         | Sets item from cache according to parameters defining key and redis cache                  |
| SQL      | QUERY       | Executes SQL query according to parameters                                                 |
| IPLOOKUP | N/A         | Gets IP address of defined in parameter                                                    |
| NSLOOKUP | N/A         | Get IP address and relevant network related information about address defined in parameter |
| INFO     | HOSTNAME    | Gets hostname of the container                                                             |
| INFO     | ENV         | Gets all environment variables                                                             |

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

Use simple test payload like this:

```rest
HTTP GET http://localhost/
```

You should get following reply:

```html
<html><body>Hello there!</body></html>
```

Now you are ready to test you configurations!

### App Service connecting to blob via service endpoint

Let's validate following architecture:

![App Service connecting to blob via service endpoint](https://user-images.githubusercontent.com/2357647/95300102-60a58700-0887-11eb-9e68-05934ef8e160.png)

You can use `IPLOOKUP` command for for fetching target resource IP:

```bash
IPLOOKUP account.blob.core.windows.net
```

=> (output abbreviated)

```bash
IP: 52.239.139.132
```

You can double check that IP address using [AzureDatacenterIPorNo](https://github.com/JanneMattila/AzureDatacenterIPOrNo):

```powershell
# Import or install if not installed earlier
Import-Module AzureDatacenterIPorNo
Get-AzureDatacenterIPOrNo -IP 52.239.139.132

Source             Ip             IpRange Region
------             --             ------- ------
PublicIPs_20200504 52.239.139.132 IpRange europenorth
```

So clearly it's Azure public IP address from North Europe region.

Next let's test that application can indeed use storage as intended:

```bash
BLOB POST hello file.csv files DefaultEndpointsProtocol=https;AccountName=account;AccountKey=key;EndpointSuffix=core.windows.net
BLOB GET file.csv files DefaultEndpointsProtocol=https;AccountName=account;AccountKey=key;EndpointSuffix=core.windows.net
```

=> (output abbreviated)

```bash
Wrote "0x8D86A928E7D78FD"
hello
```

We have now verified that application does have access to the blob storage as we wanted.
It has created container `files` and uploaded and downloaded file called `file.csv` successfully.

Now you should also validate that there is **no access**
from e.g. Azure Portal or via Azure Storage Explorer for making
sure that your setup is correctly done.

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

### App Service front with app service backend and SQL database

Here's high level architecture you might want to implement using app service:

![High level architecture with app service and SQL database](https://user-images.githubusercontent.com/2357647/95335379-01ac3600-08b8-11eb-8256-4ee5a590cc1f.png)

Above can be implemented using following architecture:

![App Service front with app service backend via service endpoint and SQL database via private endpoint](https://user-images.githubusercontent.com/2357647/95495846-a4dc7880-09a8-11eb-9bb9-a192ee2a4fa7.png)

Or using following architecture:

![App Service front with app service backend via private endpoint and SQL database via private endpoint](https://user-images.githubusercontent.com/2357647/95495938-c8072800-09a8-11eb-91d5-18c1ea0b4296.png)

**NOTE**: App services in same app service plan integrate into same subnet using
regional VNet integration. This means that you can either 1) Create 2 separate
app service plans *or* 2) setup filtering in network security group to just allow
required connectivity between services. You can use outbound IPs of `front`
app service for the filtering rules.

We can analyze our network setup if we deploy the network test tool to both
app services.

You can start analyzing with `IPLOOKUP` command for checking if private IPs are returned for the database:

```bash
IPLOOKUP account.database.windows.net
```

=> (output abbreviated)

```bash
IP: 172.17.2.5
```

Now let's try to connect to the database directly from `front`:

```bash
SQL QUERY "SELECT TOP (5) * FROM [SalesLT].[Customer]" "Server=tcp:account.database.windows.net,1433;Initial Catalog=db;Persist Security Info=False;User ID=user;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

It tries to connect to the database but then after long timeout (~2 mins)
it should fail like this (output abbreviated):

```
Microsoft.Data.SqlClient.SqlException (0x80131904): A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: TCP Provider, error: 40 - Could not open a connection to SQL Server)
```

Let's validate our `backend` IP address:

```bash
IPLOOKUP *yourbackendapp*.azurewebsites.net
```

=> (output abbreviated)

```bash
IP: 172.17.5.4
```

Instead of trying direct connection from `front`, let's pass that same request
to the `backend` app:

```bash
HTTP POST https://*yourbackendapp*.azurewebsites.net/api/commands
SQL QUERY "SELECT TOP (5) * FROM [SalesLT].[Customer]" "Server=tcp:account.database.windows.net,1433;Initial Catalog=db;Persist Security Info=False;User ID=user;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

=> (output abbreviated)

```csv
CustomerID;NameStyle;Title;FirstName
1;False;Mr.;Orlando
2;False;Mr.;Keith
3;False;Ms.;Donna
```

This prooves that connectivity is working from the `backend` app service but
you cannot directly connect from `front` to the database.


### Multi-container App Service

If you want to test App Service [multi-container](https://docs.microsoft.com/en-us/azure/app-service/tutorial-multi-container-app)
setup, then create following `docker-compose.yml` and deploy that to app service:

```yml
version: '3.3'

services:
   db:
     image: jannemattila/webapp-network-tester
     restart: always
   api:
     image: jannemattila/webapp-network-tester
     restart: always
   web:
     image: jannemattila/webapp-network-tester
     ports:
       - 80
     restart: always
```

Above will create simple `web`, `api` and `db`
containers for simulating 3-tier application.

If you now want to see the IP address of the `api`:

```bash
IPLOOKUP api
```

=> (output abbreviated)

```bash
IP: 172.16.2.3
```

If you want to create chain of calls from `web` to
`api` and from `api` to `db`, you can use following command
for that:

```bash
POST https://*yourapp*.azurewebsites.net/api/commands HTTP/1.1

HTTP POST http://api/api/commands
HTTP GET http://db
```

=> (output abbreviated)

```bash
-> Start: HTTP POST http://api/api/commands
-> Start: HTTP GET http://db
<html><body>Hello there!</body></html>
<- End: HTTP GET http://db
<- End: HTTP POST http://api/api/commands
```

## Links

Excellent article about [multi-tier web applications](https://techcommunity.microsoft.com/t5/apps-on-azure/zero-to-hero-with-app-service-part-7-multi-tier-web-applications/ba-p/1752015).

[Networking Related Commands for Azure App Services](https://techcommunity.microsoft.com/t5/apps-on-azure/networking-related-commands-for-azure-app-services/ba-p/392410)
