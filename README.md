# CoffeeNChill API (CLDV6212 POE, Group 15)

ST10476800 and ST10476901

This is our Azure Functions app for CoffeeNChill. In Part 1 we built the menu endpoints using Azure Table Storage and the staff document endpoints using Blob Storage. In Part 2 we added orders. When a student places an order, it now goes onto an Azure Storage Queue instead of being handled straight away, and a queue-triggered function picks it up in the background. We did this because at break time a lot of students order at once, and handling every order inside the request would slow the system down or crash it.

Everything runs in Docker. Azurite stands in for Azure Storage, so you don't need an Azure account to run it.

## What you need

Docker Desktop, Postman and Git. You only need .NET and Visual Studio if you want to change the code.

## How to run it

Clone the repo and start everything from the root folder:

```
git clone <repo-url>
cd <repo-folder>
docker compose up -d
```

This pulls both of our images from Docker Hub (tag v2.0), creates a bridge network called `coffeenchill-net` and a volume for Azurite's data, and starts both containers. Check that they're running:

```
docker compose ps
```

You should see `azurite` and `coffeenchill-functions` both showing "Up". The API runs at `http://localhost:7071/api`. On a fresh start, opening `http://localhost:7071/api/menu` in a browser should give you `[]`.

To watch the queue function's logs, run `docker compose logs -f functions`. To stop everything, run `docker compose down`. Your data stays in the volume. If you want to wipe it too, use `docker compose down -v`.

## The docker-compose file

`docker-compose.yml` is in the root of the repo and has two services.

`azurite` uses our image `st10476800/coffeenchill-azurite:v2.0` and emulates Tables, Blobs and Queues. We start it with `--skipApiVersionCheck`, because without it Azurite rejects requests from the newer Azure Storage SDK in our project. We found this out in Part 1 when the document endpoints kept failing with a 400.

`functions` uses `st10476800/coffeenchill-functions:v2.0`. Its storage connection string is passed in as an environment variable and points at Azurite by its service name (`azurite`), which works because both containers are on the same network.

## Endpoints

All routes start with `/api`.

Menu (Table Storage):

- `POST /menu` creates an item and returns 201. You get 400 if Category or Name is missing or the price is negative, and 409 if an item with that Category and Id already exists.
- `GET /menu` returns all items.
- `GET /menu/category/{category}` returns the items in one category.
- `PUT /menu/{category}/{id}` updates an item and returns 200, or 404 if it doesn't exist.
- `DELETE /menu/{category}/{id}` deletes an item and returns 204, or 404 if it doesn't exist.

The menu endpoints use `MenuItemDto`, so you send `Category` and `Id` and the DTO maps them to `PartitionKey` and `RowKey` in the table. Example body for creating an item:

```json
{
  "Category": "Hot Drinks",
  "Id": "COF-001",
  "Name": "Cappuccino",
  "Description": "Espresso with steamed milk",
  "Price": 35.00,
  "IsAvailable": true
}
```

Staff documents (Blob Storage, container `staff-docs`):

- `POST /documents/upload` uploads a file sent as form-data with the key `file`. It returns 201, or 400 if there's no file.
- `GET /documents` lists the files with their size and last modified date.
- `GET /documents/download/{fileName}` downloads a file, or returns 404 if it isn't there.

Orders (new in Part 2):

- `POST /orders/queue` puts a new order on the queue and returns 202 Accepted.
- `GET /orders` lists all orders.
- `GET /orders/{orderId}` shows one order and its current status.
- `PUT /orders/{orderId}/collect` marks an order as Collected. This only works once the order is Ready, otherwise you get 409.
- `GET /failed-orders` lists the orders that ended up on the poison queue.

Example body for placing an order:

```json
{
  "OrderId": "ORD-2026-8801",
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": ["COF-001", "PAS-104"],
  "TotalPrice": 65.00,
  "OrderTimestamp": "2026-10-12T10:15:00Z"
}
```

`OrderId` and `OrderTimestamp` are optional. If you leave them out, the API generates them. A bad order (no name, no items, a price of 0 or less, or broken JSON) gets a 400 with a list of everything that's wrong, and reusing an OrderId gets a 409.

## How the order queue works

When you post an order, `OrderFunctions` validates it, turns it into JSON, Base64 encodes it and sends it to `order-processing-queue`. The API answers with 202 straight away, so the student isn't left waiting while the order is being made.

