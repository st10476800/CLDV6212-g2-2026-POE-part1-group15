# CoffeeNChill API (CLDV6212 POE, Group 15)

ST10476800 and ST10476901

This is our CoffeeNChill project. In Part 1 we made the menu and staff document endpoints, and in Part 2 we added orders that go through a Storage Queue. For Part 3 we built an ASP.NET Core Web API in front of all of it. It has Swagger, DTOs and JWT login, and the roles decide who is allowed to do what. Anyone can look at the menu and place an order, but only staff can change the menu, handle documents and update order statuses.

The app is running live on a DigitalOcean server. The Swagger page is at http://209.38.245.127:5000/swagger and our demo video is at https://youtu.be/VqfbEUlmbDo

## Running it

You need Docker Desktop. Clone the repo and run this from the main folder:

```
docker compose up -d
```

It pulls our three images from Docker Hub and starts them: azurite (our storage emulator), coffeenchill-functions (the queue processing from Part 2) and coffeenchill-gateway (the new API). Once they are all running, go to http://localhost:5000/swagger. Run `docker compose down` to stop everything. The data is kept in a volume, so it is still there next time.

The compose file has a default JWT key and staff code so it works straight away. On the server we put our own values in a `.env` file instead.

## The containers

The gateway listens on port 8080 inside its container and Docker maps that to port 5000. It talks to Azurite directly for the menu, documents, orders and users, so it never calls the Functions app. The Functions container only runs the queue processing from Part 2 in the background. We did not give it a port, because its old HTTP endpoints have no login and anyone who could reach them would skip the JWT checks. On the server we also made the Azurite ports only listen on 127.0.0.1, because the Azurite account key is public and anyone could use it to read our data.

## Login and roles

Users are saved in an Azure Table called Users. The PartitionKey is the role (Admin, Staff or Customer), the RowKey is the email, and the other columns are PasswordHash, FullName and CreatedDate. Passwords are hashed with BCrypt, so we never store the real password.

Anyone can register as a Customer at POST /api/auth/register. To register as Staff or Admin you also have to send a secret StaffCode, and a wrong code gives a 403. We added this because the role is part of the register request, so otherwise anyone could just type "Admin" and make themselves an admin. The staff code on our server is GRPFIFTEEN.

POST /api/auth/login checks the email and password and gives back a JWT that lasts 60 minutes. The token holds the user's name, email and role. A wrong email and a wrong password give the same 401 message, so nobody can use the login to find out which emails are registered. In Swagger you copy the token, click Authorize and paste it in, and Swagger adds the word Bearer for you.

## What each role can do

GET /api/menu, POST /api/orders/queue and GET /api/orders/{orderId} are open to everyone, and so are register and login. Adding, updating and deleting menu items needs the Admin or Staff role. The same goes for everything under /api/documents (upload, list, download and delete), for GET /api/orders, for PUT /api/orders/status and for GET /api/failed-orders. If you send no token to one of these you get a 401, and if you send a token with the wrong role, like a Customer trying to add a menu item, you get a 403.

The brief asked for PUT /api/orders/status, so it takes the place of the collect endpoint from Part 2. A status can only move forward from Received to Preparing to Ready to Collected, and an order can only be Collected once it is Ready. Anything else gives a 409.

The menu is saved in a table called MenuItems, and documents go into the staff-docs blob container with a 10 MB limit. Orders are checked and put on order-processing-queue, and the API answers 202 straight away. Then ProcessOrderQueue moves each order from Received to Preparing to Ready, the same as in Part 2. If a message fails 3 times it goes to the poison queue and ends up in the FailedOrders table.

## DTOs and Swagger

Every endpoint uses a DTO, like MenuItemDto, OrderDto, RegisterDto and LoginResponseDto, so the table entities with their PartitionKey and RowKey never get sent out to the client. Swagger is set up with Swashbuckle and reads the XML comments above each controller method, so every endpoint has a description and a list of response codes. We also wrote a small filter so the padlock only shows on the endpoints that need a token.

## Testing

Our Postman collection is in the docs folder (CoffeeNChill-Part3.postman_collection.json). It has 35 requests that are sent as an anonymous user, a Customer, a Staff user and an Admin, and the tests check that each one gets the right status code. The login requests save the tokens, and the later requests use them as Bearer tokens. Our last run on the live server passed all 39 tests. A few of the Register requests show a red 409 in the screenshot, which only means those users were already registered from an earlier run.

![Postman run for Part 3 with all 39 tests passing](docs/postman-results-part3.png)

To run it yourself, import the collection, set baseUrl to the server address and pick a small file in the upload request. Then run the whole collection in order, because the logins have to come first.

## Docker Hub

Our images are on Docker Hub under st10476800: coffeenchill-gateway (v3.0), coffeenchill-functions (v1.0 and v2.0) and coffeenchill-azurite (v1.0 and v2.0). The links are https://hub.docker.com/r/st10476800/coffeenchill-gateway, https://hub.docker.com/r/st10476800/coffeenchill-functions and https://hub.docker.com/r/st10476800/coffeenchill-azurite

## The server

We put the app on a DigitalOcean server running Ubuntu 24.04. We installed Docker, copied over docker-compose.yml and a .env file, and ran docker compose up -d. Port 5000 is the only port open to the public.

## Problems we ran into

Swagger crashed when we added the document upload, with an error about [FromForm] and IFormFile. Putting the file inside a small UploadDocumentDto class fixed it.

In Visual Studio we kept getting a locked file error when building, because the gateway was still running from the last test. We learned to stop it before every build.

On one PC, Docker Compose wouldn't start because port 10001 was already used by an old Azurite container. Stopping the old one fixed it.

Our Azure for Students credits ran out before we could deploy, so we used DigitalOcean instead. Compose works the same on any Linux server, so nothing in the project had to change.

## Who did what

We split Part 3 in two. One of us did the setup, the login system and the Docker and server side, and the other did the menu, document and order endpoints.

ST10476800 set up the gateway project and wrote Program.cs with the JWT and Swagger settings. He built the login side: the user entity and DTOs, PasswordService, TokenService, UserTableService and AuthController. He also wrote the Swagger padlock filter and the health check. On the Docker side he wrote the Dockerfile, the .dockerignore and docker-compose.yml, and pushed the gateway image to Docker Hub. He also set up the DigitalOcean server, wrote the Postman collection and put this README together.

ST10476901 did the menu, documents and orders. For the menu he wrote MenuItemEntity, MenuItemDto, MenuItemUpdateDto, MenuTableService and MenuController, with the role checks so only Admin and Staff can change items. For documents he wrote StaffDocumentInfo, UploadDocumentDto, BlobStorageService and DocumentsController. For orders he brought the order models and the table and queue services over from Part 2 and wrote OrdersController, including the status endpoint and the failed orders endpoint.
