# PixelPainterLegacy
This is a continuation project of [PixelPainter.](https://github.com/PixelPainter3-0/PixelPainter3.0) A browser-based pixel art editor that allows users to create pixel art and share it with others. It is built with Vue.js and .NET Core.

## Required IDE Setup

[Node.JS](https://nodejs.org/en)

[Visual Studio](https://visualstudio.microsoft.com/)

[VS Code](https://code.visualstudio.com/)

[GIT](https://git-scm.com/downloads)

[Docker Desktop](https://www.docker.com/get-started/)

## Essential VS Code Extensions

Prettier - Code formatter <- *Set as default formatter*

Path Intellisense

vscode-icons

## Setup for local development

### Verified local prerequisites

- Node.js and npm
- .NET 8 SDK
- Docker Desktop with Docker Compose

This repository was validated locally with:

```
node 24.x
npm 11.x
.NET SDK 8.0.x
Docker 29.x
Docker Compose 5.x
```

### Frontend setup in VS Code

1. Open the .Client project folder in VS Code by clicking 'File' -> 'Open Folder' and selecting the .Client inside the cloned repository folder
1. Open terminal with 'ctrl+`' *The key left of 1*
1. Run the following commands:
	```
	npm install --legacy-peer-deps
	```

### Database setup

1. From the repository root, start the PostgreSQL/PostGIS development database:
	```
	docker compose up -d
	```
	The container initializes itself from the ordered SQL files in `supabase/migrations` the first time its volume is created.
1. To inspect the database, connect with any PostgreSQL client using:
	```
	Host: localhost
	Port: 5432
	Database: postgres
	Username: postgres
	Password: postgres
	```

### Backend setup in Visual Studio

1. Open the .sln file in the root directory of the project in Visual Studio
1. Local database and frontend redirect defaults are provided by `MyTestVueApp.Server/appsettings.Development.json`. To enable Google login locally, create the ignored `MyTestVueApp.Server/appsettings.json` with the following overrides:
	```json
	{
	  "ApplicationConfiguration": {
	    "ClientId": "",
	    "ClientSecret": "",
	    "OAuthRedirectUrl": "http://localhost:5054/api/v2/auth/callback"
	  }
	}
	```
1. `ClientId` and `ClientSecret` are only required if you want Google login to work locally.
1. Start the backend from Visual Studio, or from a terminal in the repository root:
	```
	"C:\Program Files\dotnet\dotnet.exe" run --project .\MyTestVueApp.Server\MyTestVueApp.Server.csproj --launch-profile http
	```
1. The backend serves locally on:
	```
	http://localhost:5054
	http://0.0.0.0:7154
	```

### Full local run sequence

1. Start the database from the repository root:
	```
	docker compose up -d
	```
1. Start the backend:
	```
	"C:\Program Files\dotnet\dotnet.exe" run --project .\MyTestVueApp.Server\MyTestVueApp.Server.csproj --launch-profile http
	```
1. Start the frontend from `mytestvueapp.client`:
	```
	npm run dev
	```
1. Open the URL printed by Vite, usually:
	```
	http://localhost:5173
	```
1. If Vite falls back to another port, such as `5174`, the app still runs. If you need Google login locally, also update `RedirectUrl` in `MyTestVueApp.Server/appsettings.json` to match that frontend origin.

## Missing HTTPS Certificates

If there is an HTTPS certificate error when running 'npm run dev', manually creating the certificate might be needed.

1. Route to the 'Roaming' folder
2. Create a new folder called 'ASP.NET' if it is not already present
3. Create a new folder in ASP.NET called 'https'
4. Copy the directory of the 'https' folder
5. Open terminal and run the following command:
	
	dotnet dev-certs https --export-path 'your directory here'/mytestvueapp.client.pem 
	
# ln /var/www/app/v3.0.0/serverconfig/nginx.conf /etc/nginx/sites-available/default