`ProcessOrderQueue` has `[QueueTrigger("order-processing-queue")]`, so it runs by itself when a message arrives, usually within 2 seconds. It decodes the message, reads the JSON into an `OrderMessage` and saves it to the `Orders` table with the status Received. The PartitionKey is the order date (`yyyy-MM-dd`) and the RowKey is the OrderId, as the brief asks. To simulate the barista, it waits 10 seconds and changes the status to Preparing, then waits another 10 seconds and changes it to Ready. The student then calls the collect endpoint, which sets it to Collected. The wait time can be changed with `ORDER_STEP_DELAY_SECONDS` in `docker-compose.yml`.

If something goes wrong while processing a message (for example it isn't valid JSON), the function logs the error and throws it again so the host retries it. In `host.json` we set `maxDequeueCount` to 3, so after 3 failed tries the message is moved to `order-processing-queue-poison`. `ProcessPoisonOrder` picks it up from there, logs the full message and saves it to a `FailedOrders` table, which you can see with `GET /failed-orders`. We also made the function safe to retry. If an order is already in the table, it carries on from the order's current status instead of adding it a second time.

## Testing

Our Postman collection is in `docs/CoffeeChill-Part2.postman_collection.json`. It has 26 requests in three folders (Menu, Staff Documents and Orders) with 71 tests, and our last full run passed all 71. Every request uses `{{baseUrl}}`, and the menu and order IDs are generated fresh on each run, so the collection can be run as many times as you like.

To run it, start the app, import the collection into Postman, and set the environment in the top right to "No environment" (the collection has its own variables). Then pick any file in the Upload Staff Document request, right-click the collection and choose Run collection. The run takes about 35 seconds because two of the order requests wait for the queue function to update the status.

One thing that caught us out: the Postman runner can only read files from its working directory. If the upload test fails because the file is empty, go to Settings > General and turn on "Allow reading files outside working directory".

To test the poison queue, you have to put a broken message on the queue yourself, because the API won't accept a bad order. We used Azure Storage Explorer for this. Connect to the local emulator, open Queues > order-processing-queue, add a message like `this is not valid json` and tick "Encode the message body in Base64". In the logs you'll see it fail 3 times and then show up as a POISON MESSAGE, and it will then appear in `GET /failed-orders`.

## Project structure

```
/
├── docker-compose.yml
├── README.md
├── docs/
│   └── CoffeeChill-Part2.postman_collection.json
└── CLDV6212-g2-2026-POE-part1-group15 (vs)/
    ├── Dockerfile
    ├── host.json
    ├── Functions/   MenuFunctions, DocumentFunctions, OrderFunctions, ProcessOrderQueue
    ├── Models/      menu, document and order models and DTOs
    └── Services/    table, blob and queue services
```

## Docker Hub

- Functions: https://hub.docker.com/r/st10476800/coffeenchill-functions (v1.0 for Part 1, v2.0 for Part 2)
- Azurite: https://hub.docker.com/r/st10476800/coffeenchill-azurite (v1.0, v2.0)

If you change the code, rebuild and push the image from the folder with the Dockerfile:

```
docker build -t st10476800/coffeenchill-functions:v2.0 .
docker push st10476800/coffeenchill-functions:v2.0
```

Then run `docker compose pull` and `docker compose up -d` from the root folder.

## Problems we ran into

Azurite doesn't support Azure File Shares, which is why our document upload didn't work in Part 1. We switched the documents over to Blob Storage, and all three document endpoints work now.

We also had a build error on Windows saying a file couldn't be copied. The cause was the path being longer than 260 characters, because the project was inside OneDrive with long folder names. Cloning the repo to a short path like `C:\dev\cldv6212` fixed it.

In Postman, a saved environment had its own `orderId` variable, and environment variables override collection variables. That made the tests keep reusing an old order, so we run the collection with "No environment" now.

## Who did what

We split Part 2 into two halves. One of us did the side that receives orders and stores them, and the other did the queue side that processes them in the background.

ST10476800 did the order endpoints and the Orders table. This was `OrderFunctions.cs` (queue an order, get all orders, get one order, collect an order and get the failed orders), `OrderEntity.cs` and `OrderDto.cs` for storing and returning orders, and `OrderTableService.cs`, which saves orders and updates their status in the Orders table. He also added the Blob Storage and Storage Queue packages to the `.csproj` file and set up `host.json` with 3 retries, the polling interval and Base64 message encoding.

ST10476901 did the queue processing and the poison queue. This was `ProcessOrderQueue.cs`, which has the queue trigger that moves each order from Received to Preparing to Ready, and the poison queue handler that saves failed messages. He also wrote `QueueStorageService.cs`, which sends orders to `order-processing-queue` as Base64 JSON, and the models behind it: `OrderStatus.cs`, `OrderMessage.cs` with its validation, `QueueSendResult.cs`, `FailedOrderEntity.cs` and `FailedOrderDto.cs`.

## Video

[add the unlisted YouTube link here]
